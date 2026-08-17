using System;
using UnityEngine;

public static class DifficultySettings
{
    public enum Difficulty { Easy, Normal, Hard, Hardcore }
    public enum Language { English, TraditionalChinese }

    public static Difficulty selectedDifficulty = Difficulty.Normal;
    public static Language selectedLanguage = Language.English;
    public static string particleSetting = "All"; // "All", "Less", "Least"

    private static float _gameVolume = -1f;

    public static float gameVolume
    {
        get
        {
            if (_gameVolume < 0f)
            {
                _gameVolume = PlayerPrefs.GetFloat("GameVolume", 100f);
            }
            return _gameVolume;
        }
        set
        {
            SetVolume(value);
        }
    }

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
        _gameVolume = Mathf.Clamp(volume, 0f, 100f);
        PlayerPrefs.SetFloat("GameVolume", _gameVolume);
        PlayerPrefs.Save();
        AudioListener.volume = _gameVolume / 100f;
        OnVolumeChanged?.Invoke(_gameVolume / 100f);
    }

    public static void SetLanguage(Language lang)
    {
        selectedLanguage = lang;
        OnLanguageChanged?.Invoke();
    }
}
