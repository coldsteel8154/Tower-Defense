using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager instance;

    public enum TutorialStep
    {
        None,
        Welcome_ClickPlay,
        DifficultyIntro_Step,
        ClickEasy,
        ClickProceed,
        GameplayWelcome,
        ExplainSidebar,
        PlaceTowerGuidance,
        SpawnWave,
        WaveActive,
        Congratulations
    }

    [Header("Current Progress")]
    public TutorialStep currentStep = TutorialStep.None;

    [Header("UI Canvas & Elements")]
    private GameObject tutorialCanvas;
    private GameObject dialogueBox;
    private TMP_Text dialogueText;
    private GameObject arrowIndicator;
    private Button endTutorialButton;
    private GameObject confirmEndPopUp;

    private Button targetButton;
    private bool stepClickToAdvance = false;

    private int difficultyDialogueSubStep = 0;
    private string[] difficultyDialogues = {
        "This is the Difficulty Selection screen where you select how challenging the enemy invasion will be.",
        "<b>Easy Mode</b>: Enemies have lower health (75%), move slower (80%), and spawn with lower frequency. You also get 20 lives!",
        "<b>Normal Mode</b>: The default balanced settings with 10 player lives.",
        "<b>Hard Mode</b>: Enemies spawn faster, move 20% quicker, have 1.25x health, and you have only 5 lives.",
        "<b>Hardcore Mode</b>: Secret mode activated by double-clicking Hard! 1 life, half reward money, 2x enemy health.",
        "For this tutorial, let's play on Easy Mode."
    };

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    public void StartTutorial()
    {
        DifficultySettings.isTutorial = true;
        currentStep = TutorialStep.Welcome_ClickPlay;
        
        CreateTutorialCanvas();
        SetupEndTutorialButton();
        
        RunStep();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!DifficultySettings.isTutorial) return;

        if (scene.name == "Help")
        {
            // We loaded the gameplay scene! Re-create/re-bind the Canvas elements in the new scene!
            CreateTutorialCanvas();
            SetupEndTutorialButton();

            currentStep = TutorialStep.GameplayWelcome;
            RunStep();
        }
        else if (scene.name == "SampleScene")
        {
            // Just in case SampleScene is loaded, treat it similarly
            CreateTutorialCanvas();
            SetupEndTutorialButton();

            currentStep = TutorialStep.GameplayWelcome;
            RunStep();
        }
    }

    private void Update()
    {
        if (currentStep == TutorialStep.None) return;

        // Mask clicking: block clicks if clicking outside targetButton or dialogue panel
        if (Input.GetMouseButtonDown(0))
        {
            HandleScreenClick();
        }

        // Keep arrow indicator floating
        if (arrowIndicator != null && arrowIndicator.activeSelf && targetButton != null)
        {
            // Point the arrow just above/near the target button
            arrowIndicator.transform.position = targetButton.transform.position + new Vector3(0, 50f + Mathf.Sin(Time.unscaledTime * 8f) * 10f, 0);
        }

        // Check if wave is done in tutorial mode
        if (currentStep == TutorialStep.WaveActive)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length == 0 && EnemyManager.main != null && EnemyManager.main.wavedone)
            {
                currentStep = TutorialStep.Congratulations;
                RunStep();
            }
        }
    }

    private void HandleScreenClick()
    {
        // If clicking on confirmation popup, don't interfere
        if (confirmEndPopUp != null && confirmEndPopUp.activeSelf) return;

        // If clicking on End Tutorial button, don't interfere
        if (RectTransformUtility.RectangleContainsScreenPoint(endTutorialButton.GetComponent<RectTransform>(), Input.mousePosition))
        {
            return;
        }

        // If targetButton is active and clicked, let it go through
        if (targetButton != null && targetButton.gameObject.activeInHierarchy)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(targetButton.GetComponent<RectTransform>(), Input.mousePosition))
            {
                // Proceed or let click execute
                if (currentStep == TutorialStep.Welcome_ClickPlay)
                {
                    StartCoroutine(WaitAndProceedStep(TutorialStep.DifficultyIntro_Step, 0.1f));
                }
                else if (currentStep == TutorialStep.ClickEasy)
                {
                    StartCoroutine(WaitAndProceedStep(TutorialStep.ClickProceed, 0.1f));
                }
                else if (currentStep == TutorialStep.PlaceTowerGuidance)
                {
                    // Proceed when tower placement starts
                    StartCoroutine(WaitAndProceedStep(TutorialStep.SpawnWave, 0.1f));
                }
                return;
            }
        }

        // If dialogue advancement is enabled, advance dialogue on click
        if (stepClickToAdvance && dialogueBox != null && dialogueBox.activeSelf)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(dialogueBox.GetComponent<RectTransform>(), Input.mousePosition))
            {
                AdvanceDialogue();
            }
        }
    }

    private IEnumerator WaitAndProceedStep(TutorialStep next, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        currentStep = next;
        RunStep();
    }

    private void RunStep()
    {
        HideArrow();
        stepClickToAdvance = false;
        targetButton = null;

        if (dialogueBox != null) dialogueBox.SetActive(true);

        switch (currentStep)
        {
            case TutorialStep.Welcome_ClickPlay:
                SetDialogue("Welcome to Simon Universe! Let's start by clicking the <b>PLAY</b> button to choose your difficulty.");
                FindAndPointToButton("PlayButton");
                break;

            case TutorialStep.DifficultyIntro_Step:
                difficultyDialogueSubStep = 0;
                stepClickToAdvance = true;
                SetDialogue(difficultyDialogues[difficultyDialogueSubStep] + "\n\n<i>[Click this panel to continue]</i>");
                break;

            case TutorialStep.ClickEasy:
                SetDialogue("Now, let's select <b>EASY</b> mode for our tutorial run.");
                // Find Easy button
                if (MainMenuManager.instance != null && MainMenuManager.instance.easyButton != null)
                {
                    targetButton = MainMenuManager.instance.easyButton;
                    ShowArrowAtButton(targetButton);
                }
                break;

            case TutorialStep.ClickProceed:
                SetDialogue("Great choice! Click the <b>START GAME</b> button below to begin.");
                if (MainMenuManager.instance != null && MainMenuManager.instance.proceedButton != null)
                {
                    targetButton = MainMenuManager.instance.proceedButton;
                    ShowArrowAtButton(targetButton);
                }
                break;

            case TutorialStep.GameplayWelcome:
                Time.timeScale = 0f; // Pause game initially
                stepClickToAdvance = true;
                SetDialogue("Welcome to the battlefield! The path is laid out, and Simons will start invading soon.\n\n<i>[Click this panel to continue]</i>");
                break;

            case TutorialStep.ExplainSidebar:
                stepClickToAdvance = true;
                SetDialogue("On the right sidebar, you can buy different defense towers: Soldier, Assault, and Sniper.\nEach has its unique range, fire rate, and damage.\n\n<i>[Click this panel to continue]</i>");
                break;

            case TutorialStep.PlaceTowerGuidance:
                SetDialogue("Let's place a tower! Open the sidebar (if closed), hover over a tower button to see its stats, then click and place a tower near the path.");
                // Point arrow to buy button in Content
                GameObject sidebarObj = GameObject.Find("TowerSidebarPanel");
                if (sidebarObj != null)
                {
                    Button sidebarBuyBtn = sidebarObj.GetComponentInChildren<Button>();
                    if (sidebarBuyBtn != null)
                    {
                        targetButton = sidebarBuyBtn;
                        ShowArrowAtButton(targetButton);
                    }
                }
                break;

            case TutorialStep.SpawnWave:
                Time.timeScale = 1f; // Unpause
                SetDialogue("Awesome! A wave of 3 basic Simons is coming. Watch your tower defend the line!");
                stepClickToAdvance = true;
                
                // Spawn a tiny wave of 3 Simons for the tutorial!
                if (EnemyManager.main != null)
                {
                    EnemyManager.main.StartNextWaveAfterVictory(); // Starts spawning wave
                }
                break;

            case TutorialStep.WaveActive:
                if (dialogueBox != null) dialogueBox.SetActive(false);
                break;

            case TutorialStep.Congratulations:
                Time.timeScale = 0f; // Pause
                stepClickToAdvance = true;
                SetDialogue("Congratulations! You've successfully completed the tutorial.\nYou are now ready to protect the universe from the Simon threat!");
                break;
        }
    }

    private void AdvanceDialogue()
    {
        if (currentStep == TutorialStep.DifficultyIntro_Step)
        {
            difficultyDialogueSubStep++;
            if (difficultyDialogueSubStep < difficultyDialogues.Length)
            {
                SetDialogue(difficultyDialogues[difficultyDialogueSubStep] + "\n\n<i>[Click this panel to continue]</i>");
            }
            else
            {
                currentStep = TutorialStep.ClickEasy;
                RunStep();
            }
        }
        else if (currentStep == TutorialStep.GameplayWelcome)
        {
            currentStep = TutorialStep.ExplainSidebar;
            RunStep();
        }
        else if (currentStep == TutorialStep.ExplainSidebar)
        {
            currentStep = TutorialStep.PlaceTowerGuidance;
            RunStep();
        }
        else if (currentStep == TutorialStep.SpawnWave)
        {
            currentStep = TutorialStep.WaveActive;
            RunStep();
        }
        else if (currentStep == TutorialStep.Congratulations)
        {
            EndTutorial();
        }
    }

    private void FindAndPointToButton(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
        {
            targetButton = go.GetComponent<Button>();
            if (targetButton != null)
            {
                ShowArrowAtButton(targetButton);
            }
        }
    }

    private void ShowArrowAtButton(Button btn)
    {
        if (arrowIndicator == null) CreateArrow();
        if (arrowIndicator != null)
        {
            arrowIndicator.SetActive(true);
            arrowIndicator.transform.position = btn.transform.position + new Vector3(0, 50f, 0);
        }
    }

    private void HideArrow()
    {
        if (arrowIndicator != null)
        {
            arrowIndicator.SetActive(false);
        }
    }

    private void SetDialogue(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = text;
        }
    }

    private void CreateTutorialCanvas()
    {
        // Instantiate/Setup Canvas
        tutorialCanvas = GameObject.Find("TutorialCanvas");
        if (tutorialCanvas == null)
        {
            tutorialCanvas = new GameObject("TutorialCanvas");
            Canvas canvas = tutorialCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Top of everything!

            CanvasScaler scaler = tutorialCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            tutorialCanvas.AddComponent<GraphicRaycaster>();
        }

        // Dialogue Box
        dialogueBox = tutorialCanvas.transform.Find("DialogueBox")?.gameObject;
        if (dialogueBox == null)
        {
            dialogueBox = new GameObject("DialogueBox");
            dialogueBox.transform.SetParent(tutorialCanvas.transform, false);

            RectTransform rect = dialogueBox.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.7f); // Near top-middle
            rect.anchorMax = new Vector2(0.5f, 0.9f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600, 150);

            UnityEngine.UI.Image img = dialogueBox.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

            // Dialogue Outline
            GameObject outline = new GameObject("Outline");
            outline.transform.SetParent(dialogueBox.transform, false);
            RectTransform oRect = outline.AddComponent<RectTransform>();
            oRect.anchorMin = Vector2.zero;
            oRect.anchorMax = Vector2.one;
            oRect.sizeDelta = new Vector2(4, 4);
            UnityEngine.UI.Image oImg = outline.AddComponent<UnityEngine.UI.Image>();
            oImg.color = new Color(0.3f, 0.6f, 1f, 1f);
            outline.transform.SetAsFirstSibling();

            // Text
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(dialogueBox.transform, false);
            RectTransform tRect = textGo.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-40, -40);

            dialogueText = textGo.AddComponent<TextMeshProUGUI>();
            dialogueText.fontSize = 24;
            dialogueText.color = Color.white;
            dialogueText.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            dialogueText = dialogueBox.GetComponentInChildren<TextMeshProUGUI>();
        }

        // Arrow
        arrowIndicator = tutorialCanvas.transform.Find("Arrow")?.gameObject;
        if (arrowIndicator == null)
        {
            CreateArrow();
        }
    }

    private void CreateArrow()
    {
        if (tutorialCanvas == null) return;

        arrowIndicator = new GameObject("Arrow");
        arrowIndicator.transform.SetParent(tutorialCanvas.transform, false);
        
        RectTransform rect = arrowIndicator.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(40, 50);

        UnityEngine.UI.Image img = arrowIndicator.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(1.0f, 0.85f, 0.1f, 1f); // Beautiful yellow pointer arrow!
        
        // Generate a simple procedural downwards triangle sprite for the arrow!
        Texture2D tex = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                if (y >= x && y >= (31 - x))
                {
                    tex.SetPixel(x, y, Color.white);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        img.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0f));
        arrowIndicator.SetActive(false);
    }

    private void SetupEndTutorialButton()
    {
        if (tutorialCanvas == null) return;

        Transform existingBtn = tutorialCanvas.transform.Find("EndTutorialButton");
        if (existingBtn != null)
        {
            endTutorialButton = existingBtn.GetComponent<Button>();
            return;
        }

        // Create End Tutorial Button
        GameObject btnGo = new GameObject("EndTutorialButton");
        btnGo.transform.SetParent(tutorialCanvas.transform, false);

        RectTransform rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f); // Bottom right
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-40f, 40f);
        rect.sizeDelta = new Vector2(180, 50);

        UnityEngine.UI.Image img = btnGo.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Red button

        Button btn = btnGo.AddComponent<Button>();
        endTutorialButton = btn;

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(btnGo.transform, false);
        RectTransform tRect = textGo.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;

        TMP_Text txt = textGo.AddComponent<TextMeshProUGUI>();
        txt.text = "End Tutorial";
        txt.fontSize = 18;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        // Click event
        btn.onClick.AddListener(ShowConfirmEndPopUp);

        // Setup confirm popup
        CreateConfirmEndPopUp();
    }

    private void CreateConfirmEndPopUp()
    {
        if (tutorialCanvas == null) return;

        confirmEndPopUp = new GameObject("ConfirmEndPopUp");
        confirmEndPopUp.transform.SetParent(tutorialCanvas.transform, false);
        confirmEndPopUp.SetActive(false);

        RectTransform rect = confirmEndPopUp.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(400, 200);

        UnityEngine.UI.Image img = confirmEndPopUp.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.15f, 0.15f, 0.18f, 0.98f);

        // Outline
        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(confirmEndPopUp.transform, false);
        RectTransform oRect = outline.AddComponent<RectTransform>();
        oRect.anchorMin = Vector2.zero;
        oRect.anchorMax = Vector2.one;
        oRect.sizeDelta = new Vector2(4, 4);
        UnityEngine.UI.Image oImg = outline.AddComponent<UnityEngine.UI.Image>();
        oImg.color = Color.red;
        outline.transform.SetAsFirstSibling();

        // Warning Text
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(confirmEndPopUp.transform, false);
        RectTransform tRect = textGo.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0f, 0.4f);
        tRect.anchorMax = new Vector2(1f, 1f);
        tRect.sizeDelta = Vector2.zero;

        TMP_Text txt = textGo.AddComponent<TextMeshProUGUI>();
        txt.text = "Are you sure you want to end the tutorial?";
        txt.fontSize = 20;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;

        // Yes Button
        GameObject yesGo = new GameObject("YesButton");
        yesGo.transform.SetParent(confirmEndPopUp.transform, false);
        RectTransform yRect = yesGo.AddComponent<RectTransform>();
        yRect.anchorMin = new Vector2(0.15f, 0.1f);
        yRect.anchorMax = new Vector2(0.45f, 0.35f);
        yRect.sizeDelta = Vector2.zero;

        UnityEngine.UI.Image yImg = yesGo.AddComponent<UnityEngine.UI.Image>();
        yImg.color = new Color(0.7f, 0.2f, 0.2f, 1f);
        Button yBtn = yesGo.AddComponent<Button>();
        yBtn.onClick.AddListener(EndTutorial);

        GameObject yTextGo = new GameObject("Text");
        yTextGo.transform.SetParent(yesGo.transform, false);
        RectTransform ytRect = yTextGo.AddComponent<RectTransform>();
        ytRect.anchorMin = Vector2.zero;
        ytRect.anchorMax = Vector2.one;
        ytRect.sizeDelta = Vector2.zero;
        TMP_Text yTxt = yTextGo.AddComponent<TextMeshProUGUI>();
        yTxt.text = "Yes";
        yTxt.fontSize = 16;
        yTxt.color = Color.white;
        yTxt.alignment = TextAlignmentOptions.Center;

        // No Button
        GameObject noGo = new GameObject("NoButton");
        noGo.transform.SetParent(confirmEndPopUp.transform, false);
        RectTransform nRect = noGo.AddComponent<RectTransform>();
        nRect.anchorMin = new Vector2(0.55f, 0.1f);
        nRect.anchorMax = new Vector2(0.85f, 0.35f);
        nRect.sizeDelta = Vector2.zero;

        UnityEngine.UI.Image nImg = noGo.AddComponent<UnityEngine.UI.Image>();
        nImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        Button nBtn = noGo.AddComponent<Button>();
        nBtn.onClick.AddListener(() => confirmEndPopUp.SetActive(false));

        GameObject nTextGo = new GameObject("Text");
        nTextGo.transform.SetParent(noGo.transform, false);
        RectTransform ntRect = nTextGo.AddComponent<RectTransform>();
        ntRect.anchorMin = Vector2.zero;
        ntRect.anchorMax = Vector2.one;
        ntRect.sizeDelta = Vector2.zero;
        TMP_Text nTxt = nTextGo.AddComponent<TextMeshProUGUI>();
        nTxt.text = "No";
        nTxt.fontSize = 16;
        nTxt.color = Color.white;
        nTxt.alignment = TextAlignmentOptions.Center;
    }

    private void ShowConfirmEndPopUp()
    {
        if (confirmEndPopUp != null)
        {
            confirmEndPopUp.SetActive(true);
        }
    }

    public void EndTutorial()
    {
        DifficultySettings.isTutorial = false;
        currentStep = TutorialStep.None;
        Time.timeScale = 1f;

        if (tutorialCanvas != null)
        {
            Destroy(tutorialCanvas);
            tutorialCanvas = null;
        }

        // Load Main Menu (or SampleScene / Help but exit to menu)
        SceneManager.LoadScene("MainMenu");
        
        Destroy(gameObject); // Self-destroy tutorial manager
    }
}
