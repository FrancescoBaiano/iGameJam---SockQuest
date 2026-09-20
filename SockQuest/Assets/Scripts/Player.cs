using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        playerMap = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        moveAction = playerMap.FindAction(moveActionName, throwIfNotFound: true);
        jumpAction = playerMap.FindAction(jumpActionName, throwIfNotFound: true);

        UpdatePoints();
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
        moveInput = moveAction.ReadValue<float>();

        isGrounded = groundCheck != null &&
                     Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

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
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool(AnimIsMoving, Mathf.Abs(moveInput) > moveAnimThreshold);
        animator.SetBool(AnimIsJumping, !isGrounded);
        animator.SetBool(AnimIsInShoe, inShoe);
    }

    public void Die()
    {
        if (animator != null) animator.SetTrigger(AnimDeath);
    }

    public void Alive()
    {
        if (animator != null) animator.SetTrigger(AnimAlive);
    }

    private void FixedUpdate()
    {
        if (inShoe) return;

        float desiredMoveX = moveInput * moveSpeed;

        if (desiredMoveX > 0f && isTouchingWallRight) desiredMoveX = 0f;
        if (desiredMoveX < 0f && isTouchingWallLeft) desiredMoveX = 0f;

        rb.linearVelocity = new Vector2(desiredMoveX, rb.linearVelocity.y);
    }

    private void Jump(float force = -1f)
    {
        if (force < 0f) force = jumpForce;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
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
        pointText.text = points.ToString();
    }
}