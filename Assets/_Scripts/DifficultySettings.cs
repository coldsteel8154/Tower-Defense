using System;
using UnityEngine;

public static class DifficultySettings
{
    public enum Difficulty { Easy, Normal, Hard, Hardcore }
    public enum Language { English, TraditionalChinese }

    public static Difficulty selectedDifficulty = Difficulty.Normal;
    public static Language selectedLanguage;
    public static string particleSetting = "All"; // "All", "Less", "Least"
    public static bool showAllTowerRangesWhenPlacing = PlayerPrefs.GetInt("ShowAllTowerRangesWhenPlacing", 1) == 1;

    private static float _gameVolume = -1f;
    private const int LanguagePreferenceVersion = 2;

    static DifficultySettings()
    {
        if (PlayerPrefs.GetInt("LanguagePreferenceVersion", 0) < LanguagePreferenceVersion)
        {
            selectedLanguage = Language.English;
            PlayerPrefs.SetInt("SelectedLanguage", (int)selectedLanguage);
            PlayerPrefs.SetInt("LanguagePreferenceVersion", LanguagePreferenceVersion);
            PlayerPrefs.Save();
        }
        else
        {
            selectedLanguage = (Language)Mathf.Clamp(
                PlayerPrefs.GetInt("SelectedLanguage", (int)Language.English),
                (int)Language.English,
                (int)Language.TraditionalChinese);
        }
    }

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
    public static event Action OnShowAllTowerRangesChanged;

    public static bool IsTraditionalChinese => selectedLanguage == Language.TraditionalChinese;

    public static string GetSpecialTowerName(SpecialTowerType towerType)
    {
        return GetSpecialTowerName(towerType, IsTraditionalChinese);
    }

    public static string GetSpecialTowerName(SpecialTowerType towerType, bool traditionalChinese)
    {
        if (!traditionalChinese)
        {
            return SplitPascalCase(towerType.ToString());
        }

        return towerType switch
        {
            SpecialTowerType.Veteran => "老兵",
            SpecialTowerType.Juggernaut => "重甲衝鋒",
            SpecialTowerType.GhostRecon => "幽靈狙擊手",
            SpecialTowerType.DrillSergeant => "鐵血教官",
            SpecialTowerType.Spotter => "前線觀測員",
            SpecialTowerType.GunnerCommander => "機槍突擊長",
            SpecialTowerType.ShredderVanguard => "碎甲先鋒",
            SpecialTowerType.Pathfinder => "巡林獵手",
            SpecialTowerType.BarrettOverload => "巴雷特過載手",
            SpecialTowerType.ShockTrooper => "震撼突擊兵",
            SpecialTowerType.Marksman => "精準步兵",
            SpecialTowerType.TacticalCaptain => "戰術隊長",
            SpecialTowerType.TrenchSweeper => "壕溝清道夫",
            SpecialTowerType.SiegeDestroyer => "攻城殲滅者",
            SpecialTowerType.IronVanguard => "鐵衛先鋒",
            SpecialTowerType.GuerillaEliminator => "游擊抹除者",
            SpecialTowerType.RailgunOperator => "磁軌砲手",
            SpecialTowerType.HawkeyeOperator => "鷹眼特戰員",
            SpecialTowerType.CombatSpecialist => "全能特遣兵",
            SpecialTowerType.LinebreakerScout => "破線斥候",
            SpecialTowerType.HeavyMarksman => "重裝神射手",
            _ => "特種防禦塔"
        };
    }

    public static string GetSpecialTowerSkillName(SpecialTowerType towerType, string englishName)
    {
        return GetSpecialTowerSkillName(towerType, englishName, IsTraditionalChinese);
    }

    public static string GetSpecialTowerSkillName(
        SpecialTowerType towerType, string englishName, bool traditionalChinese)
    {
        if (!traditionalChinese)
        {
            return englishName;
        }

        return towerType switch
        {
            SpecialTowerType.Veteran => "戰場號召",
            SpecialTowerType.Juggernaut => "加特林狂暴散射",
            SpecialTowerType.GhostRecon => "死神七殺處決",
            SpecialTowerType.DrillSergeant => "戰吼激勵",
            SpecialTowerType.Spotter => "弱點標記",
            SpecialTowerType.GunnerCommander => "破片手榴彈投擲",
            SpecialTowerType.ShredderVanguard => "高壓破甲彈",
            SpecialTowerType.Pathfinder => "弱點追獵與處決",
            SpecialTowerType.BarrettOverload => "過載高爆穿甲彈",
            SpecialTowerType.ShockTrooper => "電磁連擊與超導震波",
            SpecialTowerType.Marksman => "穿甲重矢",
            SpecialTowerType.TacticalCaptain => "三軍協調與砲火支援",
            SpecialTowerType.TrenchSweeper => "破障霰彈與貼身絞殺",
            SpecialTowerType.SiegeDestroyer => "定點重彈壓制線",
            SpecialTowerType.IronVanguard => "動能超頻展盾",
            SpecialTowerType.GuerillaEliminator => "孤狼潛行與戰術地雷",
            SpecialTowerType.RailgunOperator => "直線穿透電磁射束",
            SpecialTowerType.HawkeyeOperator => "鷹眼標記與數據連線",
            SpecialTowerType.CombatSpecialist => "戰術三連擊",
            SpecialTowerType.LinebreakerScout => "特異點向心力場",
            SpecialTowerType.HeavyMarksman => "雙管穿甲與超載爆震",
            _ => englishName
        };
    }

