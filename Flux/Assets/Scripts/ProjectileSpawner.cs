using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileSpawner : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float spawnInterval = 1.5f;
    public float projectileSpeed = 12f;

    private float timer = 0f;
    private Player player;
    private TimeFreezableObject freezable;

    void Start()
    {
        player = FindObjectOfType<Player>();
        freezable = GetComponent<TimeFreezableObject>();
    }

    void Update()
    {
        if (player != null && player.IsReversing) return;
        if (freezable != null && freezable.IsFrozen) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnProjectile();
        }
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null) return;

        GameObject proj = Instantiate(projectilePrefab, transform.position, transform.rotation);
        proj.SetActive(true);
        
        Projectile pScript = proj.GetComponent<Projectile>();
        if (pScript == null)
        {
            pScript = proj.AddComponent<Projectile>();
        }
        pScript.speed = projectileSpeed;
    }
}
