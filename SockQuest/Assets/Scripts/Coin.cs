using System;
using System.Collections;
using UnityEngine;

[RequireComponent (typeof(CircleCollider2D))]
public class Coin : MonoBehaviour
{
    [SerializeField] private float collectTime = 0.5f;

    private void Start()
    {
        CircleCollider2D collider2D = GetComponent<CircleCollider2D>();
        if (collider2D != null)
        {
            collider2D.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            Collect(player);
        }
    }

    private void Collect(Player player)
    {
        GetComponent<Collider2D>().enabled = false;
        player.points += 1;
        player.UpdatePoints();
        StartCoroutine(HandleDestruction());
    }

    private IEnumerator HandleDestruction()
    {
        yield return new WaitForSeconds(collectTime);
        Destroy(gameObject);
    }
}
