using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior : MonoBehaviour
{
    [SerializeField] Transform target;
    public NavMeshAgent agent;
    public float dist;

    // Start is called before the first frame update
    void Start()
    {
        var agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        dist = Vector3.Distance(target.position, transform.position);
    }

    void Update()
    {
        if (dist > 2)
        {
            agent.SetDestination(target.position);
        }
        dist = Vector3.Distance(target.position, transform.position);
    }
}