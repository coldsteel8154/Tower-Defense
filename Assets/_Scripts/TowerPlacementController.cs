using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class TowerPlacementController : MonoBehaviour
{
    private const float ClickDragThreshold = 8f;
    private const float SpringBackDuration = 0.2f;
    private static TowerPlacementController activeDragController;

    private Tower tower;
    private SpecialTowerCombatController specialCombat;
    private Vector2 mouseDownPosition;
    private Vector3 originalPosition;
    private bool dragged;
    private SpriteRenderer dragVisualRenderer;
    private Color originalDragVisualColor;
    private TowerRange dragRange;

    [SerializeField] private Color validDragColor = new Color(0.3f, 1f, 0.3f, 0.85f);
    [SerializeField] private Color invalidDragColor = new Color(1f, 0.3f, 0.3f, 0.85f);

    private void Awake()
    {
        tower = GetComponent<Tower>();
        specialCombat = GetComponent<SpecialTowerCombatController>();
        if (tower == null || GetComponent<Collider2D>() == null)
        {
            enabled = false;
        }
    }

    private void Update()
    {
        if (tower == null)
        {
            return;
        }

        if (activeDragController == null && (specialCombat == null || !specialCombat.IsSkillActive) &&
            Input.GetMouseButtonDown(0) &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) &&
            FindTowerAtPointer() == tower)
        {
            activeDragController = this;
            dragged = false;
            mouseDownPosition = Input.mousePosition;
            originalPosition = transform.position;
            Transform visualRoot = transform.Find("VisualRoot");
            dragVisualRenderer = visualRoot != null
                ? visualRoot.GetComponent<SpriteRenderer>()
                : GetComponent<SpriteRenderer>();
            originalDragVisualColor = dragVisualRenderer != null ? dragVisualRenderer.color : Color.white;
            dragRange = GetComponentInChildren<TowerRange>();
        }

        if (activeDragController != this)
        {
            return;
        }

        if (Input.GetMouseButton(0) && Camera.main != null)
        {
            if (!dragged && Vector2.Distance(mouseDownPosition, Input.mousePosition) > ClickDragThreshold)
            {
                dragged = true;
            }
            if (dragged)
            {
                Vector3 dragPosition = GetMouseWorldPosition();
                transform.position = dragPosition;
                bool validDrop = TowerSynthesisManager.Instance.CanDrop(tower, Input.mousePosition, dragPosition);
                ApplyDragFeedback(validDrop);
            }
        }

        if (!Input.GetMouseButtonUp(0))
        {
            return;
        }

        activeDragController = null;
        float screenDelta = Vector2.Distance(mouseDownPosition, Input.mousePosition);
        if (screenDelta <= ClickDragThreshold)
        {
            transform.position = originalPosition;
            TowerInfoPanel.Open(tower);
            return;
        }

        if (Camera.main != null)
        {
            transform.position = GetMouseWorldPosition();
        }

        bool accepted = TowerSynthesisManager.Instance.HandleDrop(tower, Input.mousePosition, transform.position);
        if (!accepted)
        {
            StartCoroutine(SpringBackRoutine(transform.position, originalPosition));
        }
        else
        {
            ClearDragFeedback();
        }
    }

    private void OnDisable()
    {
        if (activeDragController == this)
        {
            activeDragController = null;
        }
        ClearDragFeedback();
    }

    private void ApplyDragFeedback(bool valid)
    {
        if (dragVisualRenderer != null)
        {
            dragVisualRenderer.color = valid ? validDragColor : invalidDragColor;
        }
        if (dragRange != null)
        {
            dragRange.SetVisible(true);
            dragRange.SetPreviewColor(valid ? validDragColor : invalidDragColor);
        }
    }

    private void ClearDragFeedback()
    {
        if (dragVisualRenderer != null)
        {
            dragVisualRenderer.color = originalDragVisualColor;
            dragVisualRenderer = null;
        }
        if (dragRange != null)
        {
            dragRange.ClearPreviewColor();
            dragRange = null;
        }
        tower?.UpdateRangeVisibility();
    }

    private static Tower FindTowerAtPointer()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return null;
        }

        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = -camera.transform.position.z;
        Vector2 worldPosition = camera.ScreenToWorldPoint(screenPosition);
        ContactFilter2D contactFilter = new ContactFilter2D
        {
            useTriggers = true
        };
        List<Collider2D> overlaps = new List<Collider2D>();
        Physics2D.OverlapPoint(worldPosition, contactFilter, overlaps);
        Tower closestTower = null;
        float closestDistance = float.MaxValue;
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == null)
            {
                continue;
            }

            Tower candidate = overlap.GetComponent<Tower>();
            if (candidate == null)
            {
                candidate = overlap.GetComponentInParent<Tower>();
            }
            if (candidate == null)
            {
                continue;
            }

            float distance = ((Vector2)candidate.transform.position - worldPosition).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestTower = candidate;
                closestDistance = distance;
            }
        }
        return closestTower;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 screenPosition = Input.mousePosition;
        screenPosition.z = Camera.main.WorldToScreenPoint(originalPosition).z;
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
        worldPosition.z = originalPosition.z;
        return worldPosition;
    }

    private IEnumerator SpringBackRoutine(Vector3 startPosition, Vector3 destination)
    {
        float elapsed = 0f;
        Vector3 travelDirection = (startPosition - destination).normalized;
        while (elapsed < SpringBackDuration)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, SpringBackDuration);
            float progress = elapsed / SpringBackDuration;
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            float bounce = Mathf.Sin(progress * Mathf.PI * 3f) * (1f - progress) * 0.08f;
            transform.position = Vector3.Lerp(startPosition, destination, easedProgress) + travelDirection * bounce;
            yield return null;
        }

        transform.position = destination;
        ClearDragFeedback();
    }
}

