using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// GraphGrammarDungeonGenerator - An advanced generative grammar system that separate missions from spaces.
/// It builds a Directed Acyclic Graph (DAG) for the mission path (with cycles and parallel branches)
/// and places rooms on a 2D grid to resolve overlaps, integrating all existing gameplay modules.
/// </summary>
public class GraphGrammarDungeonGenerator : MonoBehaviour
{
    [Header("Mission Graph Settings")]
    [Tooltip("Minimum number of challenge nodes on the main progression path.")]
    public int minMainRooms = 4;
    [Tooltip("Maximum number of challenge nodes on the main progression path.")]
    public int maxMainRooms = 8;
    [Tooltip("Probability of branching a room into a side-challenge or reward.")]
    [Range(0f, 1f)]
    public float branchChance = 0.4f;
    [Tooltip("Probability that a branch will loop/rejoin the main path instead of being a dead end.")]
    [Range(0f, 1f)]
    public float loopRejoinChance = 0.5f;

    [Header("Spatial Layout Settings")]
    [Tooltip("Grid cell size in Unity units. Each room occupies one grid cell.")]
    public int gridCellSize = 30;
    [Tooltip("Minimum width & depth of rooms in tiles.")]
    public Vector2Int roomSizeMin = new Vector2Int(8, 8);
    [Tooltip("Maximum width & depth of rooms in tiles.")]
    public Vector2Int roomSizeMax = new Vector2Int(14, 14);
    [Tooltip("Width of corridors in tiles.")]
    [Range(1, 4)]
    public int corridorWidth = 2;

    [Header("Prefabs (Optional - Procedural Fallbacks Used If Empty)")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject chestPrefab;
    public GameObject healthPotionPrefab;
    public GameObject manaPotionPrefab;
    public GameObject stonePrefabA;
    public GameObject stonePrefabB;
    public GameObject tablePrefabA;
    public GameObject tablePrefabB;
    public GameObject domePrefab;
    public GameObject enemyPrefab;
    public GameObject enemyAgentPrefab;
    public GameObject itemGeneratorPrefab;
    public GameObject plantGeneratorPrefab;
    public GameObject collapsingBridgePrefab;
    public GameObject fallingPlatformDropperPrefab;
    public GameObject projectileSpawnerPrefab;
    public GameObject winPlatformPrefab;

    [Header("Visual Materials (Procedural Fallbacks Used If Empty)")]
    public Material groundMaterial;
    public Material wallMaterial;
    public Material bridgeMaterial;
    public Material projectileMaterial;
    public Material platformMaterial;
    public Material spawnerMaterial;
    public Material winMaterial;
    public Material lavaMaterial;

    [Header("Custom Lighting")]
    public Color cameraBackgroundColor = new Color(0.12f, 0.12f, 0.15f, 1f);

    // ─────────────────────────────────────────────
    // Data Structures & Enums
    // ─────────────────────────────────────────────

    public enum RoomType
    {
        Start,
        CombatRoom,
        BridgePuzzle,
        PlatformPuzzle,
        TurretPuzzle,
        TreasureRoom,
        BossArena,
        ExitRoom
    }

    /// <summary>Topological graph representation of the mission layout.</summary>
    public class MissionNode
    {
        public int id;
        public RoomType type;
        public List<int> connectedTo = new List<int>();

        public override string ToString()
        {
            return $"Node#{id} [{type}]";
        }
    }

    /// <summary>Geometrical layout representation of a room in 2D space.</summary>
    public class RoomLayoutNode
    {
        public int id;
        public RoomType type;
        public Vector2Int gridPos;
        public int width;
        public int depth;

        // Bottom-left origin corner in global tile coordinates
        public int originX;
        public int originZ;

        public bool hasPit;
        public List<int> connectedTo = new List<int>();

        public override string ToString()
        {
            return $"Room#{id} [{type}] at grid {gridPos} size={width}x{depth}";
        }
    }

    /// <summary>Geometrical layout of a corridor link.</summary>
    public class CorridorLink
    {
        public int fromRoom;
        public int toRoom;
        public List<Vector2Int> tiles = new List<Vector2Int>();
    }

    // ─────────────────────────────────────────────
    // Runtime Fields
    // ─────────────────────────────────────────────
    private List<RoomLayoutNode> rooms = new List<RoomLayoutNode>();
    private List<CorridorLink> corridors = new List<CorridorLink>();
    private HashSet<Vector2Int> floorTiles = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> pitTiles = new HashSet<Vector2Int>();
    private int nextNodeId = 0;
    private GameObject dungeonContainer;

    private Material groundMat;
    private Material wallMat;
    private Material bridgeMat;
    private Material projectileMat;
    private Material platformMat;
    private Material spawnerMat;
    private Material winMat;
    private Material lavaMat;

    // ─────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Generates the dungeon layout and spawns all visual elements and gameplay modules.
    /// </summary>
    public void GenerateDungeon()
    {
        // 1. Reset Scene Container
        if (dungeonContainer != null)
        {
            DestroyImmediate(dungeonContainer);
        }
        dungeonContainer = new GameObject("ProceduralDungeon_GraphGrammar");

        rooms.Clear();
        corridors.Clear();
        floorTiles.Clear();
        pitTiles.Clear();
        nextNodeId = 0;

        // Apply Custom Camera Color
        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = cameraBackgroundColor;
        }

        // Initialize materials
        InitMaterials();

        // ── Phase 1: Mission Graph Grammar Expansion ──
        List<MissionNode> missionGraph = ExpandMissionGrammar();
        Debug.Log($"[GraphGrammar] Mission topology generated with {missionGraph.Count} nodes.");

        // ── Phase 2: 2D Grid Room Placer ──
        LayoutRoomsOnGrid(missionGraph);

        // ── Phase 3: Connect Rooms with Corridors ──
        BuildCorridorPaths();

        // ── Phase 4: Emit Tiles & Meshes ──
        EmitTileSets();
        InstantiateDungeonMesh();

        // ── Phase 5: Populate Gameplay Modules ──
        PopulateDungeonObjects();

        Debug.Log($"[GraphGrammar] Generation Finished. " +
                  $"{rooms.Count} rooms, {corridors.Count} corridors spawned. " +
                  $"{floorTiles.Count} floor tiles, {pitTiles.Count} pit tiles.");
    }

