using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GameBalanceSettings", menuName = "Tower Defense/Game Balance Settings")]
public class GameBalanceSettings : ScriptableObject
{
    public const float MasterTowerVisualScaleMultiplier = 0.35f;
    private const float AbsoluteMinimumAttackInterval = 0.05f;
    private const float AbsoluteMaximumSlowCap = 0.85f;
    private static GameBalanceSettings instance;

    public enum Difficulty { Easy, Normal, Hard, Hardcore }

    [Serializable]
    public struct DifficultyPreset
    {
        public int playerLives;
        public int startingMoney;
        public float enemyHealthFactor;
        public float enemySpeedFactor;
        public float spawnFrequencyFactor;
        public float rewardMultiplier;
        public float scoreMultiplier;
    }

    [Serializable]
    public struct EnemyTypeStats
    {
        public string typeName;
        public int baseHealth;
        public float baseSpeed;
        public int baseReward;

        public EnemyTypeStats(string typeName, int baseHealth, float baseSpeed, int baseReward)
        {
            this.typeName = typeName;
            this.baseHealth = baseHealth;
            this.baseSpeed = baseSpeed;
            this.baseReward = baseReward;
        }
    }

    [Serializable]
    public struct TowerStats
    {
        public Tower.TowerClass towerClass;
        public int damage;
        public float range;
        public float fireRate;
        public int cost;

        public TowerStats(Tower.TowerClass towerClass, int damage, float range, float fireRate, int cost)
        {
            this.towerClass = towerClass;
            this.damage = damage;
            this.range = range;
            this.fireRate = fireRate;
            this.cost = cost;
        }
    }

    [Serializable]
    public struct TowerTierDelta
    {
        public int damageDelta;
        public float intervalDelta;
        public float rangeDelta;
    }

    [Serializable]
    public class BaseTowerProgression
    {
        public TowerStats level1Stats;
        public TowerTierDelta tierDelta;
    }

    [Serializable]
    public class SpecialTowerEvolutionStats
    {
        public SpecialTowerType towerType;
        public GameObject towerPrefab;
        public int soldierCount;
        public int assaultCount;
        public int sniperCount;
        public TowerStats level5Stats;
        public string skillName;
        [TextArea(2, 5)] public string skillDescription;
        [Header("Skill Settings")]
        [Tooltip("Damage for secondary attacks.")]
        public float secondaryDamage;
        [Tooltip("Range for secondary attacks and skill cones, including Iron Vanguard's cone.")]
        [Min(0f)]
        public float secondaryRange;
        [Min(0f)] public float secondaryHitWidth;
        [Min(0)] public int secondaryMaxTargets = 1;
        public float secondaryFireRate;
        [Min(0f), Tooltip("Radius for skill area effects, separate from cone range.")]
        public float abilityRadius;
        public float abilityDuration;
        public float abilityCooldown;
        public float abilityDamage;
        public float abilitySlow;
        public float abilitySlowDuration;
        public float abilityKnockback;
        [Min(0f)] public float abilityDamageMultiplier = 1f;
        [Min(0f)] public float abilityAttackIntervalMultiplier = 1f;
        [Min(0f)] public float abilityTriggerRadius;
        public float abilityChance;
        public int abilityEveryNthHit;
        public int maxTargets;
        [Range(0f, 360f), Tooltip("Full angle of cone-shaped attacks and skill effects.")]
        public float coneAngle;
        public float auraDamageBonus;
        public float auraAttackSpeedBonus;
        public float auraRangeBonus;
        public float abilityPulseInterval;
        public float visualFireDuration;
        public float abilityDazzleDuration;
        public float abilityStartupDelay;
        public float projectileAirDuration;
        public float projectileAirPeakHeight;
        public float projectileAirRotationSpeed;
        public float projectileBounceDuration;
        public float projectileBounceDistance;
        public float projectileBouncePeakHeight;
        public float projectileBounceRotationSpeed;
        public float projectileRollDuration;
        public float projectileRollDistance;
        public float projectileRollRotationSpeed;
        public float projectileExplosionForwardOffset;
        [Min(0f), Tooltip("Visual scale for Guerilla Eliminator landmines.")]
        public float landmineVisualScaleMultiplier = 1f;
        [Min(0f), Tooltip("Visual scale for Gunner Commander grenades.")]
        public float grenadeVisualScaleMultiplier = 1f;
    }

    [Serializable]
    public class TowerVisualScaleEntry
    {
        public SpecialTowerType towerType;
        public float visualScaleMultiplier = 0.25f;
    }

    [Serializable]
    public class MarkVisualScaleEntry
    {
        public string markName;
        public float visualScaleMultiplier = 1.0f;
    }

