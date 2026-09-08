using UnityEngine;
using TMPro;

/// HUD panel showing the player's current objective, kept in sync with GameController's
/// Preparation/BlessingSelection/Wave flow and WaveManager's wave/enemy state.
public class ObjectivesPanel : MonoBehaviour
{
    [SerializeField] TMP_Text objectiveText;

    GameController gameController;

    void Awake()
    {
        gameController = FindFirstObjectByType<GameController>();
    }

    // Subscribing here (rather than OnEnable) guarantees WaveManager.Awake has already
    // run and set Instance, since Unity runs Awake on every object before Start on any.
    void Start()
    {
        if (gameController != null) gameController.OnStateChanged += HandleStateChanged;
        if (WaveManager.Instance != null) WaveManager.Instance.OnObjectiveChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (gameController != null) gameController.OnStateChanged -= HandleStateChanged;
        if (WaveManager.Instance != null) WaveManager.Instance.OnObjectiveChanged -= Refresh;
    }

    void HandleStateChanged(GameController.State state)
    {
        Refresh();
    }

    void Refresh()
    {
        if (objectiveText == null || gameController == null || WaveManager.Instance == null) return;
        objectiveText.text = BuildObjectiveText();
    }

    string BuildObjectiveText()
    {
        var wm = WaveManager.Instance;

        if (wm.HasWon) return "Victory! The village is safe.";
        if (wm.HasLost) return "The tower has fallen...";

        switch (gameController.CurrentState)
        {
            case GameController.State.Preparation:
                return "Objective: place your artifacts to defend the tower.";
            case GameController.State.BlessingSelection:
                return "Objective: choose a blessing before the horde arrives.";
            case GameController.State.Wave:
                if (wm.AllWavesFinished && wm.BossTransform != null)
                    return "Objective: defeat the boss guarding the village.";
                if (wm.EnemiesAlive > 0)
                    return $"Objective: defeat the horde ({wm.EnemiesAlive} remaining) - Wave {wm.CurrentWaveIndex + 1}/{wm.TotalWaves}.";
                return "Objective: advance to the next cleansing zone.";
            default:
                return string.Empty;
        }
    }
}
