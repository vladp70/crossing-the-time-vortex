using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public NavMeshAgent agent;
    public float attackRange = 2f;
    public float rotSpeed = 5f;
    
    Animator animator;
    Rigidbody rigidbody;

    bool isFrozen = false;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
    }

    public void getFrozen(float freezeTime) {
        StartCoroutine(Freeze(freezeTime));
    }

    public IEnumerator Freeze(float freezeTime) {
        Debug.Log("Freezing for " + freezeTime + " seconds");
        Vector3 vel = rigidbody.velocity;
        Vector3 agentVel = agent.velocity;
        
        isFrozen = true;
        agent.isStopped = true;
        animator.speed = 0;
        rigidbody.velocity = Vector3.zero;
        rigidbody.isKinematic = true;

        yield return new WaitForSeconds(freezeTime);

        agent.isStopped = false;
        agent.velocity = agentVel;
        animator.speed = 1;
        rigidbody.isKinematic = false;
        //rigidbody.velocity = vel;
        isFrozen = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isFrozen)
        {
            Vector3 playerPos = GameObject.Find("Player").transform.position;

            agent.SetDestination(playerPos);
            animator.SetFloat("Forward", agent.velocity.magnitude);

            if (Vector3.Distance(transform.position, playerPos) < attackRange)
            {
                //ApplyRootRotationTo(playerPos);
                rigidbody.velocity = Vector3.zero;
                agent.velocity = Vector3.zero;
                animator.SetTrigger("Attack");
            }
        }
    }

    private void ApplyRootRotationTo(Vector3 position)
    {
        Vector3 lookDir = position - transform.position;
        Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);
        float rotSlerpFactor = Mathf.Clamp01(rotSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotSlerpFactor);
    }
}
