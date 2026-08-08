using UnityEngine;

public static class GameBalanceSettings
{
    public enum Difficulty { Easy, Normal, Hard, Hardcore }

    public struct DifficultyPreset
    {
        public int playerLives;           // Number of lives awarded to the player on this difficulty
        public int startingMoney;         // Starting money at the beginning of the match
        public float enemyHealthFactor;   // Multiplier applied to enemy base health
        public float enemySpeedFactor;    // Multiplier applied to enemy base speed
        public float spawnFrequencyFactor; // Lower values = faster spawns, higher values = slower spawns
        public float rewardMultiplier;    // Multiplier applied to enemy reward money
        public float scoreMultiplier;     // Multiplier applied to final score calculation
    }

    public static readonly DifficultyPreset Easy = new DifficultyPreset
    {
        playerLives = 20,              // Extra lives make Easy mode forgiving
        startingMoney = 120,           // Starting funds for player towers
        enemyHealthFactor = 0.75f,     // Enemies have 75% of normal health
        enemySpeedFactor = 0.8f,       // Enemies move 80% as fast as normal
        spawnFrequencyFactor = 1.3f,   // Slower spawn frequency
        rewardMultiplier = 1f,         // Normal reward money
        scoreMultiplier = 0.8f         // Lower score multiplier for Easy mode
    };

    public static readonly DifficultyPreset Normal = new DifficultyPreset
    {
        playerLives = 10,              // Balanced lives for Normal difficulty
        startingMoney = 120,           // Default starting funds
        enemyHealthFactor = 1f,        // Baseline enemy health
        enemySpeedFactor = 1f,         // Baseline enemy speed
        spawnFrequencyFactor = 1f,     // Baseline spawn frequency
        rewardMultiplier = 1f,         // Standard reward money
        scoreMultiplier = 1f           // Standard score scaling
    };

    public static readonly DifficultyPreset Hard = new DifficultyPreset
    {
        playerLives = 5,               // Fewer lives to increase challenge
        startingMoney = 120,           // Same starting money as Normal
        enemyHealthFactor = 1.25f,     // Enemies have 25% more health
        enemySpeedFactor = 1.2f,       // Enemies move 20% faster
        spawnFrequencyFactor = 0.75f,  // More frequent enemy spawns
        rewardMultiplier = 1f,         // Rewards remain unchanged by difficulty
        scoreMultiplier = 1.25f        // Higher score reward for difficult play
    };

    public static readonly DifficultyPreset Hardcore = new DifficultyPreset
    {
        playerLives = 1,               // Single life mode
        startingMoney = 120,            // Reduced starting funds for Hardcore
        enemyHealthFactor = 1.5f,        // Enemies have double base health
        enemySpeedFactor = 1.3f,      // Enemies move 25% faster
        spawnFrequencyFactor = 0.6f,   // Significantly faster spawn rates
        rewardMultiplier = 0.5f,       // Half rewards on Hardcore
        scoreMultiplier = 2f           // Double score reward for high-risk play
    };

    public const float AutoStartDelay = 30f;              // Delay between waves when cleared
    public const float MinSpawnDelayMultiplier = 0.1f;   // Base multiplier for minimum spawn delay
    public const float MaxSpawnDelayMultiplier = 2.5f;   // Base multiplier for maximum spawn delay
    public const float MinSpawnDelayCap = 0.02f;        // Hard minimum allowed spawn delay
    public const float MaxSpawnDelayCap = 0.04f;        // Hard maximum allowed spawn delay
    public const float WaveSpawnDelayDecreasePerWave = 0.04f; // How much spawn delay decreases each wave
    public const float MinWaveSpawnDelayFactor = 0.15f; // Minimum cap for wave-based delay factor
    public const float WaveHealthIncreasePerWave = 0.15f; // Health increase factor per wave

    public struct EnemyTypeStats
    {
        public string typeName;       // Identifier used for matching enemy types by name
        public int baseHealth;        // Base health before difficulty / wave scaling
        public float baseSpeed;       // Base movement speed before difficulty scaling
        public int baseReward;        // Base money reward before difficulty scaling

        public EnemyTypeStats(string typeName, int baseHealth, float baseSpeed, int baseReward)
        {
            this.typeName = typeName;
            this.baseHealth = baseHealth;
            this.baseSpeed = baseSpeed;
            this.baseReward = baseReward;
        }
    }

    public static readonly EnemyTypeStats[] EnemyTypes = new EnemyTypeStats[]
    {
        new EnemyTypeStats("simon", 50, 4.0f, 20),
        new EnemyTypeStats("simonking", 150, 2.0f, 80),
        new EnemyTypeStats("ultrasimon", 100, 6.0f, 50)
    };

    public struct TowerStats
    {
        public Tower.TowerClass towerClass; // Tower type identifier
        public int damage;                 // Damage dealt per shot
        public float range;                // Attack range radius
        public float fireRate;             // Seconds between shots
        public int cost;                   // Purchase cost for this tower

        public TowerStats(Tower.TowerClass towerClass, int damage, float range, float fireRate, int cost)
        {
            this.towerClass = towerClass;
            this.damage = damage;
            this.range = range;
            this.fireRate = fireRate;
            this.cost = cost;
        }
    }

