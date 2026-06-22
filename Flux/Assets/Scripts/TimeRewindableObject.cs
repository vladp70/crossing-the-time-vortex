using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeRewindableObject : MonoBehaviour
{
    private struct ObjectState
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public Vector3 angularVelocity;
        public bool isKinematic;

        public ObjectState(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angVel, bool kinematic)
        {
            position = pos;
            rotation = rot;
            velocity = vel;
            angularVelocity = angVel;
            isKinematic = kinematic;
        }
    }

    [Header("Rewind Settings")]
    [Tooltip("Maximum duration in seconds to store for rewinding")]
    public float maxHistorySeconds = 6f;

    private List<ObjectState> history = new List<ObjectState>();
    private Rigidbody rb;
    private Player player;
    private bool wasReversingLastFrame = false;
    private float popAccumulator = 0f;
    public float rewindSpeedMultiplier = 1.5f;

    // Event invoked when rewinding has reached the beginning of recorded history
    public System.Action OnRewindReachedBeginning;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // Find Player
        player = GameObject.FindObjectOfType<Player>();
        if (player == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
            if (playerObj != null)
            {
                player = playerObj.GetComponent<Player>();
            }
        }
    }

    void FixedUpdate()
    {
        // Check if player is reversing
        bool isReversing = player != null && player.IsReversing;

        if (isReversing)
        {
            Rewind();
        }
        else
        {
            Record();
        }

        wasReversingLastFrame = isReversing;
    }

    private void Record()
    {
        popAccumulator = 0f;
        if (wasReversingLastFrame && history.Count > 0)
        {
            ObjectState lastState = history[history.Count - 1];
            if (rb != null)
            {
                rb.isKinematic = lastState.isKinematic;
                if (!rb.isKinematic)
                {
                    rb.velocity = lastState.velocity;
                    rb.angularVelocity = lastState.angularVelocity;
                }
            }
        }

        Vector3 vel = rb != null ? rb.velocity : Vector3.zero;
        Vector3 angVel = rb != null ? rb.angularVelocity : Vector3.zero;
        bool kinematic = rb != null ? rb.isKinematic : true;

        history.Add(new ObjectState(transform.position, transform.rotation, vel, angVel, kinematic));

        int maxFrames = Mathf.RoundToInt(maxHistorySeconds / Time.fixedDeltaTime);
        if (history.Count > maxFrames)
        {
            history.RemoveAt(0);
        }
    }

    private void Rewind()
    {
        if (history.Count > 0)
        {
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            popAccumulator += rewindSpeedMultiplier;
            int pops = Mathf.FloorToInt(popAccumulator);
            popAccumulator -= pops;

            ObjectState lastState = default;
            bool poppedAny = false;

            for (int i = 0; i < pops; i++)
            {
                if (history.Count > 0)
                {
                    lastState = history[history.Count - 1];
                    history.RemoveAt(history.Count - 1);
                    poppedAny = true;
                }
            }

            if (poppedAny)
            {
                transform.position = lastState.position;
                transform.rotation = lastState.rotation;
            }

            if (history.Count == 0)
            {
                OnRewindReachedBeginning?.Invoke();
            }
        }
    }
}
