using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager main;
    public Transform spawnpoint;
    public Transform[] checkpoints;

    [SerializeField] public GameObject enemyPrefab; // Assets/Help/Enemy.prefab

    public int wave = 1;
    public bool wavedone = false;
    [Header("Wave Settings")]
    [SerializeField] private float autoStartDelay = 30f;
    private bool awaitingNextWave = false;
    private float clearTimer = 0f;
    [Header("Spawn Timing")]
    [SerializeField] private float spawnDelayMinMultiplier = 0.1f;
    [SerializeField] private float spawnDelayMaxMultiplier = 2.5f;
    private List<string> waveset = new List<string>();
    private Coroutine spawnCoroutine;
    private bool victoryShownForWave20 = false;

    [Header("UI References")]
    public TMP_Text waveText;

    [Header("BGM Settings")]
    public AudioClip horizonDefendersClip;
    public AudioClip unprecedentedEnemyClip;

    void Awake()
    {
        main = this;
    }
    
    void Start()
    {
        // Enforce delay settings
        autoStartDelay = 30f;
        spawnDelayMinMultiplier = 0.1f;
        spawnDelayMaxMultiplier = 2.5f;

        // Ensure EventSystem and Canvas exist
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("Created fallback EventSystem in EnemyManager.");
        }

        if (UnityEngine.Object.FindAnyObjectByType<Canvas>() == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("Created fallback Canvas in EnemyManager.");
        }

        // Automatically find checkpoints from Path static class if not manually assigned
        if ((checkpoints == null || checkpoints.Length == 0) && Path.path != null)
        {
            checkpoints = Path.path.point;
        }

        // Auto-find spawnpoint if not set
        if (spawnpoint == null && Path.path != null && Path.path.point.Length > 0)
        {
            spawnpoint = Path.path.point[0];
        }

        // Find or create WaveText in Canvas
        if (waveText == null)
        {
            waveText = CreateWaveTextUI();
        }

        UpdateWaveUI();

        // Disable original SpawnManager if it exists so it doesn't conflict!
        var originalSpawnManager = Object.FindAnyObjectByType<SpawnManager>();
        if (originalSpawnManager != null)
        {
            originalSpawnManager.enabled = false;
            Debug.Log("Disabled original SpawnManager to use custom Wave EnemyManager.");
        }

        // Don't auto-start in tutorial mode! TutorialManager will control spawning.
        if (!DifficultySettings.isTutorial)
        {
            SetWave();
        }
    }

    void Update()
    {
        // Skip normal updates in tutorial mode
        if (DifficultySettings.isTutorial) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        // Detect wave end
        if (wavedone && enemies.Length == 0)
        {
            if (wave == 20 && !victoryShownForWave20)
            {
                victoryShownForWave20 = true;
                if (GameManager.instance != null)
                {
                    GameManager.instance.TriggerVictory();
                }
                return;
            }

            // Begin awaiting next wave if not already
            if (!awaitingNextWave)
            {
                awaitingNextWave = true;
                clearTimer = 0f;
            }

            // Update countdown UI (show remaining seconds and Enter hint)
            if (waveText != null)
            {
                int secondsLeft = Mathf.CeilToInt(Mathf.Max(0f, autoStartDelay - clearTimer));
                waveText.text = $"Wave {wave} Cleared!\nStarting in {secondsLeft}s — Press [Enter] to start now";
            }

            // Immediate start via Enter
            if (Input.GetKeyDown(KeyCode.Return))
            {
                awaitingNextWave = false;
                wave++;
                wavedone = false;
                UpdateWaveUI();
                SetWave();
            }

            // Auto-start when timer elapses
            if (awaitingNextWave)
            {
                clearTimer += Time.deltaTime;
                if (clearTimer >= autoStartDelay)
                {
                    awaitingNextWave = false;
                    wave++;
                    wavedone = false;
                    UpdateWaveUI();
                    SetWave();
                }
            }
        }

        // Cheat: Destroy all enemies instantly
        if (Input.GetKeyDown(KeyCode.D) && wavedone)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                Destroy(enemies[i]);
            }
        }
    }

    public void StartNextWaveAfterVictory()
    {
        awaitingNextWave = false;
        clearTimer = 0f;
        wave++;
        wavedone = false;
        UpdateWaveUI();
        SetWave();
    }

    public void SetWaveForce()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        // Destroy all existing enemies
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        for (int i = 0; i < enemies.Length; i++)
        {
            Destroy(enemies[i]);
        }

        wavedone = false;
        awaitingNextWave = false;
        clearTimer = 0f;
        UpdateWaveUI();
        SetWave();
    }

    private void SetWave()
    {
        // Reset next-wave waiting state
        awaitingNextWave = false;
        clearTimer = 0f;

        UpdateBGM();
        waveset.Clear();

        // Calculate enemy count: starts at 10, progressively grows by 2 per wave
        int enemyCount = 10 + (wave - 1) * 2;

        int simonCount = 0;
        int simonkingCount = 0;
        int ultrasimonCount = 0;

        if (wave < 5)
        {
            // Waves 1 to 4: Only Regular Simons
            simonCount = enemyCount;
        }
        else if (wave < 15)
        {
            // Waves 5 to 14: Regular Simons + Simon Kings
            // Simon King ratio starts at 10% and increases by 3% per wave up to max 40%
            float kingRatio = Mathf.Min(0.4f, 0.1f + (wave - 5) * 0.03f);
            simonkingCount = Mathf.RoundToInt(enemyCount * kingRatio);
            simonCount = enemyCount - simonkingCount;
        }
        else
        {
            // Waves 15 and up: Regular Simons + Simon Kings + Ultra Simons
            // Ultra Simon ratio starts at 10% and increases by 4% per wave up to max 40%
            float ultraRatio = Mathf.Min(0.4f, 0.1f + (wave - 15) * 0.04f);
            ultrasimonCount = Mathf.RoundToInt(enemyCount * ultraRatio);
            
            // Simon King ratio remains at 25%
            simonkingCount = Mathf.RoundToInt(enemyCount * 0.25f);
            simonCount = enemyCount - ultrasimonCount - simonkingCount;
        }

        // Assemble waveset list using string identifiers
        for (int i = 0; i < simonCount; i++) waveset.Add("simon");
        for (int i = 0; i < simonkingCount; i++) waveset.Add("simonking");
        for (int i = 0; i < ultrasimonCount; i++) waveset.Add("ultrasimon");

        waveset = Shuffle(waveset);

        spawnCoroutine = StartCoroutine(spawn());
    }
    
    public List<string> Shuffle(List<string> waveSet)
    {
        List<string> temp = new List<string>();
        List<string> result = new List<string>();
        temp.AddRange(waveSet);

        int count = temp.Count;
        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, temp.Count);
            result.Add(temp[index]);
            temp.RemoveAt(index);
        }

        return result;
    }

    private Sprite GetEnemySprite(string typeName)
    {
        if (InitData.level != null && InitData.level.enemy != null)
        {
            foreach (var sprite in InitData.level.enemy)
            {
                if (sprite != null && sprite.name.ToLower() == typeName.ToLower())
                {
                    return sprite;
                }
            }
        }
        return null;
    }

    IEnumerator spawn()
    {
        // Spawning delay scaling: decreases delay as wave increases (i.e. spawning speed increases!)
        float delayFactor = Mathf.Max(0.15f, 1.0f - (wave - 1) * 0.04f);
        
        // Difficulty delay factor
        float difficultyDelayFactor = 1.0f;
        switch (DifficultySettings.selectedDifficulty)
        {
            case DifficultySettings.Difficulty.Easy:
                difficultyDelayFactor = 1.3f; // Slower frequency
                break;
            case DifficultySettings.Difficulty.Normal:
                difficultyDelayFactor = 1.0f;
                break;
            case DifficultySettings.Difficulty.Hard:
                difficultyDelayFactor = 0.75f; // Higher frequency
                break;
            case DifficultySettings.Difficulty.Hardcore:
                difficultyDelayFactor = 0.6f; // Extremely fast
                break;
        }

        float spawnDelayMin = spawnDelayMinMultiplier * delayFactor * difficultyDelayFactor;
        float spawnDelayMax = spawnDelayMaxMultiplier * delayFactor * difficultyDelayFactor;

        // Ensure min <= max and sensible lower bounds
        if (spawnDelayMax < spawnDelayMin)
        {
            float tmp = spawnDelayMax;
            spawnDelayMax = spawnDelayMin;
            spawnDelayMin = tmp;
        }

        spawnDelayMin = Mathf.Max(0.02f, spawnDelayMin);
        spawnDelayMax = Mathf.Max(0.04f, spawnDelayMax);

        // Enemy multipliers
        float waveHealthMultiplier = 1.0f + (wave - 1) * 0.15f;
        float difficultyHealthFactor = 1.0f;
        float difficultySpeedFactor = 1.0f;

        switch (DifficultySettings.selectedDifficulty)
        {
            case DifficultySettings.Difficulty.Easy:
                difficultyHealthFactor = 0.75f;
                difficultySpeedFactor = 0.8f;
                break;
            case DifficultySettings.Difficulty.Normal:
                difficultyHealthFactor = 1.0f;
                difficultySpeedFactor = 1.0f;
                break;
            case DifficultySettings.Difficulty.Hard:
                difficultyHealthFactor = 1.25f;
                difficultySpeedFactor = 1.2f;
                break;
            case DifficultySettings.Difficulty.Hardcore:
                difficultyHealthFactor = 2.0f;
                difficultySpeedFactor = 1.25f;
                break;
        }

        for (int i = 0; i < waveset.Count; i++)
        {
            string enemyType = waveset[i];

            if (enemyPrefab != null)
            {
                // Instantiate the enemy
                Vector3 spawnPos = Vector3.zero;
                if (spawnpoint != null)
                {
                    spawnPos = spawnpoint.position;
                }
                else
                {
                    Debug.LogWarning("EnemyManager.spawn: spawnpoint is null — using Vector3.zero as fallback.");
                }

                GameObject enemyInstance = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                enemyInstance.name = enemyType + "_" + System.DateTime.Now.Ticks;
                enemyInstance.SetActive(true);

                // Set its sprite dynamically
                var sr = enemyInstance.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = GetEnemySprite(enemyType);
                }

                // Set its scaled stats!
                Enemy enemyComp = enemyInstance.GetComponent<Enemy>();
                if (enemyComp != null)
                {
                    // Cache base stats first
                    int baseHealth = 50; // default base health
                    float baseSpeed = 4.0f; // default base speed

                    if (enemyType == "simonking")
                    {
                        baseHealth = 150; // Simon King has higher base health!
                        baseSpeed = 2.0f;
                    }
                    else if (enemyType == "ultrasimon")
                    {
                        baseHealth = 100; // Ultra Simon has higher base health!
                        baseSpeed = 5.0f;
                    }
                    else
                    {
                        baseHealth = 50;
                        baseSpeed = 4.0f;
                    }

                    enemyComp.health = Mathf.RoundToInt(baseHealth * waveHealthMultiplier * difficultyHealthFactor);
                    enemyComp.movespeed = baseSpeed * difficultySpeedFactor;
                    enemyComp.maxHealth = enemyComp.health; // update maxHealth for health bars
                }

                if (SpawnManager.enemy_list != null)
                {
                    SpawnManager.enemy_list.Add(enemyInstance);
                }
            }

            // Wait for scaled delay, but if game is frozen (via cheat), wait and don't spawn
            float delay = Random.Range(spawnDelayMin, spawnDelayMax);
            float elapsed = 0f;
            while (elapsed < delay)
            {
                if (!CheatCommandSystem.isFrozen)
                {
                    elapsed += Time.deltaTime;
                }
                yield return null;
            }
        }

        wavedone = true;
        spawnCoroutine = null;
    }

    public void UpdateWaveUI()
    {
        if (waveText != null)
        {
            waveText.text = $"Wave: {wave}";
        }
    }

    private TMP_Text CreateWaveTextUI()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        // Try to find if there is an existing WaveText first
        Transform existing = canvas.transform.Find("WaveText");
        if (existing != null)
        {
            return existing.GetComponent<TMP_Text>();
        }

        GameObject go = new GameObject("WaveText");
        go.transform.SetParent(canvas.transform, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f); // Top Middle
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -40f);
        rect.sizeDelta = new Vector2(400f, 100f);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        
        // Copy font from any existing TMP text in the scene!
        var existingTmp = Object.FindAnyObjectByType<TextMeshProUGUI>();
        if (existingTmp != null)
        {
            tmp.font = existingTmp.font;
        }

        tmp.fontSize = 32;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        
        // Outline
        tmp.outlineWidth = 0.2f;
        tmp.outlineColor = Color.black;

        return tmp;
    }

    private void UpdateBGM()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        AudioSource audioSource = canvas.GetComponent<AudioSource>();
        if (audioSource != null)
        {
            AudioClip targetClip = wave < 15 ? horizonDefendersClip : unprecedentedEnemyClip;
            if (audioSource.clip != targetClip)
            {
                audioSource.clip = targetClip;
                audioSource.Play();
                Debug.Log("Swapped gameplay BGM dynamically to: " + (targetClip != null ? targetClip.name : "null"));
            }
        }
    }
}


