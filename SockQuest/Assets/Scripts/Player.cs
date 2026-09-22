using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] public bool inShoe;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("OnDeath")]
    [SerializeField] Transform spawnPoint;
    [SerializeField] private float repositionTime = 2f;
    [SerializeField] private Animator scopaAnimator;
    [SerializeField] private Animator fadePanelAnimator;

    public Transform GroundCheck => groundCheck;
    public float GroundCheckRadius => groundCheckRadius;

    [Header("Collectables")]
    [SerializeField] public int points = 0;
    [SerializeField] private TextMeshProUGUI pointText;

    [Header("Controllo muri")]
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private float wallCheckRadius = 0.15f;
    [SerializeField] private LayerMask wallLayer;

    [Header("Animazioni")]
    [SerializeField] private float moveAnimThreshold = 0.05f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;      // per SFX one-shot: salto, morte
    [SerializeField] private AudioSource walkAudioSource;   // dedicato al loop dei passi
    [SerializeField] private AudioSource scopaAudioSource;
    [SerializeField] private AudioClip walkClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip deathClip;

    // Nomi/hash dei parametri dell'Animator
    private static readonly int AnimIsMoving = Animator.StringToHash("isMoving");
    private static readonly int AnimIsJumping = Animator.StringToHash("isJumping");
    private static readonly int AnimIsInShoe = Animator.StringToHash("isInShoe");
    private static readonly int AnimDeath = Animator.StringToHash("Death");
    private static readonly int AnimAlive = Animator.StringToHash("isAlive");

    private Animator animator;
    private Rigidbody2D rb;

    private InputActionMap playerMap;
    private InputAction moveAction;
    private InputAction jumpAction;

    private float moveInput;
    private bool jumpPressed;
    private bool isGrounded;
    private bool isTouchingWallRight;
    private bool isTouchingWallLeft;
    private bool facingRight = true;

    private bool isKnockedBack;
    private float knockbackTimer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        playerMap = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        moveAction = playerMap.FindAction(moveActionName, throwIfNotFound: true);
        jumpAction = playerMap.FindAction(jumpActionName, throwIfNotFound: true);

        UpdatePoints();

        if (walkAudioSource != null)
        {
            walkAudioSource.clip = walkClip;
            walkAudioSource.loop = true;
        }
    }

    private void OnEnable()
    {
        jumpAction.performed += OnJumpPerformed;
        playerMap.Enable();
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPerformed;
        playerMap.Disable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        jumpPressed = true;
    }

    private void Update()
    {
        if (PauseController.Instance.IsPaused) return;

        // 1. Calcoliamo PRIMA se il player è a terra
        isGrounded = groundCheck != null && Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // 2. Gestione del Knockback basato sull'atterraggio
        if (isKnockedBack)
        {
            // Decrementa il piccolo timer di sicurezza iniziale (0.1s)
            if (knockbackTimer > 0f)
            {
                knockbackTimer -= Time.deltaTime;
            }
            // Quando il timer minimo è passato E il player tocca di nuovo il terreno -> Sblocca i comandi
            else if (isGrounded)
            {
                isKnockedBack = false;
            }

            StopWalkSound();
            return; // Salta la lettura degli input finché non è atterrato
        }

        moveInput = moveAction.ReadValue<float>();

        isTouchingWallRight = wallCheckRight != null &&
                               Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);
        isTouchingWallLeft = wallCheckLeft != null &&
                              Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);

        if (jumpPressed)
        {
            jumpPressed = false;
            if (isGrounded && !inShoe) Jump();
        }

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();

        UpdateAnimator();
        UpdateWalkSound();
    }

    private void FixedUpdate()
    {
        if (PauseController.Instance.IsPaused)
        {
            rb.linearVelocity = Vector2.zero;
            animator.enabled = false;
            return;
        }
        animator.enabled = true;

        if (inShoe || isKnockedBack) return;

        float desiredMoveX = moveInput * moveSpeed;

        if (desiredMoveX > 0f && isTouchingWallRight) desiredMoveX = 0f;
        if (desiredMoveX < 0f && isTouchingWallLeft) desiredMoveX = 0f;

        rb.linearVelocity = new Vector2(desiredMoveX, rb.linearVelocity.y);
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool(AnimIsMoving, Mathf.Abs(moveInput) > moveAnimThreshold);
        animator.SetBool(AnimIsJumping, !isGrounded);
        animator.SetBool(AnimIsInShoe, inShoe);
    }

    // --- AUDIO ---

    private void UpdateWalkSound()
    {
        if (walkAudioSource == null || walkClip == null) return;

        // Cammina solo se: si sta muovendo abbastanza, è a terra, non è nella scarpa e non è knockback
        bool shouldWalk = Mathf.Abs(moveInput) > moveAnimThreshold && isGrounded && !inShoe;

        if (shouldWalk && !walkAudioSource.isPlaying)
        {
            walkAudioSource.Play();
        }
        else if (!shouldWalk && walkAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
        }
    }

    private void StopWalkSound()
    {
        if (walkAudioSource != null && walkAudioSource.isPlaying)
            walkAudioSource.Stop();
    }

    private void PlayJumpSound()
    {
        if (audioSource != null && jumpClip != null)
            audioSource.PlayOneShot(jumpClip);
    }

    private void PlayDeathSound()
    {
        if (audioSource != null && deathClip != null)
            audioSource.PlayOneShot(deathClip);
        scopaAudioSource.mute = false;
    }

    // --- FINE AUDIO ---

    public void Die()
    {
        DisablePlayer();
        StopWalkSound();
        PlayDeathSound();
        if (animator != null) animator.SetTrigger(AnimDeath);
        if (scopaAnimator != null) scopaAnimator.SetTrigger("PlayerDeath");
        if (fadePanelAnimator != null) fadePanelAnimator.SetTrigger("FadeOut");
        StartCoroutine(RepositionPlayer(this));
    }

    private IEnumerator RepositionPlayer(Player player)
    {
        yield return new WaitForSeconds(repositionTime);
        if (spawnPoint != null)
            player.transform.position = spawnPoint.position;
        else
            player.transform.position = Vector3.zero;
        scopaAudioSource.mute = true;
        player.EnablePlayer();
        player.Alive();
        if (fadePanelAnimator != null) fadePanelAnimator.SetTrigger("FadeIn");
    }

    public void Alive()
    {
        if (animator != null) animator.SetTrigger(AnimAlive);
    }

    private void Jump(float force = -1f)
    {
        if (force < 0f) force = jumpForce;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        PlayJumpSound();
    }

    private void Flip()
    {
        facingRight = !facingRight;
        GetComponentInChildren<SpriteRenderer>().flipX = !GetComponentInChildren<SpriteRenderer>().flipX;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = Color.cyan;
        if (wallCheckRight != null) Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
        if (wallCheckLeft != null) Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
    }

    public void DisablePlayer()
    {
        rb.simulated = false;
    }

    public void EnablePlayer()
    {
        rb.simulated = true;
    }

    public void UpdatePoints()
    {
        if (pointText != null)
            pointText.text = points.ToString();
    }

    public void ApplyKnockback(Vector2 force, float duration)
    {
        // Usa il riferimento rb già presente nell'istanza
        rb.linearVelocity = force;
        isKnockedBack = true;
        knockbackTimer = duration;
    }
}