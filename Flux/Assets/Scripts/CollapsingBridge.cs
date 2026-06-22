using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TimeRewindableObject))]
public class CollapsingBridge : MonoBehaviour
{
    [Tooltip("Delay in seconds before the segment falls after being stepped on")]
    public float collapseDelay = 0.25f;

    private Rigidbody rb;
    private TimeRewindableObject rewindable;
    private bool isCollapsed = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rewindable = GetComponent<TimeRewindableObject>();

        // Ensure it starts as kinematic (anchored in place)
        rb.isKinematic = true;

        // Subscribe to rewind reaching the beginning so we can re-anchor the bridge
        rewindable.OnRewindReachedBeginning += AnchorBridge;
    }

    void OnDestroy()
    {
        if (rewindable != null)
        {
            rewindable.OnRewindReachedBeginning -= AnchorBridge;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isCollapsed) return;

        // Check if player stepped on the bridge segment
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.name.Contains("Player"))
        {
            TriggerCollapse();
        }
    }

    public void TriggerCollapse()
    {
        if (isCollapsed) return;
        isCollapsed = true;
        StartCoroutine(CollapseCoroutine());
    }

    private IEnumerator CollapseCoroutine()
    {
        float elapsed = 0f;
        Vector3 origPos = transform.position;
        while (elapsed < collapseDelay)
        {
            // Simple shake effect
            transform.position = origPos + Random.insideUnitSphere * 0.03f;
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = origPos;

        rb.isKinematic = false;
        rb.useGravity = true;
    }

    private void AnchorBridge()
    {
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        isCollapsed = false;
    }
}
