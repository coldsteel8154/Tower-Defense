using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Runtime.CompilerServices;
using UnityEngine.UI;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public int playerLives = 10;
    public int playerMoney = 120;
    
    [Header("Panels")]
    public GameObject defeatPanel;
    public GameObject victoryPanel;

    [Header("UI Texts")]
    public TMP_Text lives;
    public TMP_Text money;

    [Header("Gameplay Stats")]
    public float damageDealt = 0f;
    public int shotsFired = 0;
    public int regularSimonKills = 0;
    public int simonKingKills = 0;
    public int ultraSimonKills = 0;

    [Header("Assets & Settings")]
    public GameObject deathParticlePrefab;
    public bool isImmortal = false;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Ensure EventSystem and Canvas exist
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("Created fallback EventSystem in GameManager.");
        }

        if (UnityEngine.Object.FindAnyObjectByType<Canvas>() == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("Created fallback Canvas in GameManager.");
        }

        // Fallback UI bindings for null variables
        if (defeatPanel == null) defeatPanel = GameObject.Find("DefeatPanel");
        if (victoryPanel == null) victoryPanel = GameObject.Find("VictoryPanel");
        if (lives == null)
        {
            var livesGo = GameObject.Find("LivesText") ?? GameObject.Find("Lives");
            if (livesGo != null) lives = livesGo.GetComponent<TMPro.TMP_Text>();
        }
        if (money == null)
        {
            var moneyGo = GameObject.Find("MoneyText") ?? GameObject.Find("Money");
            if (moneyGo != null) money = moneyGo.GetComponent<TMPro.TMP_Text>();
        }

        // Set initial stats based on difficulty!
        ApplyDifficultySettings();

        UpdateLivesUI();
        UpdateMoneyUI();

        // Bind Victory Panel Buttons
        BindButton("VictoryContinueButton", () =>
        {
            if (victoryPanel != null) victoryPanel.SetActive(false);
            Time.timeScale = 1f;
            if (EnemyManager.main != null)
            {
                EnemyManager.main.StartNextWaveAfterVictory();
            }
        });

        BindButton("VictoryBackButton", () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        });

        // Bind Defeat Panel Buttons
        BindButton("DefeatRetryButton", () =>
        {
            Restart();
        });

        BindButton("DefeatBackButton", () =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        });

        // Fallback direct bindings in case canvas-based BindButton missed them
        GameObject tmp;
        tmp = GameObject.Find("DefeatRetryButton");
        if (tmp != null)
        {
            var b = tmp.GetComponent<UnityEngine.UI.Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => Restart());
            }
        }

        tmp = GameObject.Find("DefeatBackButton");
        if (tmp != null)
        {
            var b = tmp.GetComponent<UnityEngine.UI.Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); });
            }
        }

        // Fallback bindings for Victory panel
        tmp = GameObject.Find("VictoryContinueButton");
        if (tmp != null)
        {
            var b = tmp.GetComponent<UnityEngine.UI.Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => {
                    if (victoryPanel != null) victoryPanel.SetActive(false);
                    Time.timeScale = 1f;
                    if (EnemyManager.main != null) EnemyManager.main.StartNextWaveAfterVictory();
                });
            }
        }

        tmp = GameObject.Find("VictoryBackButton");
        if (tmp != null)
        {
            var b = tmp.GetComponent<UnityEngine.UI.Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => { Time.timeScale = 1f; SceneManager.LoadScene("MainMenu"); });
            }
        }
    }

    private Button FindButtonInPanel(string btnName, GameObject panel)
    {
        if (panel == null) return null;
        foreach (var btn in panel.GetComponentsInChildren<Button>(true))
        {
            if (btn.gameObject.name == btnName) return btn;
        }
        return null;
    }

    private Button FindButtonInScene(string btnName)
    {
        GameObject namedObj = GameObject.Find(btnName);
        if (namedObj != null)
        {
            Button btn = namedObj.GetComponent<Button>();
            if (btn != null) return btn;
        }

        Button btnInVictory = FindButtonInPanel(btnName, victoryPanel);
        if (btnInVictory != null) return btnInVictory;

        Button btnInDefeat = FindButtonInPanel(btnName, defeatPanel);
        if (btnInDefeat != null) return btnInDefeat;

        Button fallbackBtn = null;
        foreach (var btn in Resources.FindObjectsOfTypeAll<Button>())
        {
            if (btn.gameObject.name != btnName) continue;

            if (btn.gameObject.scene.IsValid())
            {
                return btn;
            }

            if (fallbackBtn == null)
            {
                fallbackBtn = btn;
            }
        }

        return fallbackBtn;
    }

    private void BindButton(string buttonName, System.Action onClickAction)
    {
        Button btn = FindButtonInScene(buttonName);
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClickAction?.Invoke());
            btn.interactable = true;
            Debug.Log("Successfully bound GameManager button " + buttonName + " on " + btn.gameObject.name);
        }
        else
        {
            Debug.LogWarning("Could not find GameManager button " + buttonName + " to bind!");
        }
    }

    private void ApplyDifficultySettings()
    {
        switch (DifficultySettings.selectedDifficulty)
        {
            case DifficultySettings.Difficulty.Easy:
                playerLives = 20;
                break;
            case DifficultySettings.Difficulty.Normal:
                playerLives = 10;
                break;
            case DifficultySettings.Difficulty.Hard:
                playerLives = 5;
                break;
            case DifficultySettings.Difficulty.Hardcore:
                playerLives = 1;
                playerMoney = 60;
                break;
        }

        if (DifficultySettings.isTutorial)
        {
            playerLives = 20; // Tutorial uses Easy mode difficulty and 20 lives
        }
    }

    public void LoseLife(int dmg)
    {
        if (isImmortal && dmg > 0)
        {
            Debug.Log("Immortal: Blocked " + dmg + " damage.");
            return;
        }

        playerLives -= dmg;
        if (playerLives < 0) playerLives = 0;

        UpdateLivesUI();
        Debug.Log("剩餘生命:" + playerLives);
        if (playerLives <= 0)
        {
            Defeat();
        }
    }

    void Defeat()
    {
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true);
            
            // Populate score on defeat panel
            PopulatePanelStats(defeatPanel, "Defeat");
        }
        Time.timeScale = 0;
    }

    public void TriggerVictory()
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            
            // Populate score on victory panel
            PopulatePanelStats(victoryPanel, "Victory");
        }
        Time.timeScale = 0;
    }

    private void PopulatePanelStats(GameObject panel, string panelType)
    {
        // Find text components and fill them!
        // Expected children in ROUGH-VISIONS: Kills, Damage, Shots Fired, Overall Score, and Title
        TMP_Text[] textComponents = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        int totalKills = regularSimonKills + simonKingKills + ultraSimonKills;
        int completedWaves = EnemyManager.main != null ? EnemyManager.main.wave - 1 : 0;
        if (completedWaves < 0) completedWaves = 0;

        // Calculate score
        float baseScore = 50f + 10f * (regularSimonKills + 2f * simonKingKills + 4f * ultraSimonKills) + 
                          50f * completedWaves * (completedWaves + 1) + (damageDealt / 50f);
        
        float difficultyMultiplier = 1.0f;
        switch (DifficultySettings.selectedDifficulty)
        {
            case DifficultySettings.Difficulty.Easy: difficultyMultiplier = 0.8f; break;
            case DifficultySettings.Difficulty.Normal: difficultyMultiplier = 1.0f; break;
            case DifficultySettings.Difficulty.Hard: difficultyMultiplier = 1.25f; break;
            case DifficultySettings.Difficulty.Hardcore: difficultyMultiplier = 2.0f; break;
        }

        int finalScore = Mathf.RoundToInt(baseScore * difficultyMultiplier);

        foreach (var text in textComponents)
        {
            string name = text.gameObject.name.ToLower();
            if (name.Contains("kill"))
            {
                text.text = "kills: " + totalKills;
            }
            else if (name.Contains("damage"))
            {
                text.text = "damage dealt: " + Mathf.RoundToInt(damageDealt);
            }
            else if (name.Contains("shot"))
            {
                text.text = "shots fired: " + shotsFired;
            }
            else if (name.Contains("score"))
            {
                text.text = "overall score: " + finalScore;
            }
        }
    }

    public void Restart()
    {
        Debug.Log("Restart被按下了!");

        Time.timeScale = 1;
        // Reset cheat freeze state just in case
        CheatCommandSystem.isFrozen = false;
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void UpdateLivesUI()
    {
        if (lives != null)
        {
            lives.text = "Lives:" + playerLives;
        }
    }

    public void UpdateMoneyUI()
    {
        if (money != null)
        {
            money.text = "Money:" + playerMoney;
        }
    }
}


