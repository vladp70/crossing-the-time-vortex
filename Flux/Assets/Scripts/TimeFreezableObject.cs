using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeFreezableObject : MonoBehaviour
{
    private Rigidbody rb;
    private MeshRenderer[] renderers;
    private Dictionary<MeshRenderer, Color[]> originalColors = new Dictionary<MeshRenderer, Color[]>();

    private bool isFrozen = false;
    public bool IsFrozen => isFrozen;

    private Vector3 savedVelocity;
    private Vector3 savedAngularVelocity;
    private bool savedIsKinematic;

    private Coroutine freezeCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<MeshRenderer>();
        
        // Save original material colors
        foreach (var renderer in renderers)
        {
            if (renderer != null && renderer.materials != null)
            {
                Color[] colors = new Color[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (renderer.materials[i].HasProperty("_Color"))
                    {
                        colors[i] = renderer.materials[i].color;
                    }
                    else
                    {
                        colors[i] = Color.white;
                    }
                }
                originalColors[renderer] = colors;
            }
        }
    }

    public void Freeze(float duration)
    {
        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
        }
        freezeCoroutine = StartCoroutine(DoFreeze(duration));
    }

    private IEnumerator DoFreeze(float duration)
    {
        if (!isFrozen)
        {
            isFrozen = true;

            // Save physics state
            if (rb != null)
            {
                savedVelocity = rb.velocity;
                savedAngularVelocity = rb.angularVelocity;
                savedIsKinematic = rb.isKinematic;

                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Apply blue freeze tint
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.materials != null)
                {
                    for (int i = 0; i < renderer.materials.Length; i++)
                    {
                        if (renderer.materials[i].HasProperty("_Color"))
                        {
                            Color orig = originalColors.ContainsKey(renderer) ? originalColors[renderer][i] : Color.white;
                            renderer.materials[i].color = Color.Lerp(orig, new Color(0.2f, 0.6f, 1f, 1f), 0.7f);
                        }
                    }
                }
            }

            // Pause any particle systems on freeze
            ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particleSystems)
            {
                ps.Pause();
            }
        }

        yield return new WaitForSeconds(duration);

        Unfreeze();
    }

    public void Unfreeze()
    {
        if (!isFrozen) return;

        // Restore physics state
        if (rb != null)
        {
            rb.isKinematic = savedIsKinematic;
            if (!savedIsKinematic)
            {
                rb.velocity = savedVelocity;
                rb.angularVelocity = savedAngularVelocity;
            }
        }

        // Restore original colors
        foreach (var renderer in renderers)
        {
            if (renderer != null && renderer.materials != null && originalColors.ContainsKey(renderer))
            {
                Color[] colors = originalColors[renderer];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    if (i < colors.Length && renderer.materials[i].HasProperty("_Color"))
                    {
                        renderer.materials[i].color = colors[i];
                    }
                }
            }
        }

        // Resume any particle systems on unfreeze
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particleSystems)
        {
            ps.Play();
        }

        isFrozen = false;
        freezeCoroutine = null;
    }
}
