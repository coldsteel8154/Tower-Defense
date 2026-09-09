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
    private static Font fallbackChineseOSFont;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            defaultFontAsset = tmpText.font != null ? tmpText.font : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            defaultFontMaterial = tmpText.fontSharedMaterial;
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

    private void EnsureChineseFontAsset()
    {
        if (chineseFontAsset != null)
        {
            return;
        }

        chineseFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/ChineseDynamicFont");
        if (chineseFontAsset == null)
        {
            Font sourceFont = Resources.Load<Font>("Fonts/NotoSansTC-VF");
            if (sourceFont != null)
            {
                TMP_FontAsset generated = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                if (generated != null)
                {
                    generated.name = "NotoSansTC_Runtime";
                    generated.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    generated.isMultiAtlasTexturesEnabled = true;
                    chineseFontAsset = generated;
                }
            }
        }

        if (chineseFontAsset == null && fallbackChineseOSFont == null)
        {
            fallbackChineseOSFont = Font.CreateDynamicFontFromOSFont("Microsoft JhengHei", 32);
            if (fallbackChineseOSFont == null)
            {
                fallbackChineseOSFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 32);
            }

            if (fallbackChineseOSFont == null)
            {
                Debug.LogWarning("LocalizedText: Could not load a usable Chinese font asset or OS fallback. Some glyphs may still render incorrectly.");
            }
        }
    }

    public void SetContent(string english, string chinese)
    {
        englishText = english;
        chineseText = chinese;
        UpdateText();
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
                tmpText.font = chineseFontAsset;
                if (chineseFontAsset.material != null)
                {
                    tmpText.fontSharedMaterial = chineseFontAsset.material;
                }
            }
            else if (fallbackChineseOSFont != null)
            {
                TMP_FontAsset generated = TMP_FontAsset.CreateFontAsset(fallbackChineseOSFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
                if (generated != null)
                {
                    chineseFontAsset = generated;
                    tmpText.font = chineseFontAsset;
                    if (chineseFontAsset.material != null)
                    {
                        tmpText.fontSharedMaterial = chineseFontAsset.material;
                    }
                }
                else
                {
                    tmpText.font = null;
                    tmpText.fontSharedMaterial = null;
                }
            }
        }
        else
        {
            if (defaultFontAsset == null)
            {
                defaultFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            if (defaultFontAsset != null)
            {
                tmpText.font = defaultFontAsset;
                if (defaultFontMaterial != null)
                {
                    tmpText.fontSharedMaterial = defaultFontMaterial;
                }
            }
        }

        tmpText.text = useChinese ? chineseText : englishText;
        tmpText.SetAllDirty();
        tmpText.ForceMeshUpdate();
    }
}
