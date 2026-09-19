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
    [SerializeField] private string shoeActionMapName = "Scarpa";
    [SerializeField] private string shoeMoveActionName = "Move";
    [SerializeField] private string shoeJumpActionName = "Jump";

    [Header("Movimento")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Scarpa")]
    [SerializeField] private bool inShoes;
    [SerializeField] private float shoeMoveSpeed = 3f;
    [SerializeField] private float shoeExitJumpForce = 12f;

    [Header("Controllo muri")]
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private float wallCheckRadius = 0.15f;
    [SerializeField] private LayerMask wallLayer;

    private Rigidbody2D rb;
    private Shoos currentShoe;

    private InputActionMap playerMap;
    private InputActionMap shoeMap;

    private InputAction playerMoveAction;
    private InputAction jumpAction;
    private InputAction shoeMoveAction;
    private InputAction shoeJumpAction;

    private InputAction activeMoveAction;

    private float moveInput;
    private bool jumpPressed;
    private bool shoeExitJumpPressed;
    private bool isGrounded;
    private bool isTouchingWallRight;
    private bool isTouchingWallLeft;
    private bool facingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        playerMap = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        shoeMap = inputActions.FindActionMap(shoeActionMapName, throwIfNotFound: true);

        playerMoveAction = playerMap.FindAction(moveActionName, throwIfNotFound: true);
        jumpAction = playerMap.FindAction(jumpActionName, throwIfNotFound: true);
        shoeMoveAction = shoeMap.FindAction(shoeMoveActionName, throwIfNotFound: true);
        shoeJumpAction = shoeMap.FindAction(shoeJumpActionName, throwIfNotFound: true);
    }

    private void OnEnable()
    {
        jumpAction.performed += OnJumpPerformed;
        shoeJumpAction.performed += OnShoeJumpPerformed;
        ApplyMovementMode(inShoes);
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPerformed;
        shoeJumpAction.performed -= OnShoeJumpPerformed;
        playerMap.Disable();
        shoeMap.Disable();
    }

    // Da chiamare dallo script che gestisce l'ingresso/uscita dalla scarpa
    public void EnterShoe(Shoos shoe)
    {
        if (inShoes) return;
        currentShoe = shoe;
        inShoes = true;
        ApplyMovementMode(true);
    }

    public void ExitShoe()
    {
        if (!inShoes) return;
        inShoes = false;
        ApplyMovementMode(false);

        // Notifica solo la scarpa per la pulizia (unparent, riabilita collider):
        // OnPlayerExited NON deve richiamare ExitShoe(), altrimenti si crea un loop infinito.
        if (currentShoe != null)
        {
            currentShoe.OnPlayerExited();
            currentShoe = null;
        }
    }

    private void ApplyMovementMode(bool useShoe)
    {
        if (useShoe)
        {
            playerMap.Disable();
            shoeMap.Enable();
            activeMoveAction = shoeMoveAction;
        }
        else
        {
            shoeMap.Disable();
            playerMap.Enable();
            activeMoveAction = playerMoveAction;
        }
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        // La Jump action fa parte solo della map "Player", quindi in scarpa
        // non viene nemmeno invocata (map disabilitata): il controllo su inShoes
        // resta comunque come sicurezza extra.
        if (!inShoes) jumpPressed = true;
    }

    private void OnShoeJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (inShoes) shoeExitJumpPressed = true;
    }

    private void Update()
    {
        moveInput = activeMoveAction.ReadValue<float>();

        isGrounded = groundCheck != null &&
                     Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        isTouchingWallRight = wallCheckRight != null &&
                               Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);
        isTouchingWallLeft = wallCheckLeft != null &&
                              Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);

        if (jumpPressed)
        {
            jumpPressed = false;
            if (isGrounded && !inShoes) Jump();
        }

        if (shoeExitJumpPressed)
        {
            shoeExitJumpPressed = false;
            if (inShoes)
            {
                ExitShoe();
                Jump(shoeExitJumpForce);
            }
        }

        if (moveInput > 0 && !facingRight) Flip();
        else if (moveInput < 0 && facingRight) Flip();
    }

    private void FixedUpdate()
    {
        float currentSpeed = inShoes ? shoeMoveSpeed : moveSpeed;
        float desiredMoveX = moveInput * currentSpeed;

        // Se sto andando verso un muro, azzero la componente orizzontale
        // (funziona anche a mezz'aria, evitando che il player resti "incastrato" contro la parete)
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
        playerMap.Disable();
        shoeMap.Disable();
    }

    public void EnablePlayer()
    {
        rb.simulated = true;
        ApplyMovementMode(inShoes);
    }
}