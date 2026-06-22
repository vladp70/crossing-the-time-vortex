using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class ItemGenerator : MonoBehaviour
{

    [SerializeField] private GameObject itemPrefab1;
    [SerializeField] private GameObject itemPrefab2;
    [SerializeField] private GameObject itemPrefab3;
    [SerializeField] private Transform playerTransform;
    private string lastGeneratedItem = "";
    private string stars;

    // Start is called before the first frame update
    void Start()
    {
        if (playerTransform == null)
        {
            playerTransform = GameObject.Find("Player").transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) && (playerTransform.position - transform.position).magnitude < 20f)
        {
            GenerateNewItem();
        }
    }

    public void GenerateNewItem()
    {
        GenerateNewItemAt(transform.position + Vector3.right * 1.5f);
    }

    public string GenerateNewItemAt(Vector3 spawnPosition)
    {
        string[] types = { "Mace", "Scroll", "Artefact" };
        string[] rarities = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };
        string[] feats = { "Flaming", "Frost", "Lightning", "Poisoned", "Cursed" };

        string type = types[Random.Range(0, types.Length)];
        int starRarity = Random.Range(0, rarities.Length);
        string rarity = rarities[starRarity];
        int damage = Random.Range(1, 100);
        int durability = Random.Range(10, 100);
        string feat = feats[Random.Range(0, feats.Length)];
        Vector3 color;
        switch (rarity)
        {
            case "Common":
                color = new Vector3(1, 1, 1); // White
                break;
            case "Uncommon":
                color = new Vector3(0, 1, 0); // Green
                break;
            case "Rare":
                color = new Vector3(0, 0, 1); // Blue
                break;
            case "Epic":
                color = new Vector3(1, 0, 1); // Purple
                break;
            case "Legendary":
                color = new Vector3(1, 0.5f, 0); // Orange
                break;
            default:
                color = new Vector3(1, 1, 1); // Default to white
                break;
        }

        string featureAndName = $"{feat} {type} ({rarity}) - DMG: {damage} DUR: {durability}";
        lastGeneratedItem = $"{featureAndName}\n";
        stars = "";
        for (int i = 0; i < starRarity; i++)
        {
            stars += "★";
        }
        Debug.Log($"{lastGeneratedItem} {stars}");
        
        switch (type)
        {
            case "Mace":
                Instantiate(itemPrefab1, spawnPosition, Quaternion.identity);
                break;
            case "Scroll":
                Instantiate(itemPrefab2, spawnPosition, Quaternion.identity);
                break;
            case "Artefact":
                Instantiate(itemPrefab3, spawnPosition, Quaternion.identity);
                break;
            default:
                Debug.LogWarning("Unknown item type!");
                break;
        }

        return featureAndName;
    }
}
