using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyMovement : NetworkBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float deadZone = 0.1f;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float detectRange = 6f;  
    [SerializeField] private float loseRange = 10f;   
    [SerializeField] private LayerMask heroLayer;
    [SerializeField] private float rayHeightOffset = 0.5f;
    [SerializeField] private LayerMask visionMask;
    [SerializeField] private float fieldOfView = 60f;  

    private float waitTimer = 0f;
    [SerializeField] private float waitTime = 2f; 
    private bool isWaiting = false;

    private Rigidbody2D rb;
    private Animator anim;

    private Transform currentTarget;
    private Transform chaseTarget;
    private bool facingRight = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        currentTarget = pointA;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (chaseTarget != null)
        {
            float dist = Vector2.Distance(transform.position, chaseTarget.position);


            if (dist <= loseRange)
            {
                ChaseHero();
            }
            else
            {
                chaseTarget = null;
            }
        }
        else
        {
            DetectHero(); 
            Patrol();
        }
    }

    private void Patrol()
    {
        if (isWaiting)
        {
            waitTimer += Time.deltaTime;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); 
            anim.SetBool("isRunning", false);

            if (waitTimer >= waitTime)
            {
                isWaiting = false;
                currentTarget = (currentTarget == pointA) ? pointB : pointA;
            }
            return;
        }

        float direction = Mathf.Sign(currentTarget.position.x - transform.position.x);
        rb.linearVelocity = new Vector2(direction * patrolSpeed, rb.linearVelocity.y);

        if (direction > 0 && !facingRight) Flip();
        else if (direction < 0 && facingRight) Flip();

        anim.SetBool("isRunning", true);

        if (Vector2.Distance(transform.position, currentTarget.position) < deadZone)
        {
            isWaiting = true;
            waitTimer = 0f;
        }
    }

    private void DetectHero()
    {
        if (chaseTarget != null)
        {
            float dist = Vector2.Distance(transform.position, chaseTarget.position);

            if (dist > loseRange)
            {
                chaseTarget = null;
            }

            return;
        }

        Collider2D[] heroes = Physics2D.OverlapCircleAll(transform.position, detectRange, heroLayer);
        if (heroes.Length == 0) return;

        float minDistance = Mathf.Infinity;
        Transform nearestHero = null;

        foreach (var hero in heroes)
        {
            Vector2 dir = (hero.transform.position - transform.position).normalized;
            Vector2 forward = facingRight ? Vector2.right : Vector2.left;

            float angle = Vector2.Angle(forward, dir);
            if (angle > fieldOfView * 0.5f) continue;

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position + Vector3.up * rayHeightOffset,
                dir,
                detectRange,
                visionMask
            );

            if (hit.collider != null && hit.collider.CompareTag("Player"))
            {
                float dist = Vector2.Distance(transform.position, hero.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestHero = hero.transform;
                }
            }
        }

        if (nearestHero != null)
        {
            chaseTarget = nearestHero;
        }
    }

    private void ChaseHero()
    {
        float deltaX = chaseTarget.position.x - transform.position.x;
        float direction = (deltaX > 0.1f) ? 1 :
                          (deltaX < -0.1f) ? -1 : 0;

        rb.linearVelocity = new Vector2(direction * chaseSpeed, rb.linearVelocity.y);

        if (direction > 0 && !facingRight) Flip();
        else if (direction < 0 && facingRight) Flip();

        anim.SetBool("isRunning", direction != 0);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 localScale = transform.localScale;
        localScale.x *= -1;
        transform.localScale = localScale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return; 

        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerHealth>()?.Die();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, loseRange);

        if (pointA && pointB)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pointA.position, pointB.position);
        }
    }
}