    // ─────────────────────────────────────────────
    // Phase 1: Mission Graph Grammar Expansion
    // ─────────────────────────────────────────────

    private List<MissionNode> ExpandMissionGrammar()
    {
        List<MissionNode> graph = new List<MissionNode>();
        Dictionary<int, MissionNode> nodeMap = new Dictionary<int, MissionNode>();

        // Start Node
        MissionNode startNode = CreateMissionNode(RoomType.Start);
        graph.Add(startNode);
        nodeMap[startNode.id] = startNode;

        // Generate Main Chain of Challenges
        int mainChainLength = Random.Range(minMainRooms, maxMainRooms + 1);
        MissionNode current = startNode;

        List<MissionNode> mainChain = new List<MissionNode>();
        for (int i = 0; i < mainChainLength; i++)
        {
            RoomType challengeType = GetRandomChallengeType();
            MissionNode challenge = CreateMissionNode(challengeType);
            graph.Add(challenge);
            nodeMap[challenge.id] = challenge;
            mainChain.Add(challenge);

            // Connect in sequence
            current.connectedTo.Add(challenge.id);
            challenge.connectedTo.Add(current.id);
            current = challenge;
        }

        // Add Boss Arena
        MissionNode bossNode = CreateMissionNode(RoomType.BossArena);
        graph.Add(bossNode);
        nodeMap[bossNode.id] = bossNode;
        current.connectedTo.Add(bossNode.id);
        bossNode.connectedTo.Add(current.id);

        // Add Exit Node
        MissionNode exitNode = CreateMissionNode(RoomType.ExitRoom);
        graph.Add(exitNode);
        nodeMap[exitNode.id] = exitNode;
        bossNode.connectedTo.Add(exitNode.id);
        exitNode.connectedTo.Add(bossNode.id);

        // Stochastic Branching & Rejoining (Loops)
        for (int i = 0; i < mainChain.Count; i++)
        {
            if (Random.value < branchChance)
            {
                MissionNode parent = mainChain[i];
                MissionNode treasure = CreateMissionNode(RoomType.TreasureRoom);
                graph.Add(treasure);
                nodeMap[treasure.id] = treasure;

                parent.connectedTo.Add(treasure.id);
                treasure.connectedTo.Add(parent.id);

                // Rejoin opportunity (create a cycle/loop)
                if (Random.value < loopRejoinChance && i < mainChain.Count - 1)
                {
                    MissionNode rejoinTarget = mainChain[i + 1];
                    treasure.connectedTo.Add(rejoinTarget.id);
                    rejoinTarget.connectedTo.Add(treasure.id);
                    Debug.Log($"[GraphGrammar] Created Loop: Node {parent.id} -> Treasure Node {treasure.id} -> Rejoin Node {rejoinTarget.id}");
                }
                else
                {
                    Debug.Log($"[GraphGrammar] Created Dead-End Branch: Node {parent.id} -> Treasure Node {treasure.id}");
                }
            }
        }

        return graph;
    }

    private MissionNode CreateMissionNode(RoomType type)
    {
        return new MissionNode { id = nextNodeId++, type = type };
    }

