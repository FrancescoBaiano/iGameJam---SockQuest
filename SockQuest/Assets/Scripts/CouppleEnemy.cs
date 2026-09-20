using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CouppleEnemy : MonoBehaviour
{
    private enum State { Patrolling, Chasing, Cooldown }

    [Header("Patrol")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float patrolArriveThreshold = 0.1f;

    [Header("Vista")]
    [SerializeField] private float visionRange = 6f;
    [SerializeField] private float visionHeight = 0.5f;
    [Tooltip("Layer degli ostacoli (Muri/Terreno) che bloccano la vista.")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Terreno e Buchi")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckOffset = 0.6f;
    [SerializeField] private float groundCheckDistance = 1.0f;

    [Header("Inseguimento")]
    [SerializeField] private float chaseSpeed = 4f;

    [Header("Spinta")]
    [SerializeField] private float pushInterval = 2f;
    [SerializeField] private float pushForce = 15f;
    [SerializeField] private float pushUpForce = 4f;
    [Tooltip("IMPOSTA A 2.0 O SUPERIORE: Distanza tra i pivot per attivare la spinta.")]
    [SerializeField] private float pushRange = 2.2f;

    [Header("Pausa Post-Spinta")]
    [SerializeField] private float postPushPauseDuration = 2.0f;

    [Header("Animazioni")]
    [SerializeField] private float chaseAnimSpeed = 2f;
    [SerializeField] private float normalAnimSpeed = 1f;

    [Header("Sprite")]
    [Tooltip("Attiva SOLO se l'artwork di base del nemico guarda già a sinistra invece che a destra. " +
             "Corregge il verso in cui viene flippato lo sprite senza toccare la logica di movimento/visione.")]
    [SerializeField] private bool spriteFacesLeftByDefault = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Nomi/hash dei parametri dell'Animator
    private static readonly int AnimIsAttacking = Animator.StringToHash("isAttacking");
    private static readonly int AnimMove = Animator.StringToHash("Move");

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private State currentState = State.Patrolling;

    private Transform currentPatrolTarget;
    private Transform playerTransform;
    private Rigidbody2D playerRb;

    private bool facingRight = true;
    private float pushTimer;
    private float cooldownTimer;

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

            case State.Cooldown:
                HandleCooldown();
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
                if (showDebugLogs) Debug.Log("[Nemico] Player avvistato! Inizio inseguimento.");
                StartChasing(player);
                break;
            }

            // Se incontra un ostacolo prima del player
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
            if (showDebugLogs) Debug.Log("[Nemico] Player uscito dal raggio visivo.");
            return false;
        }

        // Controlla solo se ci sono OSTACOLI solidi tra nemico e player
        RaycastHit2D hit = Physics2D.Linecast(origin, targetPos, obstacleLayer);
        if (hit.collider != null)
        {
            if (showDebugLogs) Debug.Log($"[Nemico] Vista bloccata da: {hit.collider.name}");
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
        playerRb = player.GetComponent<Rigidbody2D>();
        pushTimer = pushInterval;
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
            if (showDebugLogs) Debug.Log("[Nemico] Presenza di un buco! Stop inseguimento.");
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            StopChasing();
            return;
        }

        rb.linearVelocity = new Vector2(direction * chaseSpeed, rb.linearVelocity.y);

        pushTimer += Time.deltaTime;

        float currentDist = Mathf.Abs(distanceX);
        if (pushTimer >= pushInterval && currentDist <= pushRange)
        {
            animator.speed = normalAnimSpeed;
            PushPlayer(direction);

            currentState = State.Cooldown;
            cooldownTimer = postPushPauseDuration;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    private void HandleCooldown()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (playerTransform != null)
        {
            float direction = Mathf.Sign(playerTransform.position.x - transform.position.x);
            FaceDirection(direction);
        }

        cooldownTimer -= Time.deltaTime;

        if (cooldownTimer <= 0f)
        {
            if (CanSeePlayer())
            {
                pushTimer = 0f;
                currentState = State.Chasing;

                // L'attacco è finito, si riprende a muoversi
                if (animator != null) animator.SetTrigger(AnimMove);
            }
            else
            {
                StopChasing();
            }
        }
    }

    private void PushPlayer(float direction)
    {
        if (playerRb == null) return;

        if (showDebugLogs) Debug.Log("[Nemico] *** ESECUZIONE SPINTA EFFETTUATA ***");

        if (animator != null) animator.SetTrigger(AnimIsAttacking);

        Vector2 forceVector = new Vector2(direction * pushForce, pushUpForce);

        // Cerca lo script Player e applica il knockback con blocco input temporaneo
        if (playerRb.TryGetComponent<Player>(out Player player))
        {
            player.ApplyKnockback(forceVector, 0.25f); // 0.25 secondi di spinta libera
        }
        else
        {
            // Fallback diretto sulla fisica se lo script non è presente
            playerRb.linearVelocity = forceVector;
        }
    }

    private void StopChasing()
    {
        if (showDebugLogs && currentState == State.Chasing) Debug.Log("[Nemico] Inseguimento interrotto.");

        playerTransform = null;
        playerRb = null;
        pushTimer = 0f;
        cooldownTimer = 0f;
        currentState = State.Patrolling;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.speed = normalAnimSpeed;
            animator.SetTrigger(AnimMove);
        }
    }

    // direction > 0 = si sta muovendo/deve guardare verso destra; direction < 0 = verso sinistra.
    // facingRight riflette SEMPRE questo significato standard: non invertirla qui per problemi
    // di sprite, usa invece "spriteFacesLeftByDefault" più sotto.
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

    // Unico punto in cui la direzione logica (facingRight) viene tradotta nel flip
    // visivo dello sprite. Se il tuo artwork guarda già a sinistra di base, attiva
    // "spriteFacesLeftByDefault" nell'Inspector invece di toccare FaceDirection/Flip.
    private void ApplySpriteFacing()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.flipX = spriteFacesLeftByDefault ? facingRight : !facingRight;
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

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pushRange);

        float checkDir = facingRight ? 1f : -1f;
        Vector2 groundOrigin = (Vector2)transform.position + new Vector2(checkDir * groundCheckOffset, 0f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDistance);
    }
}