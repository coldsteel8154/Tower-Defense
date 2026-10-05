using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class TowerCombinationsGuide
{
    private static readonly Color PanelColor = new Color(0.075f, 0.09f, 0.12f, 0.98f);
    private static readonly Color RowColor = new Color(0.15f, 0.17f, 0.21f, 0.98f);
    private static readonly Color AccentColor = new Color(0.95f, 0.67f, 0.22f, 1f);

    public static GameObject Build(Transform parent, Action onBack, GameBalanceSettings settings)
    {
        if (parent == null)
        {
            Debug.LogError("TowerCombinationsGuide requires a parent under a Canvas.");
            return null;
        }
        if (settings == null)
        {
            Debug.LogError("TowerCombinationsGuide cannot load tower recipes because GameBalanceSettings is missing.");
            return null;
        }

        GameObject root = CreateObject("TowerCombinationsGuide", parent);
        Image overlay = root.AddComponent<Image>();
        overlay.color = new Color(0.015f, 0.02f, 0.03f, 0.94f);
        SetRect(root.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

        GameObject card = CreateObject("GuideCard", root.transform);
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = PanelColor;
        SetRect(card.GetComponent<RectTransform>(), new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.965f));

        Button backButton = CreateButton(card.transform, "Back to Pause", new Vector2(0.025f, 0.89f),
            new Vector2(0.23f, 0.98f), onBack, 25);
        backButton.gameObject.name = "TowerCombinationsBackButton";
        CreateText(card.transform, "GuideTitle", "Tower Crafting Guide", 36, AccentColor, TextAlignmentOptions.Center,
            new Vector2(0.25f, 0.89f), new Vector2(0.975f, 0.98f));

        GameObject viewportObject = CreateObject("RecipeViewport", card.transform);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        SetRect(viewportRect, new Vector2(0.025f, 0.025f), new Vector2(0.975f, 0.87f));
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.025f);
        Mask viewportMask = viewportObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject contentObject = CreateObject("RecipeList", viewportObject.transform);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = viewportObject.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 35f;

        List<GameBalanceSettings.SpecialTowerEvolutionStats> recipes = GetRecipes(settings);
        if (recipes.Count == 0)
        {
            CreateText(contentObject.transform, "NoRecipes", "No special tower recipes are configured.",
                22, Color.white, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        }
        else
        {
            foreach (GameBalanceSettings.SpecialTowerEvolutionStats recipe in recipes)
            {
                CreateRecipeRow(contentObject.transform, recipe);
            }
        }

        root.SetActive(false);
        return root;
    }

    private static List<GameBalanceSettings.SpecialTowerEvolutionStats> GetRecipes(GameBalanceSettings settings)
    {
        List<GameBalanceSettings.SpecialTowerEvolutionStats> recipes =
            new List<GameBalanceSettings.SpecialTowerEvolutionStats>();
        Array towerTypes = Enum.GetValues(typeof(SpecialTowerType));
        foreach (SpecialTowerType towerType in towerTypes)
        {
            if (towerType == SpecialTowerType.None)
            {
                continue;
            }

            GameBalanceSettings.SpecialTowerEvolutionStats recipe =
                settings.GetSpecialTowerEvolutionStats(towerType);
            if (recipe.towerType != towerType)
            {
                Debug.LogWarning("Tower combination guide has no balance entry for " + towerType + ".");
                continue;
            }
            recipes.Add(recipe);
        }
        return recipes;
    }

    private static void CreateRecipeRow(Transform parent, GameBalanceSettings.SpecialTowerEvolutionStats recipe)
    {
        GameObject row = CreateObject("Recipe_" + recipe.towerType, parent);
        Image background = row.AddComponent<Image>();
        background.color = RowColor;
        LayoutElement layout = row.AddComponent<LayoutElement>();
        layout.preferredHeight = 500f;
        layout.minHeight = 500f;

        GameObject iconObject = CreateObject("TowerIcon", row.transform);
        Image icon = iconObject.AddComponent<Image>();
        icon.preserveAspect = true;
        SetRect(iconObject.GetComponent<RectTransform>(), new Vector2(0.025f, 0.07f), new Vector2(0.225f, 0.93f));
        TowerVisualController visuals = recipe.towerPrefab != null
            ? recipe.towerPrefab.GetComponentInChildren<TowerVisualController>(true)
            : null;
        Sprite sprite = visuals != null ? visuals.GetGuideIdleSprite() : null;
        if (sprite != null)
        {
            icon.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("Tower combination guide could not find an idle sprite for " + recipe.towerType + ".");
            CreateText(iconObject.transform, "MissingSprite", "No image", 18, Color.white,
                TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        }

        string title = ToDisplayName(recipe.towerType);
        CreateText(row.transform, "TowerName", title, 36, AccentColor, TextAlignmentOptions.Left,
            new Vector2(0.255f, 0.82f), new Vector2(0.975f, 0.98f));
        CreateText(row.transform, "Recipe", BuildRecipeText(recipe), 24, Color.white, TextAlignmentOptions.Left,
            new Vector2(0.255f, 0.68f), new Vector2(0.975f, 0.84f));

        float displayedRange = Tower.GetConfiguredWorldRange(recipe.towerType, recipe.level5Stats.range);
        string stats = string.Format("Damage: {0}    Range: {1:0.0}    Fire rate: {2:0.00}s    Cost: {3}",
            recipe.level5Stats.damage, displayedRange, recipe.level5Stats.fireRate, recipe.level5Stats.cost);
        CreateText(row.transform, "TowerStats", stats, 22, new Color(0.85f, 0.88f, 0.92f),
            TextAlignmentOptions.Left, new Vector2(0.255f, 0.48f), new Vector2(0.975f, 0.69f));

        CreateText(row.transform, "SkillStats", BuildSkillStats(recipe), 19, new Color(0.95f, 0.79f, 0.46f),
            TextAlignmentOptions.Left, new Vector2(0.255f, 0.34f), new Vector2(0.975f, 0.49f));

        string skillText = string.IsNullOrWhiteSpace(recipe.skillName)
            ? recipe.skillDescription
            : recipe.skillName + (string.IsNullOrWhiteSpace(recipe.skillDescription)
                ? string.Empty
                : "\n" + recipe.skillDescription);
        CreateText(row.transform, "SkillDescription", skillText, 21, new Color(0.78f, 0.81f, 0.86f),
            TextAlignmentOptions.TopLeft, new Vector2(0.255f, 0.035f), new Vector2(0.975f, 0.35f));
    }

    private static string BuildRecipeText(GameBalanceSettings.SpecialTowerEvolutionStats recipe)
    {
        List<string> parts = new List<string>();
        if (recipe.soldierCount > 0)
        {
            parts.Add(recipe.soldierCount + " Soldier");
        }
        if (recipe.assaultCount > 0)
        {
            parts.Add(recipe.assaultCount + " Assault");
        }
        if (recipe.sniperCount > 0)
        {
            parts.Add(recipe.sniperCount + " Sniper");
        }
        return "Recipe: " + string.Join(" + ", parts);
    }

    private static string BuildSkillStats(GameBalanceSettings.SpecialTowerEvolutionStats recipe)
    {
        List<string> details = new List<string>();
        AddDetail(details, "secondary dmg", recipe.secondaryDamage, "0");
        AddDetail(details, "secondary range", recipe.secondaryRange, "0.0");
        AddDetail(details, "skill dmg", recipe.abilityDamage, "0");
        AddDetail(details, "area", recipe.abilityRadius, "0.0");
        AddDetail(details, "duration", recipe.abilityDuration, "0.0", "s");
        AddDetail(details, "cooldown", recipe.abilityCooldown, "0.0", "s");
        AddDetail(details, "knockback", recipe.abilityKnockback, "0.0");
        if (recipe.abilityChance > 0f)
        {
            details.Add((recipe.abilityChance * 100f).ToString("0") + "% chance");
        }
        if (recipe.abilitySlow > 0f)
        {
            details.Add("slow " + (recipe.abilitySlow * 100f).ToString("0") + "%");
        }
        if (recipe.abilityEveryNthHit > 0)
        {
            details.Add("every " + recipe.abilityEveryNthHit + " hits");
        }
        if (recipe.maxTargets > 0)
        {
            details.Add("up to " + recipe.maxTargets + " targets");
        }
        return details.Count > 0 ? "Skill stats: " + string.Join("  |  ", details) : string.Empty;
    }

    private static void AddDetail(List<string> details, string label, float value, string format, string suffix = "")
    {
        if (value > 0f)
        {
            details.Add(label + " " + value.ToString(format) + suffix);
        }
    }

    private static string ToDisplayName(SpecialTowerType towerType)
    {
        string name = towerType.ToString();
        System.Text.StringBuilder result = new System.Text.StringBuilder(name.Length + 8);
        for (int index = 0; index < name.Length; index++)
        {
            if (index > 0 && char.IsUpper(name[index]))
            {
                result.Append(' ');
            }
            result.Append(name[index]);
        }
        return result.ToString();
    }

    private static Button CreateButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax,
        Action onClick, int fontSize)
    {
        GameObject buttonObject = CreateObject(label, parent);
        Image image = buttonObject.AddComponent<Image>();
        image.color = AccentColor;
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick?.Invoke());
        SetRect(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        CreateText(buttonObject.transform, "Label", label, fontSize, new Color(0.08f, 0.09f, 0.11f),
            TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string value, int fontSize, Color color,
        TextAlignmentOptions alignment, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textObject = CreateObject(name, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        text.raycastTarget = false;
        SetRect(textObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
        return text;
    }

    private static GameObject CreateObject(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
