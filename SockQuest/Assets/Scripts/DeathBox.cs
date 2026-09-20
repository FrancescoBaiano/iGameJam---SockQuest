using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]

public class DeathBox : MonoBehaviour
{
    [SerializeField] Transform SpawnPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null && !player.inShoes)
        {
            StartCoroutine(RepositionPlayer(player));
        }
    }

    private IEnumerator RepositionPlayer(Player player)
    {
        player.DisablePlayer();
        yield return new WaitForSeconds(2f);
        player.transform.position = SpawnPoint.position;
        player.EnablePlayer();
    }
}
