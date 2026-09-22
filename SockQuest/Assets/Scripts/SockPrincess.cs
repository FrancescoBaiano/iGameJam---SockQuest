using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]

public class SockPrincess : MonoBehaviour
{
    [SerializeField] private string nextLevelName;
    [SerializeField] private float secondsToWait = 4f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip levelCompleteClip;

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

        if (audioSource != null && levelCompleteClip != null)
            audioSource.PlayOneShot(levelCompleteClip);

        player.FadeOut();

        yield return new WaitForSeconds(secondsToWait);
        SceneManager.LoadScene(nextLevelName);
    }
}