using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Wave Config")]
    public bool Autoplay = true;
    [SerializeField] GameObject enemyPrefab;
    [SerializeField] Transform[] spawnPoints;
    [SerializeField] int enemiesInWave = 5;
    [SerializeField] float timeBetweenSpawns = 1.2f;

    [Header("Player")]
    [SerializeField] Health playerHealth;

    [Header("UI")]
    [SerializeField] TMP_Text statusText;
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject losePanel;

    int enemiesAlive;
    bool finished;

    public static bool IsFinished { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        IsFinished = false;
        if (winPanel) winPanel.SetActive(false);
        if (losePanel) losePanel.SetActive(false);
    }

    void Start()
    {
        if (playerHealth != null)
            playerHealth.OnDeath += OnPlayerDeath;

        if (!Autoplay) { return; }
        StartWave();
    }

    public void StartWave()
    {
        SetStatus("Wave starting...");
        StartCoroutine(RunWave());
    }

    IEnumerator RunWave()
    {
        yield return new WaitForSeconds(1.5f);

        enemiesAlive = enemiesInWave;
        SetStatus($"Enemies remaining: {enemiesAlive}");

        for (int i = 0; i < enemiesInWave; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }

    [SerializeField] float spawnScatterRadius = 1.5f;

    void SpawnEnemy()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return;
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Offset aleatorio en XZ para evitar que los enemigos aparezcan apilados
        Vector2 scatter = Random.insideUnitCircle * spawnScatterRadius;
        Vector3 spawnPos = sp.position + new Vector3(scatter.x, 0f, scatter.y);

        Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
    }

    public void RegisterEnemyDeath()
    {
        if (finished) return;
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        SetStatus($"Enemies remaining: {enemiesAlive}");
        if (enemiesAlive == 0)
            StartCoroutine(WaveCleared());
    }

    IEnumerator WaveCleared()
    {
        finished = true;
        IsFinished = true;
        SetStatus("Wave cleared!");
        yield return new WaitForSeconds(1.5f);
        if (winPanel) winPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void OnPlayerDeath()
    {
        if (finished) return;
        finished = true;
        IsFinished = true;
        SetStatus("You died!");
        if (losePanel) losePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void SetStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
        Debug.Log($"[WaveManager] {msg}");
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
    }
}
