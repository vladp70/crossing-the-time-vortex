using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallingPlatformDropper : MonoBehaviour
{
    public GameObject platformPrefab;
    public float spawnCooldown = 1.5f;
    public float destroyY = -15f;

    private GameObject currentPlatform;
    private float cooldownTimer = 0f;
    private Player player;

    void Start()
    {
        player = FindObjectOfType<Player>();
        SpawnPlatform();
    }

    void Update()
    {
        if (player != null && player.IsReversing) return;

        if (currentPlatform == null)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= spawnCooldown)
            {
                cooldownTimer = 0f;
                SpawnPlatform();
            }
        }
        else
        {
            if (currentPlatform.transform.position.y < destroyY)
            {
                Destroy(currentPlatform);
                currentPlatform = null;
            }
        }
    }

    private void SpawnPlatform()
    {
        if (platformPrefab == null) return;

        currentPlatform = Instantiate(platformPrefab, transform.position + Vector3.down * 1.0f, transform.rotation);
        currentPlatform.SetActive(true);
        
        Rigidbody rb = currentPlatform.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = currentPlatform.AddComponent<Rigidbody>();
            rb.mass = 100f;
        }
        rb.isKinematic = false;
        rb.useGravity = true;

        if (currentPlatform.GetComponent<TimeFreezableObject>() == null)
        {
            currentPlatform.AddComponent<TimeFreezableObject>();
        }

        if (currentPlatform.GetComponent<TimeRewindableObject>() == null)
        {
            currentPlatform.AddComponent<TimeRewindableObject>();
        }
    }
}