public static class TowerInfoPanel
{
    private static GameObject panel;
    private static TMP_Text panelText;
    private static RectTransform statusTagRow;
    private static GameObject statusTooltip;
    private static TMP_Text statusTooltipText;
    private static Tower selectedTower;
    private static int openedFrame = -1;
    private static Tower tooltipTower;
    private static StatusTagType tooltipTagType;
    private static string statusTagSignature;
    private static Tower statusTagTower;

    public static void Open(Tower tower)
    {
        if (tower == null)
        {
            return;
        }
        selectedTower = tower;
        openedFrame = Time.frameCount;
        EnsurePanel();
        Refresh();
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
    }

    public static void Refresh()
    {
        if (panel == null || panelText == null || selectedTower == null)
        {
            return;
        }

        Tower tower = selectedTower;
        StringBuilder content = new StringBuilder(256);
        string title = tower.specialTowerType != SpecialTowerType.None
            ? tower.specialTowerType.ToString()
            : tower.towerClass.ToString();
        content.Append("<b><size=120%><color=#FFD700>").Append(title).Append("</color></size></b>\n");
        content.Append("Tier: ").Append(tower.tier).Append(" / 5    Value: $").Append(tower.accumulatedValue).Append('\n');
        content.Append("Recipe: <color=#4CAF50>So:").Append(tower.soldierCount)
            .Append("</color> | <color=#FF9800>A:").Append(tower.assaultCount)
            .Append("</color> | <color=#2196F3>Sn:").Append(tower.sniperCount).Append("</color>\n\n");
        content.Append("<b>Combat</b>\nDamage: ").Append(tower.damage)
            .Append("\nAttack speed: ").Append(1f / tower.GetEffectiveAttackInterval()).Append(" /s")
            .Append("\nInterval: ").Append(tower.GetEffectiveAttackInterval().ToString("0.00"))
            .Append(" s\nRange: ").Append(tower.range.ToString("0.0")).Append(" f\n");

        TowerStatusManager statusManager = tower.GetComponent<TowerStatusManager>();
        if (statusManager != null && statusManager.ActiveEffects.Count > 0)
        {
            content.Append("\n<b>Active tags</b>\n");
            foreach (StatusEffectInstance effect in statusManager.ActiveEffects)
            {
                if (effect == null)
                {
                    continue;
                }
                content.Append(effect.tagType).Append("  ").Append(effect.description);
                if (effect.durationRemaining >= float.MaxValue * 0.5f)
                {
                    content.Append(" (active)\n");
                }
                else
                {
                    content.Append(" (").Append(Mathf.Max(0f, effect.durationRemaining).ToString("0.0"))
                        .Append(" s)\n");
                }
            }
        }
                RefreshStatusTagRow(tower, statusManager);

        if (tower.specialTowerType != SpecialTowerType.None)
        {
            GameBalanceSettings.SpecialTowerEvolutionStats skill =
                GameBalanceSettings.Instance.GetSpecialTowerEvolutionStats(tower.specialTowerType);
            content.Append("\n<b><color=#FFCC00>Special skill: ").Append(skill.skillName)
                .Append("</color></b>\n").Append(skill.skillDescription);
        }

        string nextText = content.ToString();
        if (panelText.text != nextText)
        {
            panelText.text = nextText;
        }
        RefreshTagTooltip();
    }

