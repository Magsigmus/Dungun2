using UnityEngine;
using UnityEngine.AI;

public class EnemyBehavior : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] NavMeshAgent agent;
    [SerializeField] GameObject bulletPrefab;

    public enum MovementMode
    {
        Idle,
        Waiting,
        Wandering,
        Chasing
    }

    /*Cooldowns and timers*/
    public float shootCooldownTime = 1f;  //rasj: 1 second
    private float shootCooldown = 0f;

    public float minTargetDistance = 3f;
    public float maxTargetDistance = 10f;

    private Vector3 wandDesti = new Vector3(0f, 0f, 0f);
    public float maxWanderDistance = 5f;
    public float maxWanderTime = 15f;
    private float wanderTime = 0f;
    private bool wandering = false;

    private MovementMode MMode;

    //Gameobject enemy = gameobj.GetComponent<Enemy>();

    public int healthPoints = 3;

    public int sstate = 0;

    void Start()
    {
        var agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = 2f;

        if (GameObject.FindGameObjectWithTag("Player"))
        {
            target = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    void Update()
    {
        shootCooldown += Time.deltaTime;
        wanderTime += Time.deltaTime;
        Move(target.position, minTargetDistance, maxTargetDistance);
        if (!wandering)
        {
            Shoot(target.position);
            /*
            GeneralizedInstruction instruction = new GeneralizedInstruction();
            instruction.gunObject = GetComponent<EnemyCombatBehaviour>().gunObject;
            instruction.defaultTarget = GameObject.FindGameObjectWithTag("Player").transform;
            instruction.Shoot();
            */
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "PlayerBullet")
        {
            TakeDamage(1);
        }
    }

    void Move(Vector3 targetPos, float minDist, float maxDist)
    {
        wandering = false;

        if (MMode == MovementMode.Chasing)  //rasj: if enemy is far enough away from target  dist > minDist && dist < maxDist
        {
            agent.SetDestination(targetPos);  //rasj: Sets destination to target
            Flip(targetPos);
        }
        else if (MMode == MovementMode.Idle || MMode == MovementMode.Wandering)  //rasj: if enemy is too far away from target  dist > maxDist
        {
            wandering = true;
            Wander(-maxWanderDistance, maxWanderDistance);
        }
        else  //rasj: if (MMode == MovementMode.Waiting) if enemy is too close to target
        {
            agent.SetDestination(transform.position);  //rasj: Stops enemy from going further
        }

    }

    void Wander(float min, float max)
    {
        if (wandDesti == transform.position || wanderTime >= maxWanderTime)  //rasj: if wander reached or too much time has passed
        {
            wanderTime = 0;  //rasj: Reset the time used while wandering
            Vector3 randVect = new Vector2(Random.Range(min, max), Random.Range(min, max));  //rasj: get a random vector
            wandDesti = randVect + transform.position;  //rasj: Add that vector to the currect position
            agent.SetDestination(wandDesti);  //rasj: Set that to the new desination

            Flip(wandDesti); //rasj: flip the sprite accordingly
        }
    }

    public void ChangeBehaviour(MovementMode newMovmentMode)
    {
        MMode = newMovmentMode;

        /*
        float dist = Vector3.Distance(target.position, transform.position);  //rasj: Get distance
        if (dist > minDist && dist < maxDist)  //rasj: if not too far away and not too close
        {
            MMode = MovementMode.Chasing;
        }
        else if (dist > maxDist)  //if too far away
        {
            MMode = MovementMode.Idle;
        }
        else if (dist < minDist)
        {
            MMode = MovementMode.Wandering;
        }
        */
    }

    void Flip(Vector3 targetPos)
    {
        float dirX = targetPos.x - transform.position.x;
        if (dirX < 0)  //rasj: left
        {
            transform.rotation = Quaternion.Euler(0, 180f, 0);
        }
        else if (dirX > 0) //rasj: right
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
    }

    void Shoot(Vector3 targetPos)
    {
        Vector3 dir = PointTo(targetPos);

        if (shootCooldown > shootCooldownTime)  //rasj: if shootCooldown ran out
        {
            shootCooldown = 0f;  //rasj: reset cooldown
            GameObject newBullet = Instantiate(bulletPrefab);
            newBullet.transform.up = dir;
            newBullet.transform.position = transform.position;  //rasj: set to current position
        }
        /*
        if (bulletPrefab.name == "ChargerEnemyBullet")
        {
            //todo: set bullet health to currect health
            Death();
        }
        */
    }

    Vector2 PointTo(Vector2 targetPos)  //rasj: Sets rotation
    {
        //TODO: make enemy point ahead of player's direction, to compensate for time and actually hit
        //rasj: findes the vector to the target
        Vector2 dir = new Vector2(targetPos.x - transform.position.x, targetPos.y - transform.position.y);
        //transform.up = dir;
        return dir;
    }

    void TakeDamage(int damage)
    {
        healthPoints -= damage;

        if (healthPoints <= 0)
        {
            Death();
        }
    }

    public void Death()
    {
        GameObject.Destroy(gameObject);
        //GetComponent<EnemyCombatBehaviour>().RunInstructions(GetComponent<EnemyCombatBehaviour>().OnDeath, "death");  //rasj: basically die
        //Destroy(this.gameObject);
    }
}