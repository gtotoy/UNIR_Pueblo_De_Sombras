using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [System.Serializable]
    public class WaveData
    {
        public Transform[] spawnPoints;
        public int enemiesInWave = 5;
        public float timeBetweenSpawns = 1.2f;
        [Tooltip("Enclosing sphere marking the zone this wave takes place in. Enabled while this wave is active, disabled once cleared.")]
        public GameObject enclosureSphere;
    }

    [Header("Wave Config")]
    public bool Autoplay = true;
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] WaveData[] waves;
    [SerializeField] float spawnScatterRadius = 1.5f;

    [Header("Boss (spawns once the final wave's enemies are cleared)")]
    [SerializeField] Boss boss;
    [SerializeField] Transform bossSpawnPoint;
    [SerializeField] GameObject bossHealthUI;

    [Header("Player")]
    [SerializeField] Health playerHealth;

    [Header("UI")]
    [SerializeField] TMP_Text statusText;
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;

    int currentWaveIndex;
    int enemiesAlive;
    bool finished;
    readonly List<EnemyController> aliveEnemies = new List<EnemyController>();

    public bool IsFinished { get { return finished; } }
    public bool AllWavesFinished { get { return currentWaveIndex >= waves.Length; } }
    public bool HasWon { get; private set; }
    public bool HasLost { get; private set; }
    public int EnemiesAlive { get { return enemiesAlive; } }
    public int CurrentWaveIndex { get { return currentWaveIndex; } }
    public int TotalWaves { get { return waves.Length; } }
    public IReadOnlyList<EnemyController> AliveEnemies { get { return aliveEnemies; } }

    /// Zone marker for the wave currently active (or about to become active). Used by HUD indicators.
    public Transform CurrentZoneSphere
    {
        get
        {
            if (currentWaveIndex < 0 || currentWaveIndex >= waves.Length) return null;
            var sphere = waves[currentWaveIndex].enclosureSphere;
            return sphere ? sphere.transform : null;
        }
    }

    public Transform BossTransform
    {
        get { return (boss != null && boss.gameObject.activeInHierarchy) ? boss.transform : null; }
    }

    /// Raised whenever the status text changes, so HUD panels can refresh alongside it.
    public event Action OnObjectiveChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        finished = false;
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);

        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i].enclosureSphere)
                waves[i].enclosureSphere.SetActive(i == currentWaveIndex);
        }
    }

    void Start()
    {
        if (playerHealth != null)
            playerHealth.OnDeath += OnPlayerDeath;
        if (boss != null)
            boss.OnDefeated += HandleBossDefeated;

        if (!Autoplay) { return; }
        StartWave();
    }

    public void StartWave()
    {
        if (AllWavesFinished) return;

        finished = false;
        SetStatus("Wave starting...");
        StartCoroutine(RunWave());
    }

    IEnumerator RunWave()
    {
        yield return new WaitForSeconds(1.5f);

        WaveData wave = waves[currentWaveIndex];

        enemiesAlive = wave.enemiesInWave;
        SetStatus($"Enemies remaining: {enemiesAlive}");

        for (int i = 0; i < wave.enemiesInWave; i++)
        {
            SpawnEnemy(wave);
            yield return new WaitForSeconds(wave.timeBetweenSpawns);
        }

        {
            var gameController = FindFirstObjectByType<GameController>();
            if (gameController) {
                gameController.StartArtifactsLifetimes();
            }
        }
    }

    void SpawnEnemy(WaveData wave)
    {
        if (wave.spawnPoints == null || wave.spawnPoints.Length == 0) return;
        Transform sp = wave.spawnPoints[UnityEngine.Random.Range(0, wave.spawnPoints.Length)];

        // Offset aleatorio en XZ para evitar que los enemigos aparezcan apilados
        Vector2 scatter = UnityEngine.Random.insideUnitCircle * spawnScatterRadius;
        Vector3 spawnPos = sp.position + new Vector3(scatter.x, 0f, scatter.y);

        GameObject go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        EnemyController enemy = go.GetComponent<EnemyController>();
        if (enemy != null) aliveEnemies.Add(enemy);
    }

    public void RegisterEnemyDeath(EnemyController enemy)
    {
        aliveEnemies.Remove(enemy);
        if (finished) return;
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        SetStatus($"Enemies remaining: {enemiesAlive}");
        if (enemiesAlive == 0)
        {
            bool isFinalWave = currentWaveIndex == waves.Length - 1;
            StartCoroutine(isFinalWave ? SpawnBoss() : WaveCleared());
        }
    }

    IEnumerator WaveCleared()
    {
        SetStatus("Wave cleared!");
        yield return new WaitForSeconds(1.5f);

        WaveData clearedWave = waves[currentWaveIndex];
        if (clearedWave.enclosureSphere) clearedWave.enclosureSphere.SetActive(false);

        currentWaveIndex++;

        WaveData nextWave = waves[currentWaveIndex];
        if (nextWave.enclosureSphere) nextWave.enclosureSphere.SetActive(true);

        finished = true;
    }

    IEnumerator SpawnBoss()
    {
        SetStatus("The horde is defeated... something stirs.");
        yield return new WaitForSeconds(1.5f);

        WaveData clearedWave = waves[currentWaveIndex];
        if (clearedWave.enclosureSphere) clearedWave.enclosureSphere.SetActive(false);

        currentWaveIndex++;

        if (boss != null)
        {
            if (bossSpawnPoint != null) boss.transform.position = bossSpawnPoint.position;
            boss.gameObject.SetActive(true);
            boss.SetState(Boss.State.Tracking);
            bossHealthUI.SetActive(true);
        }
    }

    void HandleBossDefeated()
    {
        TriggerGameWin("Boss defeated!");
    }

    void OnPlayerDeath()
    {
        TriggerGameLoss("You died!");
    }

    public void TriggerGameLoss(string message)
    {
        if (finished) return;
        finished = true;
        HasLost = true;
        SetStatus(message);
        if (losePanel) losePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void TriggerGameWin(string message)
    {
        if (finished) return;
        finished = true;
        HasWon = true;
        SetStatus(message);
        if (winPanel) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    /// Also used by GameController to route preparation/blessing-phase messages through the same HUD status text.
    public void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log($"[WaveManager] {msg}");
        OnObjectiveChanged?.Invoke();
    }

    // Called by UI buttons
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }

    void OnDestroy()
    {
        if (playerHealth != null)
            playerHealth.OnDeath -= OnPlayerDeath;
        if (boss != null)
            boss.OnDefeated -= HandleBossDefeated;
    }
}
