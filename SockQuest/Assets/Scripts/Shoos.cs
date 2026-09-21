using Unity.Multiplayer.PlayMode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(BoxCollider2D))]
public class Shoos : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string actionMapName = "Player";
    [SerializeField] private string jumpActionName = "Jump";

    [Header("Uscita")]
    [SerializeField] private float exitJumpForce = 12f;

    [Header("Movimento autonomo")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Transform wallCheckRight;
    [SerializeField] private Transform wallCheckLeft;
    [SerializeField] private float wallCheckRadius = 0.15f;
    [SerializeField] private LayerMask wallLayer;

    // Nome/hash del parametro dell'Animator
    private static readonly int AnimPlayerInside = Animator.StringToHash("playerInside");

    private InputAction jumpAction;
    private Animator animator;

    private Player currentPlayer;
    private Rigidbody2D currentPlayerRb;

    private int direction = 1; // 1 = destra, -1 = sinistra
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        var map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        jumpAction = map.FindAction(jumpActionName, throwIfNotFound: true);

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        jumpAction.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJumpPerformed;
    }

    private void Start()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Update()
    {
        HandleAutonomousMovement();
    }

    private void HandleAutonomousMovement()
    {
        bool touchingRight = wallCheckRight != null &&
                              Physics2D.OverlapCircle(wallCheckRight.position, wallCheckRadius, wallLayer);
        bool touchingLeft = wallCheckLeft != null &&
                             Physics2D.OverlapCircle(wallCheckLeft.position, wallCheckRadius, wallLayer);

        if (direction > 0 && touchingRight) direction = -1;
        else if (direction < 0 && touchingLeft) direction = 1;

        transform.Translate(Vector2.right * direction * moveSpeed * Time.deltaTime);

        if (spriteRenderer != null) spriteRenderer.flipX = direction < 0;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (currentPlayer != null) return; // già occupata

        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            PlayerEnter(player);
        }
    }

    private void PlayerEnter(Player player)
    {
        currentPlayer = player;
        currentPlayerRb = player.GetComponent<Rigidbody2D>();

        player.DisablePlayer(); // disabilita anche la map "Player" (quindi anche Jump)
        player.transform.position = transform.position;

        // Lo scarpone si muove in autonomia: il player lo segue come figlio.
        player.transform.parent = transform;
        player.GetComponentInChildren<SpriteRenderer>().enabled = false;
        player.inShoe = true;

        // NON disabilito più il BoxCollider2D qui: è lo stesso collider che altri
        // scarponi usano (tramite il layer) per rilevarmi come ostacolo nel loro
        // wallCheck. Disabilitandolo, uno scarpone occupato dal player "spariva"
        // per gli altri, che continuavano ad attraversarlo. La guardia
        // "currentPlayer != null" in OnTriggerEnter2D basta già a evitare un
        // secondo ingresso mentre è occupato.

        // Riabilito SOLO l'azione Jump (stessa action usata dal player normalmente),
        // senza toccare il resto della map che il player ha appena disabilitato.
        jumpAction.Enable();

        if (animator != null) animator.SetBool(AnimPlayerInside, true);
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (currentPlayer != null) PlayerExit();
    }

    private void PlayerExit()
    {
        currentPlayer.GetComponentInChildren<SpriteRenderer>().enabled = true;
        currentPlayer.transform.parent = null;
        currentPlayer.EnablePlayer(); // riabilita l'intera map "Player" (Move + Jump)

        if (currentPlayerRb != null)
        {
            currentPlayer.inShoe = false;
            currentPlayerRb.linearVelocity = new Vector2(currentPlayerRb.linearVelocity.x, 0f);
            currentPlayerRb.AddForce(Vector2.up * exitJumpForce, ForceMode2D.Impulse);
        }

        currentPlayer = null;
        currentPlayerRb = null;

        if (animator != null) animator.SetBool(AnimPlayerInside, false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        if (wallCheckRight != null) Gizmos.DrawWireSphere(wallCheckRight.position, wallCheckRadius);
        if (wallCheckLeft != null) Gizmos.DrawWireSphere(wallCheckLeft.position, wallCheckRadius);
    }
}