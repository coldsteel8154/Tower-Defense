using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.TextCore.LowLevel;

public class TowerTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private GameObject towerPrefab;
    private Tower.TowerClass tooltipTowerClass = Tower.TowerClass.Unknown;
    private static GameObject tooltipInstance;

    public void Setup(GameObject prefab, Tower.TowerClass towerClass = Tower.TowerClass.Unknown)
    {
        towerPrefab = prefab;
        tooltipTowerClass = towerClass;
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

            // Populate text using centralized tower stats so tooltip reflects GameBalanceSettings
            TMP_Text txt = tooltipInstance.GetComponentInChildren<TMP_Text>();
            if (txt != null)
            {
                var stats = GetTooltipTowerStats(tower, tooltipTowerClass);
                float attacksPerSec = stats.fireRate > 0 ? (1f / stats.fireRate) : 0f;
                string prefabNameLower = towerPrefab.name.ToLowerInvariant();
                string towerName = GetTooltipTowerName(towerPrefab.name, tower, tooltipTowerClass);
                string towerNameChinese = GetTooltipTowerNameChinese(prefabNameLower, towerName);

                string englishText = $"<b>{towerName} </b>\n" +
                                     $"Damage: {stats.damage}\n" +
                                     $"Speed: {attacksPerSec:F1}/s\n" +
                                     $"Range: {stats.range}";

                string chineseText = $"<b>{towerNameChinese}</b>\n" +
                                     $"傷害: {stats.damage}\n" +
                                     $"攻速: {attacksPerSec:F1}/秒\n" +
                                     $"範圍: {stats.range}";

                var localized = txt.GetComponent<LocalizedText>();
                if (localized != null)
                {
                    localized.SetContent(englishText, chineseText);
                }
                else
                {
                    bool useChinese = DifficultySettings.selectedLanguage == DifficultySettings.Language.TraditionalChinese;
                    txt.text = useChinese ? chineseText : englishText;
                    
                    // Apply Chinese font if needed
                    if (useChinese)
                    {
                        ApplyChineseFontToTooltip(txt);
                    }
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

    private GameBalanceSettings.TowerStats GetTooltipTowerStats(Tower tower, Tower.TowerClass overrideClass)
    {
        if (overrideClass != Tower.TowerClass.Unknown)
        {
            return GameBalanceSettings.GetTowerStats(overrideClass);
        }

        if (tower == null)
        {
            return new GameBalanceSettings.TowerStats(Tower.TowerClass.Unknown, 0, 0f, 0f, 0);
        }

        if (tower.towerClass != Tower.TowerClass.Unknown)
        {
            return GameBalanceSettings.GetTowerStats(tower.towerClass);
        }

        string nm = tower.gameObject.name.ToLowerInvariant();
        if (nm.Contains("sniper"))
        {
            return GameBalanceSettings.GetTowerStats(Tower.TowerClass.Sniper);
        }
        else if (nm.Contains("assault"))
        {
            return GameBalanceSettings.GetTowerStats(Tower.TowerClass.Assault);
        }
        else if (nm.Contains("soldier"))
        {
            return GameBalanceSettings.GetTowerStats(Tower.TowerClass.Soldier);
        }

        // Fallback to current tower values if type is unknown
        return new GameBalanceSettings.TowerStats(tower.towerClass, tower.damage, tower.range, tower.fireRate, tower.cost);
    }

    private string GetTooltipTowerName(string prefabName, Tower tower, Tower.TowerClass overrideClass)
    {
        if (overrideClass != Tower.TowerClass.Unknown)
        {
            return overrideClass.ToString();
        }

        if (tower != null && tower.towerClass != Tower.TowerClass.Unknown)
        {
            return tower.towerClass.ToString();
        }

        string nameLower = prefabName.ToLowerInvariant();
        if (nameLower.Contains("soldier")) return "Soldier";
        if (nameLower.Contains("assault")) return "Assault";
        if (nameLower.Contains("sniper")) return "Sniper";
        return prefabName.Replace("Tower_", "").Replace("Tower", "");
    }

    private string GetTooltipTowerNameChinese(string prefabNameLower, string englishName)
    {
        if (prefabNameLower.Contains("soldier")) return "士兵";
        if (prefabNameLower.Contains("assault")) return "突擊";
        if (prefabNameLower.Contains("sniper")) return "狙擊";
        return englishName;
    }

    private void ApplyChineseFontToTooltip(TMP_Text txt)
    {
        if (txt == null) return;

        TMP_FontAsset chineseFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/ChineseDynamicFont");
        if (chineseFontAsset == null)
        {
            Font sourceFont = Resources.Load<Font>("Fonts/NotoSansTC-VF");
            if (sourceFont != null)
            {
                chineseFontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                if (chineseFontAsset != null)
                {
                    chineseFontAsset.name = "NotoSansTC_Runtime_Tooltip";
                    chineseFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    chineseFontAsset.isMultiAtlasTexturesEnabled = true;
                }
            }
        }

        if (chineseFontAsset != null)
        {
            txt.font = chineseFontAsset;
            if (chineseFontAsset.material != null)
            {
                txt.fontSharedMaterial = chineseFontAsset.material;
            }
            txt.SetAllDirty();
            txt.ForceMeshUpdate();
        }
        else
        {
            Font fallbackChineseOSFont = Font.CreateDynamicFontFromOSFont("Microsoft JhengHei", 32);
            if (fallbackChineseOSFont == null)
            {
                fallbackChineseOSFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 32);
            }

            if (fallbackChineseOSFont != null)
            {
                TMP_FontAsset generatedFont = TMP_FontAsset.CreateFontAsset(fallbackChineseOSFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                if (generatedFont != null)
                {
                    txt.font = generatedFont;
                    if (generatedFont.material != null)
                    {
                        txt.fontSharedMaterial = generatedFont.material;
                    }
                }
                txt.SetAllDirty();
                txt.ForceMeshUpdate();
            }
        }
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
        var localizedText = textGo.AddComponent<LocalizedText>();
        localizedText.SetContent("Tooltip", "提示" );

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
