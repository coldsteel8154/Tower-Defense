using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    [TextArea(2, 4)]
    public string englishText;
    [TextArea(2, 4)]
    public string chineseText;

    private TMP_Text tmpText;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
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

    public void UpdateText()
    {
        if (tmpText == null) tmpText = GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = DifficultySettings.selectedLanguage == DifficultySettings.Language.English ? englishText : chineseText;
        }
    }
}