    private RoomType GetRandomChallengeType()
    {
        float roll = Random.value;
        if (roll < 0.25f) return RoomType.CombatRoom;
        if (roll < 0.50f) return RoomType.BridgePuzzle;
        if (roll < 0.75f) return RoomType.PlatformPuzzle;
        return RoomType.TurretPuzzle;
    }

    // ─────────────────────────────────────────────
    // Phase 2: 2D Grid Room Placer
    // ─────────────────────────────────────────────

    private void LayoutRoomsOnGrid(List<MissionNode> missionGraph)
    {
        Dictionary<int, RoomLayoutNode> laidOut = new Dictionary<int, RoomLayoutNode>();
        Dictionary<Vector2Int, RoomLayoutNode> grid = new Dictionary<Vector2Int, RoomLayoutNode>();

        Queue<int> queue = new Queue<int>();
        queue.Enqueue(missionGraph[0].id); // Start with Start Room

        // Quick lookup for mission nodes
        Dictionary<int, MissionNode> mNodes = new Dictionary<int, MissionNode>();
        foreach (var m in missionGraph) mNodes[m.id] = m;

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),   // North
            new Vector2Int(0, -1),  // South
            new Vector2Int(1, 0),   // East
            new Vector2Int(-1, 0)   // West
        };

        while (queue.Count > 0)
        {
            int currentId = queue.Dequeue();
            if (laidOut.ContainsKey(currentId)) continue;

            MissionNode mNode = mNodes[currentId];
            RoomLayoutNode layout = new RoomLayoutNode
            {
                id = mNode.id,
                type = mNode.type,
                width = Random.Range(roomSizeMin.x, roomSizeMax.x + 1),
                depth = Random.Range(roomSizeMin.y, roomSizeMax.y + 1),
                hasPit = (mNode.type == RoomType.BridgePuzzle || mNode.type == RoomType.PlatformPuzzle)
            };

            // Link connection info
            foreach (var conn in mNode.connectedTo)
            {
                layout.connectedTo.Add(conn);
            }

            if (currentId == missionGraph[0].id)
            {
                // Start Node at (0, 0)
                layout.gridPos = Vector2Int.zero;
                grid[Vector2Int.zero] = layout;
            }
            else
            {
                // Find placed parent
                RoomLayoutNode parentLayout = null;
                foreach (var connId in mNode.connectedTo)
                {
                    if (laidOut.ContainsKey(connId))
                    {
                        parentLayout = laidOut[connId];
                        break;
                    }
                }

                if (parentLayout != null)
                {
                    // Find an empty cardinal cell around the parent
                    Vector2Int targetPos = Vector2Int.zero;
                    bool foundSpot = false;

                    // Shuffle directions
                    List<Vector2Int> dirList = new List<Vector2Int>(directions);
                    for (int s = 0; s < dirList.Count; s++)
                    {
                        int rIndex = Random.Range(s, dirList.Count);
                        var temp = dirList[s];
                        dirList[s] = dirList[rIndex];
                        dirList[rIndex] = temp;
                    }

                    foreach (var dir in dirList)
                    {
                        Vector2Int checkPos = parentLayout.gridPos + dir;
                        if (!grid.ContainsKey(checkPos))
                        {
                            targetPos = checkPos;
                            foundSpot = true;
                            break;
                        }
                    }

                    if (!foundSpot)
                    {
                        // BFS search for closest open grid slot
                        targetPos = FindNearestFreeGridPos(parentLayout.gridPos, grid);
                    }

                    layout.gridPos = targetPos;
                    grid[targetPos] = layout;
                }
                else
                {
                    // Isolated node safety fallback
                    Vector2Int targetPos = FindNearestFreeGridPos(Vector2Int.zero, grid);
                    layout.gridPos = targetPos;
                    grid[targetPos] = layout;
                }
            }

            // Define absolute tile positions
            layout.originX = layout.gridPos.x * gridCellSize - layout.width / 2;
            layout.originZ = layout.gridPos.y * gridCellSize - layout.depth / 2;

            laidOut[currentId] = layout;
            rooms.Add(layout);

            // Queue connected unplaced rooms
            foreach (int connId in mNode.connectedTo)
            {
                if (!laidOut.ContainsKey(connId))
                {
                    queue.Enqueue(connId);
                }
            }
        }
    }

    private Vector2Int FindNearestFreeGridPos(Vector2Int start, Dictionary<Vector2Int, RoomLayoutNode> grid)
    {
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        Vector2Int[] directions = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        while (queue.Count > 0)
        {
            Vector2Int curr = queue.Dequeue();
            if (!grid.ContainsKey(curr))
            {
                return curr;
            }

            foreach (var dir in directions)
            {
                Vector2Int next = curr + dir;
                if (!visited.Contains(next))
                {
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }
        }

        return start + new Vector2Int(Random.Range(-5, 6), Random.Range(-5, 6));
    }

    // ─────────────────────────────────────────────
    // Phase 3: Connect Rooms with Corridors
    // ─────────────────────────────────────────────

    private void BuildCorridorPaths()
    {
        HashSet<string> processedEdges = new HashSet<string>();
        Dictionary<int, RoomLayoutNode> roomLookup = new Dictionary<int, RoomLayoutNode>();
        foreach (var r in rooms) roomLookup[r.id] = r;

        foreach (var room in rooms)
        {
            foreach (int connId in room.connectedTo)
            {
                if (!roomLookup.ContainsKey(connId)) continue;
                RoomLayoutNode other = roomLookup[connId];

                string edgeKey = Mathf.Min(room.id, connId) + "-" + Mathf.Max(room.id, connId);
                if (processedEdges.Contains(edgeKey)) continue;
                processedEdges.Add(edgeKey);

                CorridorLink corridor = new CorridorLink
                {
                    fromRoom = room.id,
                    toRoom = other.id
                };

                // Connect centers of rooms
                Vector2Int cA = new Vector2Int(room.originX + room.width / 2, room.originZ + room.depth / 2);
                Vector2Int cB = new Vector2Int(other.originX + other.width / 2, other.originZ + other.depth / 2);

                // Draw L-shape corridor: first along X, then Z
                int xSign = (int)Mathf.Sign(cB.x - cA.x);
                int zSign = (int)Mathf.Sign(cB.y - cA.y);

                // X movement
                int currX = cA.x;
                while (currX != cB.x)
                {
                    for (int w = -corridorWidth / 2; w < (corridorWidth + 1) / 2; w++)
                    {
                        corridor.tiles.Add(new Vector2Int(currX, cA.y + w));
                    }
                    currX += xSign;
                }

                // Z movement
                int currZ = cA.y;
                while (currZ != cB.y)
                {
                    for (int w = -corridorWidth / 2; w < (corridorWidth + 1) / 2; w++)
                    {
                        corridor.tiles.Add(new Vector2Int(cB.x + w, currZ));
                    }
                    currZ += zSign;
                }

                corridors.Add(corridor);
            }
        }
    }

    // ─────────────────────────────────────────────
    // Phase 4: Emit Tiles & Meshes
    // ─────────────────────────────────────────────

    private void EmitTileSets()
    {
        // Emit Room Tiles
        foreach (var room in rooms)
        {
            for (int x = room.originX; x < room.originX + room.width; x++)
            {
                for (int z = room.originZ; z < room.originZ + room.depth; z++)
                {
                    Vector2Int pos = new Vector2Int(x, z);
                    if (room.hasPit)
                    {
                        // Only ends of the room (along X orientation for puzzle pathing) are solid landing pads
                        if (x <= room.originX + 1 || x >= room.originX + room.width - 2)
                        {
                            floorTiles.Add(pos);
                        }
                        else
                        {
                            pitTiles.Add(pos);
                        }
                    }
                    else
                    {
                        floorTiles.Add(pos);
                    }
                }
            }
        }

        // Emit Corridor Tiles
        foreach (var corridor in corridors)
        {
            foreach (var tile in corridor.tiles)
            {
                floorTiles.Add(tile);
                // Corridor floors override pit chasms to ensure traversal paths
                if (pitTiles.Contains(tile))
                {
                    pitTiles.Remove(tile);
                }
            }
        }
    }

    private void InstantiateDungeonMesh()
    {
        // 1. Create Floor Mesh
        GameObject floorsContainer = new GameObject("Floors");
        floorsContainer.transform.parent = dungeonContainer.transform;

        foreach (var tile in floorTiles)
        {
            Vector3 pos = new Vector3(tile.x, -0.5f, tile.y);
            if (floorPrefab != null)
            {
                Instantiate(floorPrefab, pos + Vector3.up * 0.5f, Quaternion.identity, floorsContainer.transform);
            }
            else
            {
                GameObject tileObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tileObj.name = $"Floor_{tile.x}_{tile.y}";
                tileObj.transform.position = pos;
                tileObj.transform.parent = floorsContainer.transform;
                tileObj.GetComponent<MeshRenderer>().material = groundMat;
            }
        }

        // 2. Identify and Instantiate Wall Tiles
        GameObject wallsContainer = new GameObject("Walls");
        wallsContainer.transform.parent = dungeonContainer.transform;

        HashSet<Vector2Int> wallTiles = new HashSet<Vector2Int>();
        Vector2Int[] neighbors = new Vector2Int[]
        {
            new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
        };

        foreach (var tile in floorTiles)
        {
            foreach (var n in neighbors)
            {
                Vector2Int nPos = tile + n;
                if (!floorTiles.Contains(nPos) && !pitTiles.Contains(nPos))
                {
                    wallTiles.Add(nPos);
                }
            }
        }

        foreach (var wall in wallTiles)
        {
            Vector3 pos = new Vector3(wall.x, 1.0f, wall.y);
            if (wallPrefab != null)
            {
                Instantiate(wallPrefab, pos + Vector3.down * 0.5f, Quaternion.identity, wallsContainer.transform);
            }
            else
            {
                GameObject wallObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wallObj.name = $"Wall_{wall.x}_{wall.y}";
                wallObj.transform.position = pos;
                wallObj.transform.localScale = new Vector3(1f, 3f, 1f);
                wallObj.transform.parent = wallsContainer.transform;
                wallObj.GetComponent<MeshRenderer>().material = wallMat;
            }
        }
    }

    // ─────────────────────────────────────────────
    // Phase 5: Populate Room Gameplay Objects
    // ─────────────────────────────────────────────

    private void PopulateDungeonObjects()
    {
        GameObject gameplayContainer = new GameObject("Gameplay");
        gameplayContainer.transform.parent = dungeonContainer.transform;

        GameObject playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
        Transform playerTransform = playerObj != null ? playerObj.transform : null;

        foreach (var room in rooms)
        {
            float cx = room.originX + room.width / 2f;
            float cz = room.originZ + room.depth / 2f;
            Vector3 centerPos = new Vector3(cx, 0f, cz);

            switch (room.type)
            {
                case RoomType.Start:
                    // Teleport Player
                    if (playerObj != null)
                    {
                        Rigidbody prb = playerObj.GetComponent<Rigidbody>();
                        if (prb != null)
                        {
                            prb.velocity = Vector3.zero;
                            prb.angularVelocity = Vector3.zero;
                        }
                        playerTransform.position = centerPos + Vector3.up * 1f;

                        // Ensure TimeScreenEffectController is attached
                        if (playerObj.GetComponent<TimeScreenEffectController>() == null)
                        {
                            playerObj.AddComponent<TimeScreenEffectController>();
                        }
                        Debug.Log($"[GraphGrammar] Player teleported to Start Room at {playerTransform.position}");
                    }

                    // Add dynamic check point trigger
                    CreateCheckpointTrigger(centerPos, new Vector3(room.width - 2, 2, room.depth - 2), centerPos + Vector3.up * 1.2f, gameplayContainer.transform);

                    // Scatter starting decorations
                    if (tablePrefabA != null)
                    {
                        Instantiate(tablePrefabA, centerPos + Vector3.left * 2f, Quaternion.identity, gameplayContainer.transform);
                    }
                    if (stonePrefabA != null)
                    {
                        Instantiate(stonePrefabA, centerPos + Vector3.right * 2f + Vector3.forward * 1.5f, Quaternion.identity, gameplayContainer.transform);
                    }
                    break;

                case RoomType.CombatRoom:
                    // Spawn 1-3 NavMesh Enemies
                    int enemyCount = Random.Range(1, 4);
                    for (int e = 0; e < enemyCount; e++)
                    {
                        Vector3 enemySpawn = centerPos + new Vector3(Random.Range(-room.width/3f, room.width/3f), 1f, Random.Range(-room.depth/3f, room.depth/3f));
                        if (enemyPrefab != null)
                        {
                            Instantiate(enemyPrefab, enemySpawn, Quaternion.identity, gameplayContainer.transform);
                        }
                        else
                        {
                            // Procedural Enemy placeholder
                            GameObject dummyEnemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                            dummyEnemy.name = "ProceduralEnemy";
                            dummyEnemy.transform.position = enemySpawn;
                            dummyEnemy.transform.parent = gameplayContainer.transform;
                            dummyEnemy.GetComponent<MeshRenderer>().material = MakeBasicMaterial(Color.red);
                            dummyEnemy.AddComponent<NavMeshAgent>();
                            dummyEnemy.AddComponent<Enemy>();
                        }
                    }
                    break;

                case RoomType.BridgePuzzle:
                    // Spawn falling bridge blocks along X path
                    int startBlockX = room.originX + 2;
                    int endBlockX = room.originX + room.width - 3;
                    for (int bx = startBlockX; bx <= endBlockX; bx++)
                    {
                        Vector3 bridgePos = new Vector3(bx, 0f, cz);
                        GameObject segment;
                        if (collapsingBridgePrefab != null)
                        {
                            segment = Instantiate(collapsingBridgePrefab, bridgePos, Quaternion.identity, gameplayContainer.transform);
                        }
                        else
                        {
                            segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            segment.name = $"CollapsingBridgeSegment_{bx}";
                            segment.transform.position = bridgePos;
                            segment.transform.localScale = new Vector3(1f, 0.4f, 2f);
                            segment.transform.parent = gameplayContainer.transform;
                            segment.GetComponent<MeshRenderer>().material = bridgeMat;
                            segment.AddComponent<TimeRewindableObject>();
                            segment.AddComponent<CollapsingBridge>();
                        }
                    }
                    break;

                case RoomType.PlatformPuzzle:
                    // Spawn a falling platform spawner over the chasm
                    Vector3 spawnerPos = new Vector3(cx, 5f, cz);
                    GameObject dropper;
                    if (fallingPlatformDropperPrefab != null)
                    {
                        dropper = Instantiate(fallingPlatformDropperPrefab, spawnerPos, Quaternion.identity, gameplayContainer.transform);
                    }
                    else
                    {
                        dropper = new GameObject("ProceduralPlatformDropper");
                        dropper.transform.position = spawnerPos;
                        dropper.transform.parent = gameplayContainer.transform;
                        var dropScript = dropper.AddComponent<FallingPlatformDropper>();
                        
                        // Create a primitive to be used as platform prefab
                        GameObject platformPF = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        platformPF.name = "ProceduralPlatform";
                        platformPF.transform.localScale = new Vector3(2.5f, 0.4f, 2.5f);
                        platformPF.GetComponent<MeshRenderer>().material = platformMat;
                        platformPF.AddComponent<TimeFreezableObject>();
                        platformPF.AddComponent<TimeRewindableObject>();
                        platformPF.SetActive(false); // keep inactive so spawner instantiates it active

                        dropScript.platformPrefab = platformPF;
                        dropScript.spawnCooldown = 2.0f;
                        dropScript.destroyY = -12f;
                    }
                    break;

                case RoomType.TurretPuzzle:
                    // Spawn hazard Projectile Spawners on side walls
                    Vector3 leftWallSpawner = new Vector3(room.originX + 0.5f, 1f, cz);
                    Vector3 rightWallSpawner = new Vector3(room.originX + room.width - 1.5f, 1f, cz);
                    
                    SpawnTurret(leftWallSpawner, Quaternion.Euler(0f, 90f, 0f), gameplayContainer.transform);
                    SpawnTurret(rightWallSpawner, Quaternion.Euler(0f, -90f, 0f), gameplayContainer.transform);
                    break;

                case RoomType.TreasureRoom:
                    // Spawn chest at room center
                    GameObject chestInstance;
                    if (chestPrefab != null)
                    {
                        chestInstance = Instantiate(chestPrefab, centerPos + Vector3.up * 0.1f, Quaternion.identity, gameplayContainer.transform);
                    }
                    else
                    {
                        chestInstance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        chestInstance.name = "ProceduralChest";
                        chestInstance.transform.position = centerPos + Vector3.up * 0.3f;
                        chestInstance.transform.localScale = new Vector3(1.2f, 0.8f, 0.8f);
                        chestInstance.transform.parent = gameplayContainer.transform;
                        chestInstance.GetComponent<MeshRenderer>().material = MakeBasicMaterial(new Color(0.5f, 0.35f, 0.05f));
                        
                        // Make a simple child lid to let the open routine rotate it
                        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        lid.name = "ChestLid";
                        lid.transform.parent = chestInstance.transform;
                        lid.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                        lid.transform.localScale = new Vector3(1f, 0.2f, 1f);
                        lid.GetComponent<MeshRenderer>().material = MakeBasicMaterial(new Color(0.6f, 0.4f, 0.1f));
                    }

                    // Assign reward potion prefabs to Chest
                    InteractableChest chestScript = chestInstance.GetComponent<InteractableChest>();
                    if (chestScript == null)
                    {
                        chestScript = chestInstance.AddComponent<InteractableChest>();
                    }
                    chestScript.healthPotionPrefab = healthPotionPrefab;
                    chestScript.manaPotionPrefab = manaPotionPrefab;

                    // Add self-contained ItemGenerator on the chest
                    ItemGenerator ig = chestInstance.GetComponent<ItemGenerator>();
                    if (ig == null)
                    {
                        ig = chestInstance.AddComponent<ItemGenerator>();
                    }

                    // Try using reflection to assign potion/item prefabs on the generator
                    AssignPrivatePrefabs(ig);

                    // Spawn Procedural Plants around the treasure room
                    Vector3[] corners = new Vector3[]
                    {
                        new Vector3(room.originX + 1.5f, 0f, room.originZ + 1.5f),
                        new Vector3(room.originX + room.width - 2.5f, 0f, room.originZ + room.depth - 2.5f)
                    };
                    foreach (var cr in corners)
                    {
                        if (plantGeneratorPrefab != null)
                        {
                            Instantiate(plantGeneratorPrefab, cr, Quaternion.identity, gameplayContainer.transform);
                        }
                        else
                        {
                            GameObject pgObj = new GameObject("ProceduralPlantGenerator");
                            pgObj.transform.position = cr;
                            pgObj.transform.parent = gameplayContainer.transform;
                            var plantGen = pgObj.AddComponent<PlantGenerator>();
                            // Give default values
                            AssignPrivatePlantParams(plantGen);
                        }
                    }
                    break;

                case RoomType.BossArena:
                    // Spawn ML-Agent Boss
                    GameObject bossInstance;
                    if (enemyAgentPrefab != null)
                    {
                        bossInstance = Instantiate(enemyAgentPrefab, centerPos + Vector3.up * 1f, Quaternion.identity, gameplayContainer.transform);
                    }
                    else
                    {
                        bossInstance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        bossInstance.name = "MLAgent_Boss";
                        bossInstance.transform.position = centerPos + Vector3.up * 1f;
                        bossInstance.transform.parent = gameplayContainer.transform;
                        bossInstance.GetComponent<MeshRenderer>().material = MakeBasicMaterial(new Color(0.4f, 0f, 0.5f));
                        bossInstance.AddComponent<Rigidbody>();
                        bossInstance.AddComponent<EnemyAgent>();
                    }

                    // Hook up ML Agent to Player using Reflection
                    if (playerTransform != null)
                    {
                        EnemyAgent agentScript = bossInstance.GetComponent<EnemyAgent>();
                        if (agentScript != null)
                        {
                            var agentType = typeof(EnemyAgent);
                            var targetField = agentType.GetField("targetTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (targetField != null)
                            {
                                targetField.SetValue(agentScript, playerTransform);
                            }
                            var respawnField = agentType.GetField("respawnPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (respawnField != null)
                            {
                                respawnField.SetValue(agentScript, centerPos + Vector3.up * 1f);
                            }
                            var speedField = agentType.GetField("speed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            if (speedField != null)
                            {
                                speedField.SetValue(agentScript, 4f);
                            }
                        }
                    }

                    // Create surrounding animated Lava Flow tiles
                    for (int x = room.originX; x < room.originX + room.width; x++)
                    {
                        for (int z = room.originZ; z < room.originZ + room.depth; z++)
                        {
                            // Border ring inside the room
                            if (x == room.originX || x == room.originX + room.width - 1 || z == room.originZ || z == room.originZ + room.depth - 1)
                            {
                                Vector3 lavaPos = new Vector3(x, -0.4f, z);
                                GameObject lavaBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                lavaBlock.name = $"ArenaLava_{x}_{z}";
                                lavaBlock.transform.position = lavaPos;
                                lavaBlock.transform.parent = gameplayContainer.transform;
                                lavaBlock.GetComponent<MeshRenderer>().material = lavaMat;
                                lavaBlock.AddComponent<LavaFlow>();
                            }
                        }
                    }
                    break;

                case RoomType.ExitRoom:
                    // Spawn exit portal/win platform
                    GameObject winPF;
                    if (winPlatformPrefab != null)
                    {
                        winPF = Instantiate(winPlatformPrefab, centerPos, Quaternion.identity, gameplayContainer.transform);
                    }
                    else
                    {
                        winPF = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        winPF.name = "WinPlatform";
                        winPF.transform.position = centerPos - Vector3.up * 0.4f;
                        winPF.transform.localScale = new Vector3(3f, 0.2f, 3f);
                        winPF.transform.parent = gameplayContainer.transform;
                        winPF.GetComponent<MeshRenderer>().material = winMat;
                    }

                    // Win Trigger Collider
                    GameObject triggerObj = new GameObject("WinTriggerVolume");
                    triggerObj.transform.position = centerPos + Vector3.up * 0.5f;
                    triggerObj.transform.parent = winPF.transform;
                    BoxCollider triggerColl = triggerObj.AddComponent<BoxCollider>();
                    triggerColl.isTrigger = true;
                    triggerColl.size = new Vector3(3f, 2f, 3f);
                    triggerObj.AddComponent<WinTrigger>();

                    // Spawn decorative dome over the win pad
                    if (domePrefab != null)
                    {
                        Instantiate(domePrefab, centerPos, Quaternion.identity, gameplayContainer.transform);
                    }
                    break;
            }
        }
    }

    private void SpawnTurret(Vector3 pos, Quaternion rot, Transform parent)
    {
        GameObject turret;
        if (projectileSpawnerPrefab != null)
        {
            turret = Instantiate(projectileSpawnerPrefab, pos, rot, parent);
        }
        else
        {
            turret = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            turret.name = "ProceduralTurret";
            turret.transform.position = pos;
            turret.transform.rotation = rot;
            turret.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
            turret.transform.parent = parent;
            turret.GetComponent<MeshRenderer>().material = spawnerMat;
            turret.AddComponent<TimeFreezableObject>();
            var spawner = turret.AddComponent<ProjectileSpawner>();
            
            // Create projectile prefab
            GameObject projPF = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projPF.name = "ProceduralProjectile";
            projPF.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            projPF.GetComponent<MeshRenderer>().material = projectileMat;
            projPF.AddComponent<TimeFreezableObject>();
            projPF.AddComponent<Projectile>();
            projPF.SetActive(false); // spawner will clone active instances

            spawner.projectilePrefab = projPF;
            spawner.spawnInterval = 1.6f;
            spawner.projectileSpeed = 10f;
        }
    }

    private void CreateCheckpointTrigger(Vector3 pos, Vector3 scale, Vector3 targetPos, Transform parent)
    {
        GameObject checkpoint = GameObject.CreatePrimitive(PrimitiveType.Cube);
        checkpoint.name = "CheckpointTrigger";
        checkpoint.transform.position = pos;
        checkpoint.transform.localScale = scale;
        checkpoint.transform.parent = parent;
        checkpoint.GetComponent<MeshRenderer>().enabled = false;
        checkpoint.GetComponent<Collider>().isTrigger = true;

        CheckpointTeleporter ct = checkpoint.AddComponent<CheckpointTeleporter>();
        ct.targetPosition = targetPos;
    }

    private void AssignPrivatePrefabs(ItemGenerator ig)
    {
        // Try to assign item prefabs on the generator via reflection to avoid manual setup
        var igType = typeof(ItemGenerator);
        var p1 = igType.GetField("itemPrefab1", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var p2 = igType.GetField("itemPrefab2", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var p3 = igType.GetField("itemPrefab3", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        // Standard cubes as fallback items if not assigned in Inspector
        GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube1.name = "CommonMace_Fallback";
        cube1.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
        cube1.GetComponent<MeshRenderer>().material = MakeBasicMaterial(Color.white);
        cube1.SetActive(false);

        GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cube2.name = "UncommonScroll_Fallback";
        cube2.transform.localScale = new Vector3(0.2f, 0.6f, 0.2f);
        cube2.GetComponent<MeshRenderer>().material = MakeBasicMaterial(Color.green);
        cube2.SetActive(false);

        GameObject cube3 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cube3.name = "LegendaryArtefact_Fallback";
        cube3.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        cube3.GetComponent<MeshRenderer>().material = MakeBasicMaterial(Color.yellow);
        cube3.SetActive(false);

        if (p1 != null) p1.SetValue(ig, cube1);
        if (p2 != null) p2.SetValue(ig, cube2);
        if (p3 != null) p3.SetValue(ig, cube3);
    }

    private void AssignPrivatePlantParams(PlantGenerator pg)
    {
        var pgType = typeof(PlantGenerator);
        var ax = pgType.GetField("axiom", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var it = pgType.GetField("iterations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var matField = pgType.GetField("TreeMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (ax != null) ax.SetValue(pg, "FB");
        if (it != null) it.SetValue(pg, 2);
        if (matField != null && matField.GetValue(pg) == null)
        {
            matField.SetValue(pg, MakeBasicMaterial(new Color(0.1f, 0.7f, 0.15f)));
        }
    }

    // ─────────────────────────────────────────────
    // Materials & Helper Methods
    // ─────────────────────────────────────────────

    private void InitMaterials()
    {
        groundMat = groundMaterial != null ? groundMaterial : MakeEmissionMaterial(new Color(0.08f, 0.08f, 0.10f), false, 0f);
        wallMat = wallMaterial != null ? wallMaterial : MakeEmissionMaterial(new Color(0.15f, 0.15f, 0.18f), false, 0f);
        bridgeMat = bridgeMaterial != null ? bridgeMaterial : MakeEmissionMaterial(new Color(0.85f, 0.55f, 0.1f), true, 0.8f);
        projectileMat = projectileMaterial != null ? projectileMaterial : MakeEmissionMaterial(new Color(1f, 0.2f, 0.02f), true, 1.5f);
        platformMat = platformMaterial != null ? platformMaterial : MakeEmissionMaterial(new Color(0.05f, 0.7f, 1f), true, 1.0f);
        spawnerMat = spawnerMaterial != null ? spawnerMaterial : MakeEmissionMaterial(new Color(0.25f, 0.25f, 0.3f), false, 0f);
        winMat = winMaterial != null ? winMaterial : MakeEmissionMaterial(new Color(0.1f, 0.85f, 0.2f), true, 1.2f);
        lavaMat = lavaMaterial != null ? lavaMaterial : MakeEmissionMaterial(new Color(1.0f, 0.1f, 0f), true, 2.0f);
    }

    private Material MakeEmissionMaterial(Color color, bool emit, float intensity)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Metallic", 0.3f);
        mat.SetFloat("_Glossiness", 0.5f);
        if (emit)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
        }
        return mat;
    }

    private Material MakeBasicMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }
}
