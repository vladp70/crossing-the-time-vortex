using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;

public class EnemyAgent : Agent
{
    [SerializeField] private float rotSpeed = 5f;
    [SerializeField] private float speed;
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 respawnPosition;
    private Vector3 moveDir;
    private Rigidbody rBody;

    const float joystickActiveTolerance = 3f * 10e-3f;
    // Start is called before the first frame update
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float x = actions.ContinuousActions[0];
        float z = actions.ContinuousActions[1];
        //Debug.Log($"x: {x}, z: {z}");
        //Debug.Log($"{targetTransform.position}, {transform.position}");
        moveDir = new Vector3(x, 0, z).normalized;
        ApplyRootRotation();
        rBody.velocity = moveDir * speed;
        transform.position += speed * Time.deltaTime * moveDir;

        // Punish for redundant steps
        AddReward(-0.02f);

        if (Vector3.Distance(transform.position, targetTransform.position) <= 1.5f)
        {
            AddReward(1f);
            Debug.Log("Reached target!");
            EndEpisode();
        }
        if (Vector3.Distance(transform.position, targetTransform.position) > 25f)
        {
            AddReward(-0.15f);
            Debug.Log("Too far from target!");
            EndEpisode();
        }
    }

    private void ApplyRootRotation()
    {
        Vector3 lookDir = transform.forward;
        if (moveDir.magnitude > joystickActiveTolerance) //there is movement
            lookDir = moveDir;

        Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
        float rotSlerpFactor = Mathf.Clamp01(rotSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSlerpFactor);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.position);
        sensor.AddObservation(targetTransform.position);
    }

    void playerDash()
    {
        targetTransform.position += new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * 2f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // collision with player
        if (collision.gameObject.TryGetComponent<Player>(out Player player))
        {
            AddReward(1f);
            Debug.Log("Player collision!");
            EndEpisode();
        }
    }

    public override void OnEpisodeBegin()
    {
        transform.position = new Vector3(Random.Range(-1f, 9f), 0, Random.Range(8f, 17f));
        //targetTransform.position = new Vector3(Random.Range(-1f, 9f), 0, Random.Range(8f, 17f));
    }

    /*

    public override void OnEpisodeBegin()
    {
        // If the Agent fell, zero its momentum
        // TODO: Change, refactor, eliminate. Tryed collisionExit with Plane
        if (this.transform.localPosition.y < 0)
        {
            rBody.angularVelocity = Vector3.zero;
            rBody.velocity = Vector3.zero;
        }

        //spawn to safeHouse location
        transform.position = new Vector3(3.0f, -3.0f, 13.0f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continousActions = actionsOut.ContinuousActions;
        continousActions[0] = Input.GetAxisRaw("Horizontal");
        continousActions[1] = Input.GetAxisRaw("Vertical");
    }

    public override void OnEpisodeBegin()
    {
        // If the Agent fell, zero its momentum
        // TODO: Change, refactor, eliminate. Tryed collisionExit with Plane
        if (this.transform.localPosition.y < 0)
        {
            rBody.angularVelocity = Vector3.zero;
            rBody.velocity = Vector3.zero;
        }

        //spawn to safeHouse location
        transform.localPosition = safeHouse;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // collision with goal/ pickup/ apple
        if (collision.gameObject.TryGetComponent<Player>(out Player goal))
        {
            // Spawn new pickup, destroy old one
            // TODO: need to refactor interaction in medium future
            Destroy(collision.gameObject);

            AddReward(1f);
            EndEpisode();
        }
        // collision with player
        if (collision.gameObject.TryGetComponent<Player>(out Player player))
        {
            AddReward(-1f);
            Debug.Log("Player collision!");
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        // Collision with plane
        if (collision.gameObject.TryGetComponent<MeshCollider>(out MeshCollider mesh))
        {
            SetReward(-1f);
            EndEpisode();
        }
    }

    public Vector3 GetSafeHouse()
    {
        return safeHouse;
    }

    public void SetSafeHouse(Vector3 safeHouse)
    {
        this.safeHouse = safeHouse;
    }
    */
}
