using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject spawnPoint;

    // Chiamato da ogni CheckpointTrigger quando il player ci entra dentro
    public void SetSpawnPosition(Vector3 position)
    {
        if (spawnPoint == null) return;

        spawnPoint.transform.position = position;
    }
}