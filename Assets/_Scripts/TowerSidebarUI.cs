using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[System.Serializable]
public class TowerBuyOption
{
    public GameObject towerPrefab;
    public int cost = 100;
    public Sprite customIcon;
    public string displayName;
}

public class TowerSidebarUI : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform sidebarPanel;
    public Button toggleButton;
    public TMP_Text toggleArrowText;
    public RectTransform contentParent;
    public GameObject buyButtonPrefab;

    [Header("Towers Configuration")]
    public List<TowerBuyOption> buyOptions = new List<TowerBuyOption>();

    [Header("Animation Settings")]
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isOpen = true;
    private Coroutine slideCoroutine;
    private float sidebarWidth = 200f;
    private float closedPosX;
    private float openPosX;

    private void Start()
    {
        // Ensure there is an EventSystem and a Canvas so tooltips and UI events work
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        if (Object.FindObjectOfType<Canvas>() == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        if (sidebarPanel != null)
        {
            sidebarWidth = sidebarPanel.rect.width;
            openPosX = sidebarPanel.anchoredPosition.x;
            closedPosX = openPosX - sidebarWidth;
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleSidebar);
            UpdateArrowText();
        }

        PopulateButtons();
    }

    private void PopulateButtons()
    {
        if (contentParent == null || buyButtonPrefab == null) return;

        // Clear existing children
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // If options list is empty, let's automatically add the default one from TowerPlacementManager!
        if (buyOptions.Count == 0 && TowerPlacementManager.instance != null)
        {
            TowerBuyOption defaultOption = new TowerBuyOption();
            defaultOption.towerPrefab = TowerPlacementManager.instance.TowerPrefab;
            defaultOption.cost = TowerPlacementManager.instance.TowerCost;
            defaultOption.displayName = "Default Tower";
            buyOptions.Add(defaultOption);
        }

        foreach (var option in buyOptions)
        {
            if (option.towerPrefab == null) continue;

            GameObject btnObj = Instantiate(buyButtonPrefab, contentParent);
            TowerBuyButtonUI btnUI = btnObj.GetComponent<TowerBuyButtonUI>();
            if (btnUI != null)
            {
                // Determine icon: customIcon or from SpriteRenderer on prefab
                Sprite icon = option.customIcon;
                if (icon == null)
                {
                    SpriteRenderer sr = option.towerPrefab.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        icon = sr.sprite;
                    }
                }

                btnUI.Setup(icon, option.cost, () =>
                {
                    if (TowerPlacementManager.instance != null)
                    {
                        TowerPlacementManager.instance.StartPlacement(option.towerPrefab, option.cost);
                    }
                });

                // Attach hover tooltip trigger dynamically
                TowerTooltipTrigger tooltip = btnObj.AddComponent<TowerTooltipTrigger>();
                tooltip.Setup(option.towerPrefab);
            }
        }
    }

    public void ToggleSidebar()
    {
        isOpen = !isOpen;
        UpdateArrowText();

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
        }
        slideCoroutine = StartCoroutine(SlideRoutine(isOpen ? openPosX : closedPosX));
    }

    private void UpdateArrowText()
    {
        if (toggleArrowText != null)
        {
            toggleArrowText.text = isOpen ? "<" : ">";
        }
    }

    private IEnumerator SlideRoutine(float targetX)
    {
        float elapsed = 0f;
        Vector2 startPos = sidebarPanel.anchoredPosition;
        Vector2 targetPos = new Vector2(targetX, startPos.y);

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Use unscaled time so it animates when paused
            float t = slideCurve.Evaluate(elapsed / slideDuration);
            sidebarPanel.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        sidebarPanel.anchoredPosition = targetPos;
        slideCoroutine = null;
    }
}