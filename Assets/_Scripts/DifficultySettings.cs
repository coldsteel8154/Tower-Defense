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

    public static GameBalanceSettings.Difficulty GetBalanceDifficulty(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => GameBalanceSettings.Difficulty.Easy,
            Difficulty.Normal => GameBalanceSettings.Difficulty.Normal,
            Difficulty.Hard => GameBalanceSettings.Difficulty.Hard,
            Difficulty.Hardcore => GameBalanceSettings.Difficulty.Hardcore,
            _ => GameBalanceSettings.Difficulty.Normal,
        };
    }

    public static GameBalanceSettings.Difficulty GetBalanceDifficulty()
    {
        return GetBalanceDifficulty(selectedDifficulty);
    }

    public static bool isTutorial = false;

    public static event Action OnLanguageChanged;
    public static event Action<float> OnVolumeChanged;

    public static void SetVolume(float volume)
    {
        gameVolume = Mathf.Clamp(volume, 0f, 100f);
        AudioListener.volume = gameVolume / 100f;
        OnVolumeChanged?.Invoke(gameVolume / 100f);
    }

    public static void SetLanguage(Language lang)
    {
        selectedLanguage = lang;
        OnLanguageChanged?.Invoke();
    }
}
