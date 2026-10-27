using UnityEngine;

public class RunnerItemSpawner : MonoBehaviour
{
    private float nextAt = 6f;
    public void ResetRun() { nextAt = 6f; RunnerItem.Clear(); }
    private void Update()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || !manager.IsPlaying || manager.ElapsedTime < nextAt) return;
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;
        RunnerItem.Kind kind = RunnerItem.Kind.Heal;
        RunnerItem.Create(kind, new Vector3((Random.Range(0, 3) - 1) * player.LaneDistance, 6f, 0f));
        nextAt += 8f;
    }
}
