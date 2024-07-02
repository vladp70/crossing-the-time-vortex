using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform cameraTransform;
    public float moveSpeed = 3f;
    public float rotSpeed = 5f;
    public float jumpPower = 5f;
    public float groundedThreshold = .15f;
    public float minimumRespawnY = -50f;
    const float joystickActiveTolerance = 3f * 10e-3f;

    public float dashSpeed = 5f;
    public float dashTime = 1f;

    Vector3 initPos;
    Vector3 moveDir;

    Rigidbody rigidbody;
    Animator animator;
    CapsuleCollider capsule;
    bool isGrounded = true;
    bool isDashing = false;

    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log(joystickActiveTolerance.ToString());
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        capsule = GetComponent<CapsuleCollider>();
        initPos = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashing) {
            StartCoroutine(Dash());
        }

        GetMoveDir();

        SetAnimatorMoveParams();

        HandleJump();

        ApplyRootRotation();
    }
    
    private void OnAnimatorMove()
    {
        MovePlayer();
    }

    private void SetAnimatorMoveParams()
    {
        Vector3 characterSpaceMoveDir = transform.InverseTransformVector(moveDir) * 1.2f;
        animator.SetFloat("Forward", characterSpaceMoveDir.z);
        animator.SetFloat("Right", characterSpaceMoveDir.x);
    }

    private void HandleJump()
    {
        //Ray ray = new Ray();
        //ray.origin = transform.position + Vector3.up * groundedThreshold;
        //ray.direction = Vector3.down;
        //isGrounded = Physics.Raycast(ray, 2 * groundedThreshold);
        Vector3 bottomCapsuleSphereCenter = transform.position + Vector3.up * (capsule.radius + groundedThreshold);
        Vector3 topCapsuleSphereCenter = transform.position + Vector3.up * (capsule.height - capsule.radius + groundedThreshold);
        isGrounded = Physics.CapsuleCast(bottomCapsuleSphereCenter, topCapsuleSphereCenter,
                                         capsule.radius, Vector3.down, groundedThreshold * 2f);
        animator.SetBool("Grounded", isGrounded);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rigidbody.AddForce(Vector3.up * jumpPower, ForceMode.VelocityChange);
        }

        if (transform.position.y < minimumRespawnY)
            transform.position = initPos;
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

    private void GetMoveDir()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 cameraFwd_xOz = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight_xOz = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        moveDir = x * cameraRight_xOz + z * cameraFwd_xOz;
        moveDir = moveDir.normalized * Mathf.Max(Mathf.Abs(moveDir.x), Mathf.Abs(moveDir.z));
    }

    private void MovePlayer()
    {
        //transform.position += moveDir * moveSpeed * Time.deltaTime;
        if (!isGrounded)
            return;
        float velY = rigidbody.velocity.y;
        //Vector3 newVel = moveDir * moveSpeed;
        Vector3 newVel = animator.deltaPosition / Time.deltaTime * moveSpeed;
        rigidbody.velocity = new Vector3(newVel.x, velY, newVel.z);
    }

    IEnumerator Dash() {
        isDashing = true;
        Vector3 startPosition = transform.position;
        Vector3 dashDirection = moveDir.normalized;
        Vector3 endPosition = startPosition + dashDirection * dashSpeed;

        float elapsedTime = 0f;
        while (elapsedTime < dashTime) {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / dashTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        transform.position = endPosition;
        isDashing = false;
    }

    private void onCollisionEnter(Collision collision) {
    //     if (collision.gameObject.tag == "healthPotion") {
    //         increaseHealth();
    //         Destroy(collision.gameObject);
    //     } else if (collision.gameObject.tag == "manaPotion") {
    //         increaseMana();
    //         Destroy(collision.gameObject);
    //     } else if (collision.gameObject.tag == "enemy") {
    //         handleEnemyCollision();
    //     }
    }
}