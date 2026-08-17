using UnityEngine;
using TMPro;

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
    private static bool chineseFontAssetRequested;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            defaultFontAsset = tmpText.font;
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
            var sourceFont = Resources.Load<Font>("Fonts/NotoSansTC-VF");
            if (sourceFont != null)
            {
                chineseFontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
                if (chineseFontAsset != null)
                {
                    chineseFontAsset.name = "NotoSansTC_Runtime";
                    chineseFontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                    chineseFontAsset.isMultiAtlasTexturesEnabled = true;
                }
            }
        }

        if (chineseFontAsset == null)
        {
            Debug.LogWarning("LocalizedText: Could not load a usable Chinese font asset. Squares may remain until a proper TMP font asset is assigned.");
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
        if (tmpText != null)
        {
            EnsureChineseFontAsset();

            if (DifficultySettings.selectedLanguage == DifficultySettings.Language.TraditionalChinese && chineseFontAsset != null)
            {
                tmpText.font = chineseFontAsset;
                if (chineseFontAsset.material != null)
                {
                    tmpText.fontSharedMaterial = chineseFontAsset.material;
                }
            }
            else if (defaultFontAsset != null)
            {
                tmpText.font = defaultFontAsset;
                if (defaultFontMaterial != null)
                {
                    tmpText.fontSharedMaterial = defaultFontMaterial;
                }
            }

            tmpText.text = DifficultySettings.selectedLanguage == DifficultySettings.Language.English ? englishText : chineseText;
            tmpText.SetAllDirty();
            tmpText.ForceMeshUpdate();
        }
    }
}
