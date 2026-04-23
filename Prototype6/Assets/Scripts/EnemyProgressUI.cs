using TMPro;
using UnityEngine;

// Displays "X/N enemies remaining" in gameplay. N is the total enemy count for the stage
// (sum of StageData.enemiesPerWave); X starts at N and decrements on each kill.
public class EnemyProgressUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI progressText;

    [Header("Format")]
    public string prefix = "";
    public string suffix = " enemies remaining";

    private int totalEnemies;
    private int killedEnemies;
    private bool subscribed;

    void Start()
    {
        ComputeTotal();
        TrySubscribe();
        Refresh();
    }

    void Update()
    {
        // EnemySpawner.Configure() may run after our Start() depending on script execution
        // order, so retry both total calculation and event subscription until they succeed.
        if (totalEnemies == 0)
            ComputeTotal();
        if (!subscribed)
            TrySubscribe();
    }

    void ComputeTotal()
    {
        // Prefer the authoritative source (StageData via SceneFlowManager). Fall back to the
        // spawner's live arrays for edit-mode / standalone scene testing.
        float[] perWave = null;

        if (SceneFlowManager.Instance != null && SceneFlowManager.Instance.SelectedStage != null)
            perWave = SceneFlowManager.Instance.SelectedStage.enemiesPerWave;
        else if (EnemySpawner.Instance != null)
            perWave = EnemySpawner.Instance.enemiesPerStage;

        if (perWave == null) return;

        int sum = 0;
        foreach (float count in perWave)
            sum += Mathf.FloorToInt(count);

        if (sum > 0)
            totalEnemies = sum;
    }

    void TrySubscribe()
    {
        if (subscribed || EnemySpawner.Instance == null) return;
        EnemySpawner.Instance.OnEnemyKilled += HandleKill;
        subscribed = true;
    }

    void OnDestroy()
    {
        if (subscribed && EnemySpawner.Instance != null)
            EnemySpawner.Instance.OnEnemyKilled -= HandleKill;
    }

    void HandleKill()
    {
        killedEnemies++;
        Refresh();
    }

    void Refresh()
    {
        if (progressText == null) return;
        int remaining = Mathf.Max(0, totalEnemies - killedEnemies);
        progressText.text = $"{prefix}{remaining}/{totalEnemies}{suffix}";
    }
}
