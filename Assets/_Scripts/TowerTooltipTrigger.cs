using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TowerTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject towerPrefab;
    private static GameObject tooltipInstance;

    public void Setup(GameObject prefab)
    {
        towerPrefab = prefab;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (towerPrefab == null) return;

        Tower tower = towerPrefab.GetComponent<Tower>();
        if (tower == null) return;

        // Create tooltip panel if not existing
        if (tooltipInstance == null)
        {
            tooltipInstance = CreateTooltipPrefab();
        }

        if (tooltipInstance != null)
        {
            tooltipInstance.transform.SetAsLastSibling();
            tooltipInstance.SetActive(true);

            // Populate text
            TMP_Text txt = tooltipInstance.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                // Convert fire rate to speed (attacks per second)
                float attacksPerSec = tower.fireRate > 0 ? (1f / tower.fireRate) : 0f;
                
                if (DifficultySettings.selectedLanguage == DifficultySettings.Language.English)
                {
                    txt.text = $"<b>{towerPrefab.name.Replace("Tower_", "").Replace("Tower", "")} Tower</b>\n" +
                               $"Damage: {tower.damage}\n" +
                               $"Speed: {attacksPerSec:F1}/s\n" +
                               $"Range: {tower.range}";
                }
                else
                {
                    string chineseType = "防禦塔";
                    if (towerPrefab.name.Contains("Soldier")) chineseType = "士兵";
                    else if (towerPrefab.name.Contains("Sniper")) chineseType = "狙擊";
                    else chineseType = "突擊";

                    txt.text = $"<b>{chineseType}塔</b>\n" +
                               $"傷害: {tower.damage}\n" +
                               $"攻速: {attacksPerSec:F1}/秒\n" +
                               $"範圍: {tower.range}";
                }
            }

            // Position it near the button
            UpdateTooltipPosition();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipInstance != null)
        {
            tooltipInstance.SetActive(false);
        }
    }

    private void Update()
    {
        if (tooltipInstance != null && tooltipInstance.activeSelf)
        {
            UpdateTooltipPosition();
        }
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipInstance == null) return;
        // Place tooltip slightly to the left or right of the mouse pointer and clamp on screen
        Vector3 mousePos = Input.mousePosition;
        Vector3 desired = mousePos + new Vector3(-130f, 60f, 0f);

        RectTransform rt = tooltipInstance.GetComponent<RectTransform>();
        float tooltipWidth = 180f;
        float tooltipHeight = 100f;
        Vector2 pivot = new Vector2(1f, 0f);
        if (rt != null)
        {
            tooltipWidth = rt.rect.width;
            tooltipHeight = rt.rect.height;
            pivot = rt.pivot;
        }

        // Clamp within screen bounds with small padding
        float padding = 8f;
        float w = Screen.width;
        float h = Screen.height;

        // Calculate actual offsets based on pivot to clamp the whole bounding box on screen
        float leftOffset = -pivot.x * tooltipWidth;
        float rightOffset = (1f - pivot.x) * tooltipWidth;
        float bottomOffset = -pivot.y * tooltipHeight;
        float topOffset = (1f - pivot.y) * tooltipHeight;

        float minX = padding - leftOffset;
        float maxX = w - padding - rightOffset;
        float minY = padding - bottomOffset;
        float maxY = h - padding - topOffset;

        desired.x = Mathf.Clamp(desired.x, minX, maxX);
        desired.y = Mathf.Clamp(desired.y, minY, maxY);

        tooltipInstance.transform.position = desired;
    }

    private GameObject CreateTooltipPrefab()
    {
        // Find Canvas or Parent in the scene
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        GameObject go = new GameObject("TowerTooltip");
        go.transform.SetParent(canvas.transform, false);
        go.transform.localScale = Vector3.one;

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180, 100);
        rect.pivot = new Vector2(1, 0); // Bottom-right pivot

        UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        img.raycastTarget = false;

        // Outline
        GameObject outlineGo = new GameObject("Outline");
        outlineGo.transform.SetParent(go.transform, false);
        RectTransform oRect = outlineGo.AddComponent<RectTransform>();
        oRect.anchorMin = Vector2.zero;
        oRect.anchorMax = Vector2.one;
        oRect.sizeDelta = new Vector2(2, 2);
        UnityEngine.UI.Image oImg = outlineGo.AddComponent<UnityEngine.UI.Image>();
        oImg.color = new Color(0.3f, 0.4f, 0.8f, 1f);
        oImg.raycastTarget = false;
        outlineGo.transform.SetAsFirstSibling();

        GameObject textGo = new GameObject("TooltipText");
        textGo.transform.SetParent(go.transform, false);
        RectTransform tRect = textGo.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = new Vector2(-16, -16); // Padding

        TMP_Text tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 14;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.raycastTarget = false;

        return go;
    }

    private void OnDestroy()
    {
        if (tooltipInstance != null)
        {
            Destroy(tooltipInstance);
            tooltipInstance = null;
        }
    }
}