    private static void RefreshStatusTagRow(Tower tower, TowerStatusManager statusManager)
    {
        if (statusTagRow == null)
        {
            return;
        }

        List<StatusTagType> orderedTypes = new List<StatusTagType>();
        Dictionary<StatusTagType, int> counts = new Dictionary<StatusTagType, int>();
        if (statusManager != null)
        {
            foreach (StatusEffectInstance effect in statusManager.ActiveEffects)
            {
                if (effect == null)
                {
                    continue;
                }
                if (!counts.ContainsKey(effect.tagType))
                {
                    orderedTypes.Add(effect.tagType);
                    counts.Add(effect.tagType, 0);
                }
                counts[effect.tagType]++;
            }
        }

        string signature = string.Join("|", orderedTypes.ConvertAll(type => type + ":" + counts[type]));
        if (signature == statusTagSignature && statusTagTower == tower)
        {
            statusTagRow.gameObject.SetActive(orderedTypes.Count > 0);
            return;
        }

        statusTagSignature = signature;
        statusTagTower = tower;
        for (int index = statusTagRow.childCount - 1; index >= 0; index--)
        {
            Object.Destroy(statusTagRow.GetChild(index).gameObject);
        }

        foreach (StatusTagType type in orderedTypes)
        {
            GameObject iconObject = new GameObject("TowerTag_" + type, typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(statusTagRow, false);
            iconObject.GetComponent<RectTransform>().sizeDelta = new Vector2(26f, 26f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = GameBalanceSettings.Instance.GetStatusTagIcon(type);
            icon.preserveAspect = true;
            icon.raycastTarget = true;

            GameObject countObject = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countObject.transform.SetParent(iconObject.transform, false);
            RectTransform countRect = countObject.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(1f, 1f);
            countRect.anchorMax = new Vector2(1f, 1f);
            countRect.pivot = new Vector2(0f, 1f);
            countRect.sizeDelta = new Vector2(24f, 16f);
            TMP_Text countText = countObject.GetComponent<TMP_Text>();
            countText.text = counts[type] > 1 ? "^" + counts[type] : string.Empty;
            countText.fontSize = 11f;
            countText.alignment = TextAlignmentOptions.TopLeft;
            countText.color = Color.white;
            countText.raycastTarget = false;
            iconObject.AddComponent<TowerStatusTagHover>().Initialize(tower, type);
        }
        statusTagRow.gameObject.SetActive(orderedTypes.Count > 0);
    }

    public static void ShowTagTooltip(Tower tower, StatusTagType type, Vector2 screenPosition)
    {
        Canvas canvas = panel != null ? panel.GetComponentInParent<Canvas>() : null;
        if (canvas == null || tower == null)
        {
            return;
        }

        if (statusTooltip == null)
        {
            statusTooltip = new GameObject("TowerStatusTooltip", typeof(RectTransform), typeof(Image));
            statusTooltip.transform.SetParent(canvas.transform, false);
            statusTooltip.GetComponent<RectTransform>().sizeDelta = new Vector2(250f, 120f);
            Image background = statusTooltip.GetComponent<Image>();
            background.color = new Color(0.05f, 0.07f, 0.1f, 0.96f);
            background.raycastTarget = false;

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(statusTooltip.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 8f);
            textRect.offsetMax = new Vector2(-10f, -8f);
            statusTooltipText = textObject.GetComponent<TMP_Text>();
            statusTooltipText.fontSize = 14f;
            statusTooltipText.color = Color.white;
            statusTooltipText.alignment = TextAlignmentOptions.TopLeft;
            statusTooltipText.raycastTarget = false;
        }

        tooltipTower = tower;
        tooltipTagType = type;
        RefreshTagTooltip();
        statusTooltip.transform.SetAsLastSibling();
        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPosition, eventCamera, out Vector2 localPosition))
        {
            RectTransform tooltipRect = statusTooltip.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.rect.size;
            Vector2 anchoredPosition = localPosition + canvasSize * 0.5f + new Vector2(16f, 16f);
            anchoredPosition.x = Mathf.Clamp(anchoredPosition.x, 8f, canvasSize.x - tooltipRect.rect.width - 8f);
            anchoredPosition.y = Mathf.Clamp(anchoredPosition.y, 8f, canvasSize.y - tooltipRect.rect.height - 8f);
            tooltipRect.anchorMin = Vector2.zero;
            tooltipRect.anchorMax = Vector2.zero;
            tooltipRect.pivot = Vector2.zero;
            tooltipRect.anchoredPosition = anchoredPosition;
        }
        statusTooltip.SetActive(true);
    }

