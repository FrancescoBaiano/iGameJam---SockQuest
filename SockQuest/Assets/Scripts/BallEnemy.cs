using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BallEnemy : MonoBehaviour
{
    private enum State { Patrolling, Chasing, Stopped }

    [Header("Patrol")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float patrolArriveThreshold = 0.1f;

    [Header("Vista")]
    [SerializeField] private float visionRange = 6f;
    [SerializeField] private float visionHeight = 0.3f;
    [Tooltip("Layer degli ostacoli (Muri/Terreno) che bloccano la vista.")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Terreno e Buchi")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckOffset = 0.6f;
    [SerializeField] private float groundCheckDistance = 1.0f;

    [Header("Inseguimento")]
    [SerializeField] private float chaseSpeed = 6f;

    [Header("Pausa Post-Colpito")]
    [Tooltip("Tempo di attesa in secondi dopo aver colpito il Player prima di riprendere la pattuglia.")]
    [SerializeField] private float stopDuration = 5f;

    [Header("Animazioni")]
    [SerializeField] private float chaseAnimSpeed = 2f;
    [SerializeField] private float normalAnimSpeed = 1f;

    [Header("Sprite")]
    [Tooltip("Attiva se l'artwork di base del nemico guarda a sinistra.")]
    [SerializeField] private bool spriteFacesLeftByDefault = false;
    [Tooltip("Attiva se lo sprite risulta capovolto/invertito rispetto al movimento.")]
    [SerializeField] private bool invertSpriteFacing = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Hash per i parametri dell'Animator
    private static readonly int AnimIsPause = Animator.StringToHash("isPause");

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private State currentState = State.Patrolling;

    private Transform currentPatrolTarget;
    private Transform playerTransform;

    private bool facingRight = true;
    private float stopTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        currentPatrolTarget = pointB != null ? pointB : pointA;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        ApplySpriteFacing();
    }

    private void Update()
    {
        switch (currentState)
        {
            case State.Patrolling:
                Patrol();
                TryDetectPlayer();
                break;

            case State.Chasing:
                Chase();
                break;

            case State.Stopped:
                HandleStopped();
                break;
        }
    }

    private void Patrol()
    {
        if (pointA == null || pointB == null) return;

        Vector2 target = currentPatrolTarget.position;
        Vector2 pos = transform.position;

        float direction = Mathf.Sign(target.x - pos.x);

        if (!IsGroundAhead(direction))
        {
            currentPatrolTarget = currentPatrolTarget == pointA ? pointB : pointA;
            return;
        }

        rb.linearVelocity = new Vector2(direction * patrolSpeed, rb.linearVelocity.y);
        FaceDirection(direction);

        if (Mathf.Abs(pos.x - target.x) <= patrolArriveThreshold)
        {
            currentPatrolTarget = currentPatrolTarget == pointA ? pointB : pointA;
        }
    }

    private void TryDetectPlayer()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * visionHeight;
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, visionRange);

        foreach (var hit in hits)
        {
            if (hit.collider.gameObject == gameObject) continue;
            if (hit.collider.isTrigger) continue;

            if (hit.collider.TryGetComponent<Player>(out Player player))
            {
                if (showDebugLogs) Debug.Log("[BallEnemy] Player avvistato! Inizio inseguimento.");
                StartChasing(player);
                break;
            }

            if (((1 << hit.collider.gameObject.layer) & obstacleLayer.value) != 0)
            {
                break;
            }
        }
    }

    private bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        Vector2 origin = (Vector2)transform.position + Vector2.up * visionHeight;
        Vector2 targetPos = (Vector2)playerTransform.position + Vector2.up * visionHeight;
        float distance = Vector2.Distance(origin, targetPos);

        if (distance > visionRange)
        {
            if (showDebugLogs) Debug.Log("[BallEnemy] Player uscito dal raggio visivo.");
            return false;
        }

        RaycastHit2D hit = Physics2D.Linecast(origin, targetPos, obstacleLayer);
        if (hit.collider != null)
        {
            if (showDebugLogs) Debug.Log($"[BallEnemy] Vista bloccata da: {hit.collider.name}");
            return false;
        }

        return true;
    }

    private bool IsGroundAhead(float direction)
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(direction * groundCheckOffset, 0f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    private void StartChasing(Player player)
    {
        playerTransform = player.transform;
        currentState = State.Chasing;

        if (animator != null) animator.speed = chaseAnimSpeed;
    }

    private void Chase()
    {
        if (playerTransform == null || !CanSeePlayer())
        {
            StopChasing();
            return;
        }

        float distanceX = playerTransform.position.x - transform.position.x;
        float direction = Mathf.Sign(distanceX);
        FaceDirection(direction);

        if (!IsGroundAhead(direction))
        {
            if (showDebugLogs) Debug.Log("[BallEnemy] Presenza di un buco! Stop inseguimento.");
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            StopChasing();
            return;
        }

        rb.linearVelocity = new Vector2(direction * chaseSpeed, rb.linearVelocity.y);
    }

    private void HandleStopped()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        stopTimer -= Time.deltaTime;

        if (stopTimer <= 0f)
        {
            if (showDebugLogs) Debug.Log("[BallEnemy] Pausa terminata, riprendo il pattugliamento.");
            playerTransform = null;
            currentState = State.Patrolling;

            if (animator != null)
            {
                animator.SetBool(AnimIsPause, false);
            }
        }
    }

    private void StopChasing()
    {
        if (showDebugLogs && currentState == State.Chasing) Debug.Log("[BallEnemy] Inseguimento interrotto.");

        playerTransform = null;
        currentState = State.Patrolling;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.speed = normalAnimSpeed;
            animator.SetBool(AnimIsPause, false);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == State.Stopped) return;

        Player player = collision.gameObject.GetComponent<Player>();
        if (player == null) return;

        if (showDebugLogs) Debug.Log("[BallEnemy] Player colpito! Mi fermo per " + stopDuration + " secondi.");

        currentState = State.Stopped;
        stopTimer = stopDuration;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.speed = normalAnimSpeed;
            animator.SetBool(AnimIsPause, true);
        }

        player.Die();
    }

    private void FaceDirection(float direction)
    {
        if (direction > 0f && !facingRight) Flip();
        else if (direction < 0f && facingRight) Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        ApplySpriteFacing();
    }

    private void ApplySpriteFacing()
    {
        if (spriteRenderer == null) return;

        bool flip = spriteFacesLeftByDefault ? facingRight : !facingRight;

        // Se spuntato dall'Inspector, inverte la direzione finale dello sprite
        if (invertSpriteFacing) flip = !flip;

        spriteRenderer.flipX = flip;
    }

    private void OnDrawGizmosSelected()
    {
        if (pointA != null && pointB != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pointA.position, pointB.position);
            Gizmos.DrawWireSphere(pointA.position, 0.2f);
            Gizmos.DrawWireSphere(pointB.position, 0.2f);
        }

        Vector2 origin = (Vector2)transform.position + Vector2.up * visionHeight;
        Vector2 dir = facingRight ? Vector2.right : Vector2.left;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + dir * visionRange);

        float checkDir = facingRight ? 1f : -1f;
        Vector2 groundOrigin = (Vector2)transform.position + new Vector2(checkDir * groundCheckOffset, 0f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDistance);
    }
}