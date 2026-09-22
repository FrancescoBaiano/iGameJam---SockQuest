using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CouppleEnemy : MonoBehaviour
{
    private enum State { Patrolling, Chasing, Cooldown, Paused }

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
    [Tooltip("Altezza massima sopra la quale il nemico NON può colpire il player.")]
    [SerializeField] private float maxPushVerticalDistance = 1.2f;

    [Header("Pausa Post-Spinta")]
    [SerializeField] private float postPushPauseDuration = 2.0f;

    [Header("Pausa Player Sopra")]
    [Tooltip("Tempo di arresto (in secondi) quando il Player si trova sopra il nemico.")]
    [SerializeField] private float pauseOnTopDuration = 2.0f;
    [Tooltip("Distanza/Altezza sopra la testa per rilevare il Player.")]
    [SerializeField] private float overheadCheckDistance = 1.2f;
    [Tooltip("Larghezza dell'area di rilevamento sopra la testa.")]
    [SerializeField] private float overheadCheckWidth = 1.0f;

    [Header("Animazioni")]
    [SerializeField] private float chaseAnimSpeed = 2f;
    [SerializeField] private float normalAnimSpeed = 1f;

    [Header("Sprite")]
    [Tooltip("Attiva SOLO se l'artwork di base del nemico guarda già a sinistra invece che a destra. " +
             "Corregge il verso in cui viene flippato lo sprite senza toccare la logica di movimento/visione.")]
    [SerializeField] private bool spriteFacesLeftByDefault = false;

    [Header("Audio")]
    [SerializeField] private AudioSource movementAudioSource; // loop: camminata / corsa
    [SerializeField] private AudioSource kickAudioSource;      // one-shot: calcio
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip runClip;
    [SerializeField] private AudioClip kickClip;

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
    private float pauseTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        currentPatrolTarget = pointB != null ? pointB : pointA;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        ApplySpriteFacing();

        if (movementAudioSource != null)
        {
            movementAudioSource.loop = true;
        }
    }

    private void Update()
    {
        if (PauseController.Instance.IsPaused)
        {
            rb.linearVelocity = Vector2.zero;
            animator.enabled = false;
            return;
        }
        animator.enabled = true;

        // Controlla sempre se il Player si trova sopra la testa del nemico
        if (currentState != State.Paused && CheckPlayerOnTop())
        {
            TriggerPauseOnTop();
        }

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

            case State.Paused:
                HandlePause();
                break;
        }

        UpdateMovementSound();
    }

    private bool CheckPlayerOnTop()
    {
        Vector2 origin = (Vector2)transform.position + Vector2.up * visionHeight;

        // Usiamo BoxCastAll per rilevare tutti i collider nella traiettoria e ignorare se stesso
        RaycastHit2D[] hits = Physics2D.BoxCastAll(origin, new Vector2(overheadCheckWidth, 0.2f), 0f, Vector2.up, overheadCheckDistance);

        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            if (hit.collider.gameObject == gameObject) continue; // Ignora il collider del nemico stesso
            if (hit.collider.isTrigger) continue;

            if (hit.collider.TryGetComponent<Player>(out Player player))
            {
                if (playerTransform == null)
                {
                    playerTransform = player.transform;
                    playerRb = player.GetComponent<Rigidbody2D>();
                }
                return true;
            }
        }
        return false;
    }

    private void TriggerPauseOnTop()
    {
        if (showDebugLogs) Debug.Log("[Nemico] Player sopra la testa! Nemico fermo per " + pauseOnTopDuration + "s.");

        currentState = State.Paused;
        pauseTimer = pauseOnTopDuration;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null) animator.speed = normalAnimSpeed;

        UpdateMovementSound();
    }

    private void HandlePause()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        pauseTimer -= Time.deltaTime;

        if (pauseTimer <= 0f)
        {
            if (CanSeePlayer())
            {
                if (playerTransform != null && playerTransform.TryGetComponent<Player>(out Player p))
                {
                    StartChasing(p);
                }
            }
            else
            {
                StopChasing();
            }
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
        float distanceY = playerTransform.position.y - transform.position.y;
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

        float currentDistX = Mathf.Abs(distanceX);

        // Attacca solo se è nel raggio orizzontale E il player non è troppo in alto (o troppo in basso)
        if (pushTimer >= pushInterval && currentDistX <= pushRange && distanceY <= maxPushVerticalDistance)
        {
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

        PlayKickSound();

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
        pauseTimer = 0f;
        currentState = State.Patrolling;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (animator != null)
        {
            animator.speed = normalAnimSpeed;
            animator.SetTrigger(AnimMove);
        }
    }

    // --- AUDIO ---

    private void UpdateMovementSound()
    {
        if (movementAudioSource == null) return;

        // Cammina in pattuglia, corre in inseguimento; fermo in Cooldown e Paused -> silenzio.
        AudioClip desiredClip = currentState switch
        {
            State.Patrolling => walkClip,
            State.Chasing => runClip,
            _ => null
        };

        if (desiredClip == null)
        {
            if (movementAudioSource.isPlaying) movementAudioSource.Stop();
            return;
        }

        if (movementAudioSource.clip != desiredClip)
        {
            movementAudioSource.clip = desiredClip;
            movementAudioSource.Play();
        }
        else if (!movementAudioSource.isPlaying)
        {
            movementAudioSource.Play();
        }
    }

    private void PlayKickSound()
    {
        if (kickAudioSource != null && kickClip != null)
            kickAudioSource.PlayOneShot(kickClip);
    }

    // --- FINE AUDIO ---

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

        // Disegna l'area di spinta
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            (Vector2)transform.position + Vector2.up * (maxPushVerticalDistance / 2f),
            new Vector3(pushRange * 2f, maxPushVerticalDistance, 0f)
        );

        // Disegna l'area sopra la testa per la pausa
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(
            origin + Vector2.up * (overheadCheckDistance / 2f),
            new Vector3(overheadCheckWidth, overheadCheckDistance, 0f)
        );

        float checkDir = facingRight ? 1f : -1f;
        Vector2 groundOrigin = (Vector2)transform.position + new Vector2(checkDir * groundCheckOffset, 0f);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(groundOrigin, groundOrigin + Vector2.down * groundCheckDistance);
    }
}