    public static void HideTagTooltip()
    {
        if (statusTooltip != null)
        {
            statusTooltip.SetActive(false);
        }
        tooltipTower = null;
    }

    private static void RefreshTagTooltip()
    {
        if (statusTooltip == null || !statusTooltip.activeSelf || statusTooltipText == null || tooltipTower == null)
        {
            return;
        }

        TowerStatusManager statusManager = tooltipTower.GetComponent<TowerStatusManager>();
        if (statusManager == null)
        {
            HideTagTooltip();
            return;
        }

        List<StatusEffectInstance> effects = new List<StatusEffectInstance>();
        float combinedMultiplier = 1f;
        foreach (StatusEffectInstance effect in statusManager.ActiveEffects)
        {
            if (effect == null || effect.tagType != tooltipTagType)
            {
                continue;
            }
            effects.Add(effect);
            combinedMultiplier *= tooltipTagType == StatusTagType.Haste
                ? Mathf.Max(0.01f, 1f - effect.intensity)
                : 1f + effect.intensity;
        }
        if (effects.Count == 0)
        {
            HideTagTooltip();
            return;
        }

        float totalBonus = tooltipTagType switch
        {
            StatusTagType.Haste => 1f / combinedMultiplier - 1f,
            StatusTagType.Damage => statusManager.GetDamageMultiplier() - 1f,
            _ => combinedMultiplier - 1f
        };
        StringBuilder text = new StringBuilder(128);
        text.Append("Total ").Append(tooltipTagType).Append(" boost: +").Append(totalBonus.ToString("P0")).Append('\n');
        for (int index = 0; index < effects.Count; index++)
        {
            StatusEffectInstance effect = effects[index];
            text.Append(index + 1).Append(". ").Append(effect.description)
                .Append(" [").Append(effect.sourceTower).Append("] ")
                .Append(Mathf.Max(0f, effect.durationRemaining).ToString("0.0")).Append("s\n");
        }
        statusTooltipText.text = text.ToString();
    }

    public static bool ContainsScreenPoint(Vector2 screenPosition)
    {
        if (panel == null || !panel.activeInHierarchy)
        {
            return false;
        }
        Canvas canvas = panel.GetComponentInParent<Canvas>();
        Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;
        return RectTransformUtility.RectangleContainsScreenPoint(panel.GetComponent<RectTransform>(), screenPosition, eventCamera);
    }

