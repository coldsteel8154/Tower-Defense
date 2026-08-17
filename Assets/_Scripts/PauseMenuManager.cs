using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager instance;

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject optionsPanel;
    public GameObject confirmEndPanel;

    [Header("Options Panel Sub-components")]
    public Button languageToggleButton;
    public TMP_Text languageToggleText;
    public Slider volumeSlider;
    public Button particleToggleButton;
    public TMP_Text particleToggleText;

    private bool isPaused = false;
    public bool IsPaused => isPaused;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        DifficultySettings.OnVolumeChanged += OnGlobalVolumeChanged;
    }

    private void OnDisable()
    {
        DifficultySettings.OnVolumeChanged -= OnGlobalVolumeChanged;
    }

    private void Start()
    {
        // Ensure EventSystem and Canvas exist
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("Created fallback EventSystem in PauseMenuManager.");
        }

        if (UnityEngine.Object.FindAnyObjectByType<Canvas>() == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            Debug.Log("Created fallback Canvas in PauseMenuManager.");
        }

        AudioListener.volume = DifficultySettings.gameVolume / 100f;
        OnGlobalVolumeChanged(DifficultySettings.gameVolume / 100f);

        // Fallback UI bindings for null variables
        if (pausePanel == null) pausePanel = FindGameObjectInScene("PausePanel");
        if (optionsPanel == null) optionsPanel = FindGameObjectInScene("OptionsPanel") ?? FindGameObjectInScene("PauseOptionsPanel");
        if (confirmEndPanel == null) confirmEndPanel = FindGameObjectInScene("ConfirmEndPanel");
        if (languageToggleButton == null) languageToggleButton = FindButtonInScene("LanguageToggleButton");
        if (particleToggleButton == null) particleToggleButton = FindButtonInScene("ParticleToggleButton");
        if (volumeSlider == null)
        {
            var sliderGo = FindGameObjectInScene("VolumeSlider");
            if (sliderGo != null) volumeSlider = sliderGo.GetComponent<Slider>();
        }

        if (volumeSlider == null && optionsPanel != null)
        {
            Slider[] sliders = optionsPanel.GetComponentsInChildren<Slider>(true);
            foreach (var slider in sliders)
            {
                if (slider.gameObject.name == "VolumeSlider")
                {
                    volumeSlider = slider;
                    break;
                }
            }
            if (volumeSlider == null && sliders.Length > 0)
            {
                volumeSlider = sliders[0];
            }
        }

        pausePanel = ResolvePanelWithNamedChildren(pausePanel, "PausePanel", "PauseResumeButton", "PauseOptionsButton", "PauseEndGameButton");
        optionsPanel = ResolvePanelWithNamedChildren(optionsPanel, "OptionsPanel", "PauseOptionsBackButton");
        confirmEndPanel = ResolvePanelWithNamedChildren(confirmEndPanel, "ConfirmEndPanel", "ConfirmEndYesButton", "ConfirmEndNoButton");

        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (confirmEndPanel != null) confirmEndPanel.SetActive(false);

        Debug.Log("PauseMenuManager panels: pause=" + (pausePanel != null) + ", options=" + (optionsPanel != null) + ", confirmEnd=" + (confirmEndPanel != null));
        if (confirmEndPanel != null)
        {
            var confirmButtons = confirmEndPanel.GetComponentsInChildren<Button>(true);
            Debug.Log("ConfirmEndPanel button count=" + confirmButtons.Length);
            foreach (var btn in confirmButtons)
            {
                Debug.Log("ConfirmEndPanel child button=" + btn.gameObject.name + ", active=" + btn.gameObject.activeSelf + ", scene=" + btn.gameObject.scene.name);
            }
        }

        BindAllButtons();
        SetupOptionsUI();
        StartCoroutine(RetryBindingsCoroutine());
    }

    private IEnumerator RetryBindingsCoroutine()
    {
        // retry a few times with small delays to catch late-initialized UI
        for (int i = 0; i < 5; i++)
        {
            yield return new WaitForSecondsRealtime(0.1f);
            RetryBindEssentialButtons();
        }
    }

    private void RetryBindEssentialButtons()
    {
        // Ensure pause/resume and end-game popup are wired
        Button resume = FindButtonInScene("PauseResumeButton");
        if (resume != null)
        {
            resume.onClick.RemoveAllListeners();
            resume.onClick.AddListener(ResumeGame);
        }

        Button endBtn = FindButtonInScene("PauseEndGameButton");
        if (endBtn != null)
        {
            endBtn.onClick.RemoveAllListeners();
            endBtn.onClick.AddListener(ShowEndGameConfirmation);
        }

        // Ensure confirm panel reference exists and buttons are bound
        if (confirmEndPanel == null)
        {
            confirmEndPanel = FindGameObjectInScene("ConfirmEndPanel");
        }
        BindAllButtons();

    }

    private void Update()
    {
        // Don't pause during command typing or during tutorial UI steps that handle clicks
        if (CheatCommandSystem.instance != null && CheatCommandSystem.instance.commandPanel != null && CheatCommandSystem.instance.commandPanel.activeSelf)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                CloseOptions();
            }
            else if (confirmEndPanel != null && confirmEndPanel.activeSelf)
            {
                HideEndGameConfirmation();
            }
            else
            {
                TogglePause();
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

    private GameObject FindGameObjectInScene(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null) return go;

        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.name == name && obj.scene.IsValid())
            {
                return obj;
            }
        }

        return null;
    }

    private GameObject ResolvePanelWithNamedChildren(GameObject preferredPanel, string panelName, params string[] requiredChildNames)
    {
        if (preferredPanel != null && PanelContainsButtons(preferredPanel, requiredChildNames))
        {
            return preferredPanel;
        }

        GameObject bestCandidate = null;
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.name != panelName || !obj.scene.IsValid()) continue;

            if (bestCandidate == null) bestCandidate = obj;
            if (PanelContainsButtons(obj, requiredChildNames))
            {
                Debug.Log("Resolved " + panelName + " by containing required children: " + obj.name + " (" + obj.GetInstanceID() + ")");
                return obj;
            }
        }

        return bestCandidate;
    }

    private bool PanelContainsButtons(GameObject panel, string[] requiredChildNames)
    {
        if (panel == null) return false;
        foreach (var buttonName in requiredChildNames)
        {
            if (FindButtonInPanel(buttonName, panel) == null)
                return false;
        }
        return true;
    }

    private Button FindButtonInScene(string btnName)
    {
        GameObject namedObj = FindGameObjectInScene(btnName);
        if (namedObj != null)
        {
            Button btn = namedObj.GetComponent<Button>();
            if (btn != null) return btn;
        }

        Button btnInPause = FindButtonInPanel(btnName, pausePanel);
        if (btnInPause != null) return btnInPause;

        Button btnInOptions = FindButtonInPanel(btnName, optionsPanel);
        if (btnInOptions != null) return btnInOptions;

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

    private void BindConfirmEndButtons()
    {
        if (confirmEndPanel == null)
        {
            Debug.LogWarning("ConfirmEndPanel is null when binding confirm buttons.");
            return;
        }

        Button yesButton = FindButtonInPanel("ConfirmEndYesButton", confirmEndPanel);
        if (yesButton != null)
        {
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(() => { Debug.Log("ConfirmEndYesButton clicked"); ConfirmEndGame(); });
            yesButton.interactable = true;
        }
        else
        {
            Debug.LogWarning("Could not find ConfirmEndYesButton inside ConfirmEndPanel.");
        }

        Button noButton = FindButtonInPanel("ConfirmEndNoButton", confirmEndPanel);
        if (noButton != null)
        {
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(() => { Debug.Log("ConfirmEndNoButton clicked"); HideEndGameConfirmation(); });
            noButton.interactable = true;
        }
        else
        {
            Debug.LogWarning("Could not find ConfirmEndNoButton inside ConfirmEndPanel.");
        }
    }

    private void BindButtonInPanel(GameObject panel, string buttonName, System.Action onClickAction)
    {
        if (panel == null)
        {
            Debug.LogWarning("BindButtonInPanel failed because panel is null for " + buttonName);
            return;
        }

        Button btn = FindButtonInPanel(buttonName, panel);
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClickAction?.Invoke());
            btn.interactable = true;
            Debug.Log("Successfully bound panel button " + buttonName + " on " + btn.gameObject.name);
            return;
        }

        Debug.LogWarning("Could not find " + buttonName + " inside " + panel.name + ", falling back to scene search.");
        BindButton(buttonName, onClickAction);
    }

    private void BindButton(string buttonName, System.Action onClickAction)
    {
        Button btn = FindButtonInScene(buttonName);
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClickAction?.Invoke());
            btn.interactable = true;
            Debug.Log("Successfully bound pause button " + buttonName + " on " + btn.gameObject.name);
        }
        else
        {
            Debug.LogWarning("Could not find pause button " + buttonName + " to bind!");
        }
    }

    private void BindAllButtons()
    {
        if (pausePanel != null)
        {
            BindButtonInPanel(pausePanel, "PauseResumeButton", ResumeGame);
            BindButtonInPanel(pausePanel, "PauseOptionsButton", OpenOptions);
            BindButtonInPanel(pausePanel, "PauseEndGameButton", ShowEndGameConfirmation);
            BindButtonInPanel(pausePanel, "PauseOptionsBackButton", CloseOptions);
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: pausePanel is null during BindAllButtons.");
        }

        if (confirmEndPanel != null)
        {
            BindButtonInPanel(confirmEndPanel, "ConfirmEndYesButton", ConfirmEndGame);
            BindButtonInPanel(confirmEndPanel, "ConfirmEndNoButton", HideEndGameConfirmation);
        }
        else
        {
            Debug.LogWarning("PauseMenuManager: confirmEndPanel is null during BindAllButtons.");
            BindButton("ConfirmEndYesButton", ConfirmEndGame);
            BindButton("ConfirmEndNoButton", HideEndGameConfirmation);
        }
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (confirmEndPanel != null) confirmEndPanel.SetActive(false);
    }

    public void OpenOptions()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        SetupOptionsUI();
    }

    public void CloseOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void ShowEndGameConfirmation()
    {
        if (confirmEndPanel != null)
        {
            confirmEndPanel.SetActive(true);
            confirmEndPanel.transform.SetAsLastSibling();
            CanvasGroup group = confirmEndPanel.GetComponent<CanvasGroup>();
            if (group == null) group = confirmEndPanel.AddComponent<CanvasGroup>();
            group.interactable = true;
            group.blocksRaycasts = true;
            BindConfirmEndButtons();
        }
    }

    public void HideEndGameConfirmation()
    {
        Debug.Log("HideEndGameConfirmation called");
        if (confirmEndPanel != null) confirmEndPanel.SetActive(false);
    }

    public void ConfirmEndGame()
    {
        Debug.Log("ConfirmEndGame called");
        HideEndGameConfirmation();
        if (pausePanel != null) pausePanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        // Show defeat or victory panels instead of returning straight to the main menu.
        if (GameManager.instance != null && EnemyManager.main != null)
        {
            int currentWave = EnemyManager.main.wave;
            if (currentWave > 20)
            {
                GameManager.instance.TriggerVictory();
            }
            else
            {
                GameManager.instance.Defeat();
            }
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void SetupOptionsUI()
    {
        if (optionsPanel != null)
        {
            if (volumeSlider == null || !volumeSlider.transform.IsChildOf(optionsPanel.transform))
            {
                volumeSlider = optionsPanel.GetComponentInChildren<Slider>(true);
            }
        }

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
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.value = DifficultySettings.gameVolume;
            volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            Debug.Log("PauseMenuManager bound VolumeSlider with current value " + DifficultySettings.gameVolume);
        }
        else
        {
            Debug.LogWarning("PauseMenuManager could not find VolumeSlider");
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
        DifficultySettings.SetVolume(val);
        Debug.Log("PauseMenuManager volume slider changed to " + val);
    }

    private void OnGlobalVolumeChanged(float normalizedVolume)
    {
        AudioListener.volume = normalizedVolume;

        Canvas canvas = GetOverlayCanvas();
        if (canvas != null)
        {
            AudioSource audioSource = canvas.GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.volume = normalizedVolume;
            }
        }
    }

    private Canvas GetOverlayCanvas()
    {
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }
        }
        return null;
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
}

