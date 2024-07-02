using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public NavMeshAgent agent;
    
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
        rigidbody.velocity = vel;
        rigidbody.isKinematic = false;
        isFrozen = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isFrozen)
        {
            agent.SetDestination(GameObject.Find("Player").transform.position);
            animator.SetFloat("Forward", agent.velocity.magnitude);
        }
    }
}
