using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Shoos : MonoBehaviour
{
    private Player currentPlayer;

    private void Start()
    {
        BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
        boxCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            EnterPlayer(player);
        }
    }

    private void EnterPlayer(Player player)
    {
        currentPlayer = player;

        player.DisablePlayer();
        player.transform.position = transform.position;
        player.EnablePlayer();
        player.EnterShoe(this);

        GetComponent<BoxCollider2D>().enabled = false;

        // Lo scarpone si muove in autonomia: è il player a seguirlo come figlio, non il contrario.
        player.transform.parent = transform;
    }

    // Chiamato da Player.ExitShoe() per la SOLA pulizia lato scarpa.
    // NON deve richiamare player.ExitShoe(), altrimenti si crea un loop infinito
    // (ExitShoe -> OnPlayerExited -> ExitShoe -> ...).
    public void OnPlayerExited()
    {
        if (currentPlayer != null)
        {
            currentPlayer.transform.parent = null;
            currentPlayer = null;
        }

        GetComponent<BoxCollider2D>().enabled = true;
    }

    // Se in futuro ti serve forzare l'uscita dall'esterno (es. un trigger, un bottone UI),
    // chiama questo — è lui che avvia la catena, non il contrario.
    public void ForceExitPlayer(Player player)
    {
        player.ExitShoe();
    }
}