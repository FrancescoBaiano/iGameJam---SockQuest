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
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Controllo muri")]
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private float wallCheckRadius = 0.15f;
    [SerializeField] private LayerMask wallLayer;

    private Rigidbody2D rb;
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
        rb = GetComponent<Rigidbody2D>();

        var map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        moveAction = map.FindAction(moveActionName, throwIfNotFound: true);
        jumpAction = map.FindAction(jumpActionName, throwIfNotFound: true);
    }

    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
        jumpAction.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPerformed;
        moveAction.Disable();
        jumpAction.Disable();
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
            if (isGrounded) Jump();
        }

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();
    }

    private void FixedUpdate()
    {
        float desiredMoveX = moveInput * moveSpeed;

        // Se sto andando verso un muro, azzero la componente orizzontale
        // (funziona anche a mezz'aria, evitando che il player resti "incastrato" contro la parete)
        if (desiredMoveX > 0f && isTouchingWallRight) desiredMoveX = 0f;
        if (desiredMoveX < 0f && isTouchingWallLeft) desiredMoveX = 0f;

        rb.linearVelocity = new Vector2(desiredMoveX, rb.linearVelocity.y);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        GetComponentInChildren<SpriteRenderer>().flipX = !GetComponentInChildren<SpriteRenderer>().flipX;
        //Vector3 scale = transform.localScale;
        //scale.x *= -1f;
        //transform.localScale = scale;
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
        moveAction.Disable();
        jumpAction.Disable();
    }

    public void EnablePlayer()
    {
        rb.simulated = true;
        moveAction.Enable();
        jumpAction.Enable();
    }
}