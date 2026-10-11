using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;

public class LocalizedText : MonoBehaviour
{
    [TextArea(2, 4)]
    public string englishText;
    [TextArea(2, 4)]
    public string chineseText;

    private TMP_Text tmpText;
    private TMP_FontAsset defaultFontAsset;
    private Material defaultFontMaterial;
    private static TMP_FontAsset chineseFontAsset;
    private static bool fontEngineInitialized;
    private static bool reportedMissingChineseGlyphs;
    private static bool reportedMissingChineseFont;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            defaultFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (defaultFontAsset == null)
            {
                defaultFontAsset = TMP_Settings.defaultFontAsset;
            }
            defaultFontMaterial = defaultFontAsset != null ? defaultFontAsset.material : null;
        }

        EnsureChineseFontAsset();
    }

    private void OnEnable()
    {
        DifficultySettings.OnLanguageChanged += UpdateText;
        UpdateText();
    }

    private void OnDisable()
    {
        DifficultySettings.OnLanguageChanged -= UpdateText;
    }

    private static void EnsureChineseFontAsset()
    {
        if (chineseFontAsset != null)
        {
            return;
        }

        if (!fontEngineInitialized)
        {
            FontEngineError error = FontEngine.InitializeFontEngine();
            if (error != FontEngineError.Success)
            {
                if (!reportedMissingChineseFont)
                {
                    reportedMissingChineseFont = true;
                    Debug.LogError("LocalizedText could not initialize TextMesh Pro's font engine: " + error);
                }
                return;
            }
            fontEngineInitialized = true;
        }

        Font sourceFont = Resources.Load<Font>("Fonts/NotoSansTC-Regular");
        if (sourceFont != null)
        {
            TMP_FontAsset generated = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048);
            if (generated != null)
            {
                generated.name = "NotoSansTC_Regular_Runtime";
                generated.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                generated.isMultiAtlasTexturesEnabled = true;
                chineseFontAsset = generated;
            }
        }

        if (chineseFontAsset == null)
        {
            if (!reportedMissingChineseFont)
            {
                reportedMissingChineseFont = true;
                Debug.LogError(
                    "LocalizedText could not create a TMP font from Resources/Fonts/NotoSansTC-Regular.ttf. " +
                    "Chinese text cannot be rendered without this font.");
            }
        }
    }

    public static TMP_FontAsset GetChineseFontAsset()
    {
        EnsureChineseFontAsset();
        return chineseFontAsset;
    }

    public void SetContent(string english, string chinese)
    {
        if (englishText == english && chineseText == chinese)
        {
            return;
        }

        englishText = english;
        chineseText = chinese;
        UpdateText();
    }

    public static void SetLocalizedContent(TMP_Text text, string english, string chinese)
    {
        if (text == null)
        {
            return;
        }

        LocalizedText localized = text.GetComponent<LocalizedText>();
        if (localized == null)
        {
            localized = text.gameObject.AddComponent<LocalizedText>();
        }
        localized.SetContent(english, chinese);
    }

    public void UpdateText()
    {
        if (tmpText == null) tmpText = GetComponent<TMP_Text>();
        if (tmpText == null) return;

        EnsureChineseFontAsset();

        bool useChinese = DifficultySettings.selectedLanguage == DifficultySettings.Language.TraditionalChinese;

        if (useChinese)
        {
            if (chineseFontAsset != null)
            {
                if (!string.IsNullOrEmpty(chineseText) &&
                    !chineseFontAsset.TryAddCharacters(chineseText, out string missingCharacters) &&
                    !reportedMissingChineseGlyphs)
                {
                    reportedMissingChineseGlyphs = true;
                    Debug.LogError(
                        "LocalizedText: Regular-weight Noto Sans TC could not add these requested characters: " +
                        missingCharacters);
                }
                tmpText.font = chineseFontAsset;
                if (chineseFontAsset.material != null)
                {
                    tmpText.fontSharedMaterial = chineseFontAsset.material;
                }
            }
        }
        else
        {
            if (defaultFontAsset == null)
            {
                defaultFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (defaultFontAsset == null)
                {
                    defaultFontAsset = TMP_Settings.defaultFontAsset;
                }
            }

            if (defaultFontAsset != null)
            {
                tmpText.font = defaultFontAsset;
                tmpText.fontSharedMaterial = defaultFontMaterial != null ? defaultFontMaterial : defaultFontAsset.material;
            }
        }

        tmpText.text = useChinese ? chineseText : englishText;
        tmpText.SetAllDirty();
        tmpText.ForceMeshUpdate();
    }
}
