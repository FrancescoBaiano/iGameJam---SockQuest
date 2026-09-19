using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Shoos : MonoBehaviour
{
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
        player.DisablePlayer();
        player.transform.position = gameObject.transform.position;
        player.EnablePlayer();
        player.EnterShoe(this);
        GetComponent<BoxCollider2D>().enabled = false;
        GetComponentInChildren<SpriteRenderer>().enabled = false;
        gameObject.transform.parent = player.transform;
    }

    public void OnPlayerExited()
    {
        gameObject.transform.parent = null;
        GetComponentInChildren<SpriteRenderer>().enabled = true;
        StartCoroutine(ActivateCollider());
    }

    private IEnumerator ActivateCollider()
    {
        yield return new WaitForSeconds(1f);
        GetComponent<BoxCollider2D>().enabled = true;
    }

    public void ForceExitPlayer(Player player)
    {
        player.ExitShoe();
    }
}