    private static void EnsurePanel()
    {
        if (panel != null)
        {
            return;
        }

        Canvas canvas = null;
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (Canvas candidate in canvases)
        {
            if (candidate != null && candidate.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = candidate;
                break;
            }
        }
        if (canvas == null)
        {
            foreach (Canvas candidate in canvases)
            {
                if (candidate != null && candidate.renderMode == RenderMode.ScreenSpaceCamera)
                {
                    canvas = candidate;
                    break;
                }
            }
        }
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("TowerInfoCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }
        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        panel = new GameObject("TowerInfoPanel");
        panel.transform.SetParent(canvas.transform, false);
        panel.AddComponent<TowerInfoPanelUpdater>();
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 1f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-20f, -20f);
        panelRect.sizeDelta = new Vector2(380f, 420f);

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);
        background.raycastTarget = true;

        GameObject textObject = new GameObject("TowerInfoText");
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 48f);
        textRect.offsetMax = new Vector2(-42f, -14f);
        panelText = textObject.AddComponent<TextMeshProUGUI>();
        panelText.fontSize = 16f;
        panelText.color = Color.white;
        panelText.alignment = TextAlignmentOptions.TopLeft;
        panelText.raycastTarget = false;

        GameObject tagRowObject = new GameObject("TowerStatusTagRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        tagRowObject.transform.SetParent(panel.transform, false);
        statusTagRow = tagRowObject.GetComponent<RectTransform>();
        statusTagRow.anchorMin = Vector2.zero;
        statusTagRow.anchorMax = new Vector2(1f, 0f);
        statusTagRow.pivot = Vector2.zero;
        statusTagRow.offsetMin = new Vector2(14f, 12f);
        statusTagRow.offsetMax = new Vector2(-14f, 42f);
        HorizontalLayoutGroup tagLayout = tagRowObject.GetComponent<HorizontalLayoutGroup>();
        tagLayout.childAlignment = TextAnchor.MiddleLeft;
        tagLayout.childForceExpandWidth = false;
        tagLayout.childForceExpandHeight = false;
        tagLayout.childControlWidth = false;
        tagLayout.childControlHeight = false;
        tagLayout.spacing = 5f;
        statusTagRow.gameObject.SetActive(false);

        GameObject closeObject = new GameObject("CloseButton");
        closeObject.transform.SetParent(panel.transform, false);
        RectTransform closeRect = closeObject.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-6f, -6f);
        closeRect.sizeDelta = new Vector2(30f, 30f);
        Image closeImage = closeObject.AddComponent<Image>();
        closeImage.color = new Color(0.25f, 0.3f, 0.38f, 1f);
        Button closeButton = closeObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(Close);

        GameObject closeTextObject = new GameObject("Text");
        closeTextObject.transform.SetParent(closeObject.transform, false);
        RectTransform closeTextRect = closeTextObject.AddComponent<RectTransform>();
        closeTextRect.anchorMin = Vector2.zero;
        closeTextRect.anchorMax = Vector2.one;
        closeTextRect.sizeDelta = Vector2.zero;
        TMP_Text closeText = closeTextObject.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.fontSize = 16f;
        closeText.alignment = TextAlignmentOptions.Center;
        closeText.color = Color.white;
        closeText.raycastTarget = false;

        panel.SetActive(false);
    }

    public static void Close()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
        HideTagTooltip();
        statusTagSignature = null;
        statusTagTower = null;
        selectedTower = null;
    }

    public static bool WasOpenedThisFrame => Time.frameCount == openedFrame;
}

public class TowerInfoPanelUpdater : MonoBehaviour
{
    private void Update()
    {
        TowerInfoPanel.Refresh();
        if (Input.GetMouseButtonDown(0) && !TowerInfoPanel.WasOpenedThisFrame &&
            !TowerInfoPanel.ContainsScreenPoint(Input.mousePosition))
        {
            TowerInfoPanel.Close();
        }
    }
}

public class TowerStatusTagHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Tower tower;
    private StatusTagType tagType;

    public void Initialize(Tower targetTower, StatusTagType type)
    {
        tower = targetTower;
        tagType = type;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        TowerInfoPanel.ShowTagTooltip(tower, tagType, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TowerInfoPanel.HideTagTooltip();
    }
}