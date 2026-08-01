using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CheatCommandSystem : MonoBehaviour
{
    public static CheatCommandSystem instance;
    public static bool isFrozen = false;

    [Header("UI References")]
    public GameObject commandPanel;
    public TMP_InputField commandInput;

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
        if (commandPanel != null)
        {
            commandPanel.SetActive(false);
        }
    }

    private Coroutine helpCoroutine;

    private System.Collections.IEnumerator ShowHelpCoroutine(float seconds)
    {
        // Create or find a help popup in canvas
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) yield break;

        GameObject helpGo = new GameObject("CommandHelpPopup");
        helpGo.transform.SetParent(canvas.transform, false);
        RectTransform rect = helpGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 60f);
        rect.sizeDelta = new Vector2(600f, 80f);

        UnityEngine.UI.Image bg = helpGo.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0f, 0f, 0f, 0.7f);

        var textGo = new GameObject("HelpText");
        textGo.transform.SetParent(helpGo.transform, false);
        var tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.fontSize = 18;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.text = "Commands: /money [add/set/minus] [amount]  /wave [set/jump/revert] [n]  /freeze  /unfreeze  /lives [set/add/minus/kill/immortal]";

        helpGo.transform.SetAsLastSibling();

        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Destroy(helpGo);
        helpCoroutine = null;
    }

    private void Update()
    {
        // Don't show cheats during tutorial mode
        if (DifficultySettings.isTutorial) return;

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (commandPanel != null && !commandPanel.activeSelf)
            {
                OpenCommandPanel();
            }
        }

        if (commandPanel != null && commandPanel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseCommandPanel();
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                OnSubmitCommand(commandInput.text);
                CloseCommandPanel();
            }
        }
    }

    private void OpenCommandPanel()
    {
        commandPanel.SetActive(true);
        commandInput.text = "";
        commandInput.ActivateInputField();
        commandInput.Select();
        // Show help briefly when opening the panel
        if (helpCoroutine != null) StopCoroutine(helpCoroutine);
        helpCoroutine = StartCoroutine(ShowHelpCoroutine(5f));
    }

    private void CloseCommandPanel()
    {
        commandPanel.SetActive(false);
    }

    private void OnSubmitCommand(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        text = text.Trim();
        Debug.Log("Executing Cheat Command: " + text);

        string[] parts = text.Split(' ');
        if (parts.Length == 0) return;

        string cmd = parts[0].ToLower();

        if (cmd == "/money")
        {
            // /money [add/set/minus] [amount]
            // Default is add, so if no mode specified, e.g. "/money 100", parts[1] is amount
            string mode = "add";
            int amount = 0;

            if (parts.Length == 2)
            {
                if (int.TryParse(parts[1], out amount))
                {
                    mode = "add";
                }
            }
            else if (parts.Length >= 3)
            {
                mode = parts[1].ToLower();
                int.TryParse(parts[2], out amount);
            }

            if (GameManager.instance != null)
            {
                if (mode == "add")
                {
                    GameManager.instance.playerMoney += amount;
                }
                else if (mode == "set")
                {
                    GameManager.instance.playerMoney = Mathf.Max(0, amount);
                }
                else if (mode == "minus")
                {
                    GameManager.instance.playerMoney = Mathf.Max(0, GameManager.instance.playerMoney - amount);
                }
                GameManager.instance.UpdateMoneyUI();
            }
        }
        else if (cmd == "/help")
        {
            // Show help UI for 5 seconds
            if (helpCoroutine != null) StopCoroutine(helpCoroutine);
            helpCoroutine = StartCoroutine(ShowHelpCoroutine(5f));
        }
        else if (cmd == "/wave")
        {
            // /wave [jump/set/revert] [wavecount]
            string mode = "set";
            int wavecount = 1;

            if (parts.Length == 2)
            {
                if (int.TryParse(parts[1], out wavecount))
                {
                    mode = "set";
                }
            }
            else if (parts.Length >= 3)
            {
                mode = parts[1].ToLower();
                int.TryParse(parts[2], out wavecount);
            }

            if (EnemyManager.main != null)
            {
                int currentWave = EnemyManager.main.wave;
                int targetWave = currentWave;

                if (mode == "jump")
                {
                    targetWave = currentWave + wavecount;
                }
                else if (mode == "set")
                {
                    targetWave = wavecount;
                }
                else if (mode == "revert")
                {
                    targetWave = currentWave - wavecount;
                }

                targetWave = Mathf.Max(1, targetWave);

                // If going from <= 20 to > 20, trigger victory panel
                if (currentWave <= 20 && targetWave > 20)
                {
                    EnemyManager.main.wave = 20; // Set to 20 so survivedWaveCount displays nicely
                    if (GameManager.instance != null)
                    {
                        GameManager.instance.TriggerVictory();
                    }
                }
                else
                {
                    EnemyManager.main.wave = targetWave;
                    EnemyManager.main.SetWaveForce();
                }
            }
        }
        else if (cmd == "/freeze")
        {
            isFrozen = true;
            Debug.Log("Time frozen!");
        }
        else if (cmd == "/unfreeze")
        {
            isFrozen = false;
            Debug.Log("Time unfrozen!");
        }
        else if (cmd == "/lives")
        {
            // /lives [add/set/minus/kill/immortal] [amount], default to set
            string mode = "set";
            int amount = 1;

            if (parts.Length == 2)
            {
                if (parts[1].ToLower() == "kill")
                {
                    mode = "kill";
                }
                else if (parts[1].ToLower() == "immortal")
                {
                    mode = "immortal";
                }
                else if (int.TryParse(parts[1], out amount))
                {
                    mode = "set";
                }
            }
            else if (parts.Length >= 3)
            {
                mode = parts[1].ToLower();
                int.TryParse(parts[2], out amount);
            }

            if (GameManager.instance != null)
            {
                if (mode == "kill")
                {
                    GameManager.instance.playerLives = 0;
                    GameManager.instance.LoseLife(0); // Trigger defeat check
                }
                else if (mode == "immortal")
                {
                    GameManager.instance.isImmortal = !GameManager.instance.isImmortal;
                    Debug.Log("Immortal: " + GameManager.instance.isImmortal);
                }
                else if (mode == "add")
                {
                    GameManager.instance.playerLives += amount;
                    GameManager.instance.UpdateLivesUI();
                }
                else if (mode == "set")
                {
                    GameManager.instance.playerLives = Mathf.Max(1, amount);
                    GameManager.instance.UpdateLivesUI();
                }
                else if (mode == "minus")
                {
                    GameManager.instance.playerLives = Mathf.Max(0, GameManager.instance.playerLives - amount);
                    GameManager.instance.UpdateLivesUI();
                    if (GameManager.instance.playerLives <= 0)
                    {
                        GameManager.instance.LoseLife(0); // Trigger defeat check
                    }
                }
            }
        }
    }
}
