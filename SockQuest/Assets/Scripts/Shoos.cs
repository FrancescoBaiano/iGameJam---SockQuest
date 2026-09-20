using System.Collections;
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

    private InputAction jumpAction;

    private Player currentPlayer;
    private Rigidbody2D currentPlayerRb;

    private void Awake()
    {
        var map = inputActions.FindActionMap(actionMapName, throwIfNotFound: true);
        jumpAction = map.FindAction(jumpActionName, throwIfNotFound: true);
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

        GetComponent<BoxCollider2D>().enabled = false;

        // Riabilito SOLO l'azione Jump (stessa action usata dal player normalmente),
        // senza toccare il resto della map che il player ha appena disabilitato.
        jumpAction.Enable();
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (currentPlayer != null) StartCoroutine(PlayerExit());
    }

    private IEnumerator PlayerExit()
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

        yield return new WaitForSeconds(1f);
        GetComponent<BoxCollider2D>().enabled = true;
    }
}