    [SerializeField] private DifficultyPreset easy = new DifficultyPreset
    {
        playerLives = 20, startingMoney = 120, enemyHealthFactor = 0.75f,
        enemySpeedFactor = 0.8f, spawnFrequencyFactor = 1.3f,
        rewardMultiplier = 1f, scoreMultiplier = 0.8f
    };
    [SerializeField] private DifficultyPreset normal = new DifficultyPreset
    {
        playerLives = 10, startingMoney = 120, enemyHealthFactor = 1f,
        enemySpeedFactor = 1f, spawnFrequencyFactor = 1f,
        rewardMultiplier = 1f, scoreMultiplier = 1f
    };
    [SerializeField] private DifficultyPreset hard = new DifficultyPreset
    {
        playerLives = 5, startingMoney = 120, enemyHealthFactor = 1.25f,
        enemySpeedFactor = 1.2f, spawnFrequencyFactor = 0.75f,
        rewardMultiplier = 1f, scoreMultiplier = 1.25f
    };
    [SerializeField] private DifficultyPreset hardcore = new DifficultyPreset
    {
        playerLives = 1, startingMoney = 120, enemyHealthFactor = 1.5f,
        enemySpeedFactor = 1.3f, spawnFrequencyFactor = 0.6f,
        rewardMultiplier = 0.5f, scoreMultiplier = 2f
    };

    [Header("Tower Economy")]
    [SerializeField] private int soldierBaseCost = 60;
    [SerializeField] private int assaultBaseCost = 100;
    [SerializeField] private int sniperBaseCost = 250;
    [SerializeField] private BaseTowerProgression fallbackTowerProgression = new BaseTowerProgression
    {
        level1Stats = new TowerStats(Tower.TowerClass.Unknown, 20, 8f, 1f, 100)
    };
    [SerializeField, Range(0f, 1f)] private float recycleRefundRatio = 0.5f;
    [FormerlySerializedAs("maxSlowCap")]
    [SerializeField, Range(0f, AbsoluteMaximumSlowCap)] private float serializedMaxSlowCap = 0.85f;
    [SerializeField, Min(AbsoluteMinimumAttackInterval)] private float minimumAttackInterval = 0.05f;

    [Header("Base Tower Progression")]
    [SerializeField] private BaseTowerProgression soldierProgression = new BaseTowerProgression
    {
        level1Stats = new TowerStats(Tower.TowerClass.Soldier, 25, 7f, 1f, 60),
        tierDelta = new TowerTierDelta { damageDelta = 20, intervalDelta = -0.08f, rangeDelta = 0.5f }
    };
    [SerializeField] private BaseTowerProgression assaultProgression = new BaseTowerProgression
    {
        level1Stats = new TowerStats(Tower.TowerClass.Assault, 40, 7f, 0.75f, 100),
        tierDelta = new TowerTierDelta { damageDelta = 15, intervalDelta = -0.18f, rangeDelta = 0.0f }
    };
    [SerializeField] private BaseTowerProgression sniperProgression = new BaseTowerProgression
    {
        level1Stats = new TowerStats(Tower.TowerClass.Sniper, 80, 14f, 1.5f, 250),
        tierDelta = new TowerTierDelta { damageDelta = 130, intervalDelta = 0.15f, rangeDelta = 3.5f }
    };
    [Header("Base Tower Sprites")]
    [SerializeField] private Sprite soldierTowerSprite;
    [SerializeField] private Sprite assaultTowerSprite;
    [SerializeField] private Sprite sniperTowerSprite;
    [SerializeField] private Sprite soldierFireSprite;
    [SerializeField] private Sprite assaultFireSprite;
    [SerializeField] private Sprite sniperFireSprite;

    [Header("Special Tower Evolution (Level 5)")]
    [SerializeField] private List<SpecialTowerEvolutionStats> specialTowerEvolutionStats;
    [Header("Visual Scale Multipliers")]
    [SerializeField] private List<TowerVisualScaleEntry> towerVisualScaleMultipliers = CreateDefaultTowerVisualScales();
    [SerializeField] private List<MarkVisualScaleEntry> markVisualScaleMultipliers = CreateDefaultMarkVisualScales();

    [Header("Status Tag Icons")]
    [SerializeField] private Sprite slowedTagIcon;
    [SerializeField] private Sprite dazzledTagIcon;
    [SerializeField] private Sprite vulnerableTagIcon;
    [SerializeField] private Sprite hasteTagIcon;
    [SerializeField] private Sprite damageTagIcon;
    [SerializeField] private Sprite rangeTagIcon;

    [Header("Wave and Enemy Balance")]
    [SerializeField] private float autoStartDelay = 30f;
    [SerializeField] private float minSpawnDelayMultiplier = 0.1f;
    [SerializeField] private float maxSpawnDelayMultiplier = 2.5f;
    [SerializeField] private float minSpawnDelayCap = 0.02f;
    [SerializeField] private float maxSpawnDelayCap = 0.04f;
    [SerializeField] private float waveSpawnDelayDecreasePerWave = 0.04f;
    [SerializeField] private float minWaveSpawnDelayFactor = 0.15f;
    [SerializeField] private float waveHealthIncreasePerWave = 0.15f;
    [SerializeField] private int initialEnemyCount = 10;
    [SerializeField] private int enemyCountIncreasePerWave = 2;
    [SerializeField] private int firstKingWave = 5;
    [SerializeField] private int firstUltraWave = 15;
    [SerializeField] private float maxSpecialEnemyRatio = 0.4f;
    [SerializeField] private float initialKingRatio = 0.1f;
    [SerializeField] private float kingRatioIncreasePerWave = 0.03f;
    [SerializeField] private float initialUltraRatio = 0.1f;
    [SerializeField] private float ultraRatioIncreasePerWave = 0.04f;
    [SerializeField] private float lateWaveKingRatio = 0.25f;
    [SerializeField] private EnemyTypeStats[] enemyTypes =
    {
        new EnemyTypeStats("simon", 50, 3.0f, 20),
        new EnemyTypeStats("simonking", 150, 1.5f, 50),
        new EnemyTypeStats("ultrasimon", 100, 5.5f, 80)
    };

