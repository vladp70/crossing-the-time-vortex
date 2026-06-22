using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimePuzzleDemoCreator : MonoBehaviour
{
    private Player player;

    [Header("Custom Materials (Optional - Drag & Drop)")]
    public Material groundMaterial;
    public Material wallMaterial;
    public Material bridgeMaterial;
    public Material projectileMaterial;
    public Material platformMaterial;
    public Material spawnerMaterial;
    public Material winMaterial;

    [Header("Custom Lava Asset (Drag & Drop Texture or Material)")]
    public Material customLavaMaterial;
    public Texture2D customLavaTexture;

    [Header("Custom Prefabs (Optional - Drag & Drop)")]
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

    [Header("Custom Lighting")]
    public Color cameraBackgroundColor = new Color(0.4f, 0.2f, 0.2f, 1f);

    private Material groundMat;
    private Material wallMat;
    private Material bridgeMat;
    private Material projectileMat;
    private Material platformMat;
    private Material spawnerMat;
    private Material winMat;

    void Start()
    {
        player = FindObjectOfType<Player>();
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<Player>();
            }
        }

        Camera[] cameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in cameras)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = cameraBackgroundColor; 
        }

        DungeonGenerator dg = FindObjectOfType<DungeonGenerator>();
        if (dg != null)
        {
            dg.gameObject.SetActive(false);
            Debug.Log("Disabled DungeonGenerator to build Time Puzzle Dungeon.");
        }

        InitMaterials();
        BuildLevel();

        if (player != null)
        {
            Rigidbody prb = player.GetComponent<Rigidbody>();
            if (prb != null)
            {
                prb.velocity = Vector3.zero;
                prb.angularVelocity = Vector3.zero;
            }
            // Spawn player at center of Start Room
            player.transform.position = new Vector3(-14f, 1.2f, 3.5f);

            if (player.gameObject.GetComponent<TimeScreenEffectController>() == null)
            {
                player.gameObject.AddComponent<TimeScreenEffectController>();
            }

            Debug.Log("Teleported player to Start Room and attached TimeScreenEffectController.");
        }
        else
        {
            Debug.LogWarning("Player not found in scene! Please make sure a GameObject named 'Player' exists.");
        }
    }

    private void InitMaterials()
    {
        groundMat = groundMaterial != null ? groundMaterial : MakePremiumMaterial(new Color(0.12f, 0.12f, 0.15f), false, 0f);
        wallMat = wallMaterial != null ? wallMaterial : MakePremiumMaterial(new Color(0.2f, 0.2f, 0.23f), false, 0f);
        bridgeMat = bridgeMaterial != null ? bridgeMaterial : MakePremiumMaterial(new Color(0.9f, 0.65f, 0.15f), true, 0.6f);
        projectileMat = projectileMaterial != null ? projectileMaterial : MakePremiumMaterial(new Color(1f, 0.25f, 0.05f), true, 1.2f);
        platformMat = platformMaterial != null ? platformMaterial : MakePremiumMaterial(new Color(0f, 0.75f, 1f), true, 0.9f);
        spawnerMat = spawnerMaterial != null ? spawnerMaterial : MakePremiumMaterial(new Color(0.3f, 0.3f, 0.35f), false, 0f);
        winMat = winMaterial != null ? winMaterial : MakePremiumMaterial(new Color(0.15f, 0.9f, 0.25f), true, 1.0f);
    }

    private Material MakePremiumMaterial(Color color, bool emit, float glowPower)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Metallic", 0.4f);
        mat.SetFloat("_Glossiness", 0.6f);
        if (emit)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * glowPower);
        }
        return mat;
    }

    private void BuildLevel()
    {
        HashSet<Vector2Int> floorTiles = new HashSet<Vector2Int>();
        HashSet<Vector2Int> pitTiles = new HashSet<Vector2Int>();

        // 1. Define Sliced Dungeon Coordinates
        // Start Room (Room 0) - Wide
        for (int x = -17; x <= -11; x++)
            for (int z = 0; z <= 7; z++)
                floorTiles.Add(new Vector2Int(x, z));

        // Corridor 1 (Center Z is 3 and 4)
        for (int x = -10; x <= -8; x++)
            for (int z = 3; z <= 4; z++)
                floorTiles.Add(new Vector2Int(x, z));

        // Bridge Room (Room 1) - Wide
        for (int x = -7; x <= 3; x++)
        {
            for (int z = 0; z <= 7; z++)
            {
                if (x == -7 || x == 3)
                    floorTiles.Add(new Vector2Int(x, z));
                else
                    pitTiles.Add(new Vector2Int(x, z));
            }
        }

        // Corridor 2
        for (int x = 4; x <= 6; x++)
            for (int z = 3; z <= 4; z++)
                floorTiles.Add(new Vector2Int(x, z));

        // Bullet Hell Room (Room 2) - Restored to original wider width (Z spans 0 to 5)
        for (int x = 7; x <= 18; x++)
            for (int z = 0; z <= 5; z++)
                floorTiles.Add(new Vector2Int(x, z));

        // Corridor 3
        for (int x = 19; x <= 21; x++)
            for (int z = 3; z <= 4; z++)
                floorTiles.Add(new Vector2Int(x, z));

        // Platform Room (Room 3) - Wide
        for (int x = 22; x <= 32; x++)
        {
            for (int z = 0; z <= 7; z++)
            {
                if (x == 22 || x == 32)
                    floorTiles.Add(new Vector2Int(x, z));
                else
                    pitTiles.Add(new Vector2Int(x, z));
            }
        }

        // Corridor 4
        for (int x = 33; x <= 35; x++)
            for (int z = 3; z <= 4; z++)
                floorTiles.Add(new Vector2Int(x, z));

        // Win / Treasure Room (Room 4) - Wide
        for (int x = 36; x <= 41; x++)
            for (int z = 0; z <= 7; z++)
                floorTiles.Add(new Vector2Int(x, z));

        // 2. Instantiate Floor Tiles
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
                floorObj.transform.localScale = new Vector3(1f, 1f, 1f);
                floorObj.GetComponent<MeshRenderer>().material = groundMat;
            }
        }

        // 3. Cellular Wall Builder (Z ranges from -1 to 8 now)
        Vector2Int[] directions = new Vector2Int[] {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        for (int x = -18; x <= 42; x++)
        {
            for (int z = -1; z <= 8; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                if (floorTiles.Contains(pos) || pitTiles.Contains(pos))
                    continue;

                // Check if adjacent to floor or pit
                bool isWall = false;
                foreach (var dir in directions)
                {
                    Vector2Int neighbor = pos + dir;
                    if (floorTiles.Contains(neighbor) || pitTiles.Contains(neighbor))
                    {
                        isWall = true;
                        break;
                    }
                }

                if (isWall)
                {
                    Vector3 wallPos = new Vector3(x, 0f, z);
                    if (wallPrefab != null)
                    {
                        Instantiate(wallPrefab, new Vector3(x, 1.0f, z), Quaternion.identity);
                        Instantiate(wallPrefab, new Vector3(x, 3.0f, z), Quaternion.identity);
                    }
                    else
                    {
                        GameObject w1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        w1.name = "Wall1_" + x + "_" + z;
                        w1.transform.position = wallPos + Vector3.up * 0.5f;
                        w1.transform.localScale = new Vector3(1f, 1f, 1f);
                        w1.GetComponent<MeshRenderer>().material = wallMat;

                        GameObject w2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        w2.name = "Wall2_" + x + "_" + z;
                        w2.transform.position = wallPos + Vector3.up * 1.5f;
                        w2.transform.localScale = new Vector3(1f, 1f, 1f);
                        w2.GetComponent<MeshRenderer>().material = wallMat;
                    }
                }
            }
        }

        // 4. Create Lava Ocean below the level (Y = -5.0f)
        GameObject lava = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lava.name = "LavaOcean";
        lava.transform.position = new Vector3(12f, -5.0f, 3.5f);
        lava.transform.localScale = new Vector3(90f, 1f, 40f);
        Destroy(lava.GetComponent<Collider>()); // Player falls through, hitting trigger checkpoints

        Material lavaMat;
        if (customLavaMaterial != null)
        {
            lavaMat = customLavaMaterial;
        }
        else
        {
            lavaMat = new Material(Shader.Find("Standard"));
            lavaMat.color = new Color(1.0f, 0.20f, 0.0f, 1f);
            if (customLavaTexture != null)
            {
                lavaMat.mainTexture = customLavaTexture;
                lavaMat.EnableKeyword("_EMISSION");
                lavaMat.SetColor("_EmissionColor", new Color(1.0f, 0.15f, 0.0f) * 1.5f);
                lavaMat.SetTexture("_EmissionMap", customLavaTexture);
                lavaMat.SetTextureScale("_MainTex", new Vector2(250f, 120f));
                lavaMat.SetTextureScale("_EmissionMap", new Vector2(250f, 120f));
            }
            else
            {
                lavaMat.EnableKeyword("_EMISSION");
                lavaMat.SetColor("_EmissionColor", new Color(1.0f, 0.15f, 0.0f) * 1.8f);
            }
        }
        lava.GetComponent<MeshRenderer>().material = lavaMat;

        lava.AddComponent<LavaFlow>();

        // 5. Ambient Lighting & Volcanic Underlighting (Z aligned to 3.5f)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.05f, 0.08f);

        CreateVolcanicLight(new Vector3(-2f, -4f, 3.5f), new Color(1f, 0.3f, 0f), 2.5f, 20f);
        CreateVolcanicLight(new Vector3(27f, -4f, 3.5f), new Color(1f, 0.3f, 0f), 2.5f, 20f);

        // 6. Zone 1: Collapsing Bridge (Z aligned to 3.5f, wider bridge scale 4.5f)
        float[] bridgeX = new float[] { -5f, -3f, -1f, 1f };
        for (int i = 0; i < bridgeX.Length; i++)
        {
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = "BridgeSegment_" + i;
            segment.transform.position = new Vector3(bridgeX[i], 0f, 3.5f);
            segment.transform.localScale = new Vector3(1.8f, 0.4f, 4.5f);
            segment.GetComponent<MeshRenderer>().material = bridgeMat;

            segment.AddComponent<TimeRewindableObject>();
            segment.AddComponent<CollapsingBridge>();
        }

        // 7. Zone 2: Bullet Hell Spawners (Z aligned to narrow room walls at Z=1 and Z=6)
        // Projectile Template
        GameObject projTemplate = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projTemplate.name = "ProjectileTemplate";
        projTemplate.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
        projTemplate.GetComponent<MeshRenderer>().material = projectileMat;
        projTemplate.GetComponent<Collider>().isTrigger = true;
        projTemplate.AddComponent<TimeFreezableObject>();
        projTemplate.AddComponent<TimeRewindableObject>();
        projTemplate.AddComponent<Projectile>();
        projTemplate.SetActive(false);

        // Spawners attached to North/South walls of the restored corridor
        CreateSpawner(new Vector3(10f, 1f, 5.2f), Quaternion.Euler(0f, 180f, 0f), projTemplate);
        CreateSpawner(new Vector3(13f, 1f, -0.2f), Quaternion.Euler(0f, 0f, 0f), projTemplate);
        CreateSpawner(new Vector3(16f, 1f, 5.2f), Quaternion.Euler(0f, 180f, 0f), projTemplate);

        // 8. Zone 3: Platform Dropper (Z aligned to 3.5f, wider platforms)
        GameObject platTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        platTemplate.name = "FallingPlatformTemplate";
        platTemplate.transform.localScale = new Vector3(3f, 0.5f, 4.0f);
        platTemplate.GetComponent<MeshRenderer>().material = platformMat;
        platTemplate.AddComponent<Rigidbody>();
        platTemplate.AddComponent<TimeFreezableObject>();
        platTemplate.AddComponent<TimeRewindableObject>();
        platTemplate.SetActive(false);

        // Platform Dropper overhead
        GameObject dropper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dropper.name = "PlatformDropper";
        dropper.transform.position = new Vector3(27f, 7f, 3.5f);
        dropper.transform.localScale = new Vector3(1f, 1f, 1f);
        dropper.GetComponent<MeshRenderer>().material = spawnerMat;
        Destroy(dropper.GetComponent<Collider>());
        
        FallingPlatformDropper fpd = dropper.AddComponent<FallingPlatformDropper>();
        fpd.platformPrefab = platTemplate;
        fpd.spawnCooldown = 2.0f;
        fpd.destroyY = -12f;

        // 9. Win Zone Cylinder (Room 4 Win Platform, Z aligned to 3.5f)
        GameObject winVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        winVisual.name = "WinVisual";
        winVisual.transform.position = new Vector3(39f, 0.15f, 3.5f);
        winVisual.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
        winVisual.GetComponent<MeshRenderer>().material = winMat;
        winVisual.GetComponent<Collider>().isTrigger = true;
        winVisual.AddComponent<WinTrigger>();

        // 10. Reward Treasure Chests Spawning (Low probability, offset to corners, oriented facing player: -90 degrees around Y)
        float spawnChance = 0.30f; 

        if (Random.value < spawnChance)
            CreateRewardChest(new Vector3(3f, 0f, 1.2f));  // exit corner of Zone 1

        if (Random.value < spawnChance)
            CreateRewardChest(new Vector3(22f, 0f, 1.2f)); // entry corner of Zone 3 (rewards Bullet Hell)

        if (Random.value < spawnChance)
            CreateRewardChest(new Vector3(32f, 0f, 1.2f)); // exit corner of Zone 3
        
        // Final treasure room chests surrounding win pad (Exactly one guaranteed chest)
        CreateRewardChest(new Vector3(37.5f, 0f, 1.5f)); // 1 guaranteed chest positioned on the side, before the win pad, not blocking the entry path!

        // 11. Checkpoint teleporters (Z aligned, wider scale Z=15f)
        CreateCheckpoint(new Vector3(-2f, -8f, 3.5f), new Vector3(14f, 2f, 15f), new Vector3(-14f, 1.2f, 3.5f)); // Zone 1 fall
        CreateCheckpoint(new Vector3(27f, -8f, 3.5f), new Vector3(14f, 2f, 15f), new Vector3(20.5f, 1.2f, 3.5f)); // Zone 3 fall

        // 12. Floating 3D Text Instructions - Removed from sky as overlay texts are enough

        // 13. Spawn Bld_Dome_A around the victory pad (winVisual) - scaled down to look smaller
        if (domePrefab != null)
        {
            GameObject dome = Instantiate(domePrefab, new Vector3(39f, 0f, 3.5f), Quaternion.identity);
            dome.name = "VictoryDome";
            dome.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f); // Smaller, cozy temple dome
        }

        // 14. Spawn decorative room props randomly and sparsely in starting and finish rooms
        SpawnPropAtCorner(new Vector3(-16.2f, 0f, 1.2f));
        SpawnPropAtCorner(new Vector3(-16.2f, 0f, 5.8f));
        SpawnPropAtCorner(new Vector3(-11.8f, 0f, 1.2f));
        SpawnPropAtCorner(new Vector3(-11.8f, 0f, 5.8f));

        SpawnPropAtCorner(new Vector3(37.0f, 0f, 5.8f));
        SpawnPropAtCorner(new Vector3(40.5f, 0f, 1.2f));
        SpawnPropAtCorner(new Vector3(40.5f, 0f, 5.8f));

        // 15. Spawn stone stashes at solid, decorative points along path (avoiding chasms & air in Zone 3/last portion)
        SpawnStoneAt(new Vector3(-7f, 0f, 0.8f)); // Solid starting landing edge
        SpawnStoneAt(new Vector3(3f, 0f, 6.2f));  // Solid bridge exit landing edge
        SpawnStoneAt(new Vector3(8f, 0f, 0.5f));  // Solid bullet hell entrance
        SpawnStoneAt(new Vector3(17f, 0f, 5.0f)); // Solid bullet hell exit
    }

    private void SpawnPropAtCorner(Vector3 pos)
    {
        List<GameObject> availableProps = new List<GameObject>();
        if (stonePrefabA != null) availableProps.Add(stonePrefabA);
        if (stonePrefabB != null) availableProps.Add(stonePrefabB);
        if (tablePrefabA != null) availableProps.Add(tablePrefabA);
        if (tablePrefabB != null) availableProps.Add(tablePrefabB);

        if (availableProps.Count == 0) return;

        // 70% chance to spawn a prop in this corner
        if (Random.value < 0.7f)
        {
            GameObject selectedPrefab = availableProps[Random.Range(0, availableProps.Count)];
            GameObject prop = Instantiate(selectedPrefab, pos, Quaternion.identity);
            
            // Random rotation for natural feel
            prop.transform.Rotate(0f, Random.Range(0f, 360f), 0f);
            
            // Scale stone props to max 10%
            if (selectedPrefab == stonePrefabA || selectedPrefab == stonePrefabB)
            {
                float scaleVal = Random.Range(0.07f, 0.15f);
                prop.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
            }
            
            prop.name = "RoomProp_" + selectedPrefab.name + "_" + Mathf.RoundToInt(pos.x) + "_" + Mathf.RoundToInt(pos.z);
        }
    }

    private void SpawnStoneAt(Vector3 pos)
    {
        List<GameObject> stones = new List<GameObject>();
        if (stonePrefabA != null) stones.Add(stonePrefabA);
        if (stonePrefabB != null) stones.Add(stonePrefabB);

        if (stones.Count == 0) return;

        // 60% chance to spawn a stone at this location
        if (Random.value < 0.6f)
        {
            GameObject selected = stones[Random.Range(0, stones.Count)];
            GameObject stone = Instantiate(selected, pos, Quaternion.identity);
            stone.transform.Rotate(0f, Random.Range(0f, 360f), 0f);
            
            // Scale stone stashes to max 10%
            float scaleVal = Random.Range(0.25f, 0.55f);
            stone.transform.localScale = new Vector3(scaleVal, scaleVal, scaleVal);
            
            stone.name = "DungeonStone_" + selected.name + "_" + Mathf.RoundToInt(pos.x);
        }
    }

    private void CreateVolcanicLight(Vector3 pos, Color color, float intensity, float range)
    {
        GameObject lightObj = new GameObject("VolcanicLight");
        lightObj.transform.position = pos;
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
    }

    private void CreateRewardChest(Vector3 pos)
    {
        GameObject chestObj;
        if (chestPrefab != null)
        {
            // Instantiated rotated by 180 degrees around Y so the front of the model is facing the player (approaching from -X)
            chestObj = Instantiate(chestPrefab, pos, Quaternion.Euler(0f, 90f, 0f));
        }
        else
        {
            // Create a simple procedural treasure chest look, oriented facing -X (front faces path)
            chestObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chestObj.name = "TreasureChest_Procedural";
            chestObj.transform.position = pos + Vector3.up * 0.25f;
            chestObj.transform.localScale = new Vector3(0.8f, 0.5f, 0.6f);
            chestObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            
            Material chestMat = new Material(Shader.Find("Standard"));
            chestMat.color = new Color(0.5f, 0.25f, 0.05f); // Brown wood body
            chestObj.GetComponent<MeshRenderer>().material = chestMat;

            // Create a hinge gameobject on the local back edge of the chest (Z is forward/back)
            GameObject hinge = new GameObject("Lid");
            hinge.transform.SetParent(chestObj.transform, false);
            hinge.transform.localPosition = new Vector3(0f, 0.25f, 0.3f); // Back edge

            GameObject lidVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lidVisual.name = "LidVisual";
            lidVisual.transform.SetParent(hinge.transform, false);
            lidVisual.transform.localPosition = new Vector3(0f, 0.1f, -0.3f); // Center offset
            lidVisual.transform.localScale = new Vector3(0.8f, 0.2f, 0.6f);
            Destroy(lidVisual.GetComponent<Collider>());
            
            Material lidMat = new Material(Shader.Find("Standard"));
            lidMat.color = new Color(0.9f, 0.7f, 0.1f); // Golden lid trim
            lidVisual.GetComponent<MeshRenderer>().material = lidMat;
        }

        // Attach chest interaction logic
        InteractableChest ic = chestObj.AddComponent<InteractableChest>();
        ic.healthPotionPrefab = healthPotionPrefab;
        ic.manaPotionPrefab = manaPotionPrefab;
    }

    private GameObject CreateFloor(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = name;
        floor.transform.position = pos;
        floor.transform.localScale = scale;
        floor.GetComponent<MeshRenderer>().material = mat;
        return floor;
    }

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
        spawnerVisual.GetComponent<MeshRenderer>().material = spawnerMat;

        spawnerParent.AddComponent<TimeFreezableObject>(); // Allow the spawner to be frozen!

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

    private void Create3DText(string name, string text, Vector3 pos)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.position = pos;
        textObj.transform.rotation = Quaternion.Euler(15f, 90f, 0f);

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 120;
        tm.characterSize = 0.04f;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
    }
}