    public static string GetSpecialTowerSkillDescription(SpecialTowerType towerType, string englishDescription)
    {
        return GetSpecialTowerSkillDescription(towerType, englishDescription, IsTraditionalChinese);
    }

    public static string GetSpecialTowerSkillDescription(
        SpecialTowerType towerType, string englishDescription, bool traditionalChinese)
    {
        if (!traditionalChinese)
        {
            return englishDescription;
        }

        return towerType switch
        {
            SpecialTowerType.Veteran => "敵人進入 2.0 格內時自動切換匕首近戰連刺。擊殺敵人會鼓舞 4.0 格內友軍防禦塔，傷害提高 30%、攻擊間隔縮短 20%，持續 5.0 秒。",
            SpecialTowerType.Juggernaut => "每隔 10.0 秒進入持續 4.0 秒的狂暴模式，朝目標方向以 60 度扇形散射，射速翻倍，每發造成 1.5 格強制擊退。",
            SpecialTowerType.GhostRecon => "每累計擊殺 7 名敵人，無視距離狙殺全場最接近終點的一名非頭目敵人，並重置計數。",
            SpecialTowerType.DrillSergeant => "每射擊 5 次觸發戰吼，使自身與 3.5 格內友軍防禦塔的攻擊間隔縮短 25%，持續 3.0 秒。",
            SpecialTowerType.Spotter => "每 8.0 秒標記射程內生命值最高的敵人，使其受到所有友軍防禦塔的傷害提高 30%，持續 5.0 秒。",
            SpecialTowerType.GunnerCommander => "每累計射擊 30 發投擲一枚破片手榴彈；爆炸造成範圍傷害並施加 40% 緩速，投出後進入獨立冷卻。",
            SpecialTowerType.ShredderVanguard => "每次射擊有 35% 機率觸發穿甲重彈，無視目標防禦抗性並造成 1.0 格物理擊退。",
            SpecialTowerType.Pathfinder => "優先鎖定生命百分比最低的目標；目標生命低於 35% 時造成 200% 暴擊。僅遠程狙擊擊殺會立即重置冷卻。",
            SpecialTowerType.BarrettOverload => "嚴格遵循三發循環：前兩發為常規子彈，第 3 發為高爆彈，造成範圍傷害並擊退範圍內敵人。",
            SpecialTowerType.ShockTrooper => "連續攻擊同一目標時，每發傷害遞增 5%，最多疊加 10 層（+50%）。擊殺目標時觸發 1.5 格電磁震波，使範圍內敵人暈眩 0.5 秒。",
            SpecialTowerType.Marksman => "以 0.4 格厚射線貫穿直線前方最多 2 名敵人，每名承受全額傷害並造成 1.2 格物理擊退。",
            SpecialTowerType.TacticalCaptain => "常駐指揮 4.0 格內友軍防禦塔，使傷害提高 12%、射程提高 15%；每 12.0 秒呼叫迫擊砲轟炸。",
            SpecialTowerType.TrenchSweeper => "以 75 度扇形向前發射 5 枚彈丸；目標越近傷害越高，零距離最多提高 50% 並造成強力擊退。",
            SpecialTowerType.SiegeDestroyer => "雙槍管同時齊射，每一擊皆附帶擊退；每連續開火 5 次，第 5 輪齊射造成更強擊退。",
            SpecialTowerType.IronVanguard => "每次命中累積 5 點動能，滿 100 點後擊退 3.0 格內敵人並進入持續 4.0 秒的射速翻倍超頻狀態。",
            SpecialTowerType.GuerillaEliminator => "自身 3.0 格內沒有敵人時，射擊傷害提高 35%。自動於敵人必經路徑埋設地雷；踩踏後造成範圍傷害並施加 50% 緩速。",
            SpecialTowerType.RailgunOperator => "普攻即為 25.0 格、寬 0.5 格的電磁光束，瞬間貫穿直線上所有敵人，造成全額傷害、擊退與 25% 緩速，持續 2.0 秒。",
            SpecialTowerType.HawkeyeOperator => "命中會標記敵人並使其跑速降低 30%、友軍對其暴擊率提高 25%，持續 4.0 秒。被標記敵人死亡時，為自身與最近 2 座友軍塔連線並提高 20% 射程。",
            SpecialTowerType.CombatSpecialist => "依照「噠-噠-咚！」三連擊循環射擊，第 3 發必定暴擊，造成雙倍傷害並強制擊退 1.2 格。",
            SpecialTowerType.LinebreakerScout => "每 5 擊發射一發阻截彈並生成持續 3.0 秒、半徑 2.5 格的暗紫色重力漩渦；敵人緩速 50%，並每 0.5 秒被拉向中心 0.4 格。",
            SpecialTowerType.HeavyMarksman => "雙管狙擊砲左右交替射擊，穿透第 1 名敵人並打擊第 2 名；第 5 發超載爆震貫穿前 3 名敵人並造成強力擊退。",
            _ => englishDescription
        };
    }