    [Header("Score Balance")]
    [SerializeField] private float scoreBase = 50f;
    [SerializeField] private float scorePerWeightedKill = 10f;
    [SerializeField] private float scorePerWaveProduct = 50f;
    [SerializeField] private float damageScoreDivisor = 50f;

    public static GameBalanceSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GameBalanceSettings>("GameBalanceSettings");
                if (instance == null)
                {
                    instance = CreateInstance<GameBalanceSettings>();
                }
                instance.EnsureDefaultCollections();
            }
            return instance;
        }
    }

    public float AutoStartDelay => autoStartDelay;
    public float MinSpawnDelayMultiplier => minSpawnDelayMultiplier;
    public float MaxSpawnDelayMultiplier => maxSpawnDelayMultiplier;
    public float MaxSlowCap => Mathf.Clamp(serializedMaxSlowCap, 0f, AbsoluteMaximumSlowCap);
    public float maxSlowCap => MaxSlowCap;
    public float MinimumAttackInterval => Mathf.Max(minimumAttackInterval, AbsoluteMinimumAttackInterval);
    public float RecycleRefundRatio => recycleRefundRatio;
    public int SoldierBaseCost => soldierBaseCost;
    public int AssaultBaseCost => assaultBaseCost;
    public int SniperBaseCost => sniperBaseCost;

    private void OnEnable()
    {
        if (instance == null)
        {
            instance = this;
        }
        EnsureDefaultCollections();
    }

    public DifficultyPreset GetPreset(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => easy,
            Difficulty.Normal => normal,
            Difficulty.Hard => hard,
            Difficulty.Hardcore => hardcore,
            _ => normal
        };
    }

    public int GetPlayerLives(Difficulty difficulty, bool isTutorial = false)
    {
        return (isTutorial ? easy : GetPreset(difficulty)).playerLives;
    }

    public int GetStartingMoney(Difficulty difficulty) => GetPreset(difficulty).startingMoney;
    public float GetDifficultyHealthFactor(Difficulty difficulty) => GetPreset(difficulty).enemyHealthFactor;
    public float GetDifficultySpeedFactor(Difficulty difficulty) => GetPreset(difficulty).enemySpeedFactor;
    public float GetDifficultyDelayFactor(Difficulty difficulty) => GetPreset(difficulty).spawnFrequencyFactor;
    public float GetDifficultyScoreMultiplier(Difficulty difficulty) => GetPreset(difficulty).scoreMultiplier;
    public float GetDifficultyRewardMultiplier(Difficulty difficulty) => GetPreset(difficulty).rewardMultiplier;

    public float ClampAttackInterval(float interval)
    {
        return Mathf.Max(MinimumAttackInterval, interval);
    }

    public float ClampTotalSlow(float totalSlow)
    {
        return Mathf.Clamp(totalSlow, 0f, MaxSlowCap);
    }

    public int CalculateSynthesisFee(int incomingAccumulatedValue, int targetTier)
    {
        return incomingAccumulatedValue * (targetTier + 1);
    }

    public int CalculateRecycleRefund(int accumulatedValue)
    {
        return Mathf.FloorToInt(accumulatedValue * recycleRefundRatio);
    }

    public TowerStats GetBaseTowerStats(Tower.TowerClass towerClass, int tier)
    {
        BaseTowerProgression progression = GetProgression(towerClass);
        TowerStats stats = progression.level1Stats;
        int increments = Mathf.Clamp(tier, 1, 4) - 1;
        stats.towerClass = towerClass;
        stats.damage += progression.tierDelta.damageDelta * increments;
        stats.fireRate = ClampAttackInterval(stats.fireRate + progression.tierDelta.intervalDelta * increments);
        stats.range += progression.tierDelta.rangeDelta * increments;
        stats.cost = GetBaseCost(towerClass);
        return stats;
    }

    public TowerStats GetTowerStats(Tower.TowerClass towerClass)
    {
        return GetBaseTowerStats(towerClass, 1);
    }

    public Sprite GetBaseTowerSprite(Tower.TowerClass towerClass)
    {
        return towerClass switch
        {
            Tower.TowerClass.Soldier => soldierTowerSprite,
            Tower.TowerClass.Assault => assaultTowerSprite,
            Tower.TowerClass.Sniper => sniperTowerSprite,
            _ => null
        };
    }

    public Sprite GetBaseTowerFireSprite(Tower.TowerClass towerClass)
    {
        return towerClass switch
        {
            Tower.TowerClass.Soldier => soldierFireSprite,
            Tower.TowerClass.Assault => assaultFireSprite,
            Tower.TowerClass.Sniper => sniperFireSprite,
            _ => null
        };
    }

    public TowerTierDelta GetBaseTowerTierDelta(Tower.TowerClass towerClass)
    {
        return GetProgression(towerClass).tierDelta;
    }

    public SpecialTowerEvolutionStats GetSpecialTowerEvolutionStats(SpecialTowerType towerType)
    {
        EnsureDefaultCollections();
        foreach (SpecialTowerEvolutionStats stats in specialTowerEvolutionStats)
        {
            if (stats.towerType == towerType)
            {
                return stats;
            }
        }
        return new SpecialTowerEvolutionStats { towerType = towerType };
    }

    public SpecialTowerEvolutionStats GetSpecialTowerForRecipe(int soldierCount, int assaultCount, int sniperCount)
    {
        EnsureDefaultCollections();
        foreach (SpecialTowerEvolutionStats stats in specialTowerEvolutionStats)
        {
            if (stats.soldierCount == soldierCount &&
                stats.assaultCount == assaultCount &&
                stats.sniperCount == sniperCount)
            {
                return stats;
            }
        }
        return null;
    }

    public GameObject GetSpecialTowerPrefab(SpecialTowerType towerType)
    {
        SpecialTowerEvolutionStats definition = GetSpecialTowerEvolutionStats(towerType);
        return definition.towerPrefab;
    }

    public float GetTowerVisualScale(SpecialTowerType towerType)
    {
        EnsureDefaultCollections();
        float individualMultiplier = 1.0f;
        foreach (TowerVisualScaleEntry entry in towerVisualScaleMultipliers)
        {
            if (entry.towerType == towerType)
            {
                individualMultiplier = entry.visualScaleMultiplier;
                break;
            }
        }
        return MasterTowerVisualScaleMultiplier * individualMultiplier;
    }

    public float GetMarkVisualScale(string markName)
    {
        EnsureDefaultCollections();
        foreach (MarkVisualScaleEntry entry in markVisualScaleMultipliers)
        {
            if (string.Equals(entry.markName, markName, StringComparison.OrdinalIgnoreCase))
            {
                return entry.visualScaleMultiplier;
            }
        }
        return 1.0f;
    }

    public Sprite GetStatusTagIcon(StatusTagType tagType)
    {
        return tagType switch
        {
            StatusTagType.Slowed => slowedTagIcon,
            StatusTagType.Dazzled => dazzledTagIcon,
            StatusTagType.Vulnerable => vulnerableTagIcon,
            StatusTagType.Haste => hasteTagIcon,
            StatusTagType.Damage => damageTagIcon,
            StatusTagType.Range => rangeTagIcon,
            _ => null
        };
    }

    public float GetWaveSpawnDelayFactor(int wave)
    {
        return Mathf.Max(minWaveSpawnDelayFactor, 1f - (wave - 1) * waveSpawnDelayDecreasePerWave);
    }

    public (float min, float max) GetSpawnDelayRange(int wave, Difficulty difficulty)
    {
        float waveFactor = GetWaveSpawnDelayFactor(wave);
        float difficultyFactor = GetDifficultyDelayFactor(difficulty);
        float minDelay = minSpawnDelayMultiplier * waveFactor * difficultyFactor;
        float maxDelay = maxSpawnDelayMultiplier * waveFactor * difficultyFactor;
        if (maxDelay < minDelay)
        {
            float temporary = maxDelay;
            maxDelay = minDelay;
            minDelay = temporary;
        }
        minDelay = Mathf.Max(minSpawnDelayCap, minDelay);
        maxDelay = Mathf.Max(maxSpawnDelayCap, maxDelay);
        return (minDelay, maxDelay);
    }

    public float GetWaveHealthMultiplier(int wave)
    {
        return 1f + (wave - 1) * waveHealthIncreasePerWave;
    }

    public EnemyTypeStats GetEnemyTypeStats(string typeName)
    {
        EnsureDefaultCollections();
        string normalized = typeName?.ToLowerInvariant() ?? string.Empty;
        EnemyTypeStats bestMatch = enemyTypes[0];
        int bestMatchLength = -1;

        foreach (EnemyTypeStats enemyType in enemyTypes)
        {
            if (normalized == enemyType.typeName)
            {
                return enemyType;
            }
            if (normalized.Contains(enemyType.typeName) && enemyType.typeName.Length > bestMatchLength)
            {
                bestMatch = enemyType;
                bestMatchLength = enemyType.typeName.Length;
            }
        }
        return bestMatch;
    }

    public int GetEnemyReward(string typeName, Difficulty difficulty)
    {
        EnemyTypeStats stats = GetEnemyTypeStats(typeName);
        return Mathf.Max(0, Mathf.RoundToInt(stats.baseReward * GetDifficultyRewardMultiplier(difficulty)));
    }

    public int GetEnemyCountForWave(int wave)
    {
        return initialEnemyCount + (wave - 1) * enemyCountIncreasePerWave;
    }

    public void GetWaveEnemyCounts(int wave, out int simonCount, out int simonKingCount, out int ultraSimonCount)
    {
        simonCount = 0;
        simonKingCount = 0;
        ultraSimonCount = 0;
        int enemyCount = GetEnemyCountForWave(wave);

        if (wave < firstKingWave)
        {
            simonCount = enemyCount;
            return;
        }
        if (wave < firstUltraWave)
        {
            float kingRatio = Mathf.Min(maxSpecialEnemyRatio, initialKingRatio + (wave - firstKingWave) * kingRatioIncreasePerWave);
            simonKingCount = Mathf.RoundToInt(enemyCount * kingRatio);
            simonCount = enemyCount - simonKingCount;
            return;
        }

        float ultraRatio = Mathf.Min(maxSpecialEnemyRatio, initialUltraRatio + (wave - firstUltraWave) * ultraRatioIncreasePerWave);
        ultraSimonCount = Mathf.RoundToInt(enemyCount * ultraRatio);
        simonKingCount = Mathf.RoundToInt(enemyCount * lateWaveKingRatio);
        simonCount = enemyCount - simonKingCount - ultraSimonCount;
    }

    public int CalculateScore(int regularKills, int kingKills, int ultraKills, int completedWaves, float damageDealt, Difficulty difficulty)
    {
        float baseScore = scoreBase + scorePerWeightedKill * (regularKills + 2f * kingKills + 4f * ultraKills)
            + scorePerWaveProduct * completedWaves * (completedWaves + 1) + damageDealt / damageScoreDivisor;
        return Mathf.RoundToInt(baseScore * GetDifficultyScoreMultiplier(difficulty));
    }

    private BaseTowerProgression GetProgression(Tower.TowerClass towerClass)
    {
        return towerClass switch
        {
            Tower.TowerClass.Soldier => soldierProgression,
            Tower.TowerClass.Assault => assaultProgression,
            Tower.TowerClass.Sniper => sniperProgression,
            _ => fallbackTowerProgression
        };
    }

    private int GetBaseCost(Tower.TowerClass towerClass)
    {
        return towerClass switch
        {
            Tower.TowerClass.Soldier => soldierBaseCost,
            Tower.TowerClass.Assault => assaultBaseCost,
            Tower.TowerClass.Sniper => sniperBaseCost,
            _ => fallbackTowerProgression.level1Stats.cost
        };
    }

    private void EnsureDefaultCollections()
    {
        if (enemyTypes == null || enemyTypes.Length == 0)
        {
            enemyTypes = new[]
            {
                new EnemyTypeStats("simon", 50, 3.0f, 20),
                new EnemyTypeStats("simonking", 150, 1.5f, 50),
                new EnemyTypeStats("ultrasimon", 100, 5.5f, 80)
            };
        }
        if (specialTowerEvolutionStats == null || specialTowerEvolutionStats.Count == 0)
        {
            specialTowerEvolutionStats = CreateDefaultEvolutionStats();
        }
        if (towerVisualScaleMultipliers == null || towerVisualScaleMultipliers.Count == 0)
        {
            towerVisualScaleMultipliers = CreateDefaultTowerVisualScales();
        }
        if (markVisualScaleMultipliers == null || markVisualScaleMultipliers.Count == 0)
        {
            markVisualScaleMultipliers = CreateDefaultMarkVisualScales();
        }
    }

    private List<SpecialTowerEvolutionStats> CreateDefaultEvolutionStats()
    {
        return new List<SpecialTowerEvolutionStats>
        {
            CreateSpecialTower(SpecialTowerType.Veteran, 5, 0, 0, 130, 8.5f, 0.65f, "Veteran's Rally", "Melee within 2 range; kills grant nearby towers +30% damage and +20% attack speed for 5 seconds.", 140, 2f, 0.35f, abilityRadius: 4f, abilityDuration: 5f, auraDamageBonus: 0.3f, auraAttackSpeedBonus: 0.2f),
            CreateSpecialTower(SpecialTowerType.Juggernaut, 0, 5, 0, 65, 9f, 0.12f, "Gatling Frenzy", "Every 10 seconds, fire a 60-degree fan for 4 seconds at twice the attack speed with knockback.", secondaryRange: 7f, abilityDuration: 4f, abilityCooldown: 10f, abilityKnockback: 1.5f, abilityAttackIntervalMultiplier: 0.5f, coneAngle: 60f),
            CreateSpecialTower(SpecialTowerType.GhostRecon, 0, 0, 5, 680, 28f, 2.2f, "Reaper's Seventh Bullet", "Every 7 kills, execute the non-boss enemy nearest the path end.", secondaryFireRate: 0.15f, abilityDuration: 1f, abilityEveryNthHit: 7),
            CreateSpecialTower(SpecialTowerType.DrillSergeant, 4, 1, 0, 110, 8.5f, 0.5f, "Warcry Surge", "Every 5 shots, nearby towers gain 25% attack speed for 3 seconds.", abilityRadius: 3.5f, abilityDuration: 3f, abilityChance: 0.25f, abilityEveryNthHit: 5),
            CreateSpecialTower(SpecialTowerType.Spotter, 4, 0, 1, 180, 13f, 0.8f, "Spotter's Mark", "Every 8 seconds, mark the highest-health enemy; all towers deal 30% more damage to it for 5 seconds.", secondaryFireRate: 0.18f, abilityDuration: 5f, abilityCooldown: 8f, abilityDamage: 0.3f, abilitySlowDuration: 0.12f),
            CreateSpecialTower(SpecialTowerType.GunnerCommander, 1, 4, 0, 75, 9.5f, 0.16f, "Frag Grenade Surge", "Every 30 shots, throw a grenade for 250 area damage and 40% slow; grenade cooldown is 5 seconds.", abilityRadius: 1.5f, abilityDuration: 5f, abilityCooldown: 5f, abilityDamage: 250f, abilitySlow: 0.4f, abilitySlowDuration: 2f, abilityEveryNthHit: 30, projectileAirDuration: 0.45f, projectileAirPeakHeight: 1.8f, projectileAirRotationSpeed: -720f, projectileBounceDuration: 0.18f, projectileBounceDistance: 0.6f, projectileBouncePeakHeight: 0.6f, projectileBounceRotationSpeed: -360f, projectileRollDuration: 0.15f, projectileRollDistance: 0.3f, projectileRollRotationSpeed: -120f, projectileExplosionForwardOffset: 0.9f),
                        CreateSpecialTower(SpecialTowerType.GunnerCommander, 1, 4, 0, 75, 9.5f, 0.16f, "Frag Grenade Surge", "Every 30 shots, throw a grenade for 250 area damage and 40% slow; grenade cooldown is 5 seconds.", abilityRadius: 1.5f, abilityDuration: 5f, abilityCooldown: 5f, abilityDamage: 250f, abilitySlow: 0.4f, abilitySlowDuration: 2f, abilityEveryNthHit: 30, projectileAirDuration: 0.45f, projectileAirPeakHeight: 1.8f, projectileAirRotationSpeed: -720f, projectileBounceDuration: 0.18f, projectileBounceDistance: 0.6f, projectileBouncePeakHeight: 0.6f, projectileBounceRotationSpeed: -360f, projectileRollDuration: 0.15f, projectileRollDistance: 0.3f, projectileRollRotationSpeed: -120f, projectileExplosionForwardOffset: 0.9f, grenadeVisualScaleMultiplier: 1f),
            CreateSpecialTower(SpecialTowerType.ShredderVanguard, 0, 4, 1, 105, 12.5f, 0.22f, "Armor Shredder", "35% chance to fire an armor-piercing shot with 1 range knockback.", abilityKnockback: 1f, abilityChance: 0.35f),
            CreateSpecialTower(SpecialTowerType.Pathfinder, 1, 0, 4, 520, 24f, 1.6f, "Vitals Hunter", "Prioritize low-health enemies; below 35% health, deal double damage. Ranged kills reset cooldown.", 50f, 2f, 0.35f, abilityChance: 0.35f, abilityDamageMultiplier: 2f),
            CreateSpecialTower(SpecialTowerType.BarrettOverload, 0, 1, 4, 360, 22f, 0.9f, "Overload HE Shell", "Every third shot deals 450 area damage with 2 range knockback.", secondaryFireRate: 0.25f, abilityRadius: 1.25f, abilityDamage: 450f, abilityKnockback: 2f, abilityEveryNthHit: 3),
            CreateSpecialTower(SpecialTowerType.ShockTrooper, 3, 2, 0, 85, 8f, 0.35f, "Superconductive Shock", "Consecutive hits gain up to 50% damage; kills stun enemies in a 1.5 radius for 0.5 seconds.", abilityRadius: 1.5f, abilityDuration: 0.5f, abilityChance: 0.05f, abilityEveryNthHit: 10),
            CreateSpecialTower(SpecialTowerType.Marksman, 3, 0, 2, 260, 17f, 1f, "Piercing DMR", "Pierce the first 2 enemies in a 0.4-wide line; each takes full damage and 1.2 knockback.", secondaryDamage: 260f, secondaryRange: 17f, secondaryHitWidth: 0.4f, abilityKnockback: 1.2f, maxTargets: 2),
            CreateSpecialTower(SpecialTowerType.TacticalCaptain, 3, 1, 1, 120, 12f, 0.6f, "Artillery Coordination", "Nearby towers gain 12% damage and 15% range; call a 400-damage mortar strike every 12 seconds after an initial 3-second enemy-entry grace.", secondaryRange: 1.5f, secondaryFireRate: 1f, abilityRadius: 4f, abilityCooldown: 12f, abilityDamage: 400f, abilityKnockback: 1.5f, auraDamageBonus: 0.12f, auraRangeBonus: 0.15f, abilityStartupDelay: 3f),
            CreateSpecialTower(SpecialTowerType.TrenchSweeper, 2, 3, 0, 200, 7.5f, 0.45f, "Point-Blank Breach", "Fire 5 pellets in a 75-degree cone; point-blank damage increases by up to 50%.", secondaryDamage: 40f, secondaryRange: 2f, secondaryFireRate: 0.25f, abilityDuration: 0.12f, abilityChance: 0.5f, maxTargets: 5, coneAngle: 75f),
            CreateSpecialTower(SpecialTowerType.SiegeDestroyer, 0, 3, 2, 360, 18f, 0.4f, "Siege Suppression Line", "Twin 180-damage shots; every fifth volley gains stronger knockback.", secondaryDamage: 180f, abilityDuration: 0.12f, abilityEveryNthHit: 5, abilityKnockback: 1f, secondaryRange: 2f, abilityDamageMultiplier: 2f),
            CreateSpecialTower(SpecialTowerType.IronVanguard, 1, 3, 1, 90, 11f, 0.3f, "Kinetic Path Ambush", "Every 20 hits, leap to the nearest path segment, knock back and dazzle nearby enemies, then fire a 60-degree, 3-range cone toward the enemy incoming direction for 5 seconds.", secondaryRange: 3f, abilityRadius: 1f, abilityDuration: 5f, abilityKnockback: 2f, abilityEveryNthHit: 20, coneAngle: 60f, abilityPulseInterval: 0.25f, abilityDazzleDuration: 3f),
            CreateSpecialTower(SpecialTowerType.GuerillaEliminator, 2, 0, 3, 480, 20f, 1.3f, "Lone Wolf & Tactical Mine", "Gain 35% damage with no enemy within 3; mines deal 600 area damage and 50% slow.", secondaryRange: 3f, abilityRadius: 1.5f, abilityCooldown: 15f, abilityDamage: 600f, abilitySlow: 0.5f, abilitySlowDuration: 3f, abilityChance: 0.35f, abilityTriggerRadius: 0.3f),
                        CreateSpecialTower(SpecialTowerType.GuerillaEliminator, 2, 0, 3, 480, 20f, 1.3f, "Lone Wolf & Tactical Mine", "Gain 35% damage with no enemy within 3; mines deal 600 area damage and 50% slow.", secondaryRange: 3f, abilityRadius: 1.5f, abilityCooldown: 15f, abilityDamage: 600f, abilitySlow: 0.5f, abilitySlowDuration: 3f, abilityChance: 0.35f, abilityTriggerRadius: 0.3f, landmineVisualScaleMultiplier: 1f),
            CreateSpecialTower(SpecialTowerType.RailgunOperator, 0, 2, 3, 320, 25f, 1.5f, "Linear Pierce Railbeam", "A 0.5-wide beam pierces all enemies, dealing 25% slow for 2 seconds and 0.8 knockback.", secondaryRange: 0.5f, secondaryFireRate: 0.4f, abilityDuration: 2f, abilitySlow: 0.25f, abilitySlowDuration: 2f, abilityKnockback: 0.8f, visualFireDuration: 0.08f),
            CreateSpecialTower(SpecialTowerType.HawkeyeOperator, 1, 1, 3, 420, 23f, 1.1f, "Target Tag & C4I Uplink", "Hits slow and tag enemies; tagged kills link Hawkeye and the nearest 2 allies for +20% range.", secondaryDamage: 0.2f, secondaryFireRate: 0.1f, abilityRadius: 5f, abilityDuration: 4f, abilitySlow: 0.3f, abilitySlowDuration: 4f, abilityChance: 0.25f),
            CreateSpecialTower(SpecialTowerType.CombatSpecialist, 2, 2, 1, 95, 13f, 0.35f, "Tactical Burst Trio", "Every third shot deals double damage and 1.2 knockback.", abilityKnockback: 1.2f, abilityEveryNthHit: 3, abilityDamageMultiplier: 2f),
            CreateSpecialTower(SpecialTowerType.LinebreakerScout, 2, 1, 2, 240, 18f, 0.95f, "Singularity Inward Pull", "Every fifth shot creates a 2.5-radius, 3-second vortex that slows 50% and pulls 0.4 every 0.25 seconds.", secondaryDamage: 320f, secondaryRange: 0.8f, abilityRadius: 2.5f, abilityDuration: 3f, abilitySlow: 0.5f, abilitySlowDuration: 3f, abilityEveryNthHit: 5, abilityKnockback: 0.4f, abilityPulseInterval: 0.25f),
            CreateSpecialTower(SpecialTowerType.HeavyMarksman, 1, 2, 2, 160, 16f, 0.48f, "Twin Bore & Overload Blast", "Twin shots pierce enemies; every fifth shot hits 3 enemies for 260 with heavy knockback and a 0.4-second stun.", secondaryDamage: 260f, secondaryRange: 16f, secondaryHitWidth: 0.5f, secondaryMaxTargets: 2, secondaryFireRate: 0.8f, abilityDuration: 0.4f, abilityEveryNthHit: 5, abilityKnockback: 1.8f, maxTargets: 3)
        };
    }

    private SpecialTowerEvolutionStats CreateSpecialTower(
        SpecialTowerType towerType,
        int soldierCount,
        int assaultCount,
        int sniperCount,
        int damage,
        float range,
        float fireRate,
        string skillName,
        string skillDescription,
        float secondaryDamage = 0f,
        float secondaryRange = 0f,
        float secondaryHitWidth = 0f,
        int secondaryMaxTargets = 1,
        float secondaryFireRate = 0f,
        float abilityRadius = 0f,
        float abilityDuration = 0f,
        float abilityCooldown = 0f,
        float abilityDamage = 0f,
        float abilitySlow = 0f,
        float abilitySlowDuration = 0f,
        float abilityKnockback = 0f,
        float abilityDamageMultiplier = 1f,
        float abilityAttackIntervalMultiplier = 1f,
        float abilityTriggerRadius = 0f,
        float abilityChance = 0f,
        int abilityEveryNthHit = 0,
        int maxTargets = 0,
        float coneAngle = 0f,
        float auraDamageBonus = 0f,
        float auraAttackSpeedBonus = 0f,
        float auraRangeBonus = 0f,
        float abilityPulseInterval = 0f,
        float visualFireDuration = 0f,
        float abilityDazzleDuration = 0f,
        float abilityStartupDelay = 0f,
        float projectileAirDuration = 0f,
        float projectileAirPeakHeight = 0f,
        float projectileAirRotationSpeed = 0f,
        float projectileBounceDuration = 0f,
        float projectileBounceDistance = 0f,
        float projectileBouncePeakHeight = 0f,
        float projectileBounceRotationSpeed = 0f,
        float projectileRollDuration = 0f,
        float projectileRollDistance = 0f,
        float projectileRollRotationSpeed = 0f,
        float projectileExplosionForwardOffset = 0f,
        float landmineVisualScaleMultiplier = 1f,
        float grenadeVisualScaleMultiplier = 1f)
    {
        int cost = soldierCount * soldierBaseCost + assaultCount * assaultBaseCost + sniperCount * sniperBaseCost;
        return new SpecialTowerEvolutionStats
        {
            towerType = towerType,
            soldierCount = soldierCount,
            assaultCount = assaultCount,
            sniperCount = sniperCount,
            level5Stats = new TowerStats(Tower.TowerClass.Unknown, damage, range, fireRate, cost),
            skillName = skillName,
            skillDescription = skillDescription,
            secondaryDamage = secondaryDamage,
            secondaryRange = secondaryRange,
            secondaryHitWidth = secondaryHitWidth,
            secondaryMaxTargets = secondaryMaxTargets,
            secondaryFireRate = secondaryFireRate,
            abilityRadius = abilityRadius,
            abilityDuration = abilityDuration,
            abilityCooldown = abilityCooldown,
            abilityDamage = abilityDamage,
            abilitySlow = abilitySlow,
            abilitySlowDuration = abilitySlowDuration,
            abilityKnockback = abilityKnockback,
            abilityDamageMultiplier = abilityDamageMultiplier,
            abilityAttackIntervalMultiplier = abilityAttackIntervalMultiplier,
            abilityTriggerRadius = abilityTriggerRadius,
            abilityChance = abilityChance,
            abilityEveryNthHit = abilityEveryNthHit,
            maxTargets = maxTargets,
            coneAngle = coneAngle,
            auraDamageBonus = auraDamageBonus,
            auraAttackSpeedBonus = auraAttackSpeedBonus,
            auraRangeBonus = auraRangeBonus,
            abilityPulseInterval = abilityPulseInterval,
            visualFireDuration = visualFireDuration,
            abilityDazzleDuration = abilityDazzleDuration,
            abilityStartupDelay = abilityStartupDelay,
            projectileAirDuration = projectileAirDuration,
            projectileAirPeakHeight = projectileAirPeakHeight,
            projectileAirRotationSpeed = projectileAirRotationSpeed,
            projectileBounceDuration = projectileBounceDuration,
            projectileBounceDistance = projectileBounceDistance,
            projectileBouncePeakHeight = projectileBouncePeakHeight,
            projectileBounceRotationSpeed = projectileBounceRotationSpeed,
            projectileRollDuration = projectileRollDuration,
            projectileRollDistance = projectileRollDistance,
            projectileRollRotationSpeed = projectileRollRotationSpeed,
            projectileExplosionForwardOffset = projectileExplosionForwardOffset,
            landmineVisualScaleMultiplier = landmineVisualScaleMultiplier,
            grenadeVisualScaleMultiplier = grenadeVisualScaleMultiplier
        };
    }

    private static List<TowerVisualScaleEntry> CreateDefaultTowerVisualScales()
    {
        List<TowerVisualScaleEntry> entries = new List<TowerVisualScaleEntry>();
        foreach (SpecialTowerType towerType in Enum.GetValues(typeof(SpecialTowerType)))
        {
            if (towerType != SpecialTowerType.None)
            {
                entries.Add(new TowerVisualScaleEntry { towerType = towerType, visualScaleMultiplier = 1.0f });
            }
        }
        return entries;
    }

    private static List<MarkVisualScaleEntry> CreateDefaultMarkVisualScales()
    {
        return new List<MarkVisualScaleEntry>
        {
            new MarkVisualScaleEntry { markName = "Mark_Reaper", visualScaleMultiplier = 1.0f },
            new MarkVisualScaleEntry { markName = "Mark_Mortar", visualScaleMultiplier = 1.0f },
            // Obsolete mark scales removed
        };
    }
}