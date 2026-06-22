using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public Transform cameraTransform;
    public float moveSpeed = 3f;
    public Vector3 moveDir;
    public float turnSpeed = 5f;
    public float groundedThreshold = .1f;
    public float jumpForce = 5f;

    float horizontalInput;
    float verticalInput;

    Animator animator;
    private Rigidbody rigidb;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        rigidb = GetComponent<Rigidbody>();
        //rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
    }

    // Update is called once per frame
    void Update()
    {
        MyInput();
        GetMoveDir();
        UpdateAnimator();
    }

    private void OnAnimatorMove()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
    }

    private void GetMoveDir() {
        Vector3 cameraFwd_xOz = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 cameraRight_xOz = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;

        moveDir = horizontalInput * cameraRight_xOz + verticalInput * cameraFwd_xOz;
        moveDir = moveDir.normalized * Mathf.Max(Mathf.Abs(moveDir.x), Mathf.Abs(moveDir.z));
    }
    
    private void UpdateAnimator() {
        Vector3 characterSpaceMoveDir = transform.InverseTransformVector(moveDir) * 1.2f;
        animator.SetFloat("Forward", characterSpaceMoveDir.z);
        animator.SetFloat("Right", characterSpaceMoveDir.x);
        Debug.Log("Forward: " + characterSpaceMoveDir.z + " Right: " + characterSpaceMoveDir.x);
    }

    private void MovePlayer() {
        float velY = rigidb.velocity.y;
        //Vector3 newVel = moveDir * moveSpeed;
        Vector3 newVel = animator.deltaPosition / Time.deltaTime * moveSpeed;
        rigidb.velocity = new Vector3(newVel.x, velY, newVel.z);
    }

}
