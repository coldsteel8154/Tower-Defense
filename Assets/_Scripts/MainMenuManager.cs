using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager instance;

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject difficultyPanel;
    public GameObject optionsPanel;
    public GameObject supportPanel;
    public GameObject quitPopUpPanel;

    [Header("Difficulty Buttons")]
    public Button easyButton;
    public Button normalButton;
    public Button hardButton;
    public Button hardcoreButton; // Red button "hardcore"
    public GameObject playAtYourRiskText; // Text "play at your own risk" under hardcore button
    public Button proceedButton;

    [Header("Options Components")]
    public Button languageToggleButton;
    public TMP_Text languageToggleText;
    public Slider volumeSlider;
    public Button particleToggleButton;
    public TMP_Text particleToggleText;

    [Header("Audio")]
    public AudioSource bgmSource;

    private float lastHardClickTime = -1f;
    private float lastHardcoreClickTime = -1f;
    private const float doubleClickThreshold = 0.3f;

    private List<GameObject> panelHistory = new List<GameObject>();

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        // Ensure EventSystem and Canvas exist
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("Created fallback EventSystem in MainMenuManager.");
        }

        if (UnityEngine.Object.FindAnyObjectByType<Canvas>() == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("Created fallback Canvas in MainMenuManager.");
        }

        // Fallback UI bindings for null variables
        if (mainMenuPanel == null) mainMenuPanel = GameObject.Find("MainMenuPanel") ?? GameObject.Find("MenuPanel");
        if (difficultyPanel == null) difficultyPanel = GameObject.Find("DifficultyPanel");
        if (optionsPanel == null) optionsPanel = GameObject.Find("OptionsPanel");
        if (supportPanel == null) supportPanel = GameObject.Find("SupportPanel");
        if (quitPopUpPanel == null) quitPopUpPanel = GameObject.Find("QuitPopUpPanel") ?? GameObject.Find("QuitPanel");

        if (easyButton == null) easyButton = FindButtonInScene("EasyButton");
        if (normalButton == null) normalButton = FindButtonInScene("NormalButton");
        if (hardButton == null) hardButton = FindButtonInScene("HardButton");
        if (hardcoreButton == null) hardcoreButton = FindButtonInScene("HardcoreButton");
        if (proceedButton == null) proceedButton = FindButtonInScene("ProceedButton");

        if (languageToggleButton == null) languageToggleButton = FindButtonInScene("LanguageToggleButton");
        if (particleToggleButton == null) particleToggleButton = FindButtonInScene("ParticleToggleButton");
        if (volumeSlider == null)
        {
            var sliderGo = GameObject.Find("VolumeSlider");
            if (sliderGo != null) volumeSlider = sliderGo.GetComponent<Slider>();
        }

        // Set up BGM
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.volume = DifficultySettings.gameVolume / 100f;
            bgmSource.Play();
        }

        // Initialize panels: only Main Menu active at start!
        ShowPanel(mainMenuPanel);

        // Bind Main Menu Buttons (using recursive search!)
        BindButton("PlayButton", () => ShowPanel(difficultyPanel));
        BindButton("OptionsButton", () => ShowPanel(optionsPanel));
        BindButton("SupportButton", () => ShowPanel(supportPanel));
        BindButton("QuitButton", () => quitPopUpPanel.SetActive(true));

        // Bind Quit Pop-up Buttons
        BindButton("QuitConfirmYesButton", QuitGame);
        BindButton("QuitConfirmNoButton", () => quitPopUpPanel.SetActive(false));

        // Bind Tutorial Button
        BindButton("TutorialButton", () => {
            GameObject tutorialMgrGo = new GameObject("TutorialManager");
            TutorialManager tm = tutorialMgrGo.AddComponent<TutorialManager>();
            tm.StartTutorial();
        });

        // Bind Back Buttons (using recursive search!)
        BindButton("DifficultyBackButton", GoBack);
        BindButton("OptionsBackButton", GoBack);
        BindButton("SupportBackButton", GoBack);

        // Difficulty Selection setup
        if (easyButton != null) easyButton.onClick.AddListener(() => SelectDifficulty(DifficultySettings.Difficulty.Easy));
        if (normalButton != null) normalButton.onClick.AddListener(() => SelectDifficulty(DifficultySettings.Difficulty.Normal));
        if (hardButton != null) hardButton.onClick.AddListener(OnHardButtonClick);
        if (hardcoreButton != null) hardcoreButton.onClick.AddListener(OnHardcoreButtonClick);
        if (proceedButton != null) proceedButton.onClick.AddListener(ProceedToGame);

        // Initialize hardcore state (hidden by default)
        if (hardcoreButton != null) hardcoreButton.gameObject.SetActive(false);
        if (playAtYourRiskText != null) playAtYourRiskText.SetActive(false);

        // Defaults to Normal
        SelectDifficulty(DifficultySettings.Difficulty.Normal);

        // Options Setup
        SetupOptionsUI();

        // Fallback: ensure main-menu buttons are bound even if BindButton misses them
        GameObject tmp;
        tmp = GameObject.Find("PlayButton");
        if (tmp != null)
        {
            var b = tmp.GetComponent<UnityEngine.UI.Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ShowPanel(difficultyPanel));
            }
        }

        tmp = GameObject.Find("OptionsButton");
        if (tmp != null)
        {
            var b = tmp.GetComponent<UnityEngine.UI.Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => ShowPanel(optionsPanel));
            }
        }

        tmp = GameObject.Find("TutorialButton");
        if (tmp != null)
        {
            var b = tmp.GetComponent<UnityEngine.UI.Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => {
                    GameObject tutorialMgrGo = new GameObject("TutorialManager");
                    TutorialManager tm = tutorialMgrGo.AddComponent<TutorialManager>();
                    tm.StartTutorial();
                });
            }
        }

        tmp = GameObject.Find("QuitButton");
        if (tmp != null)
        {
            var b = tmp.GetComponent<UnityEngine.UI.Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => quitPopUpPanel.SetActive(true));
            }
        }

        tmp = GameObject.Find("QuitConfirmYesButton");
        if (tmp != null)
        {
            var b = tmp.GetComponent<UnityEngine.UI.Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(QuitGame);
            }
        }

        tmp = GameObject.Find("QuitConfirmNoButton");
        if (tmp != null)
        {
            var b = tmp.GetComponent<UnityEngine.UI.Button>();
            if (b != null)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => quitPopUpPanel.SetActive(false));
            }
        }

        // Check if coming back from game to tutorial or regular
        DifficultySettings.isTutorial = false;
    }

    private void Update()
    {
        // Back/Escape support
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (quitPopUpPanel != null && quitPopUpPanel.activeSelf)
            {
                quitPopUpPanel.SetActive(false);
            }
            else if (panelHistory.Count > 1)
            {
                GoBack();
            }
        }
    }

    private Button FindButtonInScene(string btnName)
    {
        GameObject namedObj = GameObject.Find(btnName);
        if (namedObj != null)
        {
            Button btn = namedObj.GetComponent<Button>();
            if (btn != null) return btn;
        }

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
            Debug.Log("Successfully bound " + buttonName);
        }
        else
        {
            Debug.LogWarning("Could not find button " + buttonName + " to bind!");
        }
    }

    public void ShowPanel(GameObject targetPanel)
    {
        if (targetPanel == null) return;

        // Hide current if any
        if (panelHistory.Count > 0)
        {
            panelHistory[panelHistory.Count - 1].SetActive(false);
        }

        panelHistory.Add(targetPanel);
        targetPanel.SetActive(true);
    }

    public void GoBack()
    {
        if (panelHistory.Count <= 1) return;

        GameObject current = panelHistory[panelHistory.Count - 1];
        current.SetActive(false);
        panelHistory.RemoveAt(panelHistory.Count - 1);

        GameObject previous = panelHistory[panelHistory.Count - 1];
        previous.SetActive(true);
    }

    private void SelectDifficulty(DifficultySettings.Difficulty diff)
    {
        DifficultySettings.selectedDifficulty = diff;
        Debug.Log("Selected Difficulty: " + diff);

        // Visually highlight buttons if they exist
        HighlightButton(easyButton, diff == DifficultySettings.Difficulty.Easy);
        HighlightButton(normalButton, diff == DifficultySettings.Difficulty.Normal);
        HighlightButton(hardButton, diff == DifficultySettings.Difficulty.Hard);
        HighlightButton(hardcoreButton, diff == DifficultySettings.Difficulty.Hardcore);
    }

    private void HighlightButton(Button btn, bool selected)
    {
        if (btn == null) return;
        ColorBlock cb = btn.colors;
        cb.normalColor = selected ? new Color(0.7f, 1f, 0.7f, 1f) : Color.white;
        cb.selectedColor = cb.normalColor;
        btn.colors = cb;
    }

    private void OnHardButtonClick()
    {
        float timeSinceLastClick = Time.time - lastHardClickTime;
        if (timeSinceLastClick <= doubleClickThreshold)
        {
            // Toggle to Hardcore Mode!
            if (hardButton != null) hardButton.gameObject.SetActive(false);
            if (hardcoreButton != null) hardcoreButton.gameObject.SetActive(true);
            if (playAtYourRiskText != null) playAtYourRiskText.SetActive(true);

            SelectDifficulty(DifficultySettings.Difficulty.Hardcore);
        }
        else
        {
            SelectDifficulty(DifficultySettings.Difficulty.Hard);
        }
        lastHardClickTime = Time.time;
    }

    private void OnHardcoreButtonClick()
    {
        float timeSinceLastClick = Time.time - lastHardcoreClickTime;
        if (timeSinceLastClick <= doubleClickThreshold)
        {
            // Toggle back to Hard Mode!
            if (hardcoreButton != null) hardcoreButton.gameObject.SetActive(false);
            if (playAtYourRiskText != null) playAtYourRiskText.SetActive(false);
            if (hardButton != null) hardButton.gameObject.SetActive(true);

            SelectDifficulty(DifficultySettings.Difficulty.Hard);
        }
        else
        {
            SelectDifficulty(DifficultySettings.Difficulty.Hardcore);
        }
        lastHardcoreClickTime = Time.time;
    }

    private void ProceedToGame()
    {
        SceneManager.LoadScene("Help"); // The main game scene is in "Help" as requested!
    }

    private void SetupOptionsUI()
    {
        // Language setup
        if (languageToggleButton != null)
        {
            languageToggleButton.onClick.RemoveAllListeners();
            languageToggleButton.onClick.AddListener(ToggleLanguage);
            UpdateLanguageText();
        }

        // Volume setup
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 100f;
            volumeSlider.value = DifficultySettings.gameVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
        }

        // Particle toggle setup
        if (particleToggleButton != null)
        {
            particleToggleButton.onClick.RemoveAllListeners();
            particleToggleButton.onClick.AddListener(OnParticleTogglePressed);
            UpdateParticleText();
        }
    }

    private void ToggleLanguage()
    {
        if (DifficultySettings.selectedLanguage == DifficultySettings.Language.English)
        {
            DifficultySettings.SetLanguage(DifficultySettings.Language.TraditionalChinese);
        }
        else
        {
            DifficultySettings.SetLanguage(DifficultySettings.Language.English);
        }
        UpdateLanguageText();
    }

    private void UpdateLanguageText()
    {
        if (languageToggleText != null)
        {
            languageToggleText.text = DifficultySettings.selectedLanguage == DifficultySettings.Language.English 
                ? "Language: English" 
                : "語言: 繁體中文";
        }
    }

    private void OnVolumeSliderChanged(float val)
    {
        DifficultySettings.gameVolume = val;
        if (bgmSource != null)
        {
            bgmSource.volume = val / 100f;
        }
    }

    private void OnParticleTogglePressed()
    {
        if (DifficultySettings.particleSetting == "All")
        {
            DifficultySettings.particleSetting = "Less";
        }
        else if (DifficultySettings.particleSetting == "Less")
        {
            DifficultySettings.particleSetting = "Least";
        }
        else
        {
            DifficultySettings.particleSetting = "All";
        }
        UpdateParticleText();
    }

    private void UpdateParticleText()
    {
        if (particleToggleText != null)
        {
            particleToggleText.text = DifficultySettings.selectedLanguage == DifficultySettings.Language.English
                ? "Particles: " + DifficultySettings.particleSetting
                : "粒子特效: " + (DifficultySettings.particleSetting == "All" ? "全部" : (DifficultySettings.particleSetting == "Less" ? "較少" : "最少"));
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

