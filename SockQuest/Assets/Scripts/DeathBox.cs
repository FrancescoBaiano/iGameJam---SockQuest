using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]

public class DeathBox : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null && !player.inShoe)
        {
            player.Die();
        }
    }    
}
