using System;
using UnityEngine;

public static class DifficultySettings
{
    public enum Difficulty { Easy, Normal, Hard, Hardcore }
    public enum Language { English, TraditionalChinese }

    public static Difficulty selectedDifficulty = Difficulty.Normal;
    public static Language selectedLanguage = Language.English;
    public static string particleSetting = "All"; // "All", "Less", "Least"
    public static float gameVolume = 100f; // 0 to 100

    public static bool isTutorial = false;

    public static event Action OnLanguageChanged;

    public static void SetLanguage(Language lang)
    {
        selectedLanguage = lang;
        OnLanguageChanged?.Invoke();
    }
}
