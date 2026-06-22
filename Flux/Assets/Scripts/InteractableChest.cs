using System.Collections;
using UnityEngine;

public class InteractableChest : MonoBehaviour
{
    [Header("Potion Prefabs (Passed from Level Creator)")]
    public GameObject healthPotionPrefab;
    public GameObject manaPotionPrefab;

    private Transform player;
    private bool playerNearby = false;
    private bool isOpened = false;

    private string discoveredItemName = "";

    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        playerNearby = (dist < 2.5f);

        if (isOpened) return;

        if (playerNearby && Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(OpenRoutine());
        }
    }

    IEnumerator OpenRoutine()
    {
        isOpened = true;

        // Find the lid child object to rotate
        Transform lid = null;
        foreach (Transform child in GetComponentsInChildren<Transform>())
        {
            string nameLower = child.name.ToLower();
            if (nameLower.Contains("lid") || nameLower.Contains("cover"))
            {
                lid = child;
                break;
            }
        }

        if (lid != null)
        {
            Quaternion startRot = lid.localRotation;
            // Changed from -75f to 75f so that the lid swings upwards/backwards instead of downwards/inside itself
            Quaternion endRot = startRot * Quaternion.Euler(75f, 0f, 0f);
            
            float elapsed = 0f;
            float duration = 0.6f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                lid.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
                yield return null;
            }
            lid.localRotation = endRot;
        }

        // Spawn rewards
        SpawnRewards();
    }

    private void SpawnRewards()
    {
        // 1. Trigger the ItemGenerator on this chest (or anywhere in the scene) to spawn a mace, scroll, or artefact
        ItemGenerator ig = GetComponent<ItemGenerator>();
        if (ig == null)
        {
            ig = FindObjectOfType<ItemGenerator>();
        }
        if (ig != null)
        {
            discoveredItemName = ig.GenerateNewItemAt(transform.position + Vector3.up * 0.15f); // Spawns directly inside the chest base
        }

        // 2. Also spawn 1 health or mana potion/orb floating a bit in front of the chest
        Vector3 frontPos = transform.position - Vector3.right * 1.2f + Vector3.up * 0.3f; // Spawn in front (facing the player)
        bool isManaPickup = Random.value > 0.5f;
        CreatePickup(frontPos, isManaPickup);

        // Spawn some feedback particles
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.transform.position = transform.position + Vector3.up * 0.5f;
        flash.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Destroy(flash.GetComponent<Collider>());
        
        Material flashMat = new Material(Shader.Find("Standard"));
        flashMat.color = new Color(1f, 0.8f, 0.3f, 0.7f);
        flashMat.EnableKeyword("_EMISSION");
        flashMat.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.3f) * 2f);
        flash.GetComponent<MeshRenderer>().material = flashMat;

        Destroy(flash, 0.4f);
    }

    private void CreatePickup(Vector3 pos, bool isMana)
    {
        GameObject pickup;
        if (isMana && manaPotionPrefab != null)
        {
            pickup = Instantiate(manaPotionPrefab, pos, Quaternion.identity);
            pickup.name = "ManaPotionPickup";
        }
        else if (!isMana && healthPotionPrefab != null)
        {
            pickup = Instantiate(healthPotionPrefab, pos, Quaternion.identity);
            pickup.name = "HealthPotionPickup";
        }
        else
        {
            pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = isMana ? "ManaPickup" : "HealthPickup";
            pickup.transform.position = pos;
            pickup.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);

            // Apply a glowing visual material
            Material mat = new Material(Shader.Find("Standard"));
            Color col = isMana ? new Color(0.1f, 0.75f, 1f, 1f) : new Color(1f, 0.15f, 0.25f, 1f);
            mat.color = col;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", col * 2f);
            pickup.GetComponent<MeshRenderer>().material = mat;
        }

        pickup.tag = isMana ? "Mana" : "Health";

        // Replace standard collider with sphere/mesh trigger collider
        Collider colComp = pickup.GetComponent<Collider>();
        if (colComp != null)
        {
            colComp.isTrigger = true;
        }
        else
        {
            SphereCollider sc = pickup.AddComponent<SphereCollider>();
            sc.isTrigger = true;
        }

        // Add hovering behavior script
        pickup.AddComponent<FloatingPickup>();

        // Add light component to cast a glow on the ground
        Light l = pickup.AddComponent<Light>();
        if (l == null) l = pickup.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = isMana ? new Color(0.1f, 0.75f, 1f) : new Color(1f, 0.15f, 0.25f);
        l.intensity = 1.5f;
        l.range = 3f;
    }

    void OnGUI()
    {
        if (playerNearby && !isOpened)
        {
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;

            Rect rect = new Rect(Screen.width / 2 - 150, Screen.height * 0.75f, 300, 40);

            // Draw a black drop shadow outline
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x - 1, rect.y - 1, rect.width, rect.height), "[E] Open Chest", style);
            GUI.Label(new Rect(rect.x + 1, rect.y - 1, rect.width, rect.height), "[E] Open Chest", style);
            GUI.Label(new Rect(rect.x - 1, rect.y + 1, rect.width, rect.height), "[E] Open Chest", style);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), "[E] Open Chest", style);

            // Draw primary gold-yellow text
            GUI.color = new Color(0.95f, 0.75f, 0.15f);
            GUI.Label(rect, "[E] Open Chest", style);
            GUI.color = Color.white; // restore color
        }
        else if (isOpened && playerNearby && !string.IsNullOrEmpty(discoveredItemName))
        {
            GUIStyle style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 22;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = Color.white;

            Rect rect = new Rect(Screen.width / 2 - 300, Screen.height * 0.25f, 600, 50);

            // Draw a black drop shadow outline
            GUI.color = Color.black;
            GUI.Label(new Rect(rect.x - 1, rect.y - 1, rect.width, rect.height), $"Discovered {discoveredItemName}!", style);
            GUI.Label(new Rect(rect.x + 1, rect.y - 1, rect.width, rect.height), $"Discovered {discoveredItemName}!", style);
            GUI.Label(new Rect(rect.x - 1, rect.y + 1, rect.width, rect.height), $"Discovered {discoveredItemName}!", style);
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), $"Discovered {discoveredItemName}!", style);

            // Draw primary gold-yellow text
            GUI.color = new Color(0.95f, 0.75f, 0.15f);
            GUI.Label(rect, $"Discovered {discoveredItemName}!", style);
            GUI.color = Color.white; // restore color
        }
    }
}

public class FloatingPickup : MonoBehaviour
{
    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(0f, 120f * Time.deltaTime, 0f);
        transform.position = startPos + Vector3.up * Mathf.Sin(Time.time * 2.5f) * 0.12f;
    }
}
