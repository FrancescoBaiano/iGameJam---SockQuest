using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckpointTrigger : MonoBehaviour
{
    [SerializeField] private SpawnManager spawnManager;

    [System.Obsolete]
    private void Awake()
    {
        spawnManager = GetComponentInParent<SpawnManager>();

        // Tutti i collider su questo GameObject vengono impostati come trigger
        foreach (Collider2D col in GetComponents<Collider2D>())
        {
            col.isTrigger = true;
        }

        if (spawnManager == null)
        {
            spawnManager = FindObjectOfType<SpawnManager>();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player == null) return;

        if (spawnManager != null)
        {
            spawnManager.SetSpawnPosition(transform.position);
        }
    }
}