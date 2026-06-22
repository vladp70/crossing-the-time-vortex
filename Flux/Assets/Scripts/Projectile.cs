using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TimeFreezableObject))]
[RequireComponent(typeof(TimeRewindableObject))]
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float maxLifetime = 8f;

    private Rigidbody rb;
    private Collider col;
    private MeshRenderer meshRenderer;
    private TimeRewindableObject rewindable;
    private TimeFreezableObject freezable;

    private bool isSpent = false;
    private float lifetimeTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        meshRenderer = GetComponentInChildren<MeshRenderer>();
        rewindable = GetComponent<TimeRewindableObject>();
        freezable = GetComponent<TimeFreezableObject>();

        rb.useGravity = false;
        rb.velocity = transform.forward * speed;

        rewindable.OnRewindReachedBeginning += OnRewindedToStart;

        CreateFireTrail();
    }

    void OnDestroy()
    {
        if (rewindable != null)
        {
            rewindable.OnRewindReachedBeginning -= OnRewindedToStart;
        }
    }

    void FixedUpdate()
    {
        Player player = FindObjectOfType<Player>();
        bool playerReversing = player != null && player.IsReversing;

        if (playerReversing)
        {
            if (isSpent)
            {
                Reactivate();
            }
        }
        else if (!freezable.IsFrozen && !isSpent)
        {
            rb.velocity = transform.forward * speed;

            lifetimeTimer += Time.fixedDeltaTime;
            if (lifetimeTimer >= maxLifetime)
            {
                Deactivate();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isSpent || other.isTrigger) return;

        if (other.gameObject.CompareTag("Player") || other.gameObject.name.Contains("Player"))
        {
            Player p = other.gameObject.GetComponent<Player>();
            if (p != null)
            {
                p.TakeDamage(25f, new Vector3(4.5f, 1.2f, 2.5f));
            }
            Deactivate();
        }
        else if (other.gameObject.GetComponent<Projectile>() == null && !other.gameObject.name.Contains("Spawner"))
        {
            Deactivate();
        }
    }

    private void Deactivate()
    {
        isSpent = true;
        col.enabled = false;
        if (meshRenderer != null) meshRenderer.enabled = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        StopFireTrail();
    }

    private void Reactivate()
    {
        isSpent = false;
        col.enabled = true;
        if (meshRenderer != null) meshRenderer.enabled = true;
        rb.isKinematic = false;
        PlayFireTrail();
    }

    private void OnRewindedToStart()
    {
        Destroy(gameObject);
    }

    private void CreateFireTrail()
    {
        GameObject trailObj = new GameObject("FireTrail");
        trailObj.transform.SetParent(transform, false);
        trailObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = trailObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // Makes it trail!
        main.startLifetime = 0.5f;
        main.startSize = 0.035f;
        main.startColor = new Color(1f, 0.65f, 0.15f, 0.8f);
        main.maxParticles = 80;
        main.duration = 1.0f;
        main.loop = true;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 100;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.04f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 1.0f);
        curve.AddKey(1.0f, 0.1f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(new Color(1f, 0.85f, 0.2f), 0.0f), 
                new GradientColorKey(new Color(1f, 0.3f, 0f), 0.6f),
                new GradientColorKey(new Color(0.4f, 0f, 0f), 1.0f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(1.0f, 0.0f), 
                new GradientAlphaKey(0.0f, 1.0f) 
            }
        );
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

        ParticleSystemRenderer psr = trailObj.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.renderMode = ParticleSystemRenderMode.Mesh;
            
            GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tempSphere);
            
            psr.mesh = sphereMesh;

            Material particleMat = new Material(Shader.Find("Standard"));
            particleMat.color = new Color(1f, 0.4f, 0f, 0.7f);
            particleMat.EnableKeyword("_EMISSION");
            particleMat.SetColor("_EmissionColor", new Color(1f, 0.4f, 0f) * 2f);
            psr.material = particleMat;
        }
    }

    private void StopFireTrail()
    {
        ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.Clear();
            ps.Stop();
        }
    }

    private void PlayFireTrail()
    {
        ParticleSystem ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
    }
}
