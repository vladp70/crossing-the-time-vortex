using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GrammarDungeonGenerator — A generative grammar (graph rewriting) system for
/// procedural dungeon layout creation with multiple rooms and branching paths.
///
/// HOW IT WORKS (Generative Grammar Overview):
/// ============================================
///
/// 1. ALPHABET (Terminal & Non-Terminal Symbols):
///    - Non-terminals: DUNGEON, SEQUENCE, ROOM, CHALLENGE, BRANCH
///    - Terminals (leaf room types): Start, BridgeChallenge, BulletHell,
///      PlatformChallenge, TreasureRoom, BossArena, Finish
///
/// 2. PRODUCTION RULES (applied stochastically):
///    DUNGEON       → Start → SEQUENCE → Finish
///    SEQUENCE      → ROOM → Corridor → SEQUENCE   (recursive chain)
///                  | ROOM                           (base case)
///    ROOM          → CHALLENGE                      (70%)
///                  | TreasureRoom                   (20%)
///                  | BRANCH                         (10%)
///    CHALLENGE     → BridgeChallenge                (33%)
///                  | BulletHell                     (33%)
///                  | PlatformChallenge              (34%)
///    BRANCH        → SEQUENCE + SEQUENCE            (fork into two paths that rejoin)
///
/// 3. GENERATION PIPELINE:
///    Step A: Expand the grammar into an abstract room graph (nodes + edges).
///    Step B: Assign each node a concrete 2D footprint (width × depth) and
///            position it in world space using a greedy left-to-right placer.
///    Step C: Connect adjacent rooms with corridor tiles.
///    Step D: Emit the tile sets (floor, pit, wall) for the full dungeon.
///    Step E: Populate rooms with gameplay objects (bridge segments, spawners,
///            platform droppers, chests, win triggers) based on room type.
///
/// This script is NOT enabled in the scene. Attach it to a GameObject and call
/// Generate() manually, or enable it in Start() when ready.
/// </summary>
public class GrammarDungeonGenerator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector-Tunable Parameters
    // ─────────────────────────────────────────────
    [Header("Grammar Parameters")]
    [Tooltip("Minimum number of challenge rooms in the main path (excluding Start/Finish).")]
    [Range(2, 10)]
    public int minRooms = 3;

    [Tooltip("Maximum number of challenge rooms in the main path (excluding Start/Finish).")]
    [Range(3, 15)]
    public int maxRooms = 6;

    [Tooltip("Maximum recursion depth for branching rules.")]
    [Range(0, 3)]
    public int maxBranchDepth = 1;

    [Tooltip("Probability that a ROOM expands into a BRANCH (fork) instead of a single challenge.")]
    [Range(0f, 0.3f)]
    public float branchProbability = 0.10f;

    [Tooltip("Probability that a ROOM expands into a TreasureRoom instead of a challenge.")]
    [Range(0f, 0.4f)]
    public float treasureProbability = 0.20f;

    [Header("Room Size Ranges")]
    public Vector2Int startRoomSize = new Vector2Int(7, 8);
    public Vector2Int finishRoomSize = new Vector2Int(6, 8);
    public Vector2Int challengeRoomSizeMin = new Vector2Int(8, 6);
    public Vector2Int challengeRoomSizeMax = new Vector2Int(14, 8);
    public Vector2Int treasureRoomSizeMin = new Vector2Int(5, 5);
    public Vector2Int treasureRoomSizeMax = new Vector2Int(7, 7);
    public int corridorLength = 3;
    public int corridorWidth = 2;

    [Header("Materials (Optional — falls back to procedural)")]
    public Material groundMaterial;
    public Material wallMaterial;
    public Material bridgeMaterial;
    public Material projectileMaterial;
    public Material platformMaterial;
    public Material winMaterial;

    [Header("Prefabs (Optional)")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;

    // ─────────────────────────────────────────────
    // Internal Data Structures
    // ─────────────────────────────────────────────

    /// <summary>The type of challenge or function a room serves.</summary>
    public enum RoomType
    {
        Start,
        BridgeChallenge,
        BulletHell,
        PlatformChallenge,
        TreasureRoom,
        BossArena,
        Finish
    }

    /// <summary>A node in the dungeon graph produced by the grammar.</summary>
    public class RoomNode
    {
        public int id;
        public RoomType type;
        public int width;
        public int depth;

        // World-space origin (bottom-left corner in XZ plane)
        public int originX;
        public int originZ;

        // Whether this room has a pit floor (bridge / platform rooms)
        public bool hasPit;

        // Adjacency
        public List<int> connectedTo = new List<int>();

        public override string ToString()
        {
            return $"Room#{id} [{type}] origin=({originX},{originZ}) size=({width}x{depth})";
        }
    }

    /// <summary>A corridor connecting two rooms.</summary>
    public class CorridorEdge
    {
        public int fromRoom;
        public int toRoom;
        public int originX;
        public int originZ;
        public int width;
        public int length;
    }

    // The generated graph
    private List<RoomNode> rooms = new List<RoomNode>();
    private List<CorridorEdge> corridors = new List<CorridorEdge>();
    private int nextRoomId = 0;

    // Tile sets for rendering
    private HashSet<Vector2Int> floorTiles = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> pitTiles = new HashSet<Vector2Int>();

    // Runtime materials (initialised on generate)
    private Material groundMat;
    private Material wallMat;
    private Material bridgeMat;
    private Material projectileMat;
    private Material platformMat;
    private Material winMat;

    // ─────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Call this to run the full generation pipeline.
    /// </summary>
    public void Generate()
    {
        // Reset state
        rooms.Clear();
        corridors.Clear();
        floorTiles.Clear();
        pitTiles.Clear();
        nextRoomId = 0;

        InitMaterials();

        // ── Step A: Grammar Expansion ──
        List<RoomNode> sequence = ExpandGrammar();
        rooms = sequence;

        Debug.Log($"[GrammarDungeon] Grammar produced {rooms.Count} rooms.");

        // ── Step B: Spatial Layout ──
        LayoutRooms();

        // ── Step C: Connect with Corridors ──
        BuildCorridors();

        // ── Step D: Emit Tiles ──
        EmitTiles();
        InstantiateFloorAndWalls();

        // ── Step E: Populate Gameplay ──
        PopulateRooms();

        Debug.Log($"[GrammarDungeon] Dungeon generation complete. " +
                  $"{floorTiles.Count} floor tiles, {pitTiles.Count} pit tiles, " +
                  $"{corridors.Count} corridors.");
    }

    // ─────────────────────────────────────────────
    // Step A: Grammar Expansion
    // ─────────────────────────────────────────────

    /// <summary>
    /// Expands the generative grammar starting from the DUNGEON rule.
    /// Returns an ordered list of RoomNodes (the main path from Start to Finish).
    /// </summary>
    private List<RoomNode> ExpandGrammar()
    {
        List<RoomNode> result = new List<RoomNode>();

        // Rule: DUNGEON → Start → SEQUENCE → Finish
        result.Add(CreateRoom(RoomType.Start, startRoomSize.x, startRoomSize.y));

        int roomCount = Random.Range(minRooms, maxRooms + 1);
        ExpandSequence(result, roomCount, 0);

        result.Add(CreateRoom(RoomType.Finish, finishRoomSize.x, finishRoomSize.y));

        // Link adjacency along the main path
        for (int i = 0; i < result.Count - 1; i++)
        {
            result[i].connectedTo.Add(result[i + 1].id);
            result[i + 1].connectedTo.Add(result[i].id);
        }

        return result;
    }

    /// <summary>
    /// Recursively expands the SEQUENCE non-terminal.
    /// SEQUENCE → ROOM → Corridor → SEQUENCE | ROOM (base case)
    /// </summary>
    private void ExpandSequence(List<RoomNode> result, int remaining, int branchDepth)
    {
        if (remaining <= 0) return;

        RoomNode room = ExpandRoom(branchDepth);
        result.Add(room);

        // Recurse for the rest of the sequence
        ExpandSequence(result, remaining - 1, branchDepth);
    }

    /// <summary>
    /// Expands the ROOM non-terminal into a terminal room type.
    /// ROOM → CHALLENGE (70%) | TreasureRoom (20%) | BRANCH (10%)
    /// </summary>
    private RoomNode ExpandRoom(int branchDepth)
    {
        float roll = Random.value;

        // BRANCH rule (only if we haven't exceeded depth)
        if (roll < branchProbability && branchDepth < maxBranchDepth)
        {
            // For a branch, we just create an extra side-path room and return a challenge
            // The branch creates a fork: main path continues, and a side treasure room is attached
            RoomNode mainRoom = ExpandChallenge();

            // Create a side branch room (always treasure for simplicity)
            RoomNode sideRoom = CreateRoom(
                RoomType.TreasureRoom,
                Random.Range(treasureRoomSizeMin.x, treasureRoomSizeMax.x + 1),
                Random.Range(treasureRoomSizeMin.y, treasureRoomSizeMax.y + 1)
            );
            rooms.Add(sideRoom);

            // Link the side branch to the main room
            mainRoom.connectedTo.Add(sideRoom.id);
            sideRoom.connectedTo.Add(mainRoom.id);

            Debug.Log($"[GrammarDungeon] BRANCH: {mainRoom} has side-path to {sideRoom}");
            return mainRoom;
        }

        // TreasureRoom rule
        if (roll < branchProbability + treasureProbability)
        {
            return CreateRoom(
                RoomType.TreasureRoom,
                Random.Range(treasureRoomSizeMin.x, treasureRoomSizeMax.x + 1),
                Random.Range(treasureRoomSizeMin.y, treasureRoomSizeMax.y + 1)
            );
        }

        // CHALLENGE rule (default)
        return ExpandChallenge();
    }

    /// <summary>
    /// Expands the CHALLENGE non-terminal into one of the three challenge types.
    /// CHALLENGE → BridgeChallenge | BulletHell | PlatformChallenge (uniform)
    /// </summary>
    private RoomNode ExpandChallenge()
    {
        int w = Random.Range(challengeRoomSizeMin.x, challengeRoomSizeMax.x + 1);
        int d = Random.Range(challengeRoomSizeMin.y, challengeRoomSizeMax.y + 1);

        int pick = Random.Range(0, 3);
        switch (pick)
        {
            case 0:
                var bridge = CreateRoom(RoomType.BridgeChallenge, w, d);
                bridge.hasPit = true;
                return bridge;
            case 1:
                return CreateRoom(RoomType.BulletHell, w, d);
            case 2:
                var plat = CreateRoom(RoomType.PlatformChallenge, w, d);
                plat.hasPit = true;
                return plat;
            default:
                return CreateRoom(RoomType.BulletHell, w, d);
        }
    }

    private RoomNode CreateRoom(RoomType type, int width, int depth)
    {
        RoomNode node = new RoomNode
        {
            id = nextRoomId++,
            type = type,
            width = width,
            depth = depth
        };
        return node;
    }

    // ─────────────────────────────────────────────
    // Step B: Spatial Layout (greedy left-to-right)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Places rooms sequentially along the +X axis, each separated by corridor space.
    /// Side-branch rooms are placed along the +Z axis from their parent.
    /// </summary>
    private void LayoutRooms()
    {
        // Identify the main path (rooms in order of the main list)
        // Side-branch rooms are those connected but not in the main path list
        HashSet<int> mainPathIds = new HashSet<int>();
        foreach (var r in rooms)
            mainPathIds.Add(r.id);

        int cursorX = 0;

        // Dictionary for quick lookup
        Dictionary<int, RoomNode> roomById = new Dictionary<int, RoomNode>();
        foreach (var r in rooms)
            roomById[r.id] = r;

        // Layout main-path rooms left to right
        for (int i = 0; i < rooms.Count; i++)
        {
            RoomNode room = rooms[i];

            room.originX = cursorX;
            room.originZ = 0;

            // Advance cursor past this room + corridor gap
            cursorX += room.width + corridorLength;

            // Check for side-branch rooms connected to this room
            foreach (int connId in room.connectedTo)
            {
                if (roomById.ContainsKey(connId))
                {
                    RoomNode connected = roomById[connId];
                    // If not in main path order (i.e. it's a branch room added separately)
                    if (!mainPathIds.Contains(connId))
                    {
                        // Place it above this room along +Z
                        connected.originX = room.originX + (room.width - connected.width) / 2;
                        connected.originZ = room.originZ + room.depth + corridorLength;
                    }
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    // Step C: Corridor Generation
    // ─────────────────────────────────────────────

    /// <summary>
    /// Builds corridor tiles between each pair of connected rooms.
    /// Main-path corridors run along X; branch corridors run along Z.
    /// </summary>
    private void BuildCorridors()
    {
        HashSet<string> processed = new HashSet<string>();

        Dictionary<int, RoomNode> roomById = new Dictionary<int, RoomNode>();
        foreach (var r in rooms)
            roomById[r.id] = r;

        foreach (var room in rooms)
        {
            foreach (int connId in room.connectedTo)
            {
                string edgeKey = Mathf.Min(room.id, connId) + "-" + Mathf.Max(room.id, connId);
                if (processed.Contains(edgeKey)) continue;
                processed.Add(edgeKey);

                if (!roomById.ContainsKey(connId)) continue;
                RoomNode other = roomById[connId];

                CorridorEdge corridor = new CorridorEdge
                {
                    fromRoom = room.id,
                    toRoom = other.id
                };

                // Determine corridor orientation
                if (Mathf.Abs(room.originZ - other.originZ) > 1)
                {
                    // Vertical corridor (branch, runs along Z)
                    RoomNode lower = room.originZ < other.originZ ? room : other;
                    RoomNode upper = room.originZ < other.originZ ? other : room;

                    int midX = lower.originX + lower.width / 2;
                    int startZ = lower.originZ + lower.depth;
                    int endZ = upper.originZ;

                    corridor.originX = midX;
                    corridor.originZ = startZ;
                    corridor.width = corridorWidth;
                    corridor.length = endZ - startZ;
                }
                else
                {
                    // Horizontal corridor (main path, runs along X)
                    RoomNode left = room.originX < other.originX ? room : other;
                    RoomNode right = room.originX < other.originX ? other : room;

                    int startX = left.originX + left.width;
                    int endX = right.originX;
                    int midZ = left.originZ + left.depth / 2;

                    corridor.originX = startX;
                    corridor.originZ = midZ;
                    corridor.width = endX - startX;
                    corridor.length = corridorWidth;
                }

                corridors.Add(corridor);
            }
        }
    }

    // ─────────────────────────────────────────────
    // Step D: Tile Emission
    // ─────────────────────────────────────────────

    /// <summary>
    /// Converts the abstract room graph into concrete floor/pit tile sets.
    /// </summary>
    private void EmitTiles()
    {
        // Emit room tiles
        foreach (var room in rooms)
        {
            for (int x = room.originX; x < room.originX + room.width; x++)
            {
                for (int z = room.originZ; z < room.originZ + room.depth; z++)
                {
                    if (room.hasPit)
                    {
                        // Pit rooms: only first and last X columns are solid floor (landing pads)
                        if (x == room.originX || x == room.originX + room.width - 1)
                            floorTiles.Add(new Vector2Int(x, z));
                        else
                            pitTiles.Add(new Vector2Int(x, z));
                    }
                    else
                    {
                        floorTiles.Add(new Vector2Int(x, z));
                    }
                }
            }
        }

        // Emit corridor tiles
        foreach (var cor in corridors)
        {
            for (int x = cor.originX; x < cor.originX + cor.width; x++)
            {
                for (int z = cor.originZ; z < cor.originZ + cor.length; z++)
                {
                    floorTiles.Add(new Vector2Int(x, z));
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    // Step D (cont.): Instantiate Floor & Walls
    // ─────────────────────────────────────────────

    /// <summary>
    /// Creates the visible floor cubes and surrounding walls from the tile sets.
    /// Uses the same cellular wall-building approach as TimePuzzleDemoCreator.
    /// </summary>
    private void InstantiateFloorAndWalls()
    {
        // Floor tiles
        foreach (var tile in floorTiles)
        {
            Vector3 pos = new Vector3(tile.x, 0f, tile.y);
            if (floorPrefab != null)
            {
                Instantiate(floorPrefab, pos + Vector3.up * 0.05f, Quaternion.Euler(90f, 0f, 0f));
            }
            else
            {
                GameObject floorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                floorObj.name = "Floor_" + tile.x + "_" + tile.y;
                floorObj.transform.position = pos - Vector3.up * 0.5f;
                floorObj.transform.localScale = Vector3.one;
                floorObj.GetComponent<MeshRenderer>().material = groundMat;
            }
        }

        // Cellular wall builder — scan bounding box and place walls adjacent to floor/pit
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var t in floorTiles) { minX = Mathf.Min(minX, t.x); maxX = Mathf.Max(maxX, t.x); minZ = Mathf.Min(minZ, t.y); maxZ = Mathf.Max(maxZ, t.y); }
        foreach (var t in pitTiles) { minX = Mathf.Min(minX, t.x); maxX = Mathf.Max(maxX, t.x); minZ = Mathf.Min(minZ, t.y); maxZ = Mathf.Max(maxZ, t.y); }

        Vector2Int[] dirs = { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1) };

        for (int x = minX - 1; x <= maxX + 1; x++)
        {
            for (int z = minZ - 1; z <= maxZ + 1; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                if (floorTiles.Contains(pos) || pitTiles.Contains(pos)) continue;

                bool adjacentToRoom = false;
                foreach (var d in dirs)
                {
                    Vector2Int nb = pos + d;
                    if (floorTiles.Contains(nb) || pitTiles.Contains(nb))
                    {
                        adjacentToRoom = true;
                        break;
                    }
                }

                if (adjacentToRoom)
                {
                    if (wallPrefab != null)
                    {
                        Instantiate(wallPrefab, new Vector3(x, 1.0f, z), Quaternion.identity);
                        Instantiate(wallPrefab, new Vector3(x, 3.0f, z), Quaternion.identity);
                    }
                    else
                    {
                        GameObject w1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        w1.name = "Wall1_" + x + "_" + z;
                        w1.transform.position = new Vector3(x, 0.5f, z);
                        w1.GetComponent<MeshRenderer>().material = wallMat;

                        GameObject w2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        w2.name = "Wall2_" + x + "_" + z;
                        w2.transform.position = new Vector3(x, 1.5f, z);
                        w2.GetComponent<MeshRenderer>().material = wallMat;
                    }
                }
            }
        }
    }

    // ─────────────────────────────────────────────
    // Step E: Gameplay Population
    // ─────────────────────────────────────────────

    /// <summary>
    /// Populates each room with gameplay objects appropriate to its type.
    /// Mirrors the zone-building logic from TimePuzzleDemoCreator.
    /// </summary>
    private void PopulateRooms()
    {
        foreach (var room in rooms)
        {
            float centerX = room.originX + room.width * 0.5f;
            float centerZ = room.originZ + room.depth * 0.5f;

            switch (room.type)
            {
                case RoomType.Start:
                    PopulateStartRoom(room);
                    break;

                case RoomType.BridgeChallenge:
                    PopulateBridgeRoom(room);
                    break;

                case RoomType.BulletHell:
                    PopulateBulletHellRoom(room);
                    break;

                case RoomType.PlatformChallenge:
                    PopulatePlatformRoom(room);
                    break;

                case RoomType.TreasureRoom:
                    PopulateTreasureRoom(room);
                    break;

                case RoomType.BossArena:
                    // Reserved for future boss encounter logic
                    Debug.Log($"[GrammarDungeon] BossArena room {room.id} — populate boss here.");
                    break;

                case RoomType.Finish:
                    PopulateFinishRoom(room);
                    break;
            }
        }
    }

    // ── Room Population Helpers ──

    private void PopulateStartRoom(RoomNode room)
    {
        // The player spawns here; just log it
        Debug.Log($"[GrammarDungeon] Start room at ({room.originX},{room.originZ}). " +
                  $"Teleport player to ({room.originX + room.width/2}, 1.2, {room.originZ + room.depth/2}).");
    }

    private void PopulateBridgeRoom(RoomNode room)
    {
        // Place collapsing bridge segments across the pit
        int segmentCount = Mathf.Max(2, (room.width - 2) / 2);
        float startX = room.originX + 1.5f;
        float step = (float)(room.width - 3) / Mathf.Max(1, segmentCount - 1);
        float midZ = room.originZ + room.depth * 0.5f;

        for (int i = 0; i < segmentCount; i++)
        {
            float sx = startX + i * step;
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = $"BridgeSegment_{room.id}_{i}";
            segment.transform.position = new Vector3(sx, 0f, midZ);
            segment.transform.localScale = new Vector3(1.8f, 0.4f, room.depth * 0.55f);
            segment.GetComponent<MeshRenderer>().material = bridgeMat;

            // Attach time-puzzle bridge components
            segment.AddComponent<TimeRewindableObject>();
            segment.AddComponent<CollapsingBridge>();
        }

        // Checkpoint teleporter below the pit
        CreateCheckpoint(
            new Vector3(room.originX + room.width * 0.5f, -8f, midZ),
            new Vector3(room.width + 4f, 2f, 15f),
            new Vector3(room.originX + 0.5f, 1.2f, midZ)
        );

        Debug.Log($"[GrammarDungeon] Bridge room {room.id}: {segmentCount} segments placed.");
    }

    private void PopulateBulletHellRoom(RoomNode room)
    {
        // Projectile template (shared across spawners in this room)
        GameObject projTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projTemplate.name = $"ProjectileTemplate_{room.id}";
        projTemplate.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        projTemplate.GetComponent<MeshRenderer>().material = projectileMat;
        projTemplate.GetComponent<Collider>().isTrigger = true;
        projTemplate.AddComponent<TimeFreezableObject>();
        projTemplate.AddComponent<TimeRewindableObject>();
        projTemplate.AddComponent<Projectile>();
        projTemplate.SetActive(false);

        // Place 2-4 spawners along the north/south walls
        int spawnerCount = Random.Range(2, 5);
        for (int i = 0; i < spawnerCount; i++)
        {
            float sx = room.originX + 2f + i * ((float)(room.width - 4) / Mathf.Max(1, spawnerCount - 1));
            bool northSide = (i % 2 == 0);
            float sz = northSide
                ? room.originZ + room.depth + 0.2f
                : room.originZ - 0.2f;
            Quaternion rot = northSide
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;

            CreateSpawner(new Vector3(sx, 1f, sz), rot, projTemplate);
        }

        Debug.Log($"[GrammarDungeon] BulletHell room {room.id}: {spawnerCount} turrets placed.");
    }

    private void PopulatePlatformRoom(RoomNode room)
    {
        float midZ = room.originZ + room.depth * 0.5f;

        // Platform template
        GameObject platTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platTemplate.name = $"FallingPlatformTemplate_{room.id}";
        platTemplate.transform.localScale = new Vector3(3f, 0.5f, room.depth * 0.5f);
        platTemplate.GetComponent<MeshRenderer>().material = platformMat;
        platTemplate.AddComponent<Rigidbody>();
        platTemplate.AddComponent<TimeFreezableObject>();
        platTemplate.AddComponent<TimeRewindableObject>();
        platTemplate.SetActive(false);

        // Dropper above the center
        GameObject dropper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dropper.name = $"PlatformDropper_{room.id}";
        dropper.transform.position = new Vector3(room.originX + room.width * 0.5f, 7f, midZ);
        dropper.transform.localScale = Vector3.one;
        dropper.GetComponent<MeshRenderer>().enabled = false;
        Destroy(dropper.GetComponent<Collider>());

        FallingPlatformDropper fpd = dropper.AddComponent<FallingPlatformDropper>();
        fpd.platformPrefab = platTemplate;
        fpd.spawnCooldown = 2.0f;
        fpd.destroyY = -12f;

        // Checkpoint teleporter
        CreateCheckpoint(
            new Vector3(room.originX + room.width * 0.5f, -8f, midZ),
            new Vector3(room.width + 4f, 2f, 15f),
            new Vector3(room.originX + 0.5f, 1.2f, midZ)
        );

        Debug.Log($"[GrammarDungeon] Platform room {room.id}: dropper placed at ({dropper.transform.position}).");
    }

    private void PopulateTreasureRoom(RoomNode room)
    {
        float cx = room.originX + room.width * 0.5f;
        float cz = room.originZ + room.depth * 0.5f;

        // Place 1-2 reward chests
        int chestCount = Random.Range(1, 3);
        for (int i = 0; i < chestCount; i++)
        {
            float ox = cx + (i == 0 ? -1f : 1f);
            GameObject chest = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chest.name = $"TreasureChest_{room.id}_{i}";
            chest.transform.position = new Vector3(ox, 0.25f, cz);
            chest.transform.localScale = new Vector3(0.8f, 0.5f, 0.6f);

            Material chestMat = new Material(Shader.Find("Standard"));
            chestMat.color = new Color(0.5f, 0.25f, 0.05f);
            chest.GetComponent<MeshRenderer>().material = chestMat;
        }

        Debug.Log($"[GrammarDungeon] Treasure room {room.id}: {chestCount} chests placed.");
    }

    private void PopulateFinishRoom(RoomNode room)
    {
        float cx = room.originX + room.width * 0.5f;
        float cz = room.originZ + room.depth * 0.5f;

        // Win zone cylinder
        GameObject winVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        winVisual.name = "WinVisual";
        winVisual.transform.position = new Vector3(cx, 0.15f, cz);
        winVisual.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
        winVisual.GetComponent<MeshRenderer>().material = winMat;
        winVisual.GetComponent<Collider>().isTrigger = true;
        winVisual.AddComponent<WinTrigger>();

        Debug.Log($"[GrammarDungeon] Finish room {room.id}: win trigger at ({cx}, {cz}).");
    }

    // ─────────────────────────────────────────────
    // Shared Helpers (mirrored from TimePuzzleDemoCreator)
    // ─────────────────────────────────────────────

    private void CreateSpawner(Vector3 pos, Quaternion rot, GameObject template)
    {
        GameObject spawnerParent = new GameObject("TurretSpawner_Container");
        spawnerParent.transform.position = pos;
        spawnerParent.transform.rotation = rot;

        GameObject spawnerVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spawnerVisual.name = "TurretSpawner_Visual";
        spawnerVisual.transform.SetParent(spawnerParent.transform);
        spawnerVisual.transform.localPosition = Vector3.zero;
        spawnerVisual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spawnerVisual.transform.localScale = new Vector3(0.6f, 0.4f, 0.6f);

        spawnerParent.AddComponent<TimeFreezableObject>();

        ProjectileSpawner ps = spawnerParent.AddComponent<ProjectileSpawner>();
        ps.projectilePrefab = template;
        ps.spawnInterval = 1.2f;
        ps.projectileSpeed = 8f;
    }

    private void CreateCheckpoint(Vector3 pos, Vector3 scale, Vector3 targetPos)
    {
        GameObject checkpoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
        checkpoint.name = "CheckpointTrigger";
        checkpoint.transform.position = pos;
        checkpoint.transform.localScale = scale;
        checkpoint.GetComponent<MeshRenderer>().enabled = false;
        checkpoint.GetComponent<Collider>().isTrigger = true;

        CheckpointTeleporter ct = checkpoint.AddComponent<CheckpointTeleporter>();
        ct.targetPosition = targetPos;
    }

    private void InitMaterials()
    {
        groundMat = groundMaterial != null ? groundMaterial : MakeMat(new Color(0.12f, 0.12f, 0.15f), false, 0f);
        wallMat = wallMaterial != null ? wallMaterial : MakeMat(new Color(0.2f, 0.2f, 0.23f), false, 0f);
        bridgeMat = bridgeMaterial != null ? bridgeMaterial : MakeMat(new Color(0.9f, 0.65f, 0.15f), true, 0.6f);
        projectileMat = projectileMaterial != null ? projectileMaterial : MakeMat(new Color(1f, 0.25f, 0.05f), true, 1.2f);
        platformMat = platformMaterial != null ? platformMaterial : MakeMat(new Color(0f, 0.75f, 1f), true, 0.9f);
        winMat = winMaterial != null ? winMaterial : MakeMat(new Color(0.15f, 0.9f, 0.25f), true, 1.0f);
    }

    private Material MakeMat(Color color, bool emit, float glow)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Metallic", 0.4f);
        mat.SetFloat("_Glossiness", 0.6f);
        if (emit)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * glow);
        }
        return mat;
    }

    // ─────────────────────────────────────────────
    // Debug Utility
    // ─────────────────────────────────────────────

    /// <summary>
    /// Prints the full grammar expansion and layout to the console.
    /// Call this after Generate() to inspect the result.
    /// </summary>
    public void PrintDungeonGraph()
    {
        Debug.Log("=== DUNGEON GRAMMAR GRAPH ===");
        foreach (var r in rooms)
        {
            string connections = string.Join(", ", r.connectedTo);
            Debug.Log($"  {r} → connected to [{connections}]");
        }
        Debug.Log($"=== {corridors.Count} corridors ===");
        foreach (var c in corridors)
        {
            Debug.Log($"  Corridor: Room#{c.fromRoom} ↔ Room#{c.toRoom} at ({c.originX},{c.originZ}) size({c.width}x{c.length})");
        }
    }
}