    public static string GetTowerClassName(Tower.TowerClass towerClass)
    {
        return GetTowerClassName(towerClass, IsTraditionalChinese);
    }

    public static string GetTowerClassName(Tower.TowerClass towerClass, bool traditionalChinese)
    {
        if (!traditionalChinese)
        {
            return towerClass.ToString();
        }

        return towerClass switch
        {
            Tower.TowerClass.Soldier => "步兵",
            Tower.TowerClass.Assault => "突擊兵",
            Tower.TowerClass.Sniper => "狙擊手",
            _ => "防禦塔"
        };
    }

    public static string GetStatusTagName(StatusTagType tagType)
    {
        return GetStatusTagName(tagType, IsTraditionalChinese);
    }

    public static string GetStatusTagName(StatusTagType tagType, bool traditionalChinese)
    {
        if (!traditionalChinese)
        {
            return tagType.ToString();
        }

        return tagType switch
        {
            StatusTagType.Slowed => "緩速",
            StatusTagType.Dazzled => "致盲",
            StatusTagType.Vulnerable => "易傷",
            StatusTagType.Haste => "加速",
            StatusTagType.Damage => "傷害提升",
            StatusTagType.Range => "射程提升",
            _ => tagType.ToString()
        };
    }

    public static string GetStatusEffectDescription(StatusEffectInstance effect)
    {
        return GetStatusEffectDescription(effect, IsTraditionalChinese);
    }

    public static string GetStatusEffectDescription(StatusEffectInstance effect, bool traditionalChinese)
    {
        if (!traditionalChinese || effect == null)
        {
            return effect != null ? effect.description : string.Empty;
        }

        string description = effect.description ?? string.Empty;
        if (description.StartsWith("Tactical Captain +", StringComparison.Ordinal))
        {
            string bonus = description.Substring("Tactical Captain +".Length);
            if (bonus.EndsWith("damage", StringComparison.Ordinal))
            {
                return "戰術隊長傷害 +" + bonus.Substring(0, bonus.Length - "damage".Length);
            }
            if (bonus.EndsWith("range", StringComparison.Ordinal))
            {
                return "戰術隊長射程 +" + bonus.Substring(0, bonus.Length - "range".Length);
            }
        }

        return description switch
        {
            "Hawkeye uplink +20% range" => "鷹眼指管鏈路：射程 +20%",
            "Lone Wolf +35% damage" => "孤狼：傷害 +35%",
            "Drill Sergeant +25% attack speed" => "訓練士官：攻擊速度 +25%",
            "Veteran Rally +30% damage" => "老兵集結：傷害 +30%",
            "Veteran Rally +20% attack speed" => "老兵集結：攻擊速度 +20%",
            "Spotter +30% damage taken" => "觀測標記：受到傷害 +30%",
            _ => description
        };
    }

    private static string SplitPascalCase(string value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder(value.Length + 8);
        for (int index = 0; index < value.Length; index++)
        {
            if (index > 0 && char.IsUpper(value[index]))
            {
                result.Append(' ');
            }
            result.Append(value[index]);
        }
        return result.ToString();
    }

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
        if (!Enum.IsDefined(typeof(Language), lang))
        {
            Debug.LogError("Cannot select an unsupported language: " + lang);
            return;
        }

        selectedLanguage = lang;
        PlayerPrefs.SetInt("SelectedLanguage", (int)lang);
        PlayerPrefs.Save();
        OnLanguageChanged?.Invoke();
    }

    public static void SetShowAllTowerRangesWhenPlacing(bool showAll)
    {
        showAllTowerRangesWhenPlacing = showAll;
        PlayerPrefs.SetInt("ShowAllTowerRangesWhenPlacing", showAll ? 1 : 0);
        PlayerPrefs.Save();
        OnShowAllTowerRangesChanged?.Invoke();
    }
}
