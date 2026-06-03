using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    [SerializeField] private int talkativeKillTarget = 2;
    [SerializeField] private int enemyKillTarget = 20;

    [Header("Spawning")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform[] itemSpawnPoints;
    [SerializeField] private int enemiesToSpawn = 20;
    [SerializeField] private int itemsToSpawn = 10;
    [SerializeField] private float preparationDelay = 3f;

    [Header("Audio")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
    [SerializeField] private AudioClip cheatSound;
    [SerializeField, Range(0f, 1f)] private float cheatVolume = 1f;
    [SerializeField] private AudioClip victorySound;
    [SerializeField, Range(0f, 1f)] private float victoryVolume = 1f;
    [SerializeField] private float victorySoundDelay = 1.5f;

    [Header("UI - TextMeshPro")]
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TextMeshProUGUI killCounterText;

    private AudioSource _musicSource;
    private AudioSource _cheatAudioSource;
    private AudioSource _victoryAudioSource;
    private int _talkativeKills = 0;
    private int _enemyKills = 0;
    private int _currentPhase = 0;
    private bool _gameWon = false;
    private bool _showKillCounter = false;
    private List<EnemyController> _enemies = new List<EnemyController>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.loop = true;
        _musicSource.volume = musicVolume;

        _cheatAudioSource = gameObject.AddComponent<AudioSource>();
        _cheatAudioSource.loop = false;
        _cheatAudioSource.volume = cheatVolume;

        _victoryAudioSource = gameObject.AddComponent<AudioSource>();
        _victoryAudioSource.loop = false;
        _victoryAudioSource.volume = victoryVolume;

        if (victoryPanel != null)
            victoryPanel.SetActive(false);

        _showKillCounter = false;
        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            KillAllEnemies();
        }
    }

    public void RegisterTalkativeKill()
    {
        if (_currentPhase >= 1) return;

        _talkativeKills++;

        if (_talkativeKills >= talkativeKillTarget)
        {
            StartPhase2();
        }
    }

    public void RegisterEnemyKill(EnemyController enemy)
    {
        if (_currentPhase != 1) return;

        _enemies.Remove(enemy);
        _enemyKills++;
        UpdateUI();

        if (_enemyKills >= enemyKillTarget)
        {
            WinGame();
        }
    }

    public void RegisterEnemySpawn(EnemyController enemy)
    {
        _enemies.Add(enemy);
    }

    private void StartPhase2()
    {
        _currentPhase = 1;

        if (backgroundMusic != null)
        {
            _musicSource.clip = backgroundMusic;
            _musicSource.Play();
        }
        StartCoroutine(SpawnAfterDelay());
    }

    private IEnumerator SpawnAfterDelay()
    {
        yield return new WaitForSeconds(preparationDelay);

        SpawnEnemies();
        SpawnItems();
        _showKillCounter = true;
        UpdateUI();
    }

    private void KillAllEnemies()
    {
        if (_currentPhase != 1) return;

        if (cheatSound != null && _cheatAudioSource != null)
        {
            _cheatAudioSource.PlayOneShot(cheatSound, cheatVolume);
        }

        var enemiesCopy = new List<EnemyController>(_enemies);

        foreach (EnemyController enemy in enemiesCopy)
        {
            if (enemy != null)
            {
                enemy.TakeDamage(9999);
            }
        }
    }

    private void SpawnEnemies()
    {
        if (enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0) return;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            Vector3 spawnPos = spawnPoint.position + Random.insideUnitSphere * 2f;
            spawnPos.y = spawnPoint.position.y;

            GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.Euler(0, Random.Range(0, 360), 0));
            EnemyController enemyController = enemyObj.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                RegisterEnemySpawn(enemyController);
            }
        }
    }

    private void SpawnItems()
    {
        if (itemPrefab == null || itemSpawnPoints == null || itemSpawnPoints.Length == 0) return;

        for (int i = 0; i < itemsToSpawn; i++)
        {
            Transform spawnPoint = itemSpawnPoints[Random.Range(0, itemSpawnPoints.Length)];
            Vector3 spawnPos = spawnPoint.position + Random.insideUnitSphere * 2f;
            spawnPos.y = spawnPoint.position.y;

            Instantiate(itemPrefab, spawnPos, Quaternion.identity);
        }
    }

    private void WinGame()
    {
        if (_gameWon) return;
        _gameWon = true;
        _currentPhase = 2;

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);

            CanvasGroup cg = victoryPanel.GetComponent<CanvasGroup>();
            if (cg == null) cg = victoryPanel.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.DOFade(1f, 1f);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (victorySound != null)
        {
            StartCoroutine(PlayVictorySoundAfterDelay());
        }

        UpdateUI();
    }

    private IEnumerator PlayVictorySoundAfterDelay()
    {
        yield return new WaitForSeconds(victorySoundDelay);
        _victoryAudioSource.PlayOneShot(victorySound, victoryVolume);
    }

    private void UpdateUI()
    {
        if (killCounterText != null)
        {
            if (_currentPhase == 0)
            {
                killCounterText.gameObject.SetActive(false);
            }
            else if (_currentPhase == 1)
            {
                if (_showKillCounter)
                {
                    killCounterText.text = $"Enemies: {_enemyKills}/{enemyKillTarget}";
                    killCounterText.gameObject.SetActive(true);
                }
                else
                {
                    killCounterText.gameObject.SetActive(false);
                }
            }
            else
            {
                killCounterText.text = "🏆 VICTORY!";
                killCounterText.gameObject.SetActive(true);
            }
        }
    }
}