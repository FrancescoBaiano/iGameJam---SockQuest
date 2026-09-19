using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]

public class SockPrincess : MonoBehaviour
{
    [SerializeField] private string nextLevelName;

    private void Start()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            StartCoroutine(LoadNextLevel(player));
        }
    }

    private IEnumerator LoadNextLevel(Player player)
    {
        player.DisablePlayer();
        yield return new WaitForSeconds(3f);
        SceneManager.LoadScene(nextLevelName);
    }
}