    public static readonly TowerStats[] TowerPresets = new TowerStats[]
    {
        new TowerStats(Tower.TowerClass.Soldier, 25, 7f, 1f, 60),
        new TowerStats(Tower.TowerClass.Assault, 40, 7f, 0.75f, 100),
        new TowerStats(Tower.TowerClass.Sniper, 80, 14f, 1.5f, 250)
    };

    public static DifficultyPreset GetPreset(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => Easy,
            Difficulty.Normal => Normal,
            Difficulty.Hard => Hard,
            Difficulty.Hardcore => Hardcore,
            _ => Normal
        };
    }

    public static int GetPlayerLives(Difficulty difficulty, bool isTutorial = false)
    {
        if (isTutorial)
        {
            return Easy.playerLives;
        }
        return GetPreset(difficulty).playerLives;
    }

    public static int GetStartingMoney(Difficulty difficulty)
    {
        return GetPreset(difficulty).startingMoney;
    }

    public static float GetDifficultyHealthFactor(Difficulty difficulty)
    {
        return GetPreset(difficulty).enemyHealthFactor;
    }

    public static float GetDifficultySpeedFactor(Difficulty difficulty)
    {
        return GetPreset(difficulty).enemySpeedFactor;
    }

    public static float GetDifficultyDelayFactor(Difficulty difficulty)
    {
        return GetPreset(difficulty).spawnFrequencyFactor;
    }

    public static float GetDifficultyScoreMultiplier(Difficulty difficulty)
    {
        return GetPreset(difficulty).scoreMultiplier;
    }

    public static float GetDifficultyRewardMultiplier(Difficulty difficulty)
    {
        return GetPreset(difficulty).rewardMultiplier;
    }

    public static float GetWaveSpawnDelayFactor(int wave)
    {
        return Mathf.Max(MinWaveSpawnDelayFactor, 1f - (wave - 1) * WaveSpawnDelayDecreasePerWave);
    }

    public static (float min, float max) GetSpawnDelayRange(int wave, Difficulty difficulty)
    {
        float waveFactor = GetWaveSpawnDelayFactor(wave);
        float difficultyFactor = GetDifficultyDelayFactor(difficulty);
        float minDelay = MinSpawnDelayMultiplier * waveFactor * difficultyFactor;
        float maxDelay = MaxSpawnDelayMultiplier * waveFactor * difficultyFactor;
        if (maxDelay < minDelay)
        {
            float tmp = maxDelay;
            maxDelay = minDelay;
            minDelay = tmp;
        }
        minDelay = Mathf.Max(MinSpawnDelayCap, minDelay);
        maxDelay = Mathf.Max(MaxSpawnDelayCap, maxDelay);
        return (minDelay, maxDelay);
    }

    public static float GetWaveHealthMultiplier(int wave)
    {
        return 1f + (wave - 1) * WaveHealthIncreasePerWave;
    }

    public static EnemyTypeStats GetEnemyTypeStats(string typeName)
    {
        string normalized = typeName?.ToLowerInvariant() ?? string.Empty;
        foreach (var enemyType in EnemyTypes)
        {
            if (normalized.Contains(enemyType.typeName))
            {
                return enemyType;
            }
        }
        return EnemyTypes[0];
    }

    public static int GetEnemyReward(string typeName, Difficulty difficulty)
    {
        var stats = GetEnemyTypeStats(typeName);
        int reward = Mathf.RoundToInt(stats.baseReward * GetDifficultyRewardMultiplier(difficulty));
        return Mathf.Max(0, reward);
    }

    public static int GetEnemyCountForWave(int wave)
    {
        return 10 + (wave - 1) * 2;
    }

    public static void GetWaveEnemyCounts(int wave, out int simonCount, out int simonKingCount, out int ultraSimonCount)
    {
        simonCount = 0;
        simonKingCount = 0;
        ultraSimonCount = 0;

        int enemyCount = GetEnemyCountForWave(wave);
        if (wave < 5)
        {
            simonCount = enemyCount;
            return;
        }

        if (wave < 15)
        {
            float kingRatio = Mathf.Min(0.4f, 0.1f + (wave - 5) * 0.03f);
            simonKingCount = Mathf.RoundToInt(enemyCount * kingRatio);
            simonCount = enemyCount - simonKingCount;
            return;
        }

        float ultraRatio = Mathf.Min(0.4f, 0.1f + (wave - 15) * 0.04f);
        ultraSimonCount = Mathf.RoundToInt(enemyCount * ultraRatio);
        simonKingCount = Mathf.RoundToInt(enemyCount * 0.25f);
        simonCount = enemyCount - simonKingCount - ultraSimonCount;
    }

    public static TowerStats GetTowerStats(Tower.TowerClass towerClass)
    {
        foreach (var preset in TowerPresets)
        {
            if (preset.towerClass == towerClass)
            {
                return preset;
            }
        }
        return new TowerStats(towerClass, 20, 8f, 1f, 100);
    }

    public static int CalculateScore(int regularKills, int kingKills, int ultraKills, int completedWaves, float damageDealt, Difficulty difficulty)
    {
        float baseScore = 50f + 10f * (regularKills + 2f * kingKills + 4f * ultraKills) + 50f * completedWaves * (completedWaves + 1) + (damageDealt / 50f);
        return Mathf.RoundToInt(baseScore * GetDifficultyScoreMultiplier(difficulty));
    }
}