public class CheckpointTeleporter : MonoBehaviour
{
    public Vector3 targetPosition;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") || other.gameObject.name.Contains("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            other.transform.position = targetPosition;
            Debug.Log("Player fell! Teleporting to checkpoint: " + targetPosition);
        }
    }
}

public class WinTrigger : MonoBehaviour
{
    private bool playerWon = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player") || other.gameObject.name.Contains("Player"))
        {
            if (playerWon) return; // Only trigger once

            playerWon = true;
            Debug.Log("VICTORY! Player completed the level.");

            // Spawn victory green particles
            GameObject victoryBurst = new GameObject("VictoryBurst");
            victoryBurst.transform.position = transform.position + Vector3.up * 0.2f;
            ParticleSystem ps = victoryBurst.AddComponent<ParticleSystem>();
            
            var main = ps.main;
            main.startColor = new Color(0.1f, 1.0f, 0.2f, 0.8f);
            main.startSize = 0.08f;
            main.startLifetime = 1.2f;
            main.maxParticles = 100;
            main.duration = 1.0f;
            main.loop = false;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.rateOverTime = 120;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.5f;

            ParticleSystemRenderer psr = victoryBurst.GetComponent<ParticleSystemRenderer>();
            if (psr != null)
            {
                psr.renderMode = ParticleSystemRenderMode.Mesh;
                GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Mesh sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
                Destroy(tempSphere);
                psr.mesh = sphereMesh;

                Material pMat = new Material(Shader.Find("Standard"));
                pMat.color = new Color(0.1f, 1.0f, 0.2f, 0.8f);
                pMat.EnableKeyword("_EMISSION");
                pMat.SetColor("_EmissionColor", new Color(0.1f, 1.0f, 0.2f) * 3f);
                psr.material = pMat;
            }
            Destroy(victoryBurst, 2.5f);
        }
    }

    private void OnGUI()
    {
        if (playerWon)
        {
            // Draw a screen-space overlay
            GUIStyle winTitleStyle = new GUIStyle();
            winTitleStyle.alignment = TextAnchor.MiddleCenter;
            winTitleStyle.fontSize = 42;
            winTitleStyle.fontStyle = FontStyle.Bold;
            winTitleStyle.normal.textColor = new Color(0.95f, 0.75f, 0.15f); // Gold

            GUIStyle winSubStyle = new GUIStyle();
            winSubStyle.alignment = TextAnchor.MiddleCenter;
            winSubStyle.fontSize = 22;
            winSubStyle.fontStyle = FontStyle.Bold;
            winSubStyle.normal.textColor = Color.white;

            // Background dark tint overlay (65% opacity black)
            Texture2D overlayTex = new Texture2D(1, 1);
            overlayTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.65f));
            overlayTex.Apply();
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTex);

            Rect titleRect = new Rect(Screen.width / 2 - 300, Screen.height / 2 - 80, 600, 60);
            Rect subRect = new Rect(Screen.width / 2 - 300, Screen.height / 2 + 10, 600, 100);

            // Draw title text drop-shadow
            GUI.color = Color.black;
            GUI.Label(new Rect(titleRect.x - 2, titleRect.y - 2, titleRect.width, titleRect.height), "LEVEL COMPLETE", winTitleStyle);
            GUI.Label(new Rect(titleRect.x + 2, titleRect.y - 2, titleRect.width, titleRect.height), "LEVEL COMPLETE", winTitleStyle);
            GUI.Label(new Rect(titleRect.x - 2, titleRect.y + 2, titleRect.width, titleRect.height), "LEVEL COMPLETE", winTitleStyle);
            GUI.Label(new Rect(titleRect.x + 2, titleRect.y + 2, titleRect.width, titleRect.height), "LEVEL COMPLETE", winTitleStyle);

            GUI.color = new Color(0.95f, 0.75f, 0.15f); // Gold
            GUI.Label(titleRect, "LEVEL COMPLETE", winTitleStyle);

            // Draw sub-text drop-shadow
            GUI.color = Color.black;
            string subText = "Congratulations!\nYou solved all time vortex puzzles.";
            GUI.Label(new Rect(subRect.x - 1, subRect.y - 1, subRect.width, subRect.height), subText, winSubStyle);
            GUI.Label(new Rect(subRect.x + 1, subRect.y - 1, subRect.width, subRect.height), subText, winSubStyle);
            GUI.Label(new Rect(subRect.x - 1, subRect.y + 1, subRect.width, subRect.height), subText, winSubStyle);
            GUI.Label(new Rect(subRect.x + 1, subRect.y + 1, subRect.width, subRect.height), subText, winSubStyle);

            GUI.color = Color.white;
            GUI.Label(subRect, subText, winSubStyle);
        }
    }
}

public class LavaFlow : MonoBehaviour
{
    private Material mat;

    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;
    }

    void Update()
    {
        float offset = Time.time * 0.012f; // Slower scrolling
        Vector2 texOffset = new Vector2(offset, offset * 0.5f);
        mat.SetTextureOffset("_MainTex", texOffset);
        if (mat.HasProperty("_EmissionMap"))
        {
            mat.SetTextureOffset("_EmissionMap", texOffset);
        }

        // Gently pulse the emission intensity to simulate bubbling lava heat waves (slower sin wave)
        float pulse = 1.4f + Mathf.Sin(Time.time * 0.4f) * 0.3f;
        mat.SetColor("_EmissionColor", new Color(1.0f, 0.15f, 0.0f) * pulse);
    }
}
