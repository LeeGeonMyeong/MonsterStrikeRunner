using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    [SerializeField] private ObstacleMover obstaclePrefab;
    [SerializeField] private PlayerController player;
    [SerializeField] private float spawnInterval = 1.7f;
    [SerializeField] private float minimumSpawnInterval = 1.05f;
    [SerializeField] private float spawnY = 6f;
    private float nextSpawn;
    private int spawned;
    public float CurrentSpawnInterval => Mathf.Lerp(Mathf.Max(2.3f, spawnInterval),
        Mathf.Clamp(minimumSpawnInterval, 0.3f, Mathf.Max(0.3f, spawnInterval)),
        GameManager.Instance != null ? GameManager.Instance.DifficultyProgress : 0f);
    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        if (GameManager.Instance.ElapsedTime < 2f) return;
        nextSpawn -= Time.deltaTime;
        if (nextSpawn > 0f) return;
        nextSpawn = CurrentSpawnInterval;
        int lane = Random.Range(0, 3);
        ObstacleMover monster = Instantiate(obstaclePrefab, new Vector3((lane - 1) * player.LaneDistance, spawnY, 0f), Quaternion.identity);
        spawned++;
        monster.ConfigureHealth(GameManager.Instance.ElapsedTime >= 15f && spawned % 4 == 0 ? 2 : 1);
    }
}
