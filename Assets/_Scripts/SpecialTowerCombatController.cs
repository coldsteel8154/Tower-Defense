using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpecialTowerCombatController : MonoBehaviour
{
    private struct MinePathSegment
    {
        public Vector2 start;
        public Vector2 delta;
        public float minimumProgress;
        public float maximumProgress;
        public float weight;
    }

    [SerializeField] private Sprite grenadeSprite;
    [SerializeField] private Sprite landmineSprite;
    [SerializeField] private GameObject reaperMarkPrefab;
    [SerializeField] private GameObject mortarMarkPrefab;
    [SerializeField] private GameObject impactVfxPrefab;
    [SerializeField] private Color railgunBeamColor = new Color(0.05f, 0.45f, 1f, 1f);

    private Tower tower;
    private TowerVisualController visual;
    private TowerRange towerRange;
    private ShockTrooperCombat shockTrooper;
    private HawkeyeOperatorCombat hawkeye;
    private LinebreakerVortexController linebreaker;
    private readonly HashSet<Tower> captainAuraTargets = new HashSet<Tower>();
    private int attackCount;
    private int killCount;
    private int tacticalCaptainKills;
    private int spotterKills;
    private int combatSpecialistStep;
    private int ironVanguardChargeHits;
    private float nextAbilityTime;
    private float frenzyUntil;
    private float ironVanguardSkillUntil;
    private float mortarCallUntil;
    private float nextMineTime;
    private float nextAuraRefreshTime;
    private float tacticalCaptainInitialStrikeTime;
    private float nextIronVanguardShotTime;
    private bool abilityInitialized;
    private bool reaperExecutionInProgress;
    private bool pathfinderLastAttackWasRanged;
    private bool ironVanguardSkillActive;
    private Vector3 ironVanguardReturnPosition;
    private Quaternion ironVanguardReturnRotation;
    private Vector2 ironVanguardFireDirection;
    private float nextIronVanguardFieldPulse;
    private bool gunnerModeInitialized;
    private bool gunnerReadyVisual;
    private bool guerillaModeInitialized;
    private bool guerillaReadyVisual;
    private bool tacticalCaptainInitialDelayStarted;
    private bool tacticalCaptainInitialDelayComplete;
    private string tacticalIdleState;

    private GameBalanceSettings.SpecialTowerEvolutionStats Definition =>
        GameBalanceSettings.Instance.GetSpecialTowerEvolutionStats(tower.specialTowerType);

    public bool IsSkillActive => ironVanguardSkillActive && Time.time < ironVanguardSkillUntil;

    private void Awake()
    {
        tower = GetComponent<Tower>();
        visual = GetComponentInChildren<TowerVisualController>();
        towerRange = GetComponentInChildren<TowerRange>();
        shockTrooper = GetComponent<ShockTrooperCombat>();
        hawkeye = GetComponent<HawkeyeOperatorCombat>();
        linebreaker = GetComponent<LinebreakerVortexController>();
    }

    private void Start()
    {
        InitializeAbility();
    }

    private void Update()
    {
        if (tower == null || CheatCommandSystem.isFrozen)
        {
            return;
        }

        if (!abilityInitialized)
        {
            InitializeAbility();
        }

        if (tower.specialTowerType == SpecialTowerType.TacticalCaptain && Time.time >= nextAuraRefreshTime)
        {
            RefreshCaptainAura();
            nextAuraRefreshTime = Time.time + 0.25f;
        }

        if (tower.specialTowerType == SpecialTowerType.Juggernaut && frenzyUntil > 0f && Time.time >= frenzyUntil)
        {
            frenzyUntil = 0f;
            visual?.StopAnimation();
        }

        if (tower.specialTowerType == SpecialTowerType.Spotter && Time.time >= nextAbilityTime)
        {
            MarkHighestHealthEnemy();
        }
        else if (tower.specialTowerType == SpecialTowerType.TacticalCaptain)
        {
            UpdateTacticalVisual();
            bool hasEnemies = HasLivingEnemies();
            if (!tacticalCaptainInitialDelayComplete && hasEnemies)
            {
                if (!tacticalCaptainInitialDelayStarted)
                {
                    tacticalCaptainInitialDelayStarted = true;
                    tacticalCaptainInitialStrikeTime = Time.time + Definition.abilityStartupDelay;
                }
                if (Time.time >= tacticalCaptainInitialStrikeTime)
                {
                    tacticalCaptainInitialDelayComplete = true;
                }
            }
            if (tacticalCaptainInitialDelayComplete && Time.time >= nextAbilityTime && hasEnemies)
            {
                CallMortar();
            }
        }
        else if (tower.specialTowerType == SpecialTowerType.GunnerCommander)
        {
            bool grenadeReady = Time.time >= nextAbilityTime;
            if (!gunnerModeInitialized || grenadeReady != gunnerReadyVisual)
            {
                gunnerReadyVisual = grenadeReady;
                gunnerModeInitialized = true;
                visual?.SetIdleState(grenadeReady ? "idle_ready" : "idle_empty");
            }
        }
        else if (tower.specialTowerType == SpecialTowerType.GuerillaEliminator)
        {
            if (IsWaveActive() && Time.time >= nextMineTime)
            {
                PlantMine();
            }
            bool loneWolf = !HasEnemyNear(tower.transform.position, Definition.secondaryRange);
            TowerStatusManager towerStatus = tower.GetComponent<TowerStatusManager>();
            bool hasLoneWolfBuff = towerStatus != null && towerStatus.HasEffectFromSource(
                StatusTagType.Damage, SpecialTowerType.GuerillaEliminator, "Lone Wolf +35% damage");
            if (loneWolf && !hasLoneWolfBuff)
            {
                tower.AddOrRefreshStatusEffect(StatusTagType.Damage, SpecialTowerType.GuerillaEliminator,
                    float.MaxValue, Definition.abilityChance, "Lone Wolf +35% damage");
            }
            else if (!loneWolf && hasLoneWolfBuff)
            {
                towerStatus.RemoveEffectsFromSource(StatusTagType.Damage,
                    SpecialTowerType.GuerillaEliminator, "Lone Wolf +35% damage");
            }
            bool mineReady = Time.time >= nextMineTime;
            if (!guerillaModeInitialized || mineReady != guerillaReadyVisual)
            {
                guerillaReadyVisual = mineReady;
                guerillaModeInitialized = true;
                visual?.SetIdleState(mineReady ? "idle_ready" : "idle_empty");
            }
        }
        else if (tower.specialTowerType == SpecialTowerType.IronVanguard && ironVanguardSkillActive)
        {
            if (Time.time >= ironVanguardSkillUntil)
            {
                EndIronVanguardSkill();
            }
            else
            {
                if (Time.time >= nextIronVanguardShotTime)
                {
                    FireIronVanguardShot();
                    nextIronVanguardShotTime = Time.time + Definition.level5Stats.fireRate;
                }
                if (Time.time >= nextIronVanguardFieldPulse)
                {
                    ApplyIronVanguardFieldEffects(false);
                    nextIronVanguardFieldPulse = Time.time + Mathf.Max(0.05f, Definition.abilityPulseInterval);
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (IsSkillActive && tower != null && ironVanguardFireDirection.sqrMagnitude > 0f)
        {
            tower.transform.up = -ironVanguardFireDirection;
        }
    }

    public float GetAttackIntervalMultiplier()
    {
        if (tower == null)
        {
            return 1f;
        }
        if (tower.specialTowerType == SpecialTowerType.Veteran &&
            HasEnemyNear(tower.transform.position, Definition.secondaryRange))
        {
            return Definition.secondaryFireRate / Definition.level5Stats.fireRate;
        }
        if (tower.specialTowerType == SpecialTowerType.Juggernaut && Time.time < frenzyUntil)
        {
            return Definition.abilityAttackIntervalMultiplier;
        }
        return 1f;
    }

    public bool TryAttack(Tower attackingTower, Enemy target)
    {
        if (attackingTower == null || target == null || target.health <= 0)
        {
            return false;
        }
        tower = attackingTower;
        if (tower.specialTowerType == SpecialTowerType.Veteran)
        {
            Enemy meleeTarget = FindNearestEnemy();
            if (meleeTarget != null && Vector2.Distance(tower.transform.position, meleeTarget.transform.position) <= Definition.secondaryRange)
            {
                target = meleeTarget;
            }
        }
        else if (tower.specialTowerType == SpecialTowerType.Pathfinder)
        {
            Enemy lowestHealthEnemy = FindLowestHealthEnemy();
            if (lowestHealthEnemy != null)
            {
                target = lowestHealthEnemy;
            }
        }
        InitializeAbility();

        if (tower.specialTowerType == SpecialTowerType.ShockTrooper && shockTrooper != null)
        {
            return shockTrooper.TryAttack(target);
        }

        attackCount++;
        float targetDistance = Vector2.Distance(tower.transform.position, target.transform.position);
        int damage = tower.damage;
        string fireKey = "fire";
        string idleKey = "idle";
        float fireDuration = 0.08f;

        if (tower.specialTowerType == SpecialTowerType.IronVanguard && IsSkillActive)
        {
            return true;
        }

        switch (tower.specialTowerType)
        {
            case SpecialTowerType.Veteran:
                if (targetDistance <= Definition.secondaryRange)
                {
                    damage = Mathf.RoundToInt(Definition.secondaryDamage);
                    fireKey = "melee";
                    fireDuration = Definition.secondaryFireRate;
                }
                break;

            case SpecialTowerType.Juggernaut:
                if (Time.time >= nextAbilityTime)
                {
                    frenzyUntil = Time.time + Definition.abilityDuration;
                    nextAbilityTime = Time.time + Definition.abilityCooldown;
                    visual?.PlayAnimation("skill_fire");
                }
                if (Time.time < frenzyUntil)
                {
                    HitCone(target, Definition.coneAngle, Definition.secondaryRange, Definition.level5Stats.damage,
                        Definition.abilityKnockback);
                    return true;
                }
                fireKey = attackCount % 2 == 0 ? "fire2" : "fire1";
                fireDuration = Definition.level5Stats.fireRate;
                break;

            case SpecialTowerType.GhostRecon:
                break;

            case SpecialTowerType.DrillSergeant:
                if (attackCount % Definition.abilityEveryNthHit == 0)
                {
                    ApplyAura(StatusTagType.Haste, Definition.abilityRadius, Definition.abilityDuration, Definition.abilityChance,
                        "Drill Sergeant +25% attack speed");
                    fireKey = "skill_fire";
                    fireDuration = 0.12f;
                }
                break;

            case SpecialTowerType.Spotter:
                break;

            case SpecialTowerType.GunnerCommander:
                bool grenadeReady = Time.time >= nextAbilityTime;
                fireKey = grenadeReady ? "fire_ready" : "fire_empty";
                idleKey = grenadeReady ? "idle_ready" : "idle_empty";
                break;

            case SpecialTowerType.ShredderVanguard:
                if (Random.value < Definition.abilityChance)
                {
                    ApplyKnockback(target, Definition.abilityKnockback);
                    fireKey = "skill_fire";
                }
                break;

            case SpecialTowerType.Pathfinder:
                if (targetDistance > Definition.secondaryRange)
                {
                    pathfinderLastAttackWasRanged = true;
                }
                else
                {
                    pathfinderLastAttackWasRanged = false;
                }
                if (targetDistance <= Definition.secondaryRange)
                {
                    damage = Mathf.RoundToInt(Definition.secondaryDamage);
                    fireKey = target.health / (float)target.maxHealth < Definition.abilityChance ? "skill_melee" : "melee";
                    fireDuration = Definition.secondaryFireRate;
                }
                else if (target.health / (float)target.maxHealth < Definition.abilityChance)
                {
                    damage = Mathf.RoundToInt(damage * Definition.abilityDamageMultiplier);
                    fireKey = "skill_fire";
                }
                break;

            case SpecialTowerType.BarrettOverload:
                int barrettShot = (attackCount - 1) % Definition.abilityEveryNthHit;
                if (barrettShot == 2)
                {
                    damage = Mathf.RoundToInt(Definition.abilityDamage);
                    fireKey = "fire_he";
                    HitArea(target.transform.position, Definition.abilityRadius, damage, Definition.abilityKnockback);
                    visual?.PlayOneShotEffect("skill_blast", target.transform.position,
                        Definition.secondaryFireRate, Definition.abilityRadius * 2f, 3);
                    PlayState(fireKey, idleKey, 0.15f);
                    return true;
                }
                if (barrettShot == 1)
                {
                    idleKey = "heat_idle";
                }
                break;

            case SpecialTowerType.Marksman:
                int marksmanTargetsHit = HitHitscan(Definition.secondaryRange, Definition.secondaryDamage,
                    Definition.secondaryHitWidth, Definition.maxTargets, Definition.abilityKnockback);
                bool piercedMultipleTargets = marksmanTargetsHit >= 2;
                PlayState(piercedMultipleTargets ? "skill_fire" : "fire", "idle",
                    piercedMultipleTargets ? 0.12f : 0.08f);
                return true;

            case SpecialTowerType.TacticalCaptain:
                if (Time.time < mortarCallUntil)
                {
                    fireKey = "fire_skill";
                    idleKey = "idle_skill";
                    fireDuration = Definition.secondaryFireRate;
                }
                else if (Time.time >= nextAbilityTime)
                {
                    fireKey = "fire_ready";
                    idleKey = "idle_ready";
                }
                else
                {
                    fireKey = "fire_empty";
                    idleKey = "idle_empty";
                }
                break;

            case SpecialTowerType.TrenchSweeper:
                HitConePellets(target, Definition.maxTargets, Definition.coneAngle,
                    tower.range, Definition.secondaryDamage, Definition.abilityChance);
                bool pointBlank = targetDistance <= Definition.secondaryRange;
                if (pointBlank)
                {
                    fireKey = "skill_fire";
                    fireDuration = Definition.abilityDuration;
                    visual?.PlayOneShotEffect("skill_blast", target.transform.position,
                        Definition.secondaryFireRate, sortingOrder: 3);
                }
                PlayState(pointBlank ? fireKey : "fire", "idle", pointBlank ? fireDuration : 0.08f);
                return true;

            case SpecialTowerType.SiegeDestroyer:
                damage = Mathf.RoundToInt(Definition.secondaryDamage * Definition.abilityDamageMultiplier);
                bool siegeOverload = attackCount % Definition.abilityEveryNthHit == 0;
                ApplyKnockback(target, siegeOverload ? Definition.secondaryRange : Definition.abilityKnockback);
                if (siegeOverload)
                {
                    fireKey = "skill_fire";
                    fireDuration = Definition.abilityDuration;
                }
                break;

            case SpecialTowerType.IronVanguard:
                if (IsSkillActive)
                {
                    fireKey = "shield_fire";
                    idleKey = "shield_idle";
                    fireDuration = 0.06f;
                }
                break;

            case SpecialTowerType.GuerillaEliminator:
                bool mineReady = Time.time >= nextMineTime;
                fireKey = mineReady ? "fire_ready" : "fire_empty";
                idleKey = mineReady ? "idle_ready" : "idle_empty";
                if (!HasEnemyNear(tower.transform.position, Definition.secondaryRange))
                {
                    tower.AddOrRefreshStatusEffect(StatusTagType.Damage, SpecialTowerType.GuerillaEliminator,
                        float.MaxValue, Definition.abilityChance, "Lone Wolf +35% damage");
                    damage = tower.damage;
                }
                break;

            case SpecialTowerType.RailgunOperator:
                StartCoroutine(RailgunAttackRoutine());
                return true;

            case SpecialTowerType.HawkeyeOperator:
                if (hawkeye != null && hawkeye.IsUplinkActive)
                {
                    fireKey = "skill_fire";
                    idleKey = "skill_idle";
                    fireDuration = Definition.secondaryFireRate;
                }
                break;

            case SpecialTowerType.CombatSpecialist:
                combatSpecialistStep = (combatSpecialistStep % Definition.abilityEveryNthHit) + 1;
                fireKey = combatSpecialistStep == 3 ? "skill_fire" : combatSpecialistStep == 2 ? "fire2" : "fire1";
                idleKey = combatSpecialistStep == 2 ? "idle2" : "idle1";
                if (combatSpecialistStep == 3)
                {
                    damage = Mathf.RoundToInt(damage * Definition.abilityDamageMultiplier);
                    ApplyKnockback(target, Definition.abilityKnockback);
                    combatSpecialistStep = 0;
                }
                break;

            case SpecialTowerType.LinebreakerScout:
                if (attackCount % Definition.abilityEveryNthHit == 0)
                {
                    damage = Mathf.RoundToInt(Definition.secondaryDamage);
                    ApplyKnockback(target, Definition.secondaryRange);
                    linebreaker?.ActivateVortex(target.transform.position);
                    fireKey = "skill_fire";
                }
                break;

            case SpecialTowerType.HeavyMarksman:
                if (attackCount % Definition.abilityEveryNthHit == 0)
                {
                    HitHitscan(Definition.secondaryRange, Definition.secondaryDamage, Definition.secondaryHitWidth, Definition.maxTargets,
                        Definition.abilityKnockback, true, Definition.abilityDuration);
                    PlayState("skill_fire", "idle", 0.1f);
                    return true;
                }
                fireKey = attackCount % 2 == 0 ? "fire2" : "fire1";
                HitHitscan(Definition.secondaryRange, Definition.level5Stats.damage, Definition.secondaryHitWidth,
                    Definition.secondaryMaxTargets,
                    Definition.secondaryFireRate);
                PlayState(fireKey, "idle", 0.06f);
                return true;
        }

        if (tower.specialTowerType == SpecialTowerType.HawkeyeOperator)
        {
            hawkeye?.ApplyAttackEffect(target);
        }
        int healthBeforeDamage = target.health;
        DealDamage(target, damage);
        if (tower.specialTowerType == SpecialTowerType.IronVanguard && target.health < healthBeforeDamage &&
            !ironVanguardSkillActive)
        {
            ironVanguardChargeHits++;
            if (ironVanguardChargeHits >= Definition.abilityEveryNthHit)
            {
                ironVanguardChargeHits = 0;
                BeginIronVanguardSkill();
                fireKey = "shield_fire";
                idleKey = "shield_idle";
                fireDuration = 0.06f;
            }
        }
        if (tower.specialTowerType == SpecialTowerType.GuerillaEliminator && Time.time >= nextMineTime)
        {
            fireKey = "fire_empty";
            idleKey = "idle_empty";
        }
        if (tower.specialTowerType == SpecialTowerType.GunnerCommander &&
            attackCount % Definition.abilityEveryNthHit == 0 && Time.time >= nextAbilityTime)
        {
            ThrowGrenade(target.transform.position);
            fireKey = "fire_empty";
            idleKey = "idle_empty";
        }

        PlayState(fireKey, idleKey, fireDuration);
        return true;
    }

    public void OnAttackHit(Tower attackingTower, Enemy target)
    {
        if (tower == null)
        {
            tower = attackingTower;
            InitializeAbility();
        }
        if (tower.specialTowerType == SpecialTowerType.DrillSergeant)
        {
            attackCount++;
            if (attackCount % Definition.abilityEveryNthHit == 0)
            {
                ApplyAura(StatusTagType.Haste, Definition.abilityRadius, Definition.abilityDuration, Definition.abilityChance,
                    "Drill Sergeant +25% attack speed");
                PlayState("skill_fire", "idle", 0.12f);
            }
        }
        else if (tower.specialTowerType == SpecialTowerType.GuerillaEliminator && !HasEnemyNear(tower.transform.position, Definition.secondaryRange))
        {
            tower.AddOrRefreshStatusEffect(StatusTagType.Damage, SpecialTowerType.GuerillaEliminator, float.MaxValue,
                Definition.abilityChance, "Lone Wolf +35% damage");
        }
    }

    public void OnAttackKill(Tower attackingTower, Enemy target)
    {
        if (tower == null)
        {
            tower = attackingTower;
            InitializeAbility();
        }
        HandleKill(target);
    }

    private void InitializeAbility()
    {
        if (abilityInitialized || tower == null || tower.specialTowerType == SpecialTowerType.None)
        {
            return;
        }
        abilityInitialized = true;
        SpecialTowerType type = tower.specialTowerType;
        if (type == SpecialTowerType.ShockTrooper)
        {
            shockTrooper = shockTrooper != null ? shockTrooper : GetComponent<ShockTrooperCombat>();
            if (shockTrooper != null)
            {
                shockTrooper.Configure(Definition.level5Stats.damage, Definition.level5Stats.fireRate,
                    tower.range, Definition.abilityChance, Definition.abilityEveryNthHit,
                    Definition.abilityRadius, Definition.abilityDuration);
            }
        }
        else if (type == SpecialTowerType.HawkeyeOperator)
        {
            hawkeye = hawkeye != null ? hawkeye : GetComponent<HawkeyeOperatorCombat>();
            hawkeye?.Configure(Definition.abilityDuration, Definition.abilitySlow, Definition.abilityChance,
                Definition.abilityRadius, Definition.secondaryDamage, Definition.abilityDuration);
        }
        else if (type == SpecialTowerType.LinebreakerScout)
        {
            linebreaker = linebreaker != null ? linebreaker : GetComponent<LinebreakerVortexController>();
            linebreaker?.Configure(Definition.abilityRadius, Definition.abilityDuration, Definition.abilitySlow,
                Definition.abilityPulseInterval, Definition.abilityKnockback,
                visual != null ? visual.GetSpriteStateFrames("skill_swirl") : null,
                visual != null ? visual.GetSpriteStateFrameRate("skill_swirl") : 15f);
        }
        if (type == SpecialTowerType.TacticalCaptain)
        {
            RefreshCaptainAura();
            nextAuraRefreshTime = Time.time + 0.25f;
            nextAbilityTime = Time.time + Definition.abilityCooldown;
        }
        else if (type == SpecialTowerType.Juggernaut)
        {
            nextAbilityTime = Time.time + Definition.abilityCooldown;
        }
        else if (type == SpecialTowerType.Spotter)
        {
            nextAbilityTime = Time.time;
        }
        else if (type == SpecialTowerType.GuerillaEliminator)
        {
            nextMineTime = Time.time;
        }
    }

    private void HandleKill(Enemy target)
    {
        if (target == null || tower == null)
        {
            return;
        }

        if (tower.specialTowerType == SpecialTowerType.Veteran)
        {
            ApplyAura(StatusTagType.Damage, Definition.abilityRadius, Definition.abilityDuration, Definition.auraDamageBonus,
                "Veteran Rally +30% damage");
            ApplyAura(StatusTagType.Haste, Definition.abilityRadius, Definition.abilityDuration, Definition.auraAttackSpeedBonus,
                "Veteran Rally +20% attack speed");
        }
        else if (tower.specialTowerType == SpecialTowerType.GhostRecon)
        {
            if (reaperExecutionInProgress)
            {
                return;
            }
            killCount++;
            if (killCount >= Definition.abilityEveryNthHit)
            {
                killCount = 0;
                ExecuteNearestToPathEnd();
            }
        }
        else if (tower.specialTowerType == SpecialTowerType.Pathfinder)
        {
            if (pathfinderLastAttackWasRanged)
            {
                tower.ResetAttackCooldown();
            }
        }
        else if (tower.specialTowerType == SpecialTowerType.TacticalCaptain)
        {
            tacticalCaptainKills++;
        }
        else if (tower.specialTowerType == SpecialTowerType.Spotter)
        {
            spotterKills++;
        }
    }

    private void ApplyAura(StatusTagType type, float radius, float duration, float intensity, string description)
    {
        Tower[] towers = FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
        foreach (Tower ally in towers)
        {
            if (ally != null && Vector2.Distance(ally.transform.position, tower.transform.position) <= radius)
            {
                ally.AddOrRefreshStatusEffect(type, tower.specialTowerType, duration, intensity, description);
            }
        }
    }

    private void RefreshCaptainAura()
    {
        string damageDescription = $"Tactical Captain +{Definition.auraDamageBonus:P0} damage";
        string rangeDescription = $"Tactical Captain +{Definition.auraRangeBonus:P0} range";
        HashSet<Tower> currentTargets = new HashSet<Tower>();
        Tower[] towers = FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
        foreach (Tower ally in towers)
        {
            if (ally == null || Vector2.Distance(ally.transform.position, tower.transform.position) > Definition.abilityRadius)
            {
                continue;
            }

            currentTargets.Add(ally);
            ally.AddOrRefreshStatusEffect(StatusTagType.Damage, SpecialTowerType.TacticalCaptain,
                float.MaxValue, Definition.auraDamageBonus, damageDescription);
            ally.AddOrRefreshStatusEffect(StatusTagType.Range, SpecialTowerType.TacticalCaptain,
                float.MaxValue, Definition.auraRangeBonus, rangeDescription);
        }

        foreach (Tower previousTarget in captainAuraTargets)
        {
            if (previousTarget == null || currentTargets.Contains(previousTarget))
            {
                continue;
            }

            TowerStatusManager statusManager = previousTarget.GetComponent<TowerStatusManager>();
            statusManager?.RemoveEffectsFromSource(StatusTagType.Damage, SpecialTowerType.TacticalCaptain, damageDescription);
            statusManager?.RemoveEffectsFromSource(StatusTagType.Range, SpecialTowerType.TacticalCaptain, rangeDescription);
        }

        captainAuraTargets.Clear();
        captainAuraTargets.UnionWith(currentTargets);
    }

    private void UpdateTacticalVisual()
    {
        string idleKey = Time.time < mortarCallUntil
            ? "idle_skill"
            : Time.time >= nextAbilityTime ? "idle_ready" : "idle_empty";
        if (!string.Equals(idleKey, tacticalIdleState, System.StringComparison.Ordinal))
        {
            tacticalIdleState = idleKey;
            visual?.SetIdleState(idleKey);
        }
    }

    private bool HasLivingEnemies()
    {
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
        {
            if (enemy != null && enemy.health > 0)
            {
                return true;
            }
        }
        return false;
    }

    private void MarkHighestHealthEnemy()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude);
        Enemy highestHealth = null;
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.health <= 0 || Vector2.Distance(tower.transform.position, enemy.transform.position) > tower.range)
            {
                continue;
            }
            if (highestHealth == null || enemy.health > highestHealth.health)
            {
                highestHealth = enemy;
            }
        }
        nextAbilityTime = Time.time + Definition.abilityCooldown;
        if (highestHealth != null)
        {
            StartCoroutine(SpotterMarkRoutine(highestHealth));
        }
    }

    private System.Collections.IEnumerator SpotterMarkRoutine(Enemy target)
    {
        visual?.PlaySpriteStateWithNextIdle("skill_idle", Definition.secondaryFireRate, "idle");
        yield return new WaitForSeconds(Definition.secondaryFireRate);
        if (target == null || target.health <= 0)
        {
            yield break;
        }

        visual?.PlaySpriteStateWithNextIdle("skill_fire", Definition.abilitySlowDuration, "idle");
        target.GetComponent<EnemyStatusManager>()?.AddEffect(StatusTagType.Vulnerable, SpecialTowerType.Spotter,
            Definition.abilityDuration, Definition.abilityDamage, "Spotter +30% damage taken");
    }

    private void CallMortar()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude);
        Vector2 bestCenter = tower.transform.position;
        int mostEnemies = 0;
        foreach (Enemy candidate in enemies)
        {
            if (candidate == null || candidate.health <= 0)
            {
                continue;
            }
            int count = 0;
            foreach (Enemy other in enemies)
            {
                if (other != null && Vector2.Distance(candidate.transform.position, other.transform.position) <= Definition.secondaryRange)
                {
                    count++;
                }
            }
            if (count > mostEnemies)
            {
                mostEnemies = count;
                bestCenter = candidate.transform.position;
            }
        }

        if (mostEnemies == 0)
        {
            return;
        }
        nextAbilityTime = Time.time + Definition.abilityCooldown;
        if (mortarMarkPrefab != null)
        {
            GameObject mark = Instantiate(mortarMarkPrefab, bestCenter, Quaternion.identity);
            Destroy(mark, Definition.secondaryFireRate);
        }
        mortarCallUntil = Time.time + Definition.secondaryFireRate;
        tacticalIdleState = "idle_skill";
        visual?.SetIdleState(tacticalIdleState);
        StartCoroutine(MortarImpact(bestCenter));
    }

    private System.Collections.IEnumerator MortarImpact(Vector2 center)
    {
        yield return new WaitForSeconds(Definition.secondaryFireRate);
        HitArea(center, Definition.secondaryRange, Definition.abilityDamage, Definition.abilityKnockback);
    }

    private void ExecuteNearestToPathEnd()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsInactive.Exclude);
        Transform[] points = EnemyManager.main != null ? EnemyManager.main.checkpoints : null;
        if (points == null || points.Length == 0)
        {
            return;
        }
        Enemy target = null;
        float closestDistance = float.MaxValue;
        Vector2 end = points[points.Length - 1].position;
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.health <= 0)
            {
                continue;
            }
            string enemyName = enemy.gameObject.name.ToLowerInvariant();
            if (enemyName.Contains("king") || enemyName.Contains("ultra"))
            {
                continue;
            }
            float distance = Vector2.Distance(enemy.transform.position, end);
            if (distance < closestDistance)
            {
                target = enemy;
                closestDistance = distance;
            }
        }
        if (target == null)
        {
            return;
        }

        StartCoroutine(ReaperExecutionRoutine(target));
    }

    private System.Collections.IEnumerator ReaperExecutionRoutine(Enemy target)
    {
        reaperExecutionInProgress = true;
        GameObject mark = null;
        if (reaperMarkPrefab != null)
        {
            mark = Instantiate(reaperMarkPrefab, target.transform.position + Vector3.up,
                Quaternion.identity, target.transform);
            Vector3 targetScale = target.transform.lossyScale;
            mark.transform.localScale = new Vector3(
                1f / Mathf.Max(0.01f, Mathf.Abs(targetScale.x)),
                1f / Mathf.Max(0.01f, Mathf.Abs(targetScale.y)),
                1f / Mathf.Max(0.01f, Mathf.Abs(targetScale.z)));
        }

        GameObject beamObject = new GameObject("ReaperExecutionBeam");
        LineRenderer beam = beamObject.AddComponent<LineRenderer>();
        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.startWidth = 0.015f;
        beam.endWidth = 0.015f;
        beam.startColor = new Color(1f, 0.05f, 0.08f, 0.9f);
        beam.endColor = new Color(1f, 0.05f, 0.08f, 0.6f);
        beam.sortingLayerName = "VFX";
        Shader shader = Shader.Find("Sprites/Default");
        Material beamMaterial = shader != null ? new Material(shader) : null;
        if (beamMaterial != null)
        {
            beam.sharedMaterial = beamMaterial;
        }

        visual?.PlaySpriteStateWithNextIdle("skill_idle", Definition.abilityDuration, "idle");
        float elapsed = 0f;
        while (elapsed < Definition.abilityDuration && target != null && target.health > 0)
        {
            beam.SetPosition(0, tower.transform.position);
            beam.SetPosition(1, target.transform.position);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (target != null && target.health > 0)
        {
            visual?.PlaySpriteStateWithNextIdle("skill_fire", Definition.secondaryFireRate, "idle");
            elapsed = 0f;
            while (elapsed < Definition.secondaryFireRate && target != null && target.health > 0)
            {
                beam.SetPosition(0, tower.transform.position);
                beam.SetPosition(1, target.transform.position);
                elapsed += Time.deltaTime;
                yield return null;
            }
            if (target != null && target.health > 0)
            {
                DealDamage(target, target.health);
            }
        }

        if (mark != null)
        {
            Destroy(mark);
        }
        Destroy(beamObject);
        if (beamMaterial != null)
        {
            Destroy(beamMaterial);
        }
        reaperExecutionInProgress = false;
    }

    private void PlantMine()
    {
        if (landmineSprite == null || Path.path == null || Path.path.point == null)
        {
            return;
        }

        if (!TryFindRandomMinePosition(out Vector3 minePosition))
        {
            return;
        }

        GameObject mine = new GameObject("GuerillaLandmine");
        mine.transform.position = minePosition;
        SpriteRenderer renderer = mine.AddComponent<SpriteRenderer>();
        renderer.sprite = landmineSprite;
        renderer.sortingLayerName = "Path";
        renderer.sortingOrder = 2;
        mine.transform.localScale = Vector3.one * Definition.landmineVisualScaleMultiplier;
        mine.AddComponent<GuerillaLandmine>().Configure(Definition.abilityTriggerRadius, Definition.abilityRadius,
            Definition.abilityDamage, Definition.abilityKnockback, Definition.abilitySlow,
            Definition.abilitySlowDuration);
        nextMineTime = Time.time + Definition.abilityCooldown;
        guerillaReadyVisual = false;
        guerillaModeInitialized = true;
        visual?.SetIdleState("idle_empty");
    }

    private bool TryFindRandomMinePosition(out Vector3 position)
    {
        position = tower.transform.position;
        Transform[] points = Path.path != null ? Path.path.point : null;
        if (points == null || points.Length == 0)
        {
            return false;
        }
        if (points.Length == 1)
        {
            if (points[0] == null || Vector2.Distance(tower.transform.position, points[0].position) > tower.range)
            {
                return false;
            }
            position = points[0].position;
            return true;
        }

        Vector2 towerPosition = tower.transform.position;
        float range = Mathf.Max(0f, tower.range);
        List<MinePathSegment> candidateSegments = new List<MinePathSegment>();
        float totalWeight = 0f;
        for (int index = 0; index < points.Length - 1; index++)
        {
            if (points[index] == null || points[index + 1] == null)
            {
                continue;
            }

            Vector2 start = points[index].position;
            Vector2 delta = (Vector2)points[index + 1].position - start;
            float lengthSquared = delta.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                continue;
            }

            Vector2 offset = start - towerPosition;
            float projection = Vector2.Dot(offset, delta);
            float discriminant = projection * projection - lengthSquared * (offset.sqrMagnitude - range * range);
            if (discriminant < 0f)
            {
                continue;
            }

            float root = Mathf.Sqrt(discriminant);
            float minimumProgress = Mathf.Clamp01((-projection - root) / lengthSquared);
            float maximumProgress = Mathf.Clamp01((-projection + root) / lengthSquared);
            if (maximumProgress <= minimumProgress)
            {
                continue;
            }

            float weight = Mathf.Sqrt(lengthSquared) * (maximumProgress - minimumProgress);
            candidateSegments.Add(new MinePathSegment
            {
                start = start,
                delta = delta,
                minimumProgress = minimumProgress,
                maximumProgress = maximumProgress,
                weight = weight
            });
            totalWeight += weight;
        }

        if (candidateSegments.Count == 0 || totalWeight <= 0f)
        {
            return false;
        }

        float selection = Random.value * totalWeight;
        MinePathSegment selected = candidateSegments[candidateSegments.Count - 1];
        foreach (MinePathSegment segment in candidateSegments)
        {
            selection -= segment.weight;
            if (selection <= 0f)
            {
                selected = segment;
                break;
            }
        }

        float progress = Random.Range(selected.minimumProgress, selected.maximumProgress);
        Vector2 selectedPoint = selected.start + selected.delta * progress;
        position = new Vector3(selectedPoint.x, selectedPoint.y, points[0].position.z);
        return true;
    }

    private bool IsWaveActive()
    {
        EnemyManager enemyManager = EnemyManager.main;
        if (enemyManager == null || DifficultySettings.isTutorial)
        {
            return false;
        }

        bool enemiesPresent = SpawnManager.enemy_list != null && SpawnManager.enemy_list.Exists(enemy => enemy != null);
        return !enemyManager.wavedone || enemiesPresent;
    }

    private bool HasEnemyNear(Vector2 center, float radius)
    {
        return Physics2D.OverlapCircle(center, radius, LayerMask.GetMask("Enemy")) != null;
    }

    private Enemy FindLowestHealthEnemy()
    {
        Enemy bestTarget = null;
        float lowestHealthRatio = float.MaxValue;
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
        {
            if (enemy == null || enemy.health <= 0 ||
                Vector2.Distance(tower.transform.position, enemy.transform.position) > tower.range)
            {
                continue;
            }

            float healthRatio = enemy.maxHealth > 0 ? enemy.health / (float)enemy.maxHealth : 1f;
            if (healthRatio < lowestHealthRatio)
            {
                lowestHealthRatio = healthRatio;
                bestTarget = enemy;
            }
        }
        return bestTarget;
    }

    private Enemy FindNearestEnemy()
    {
        Enemy bestTarget = null;
        float nearestDistance = tower.range;
        foreach (Enemy enemy in FindObjectsByType<Enemy>(FindObjectsInactive.Exclude))
        {
            if (enemy == null || enemy.health <= 0)
            {
                continue;
            }

            float distance = Vector2.Distance(tower.transform.position, enemy.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                bestTarget = enemy;
            }
        }
        return bestTarget;
    }

    private void HitArea(Vector2 center, float radius, float damage, float knockback,
        StatusTagType statusType = StatusTagType.Slowed, float statusIntensity = 0f, float statusDuration = 0f)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Enemy"));
        HashSet<Enemy> hitEnemies = new HashSet<Enemy>();
        foreach (Collider2D hitCollider in colliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>() ?? hitCollider.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.health <= 0 || !hitEnemies.Add(enemy))
            {
                continue;
            }
            DealDamage(enemy, Mathf.RoundToInt(damage));
            ApplyKnockback(enemy, knockback);
            if (statusDuration > 0f)
            {
                enemy.GetComponent<EnemyStatusManager>()?.AddEffect(statusType, tower.specialTowerType,
                    statusDuration, statusIntensity, "Special tower area effect");
            }
        }
        if (impactVfxPrefab != null)
        {
            Instantiate(impactVfxPrefab, center, Quaternion.identity);
        }
    }

    private void HitCone(Enemy target, float coneAngle, float range, float damage, float knockback)
    {
        List<Enemy> enemies = CombatMathEngine.GetEnemiesInCone(tower.transform.position, -tower.transform.up,
            range, coneAngle, LayerMask.GetMask("Enemy"));
        foreach (Enemy enemy in enemies)
        {
            DealDamage(enemy, Mathf.RoundToInt(damage));
            ApplyKnockback(enemy, knockback);
        }
    }

    private void HitConePellets(Enemy target, int pelletCount, float coneAngle, float range, float pelletDamage, float closeBonus)
    {
        if (pelletCount <= 0 || range <= 0f)
        {
            return;
        }

        Vector2 origin = tower.transform.position;
        Vector2 forward = -tower.transform.up;
        LayerMask enemyMask = LayerMask.GetMask("Enemy");
        for (int pelletIndex = 0; pelletIndex < pelletCount; pelletIndex++)
        {
            float spreadProgress = pelletCount == 1 ? 0.5f : pelletIndex / (float)(pelletCount - 1);
            float angle = Mathf.Lerp(-coneAngle * 0.5f, coneAngle * 0.5f, spreadProgress);
            Vector2 direction = Quaternion.Euler(0f, 0f, angle) * forward;
            RaycastHit2D[] hits = CombatMathEngine.PerformHitscan(origin, direction, range, 0f, enemyMask);
            foreach (RaycastHit2D hit in hits)
            {
                Enemy enemy = hit.collider != null
                    ? hit.collider.GetComponent<Enemy>() ?? hit.collider.GetComponentInParent<Enemy>()
                    : null;
                if (enemy == null || enemy.health <= 0)
                {
                    continue;
                }

                float distanceFactor = 1f - Mathf.Clamp01(Vector2.Distance(origin, enemy.transform.position) / range);
                float damage = pelletDamage * (1f + closeBonus * distanceFactor);
                DealDamage(enemy, Mathf.RoundToInt(damage));
                break;
            }
        }
    }

    private int HitHitscan(float range, float damage, float width, int maxTargets, float knockback,
        bool stun = false, float stunDuration = 0f)
    {
        RaycastHit2D[] hits = CombatMathEngine.PerformHitscan(tower.transform.position, -tower.transform.up,
            range, width, LayerMask.GetMask("Enemy"));
        HashSet<Enemy> uniqueEnemies = new HashSet<Enemy>();
        int count = 0;
        foreach (RaycastHit2D hit in hits)
        {
            Enemy enemy = hit.collider != null ? hit.collider.GetComponent<Enemy>() ?? hit.collider.GetComponentInParent<Enemy>() : null;
            if (enemy == null || enemy.health <= 0 || !uniqueEnemies.Add(enemy))
            {
                continue;
            }
            DealDamage(enemy, Mathf.RoundToInt(damage));
            ApplyKnockback(enemy, knockback);
            if (stun)
            {
                enemy.GetComponent<EnemyStatusManager>()?.AddEffect(StatusTagType.Dazzled, tower.specialTowerType,
                    stunDuration, 0f, "Heavy Marksman micro-stun");
            }
            count++;
            if (maxTargets > 0 && count >= maxTargets)
            {
                break;
            }
        }
        return count;
    }

    private void HitRailgun(float range, float width, float damage, float knockback, float slow, float slowDuration)
    {
        RaycastHit2D[] hits = CombatMathEngine.PerformHitscan(tower.transform.position, -tower.transform.up,
            range, width, LayerMask.GetMask("Enemy"));
        HashSet<Enemy> uniqueEnemies = new HashSet<Enemy>();
        foreach (RaycastHit2D hit in hits)
        {
            Enemy enemy = hit.collider != null ? hit.collider.GetComponent<Enemy>() ?? hit.collider.GetComponentInParent<Enemy>() : null;
            if (enemy == null || enemy.health <= 0 || !uniqueEnemies.Add(enemy))
            {
                continue;
            }
            DealDamage(enemy, Mathf.RoundToInt(damage));
            ApplyKnockback(enemy, knockback);
            enemy.GetComponent<EnemyStatusManager>()?.AddEffect(StatusTagType.Slowed, SpecialTowerType.RailgunOperator,
                slowDuration, slow, "Railgun beam slow");
        }
    }

    private System.Collections.IEnumerator RailgunAttackRoutine()
    {
        visual?.PlaySpriteStateWithNextIdle("charge", Definition.secondaryFireRate, "idle");
        yield return new WaitForSeconds(Definition.secondaryFireRate);
        if (tower == null)
        {
            yield break;
        }

        HitRailgun(tower.range, Definition.secondaryRange, Definition.level5Stats.damage,
            Definition.abilityKnockback, Definition.abilitySlow, Definition.abilitySlowDuration);
        Vector2 beamDirection = -tower.transform.up;
        visual?.PlaySpriteStateWithNextIdle("fire", Definition.visualFireDuration, "idle");
        StartCoroutine(RailgunBeamVisualRoutine(tower.transform.position, beamDirection,
            tower.range, Definition.secondaryRange, Definition.visualFireDuration, railgunBeamColor));
    }

    private System.Collections.IEnumerator RailgunBeamVisualRoutine(Vector2 origin, Vector2 direction,
        float length, float width, float duration, Color beamColor)
    {
        if (duration <= 0f || direction.sqrMagnitude <= Mathf.Epsilon)
        {
            yield break;
        }

        direction.Normalize();
        Vector3 start = origin;
        Vector3 end = origin + direction * length;
        Shader beamShader = Shader.Find("Sprites/Default");
        if (beamShader == null)
        {
            Debug.LogError("Railgun beam requires the Sprites/Default shader.", this);
            yield break;
        }
        Material beamMaterial = new Material(beamShader);
        if (beamMaterial.HasProperty("_Color"))
        {
            beamMaterial.SetColor("_Color", Color.white);
        }

        GameObject beamObject = new GameObject("RailgunBeamVFX");
        LineRenderer beam = beamObject.AddComponent<LineRenderer>();
        beam.useWorldSpace = true;
        beam.sharedMaterial = beamMaterial;
        beam.positionCount = 2;
        beam.startWidth = width;
        beam.endWidth = width * 0.2f;
        Color beamTailColor = beamColor;
        beamTailColor.a *= 0.35f;
        beam.startColor = beamColor;
        beam.endColor = beamTailColor;
        beam.sortingLayerName = "VFX";
        beam.sortingOrder = 4;
        beam.SetPosition(0, start);
        beam.SetPosition(1, end);

        GameObject sparkObject = new GameObject("RailgunBeamParticles");
        ParticleSystem particles = sparkObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = duration;
        main.loop = false;
        main.startLifetime = duration;
        main.startSpeed = 0f;
        main.startSize = width * 0.3f;
        main.startColor = beamColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = false;
        ParticleSystemRenderer particleRenderer = sparkObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = beamMaterial;
        particleRenderer.sortingLayerName = "VFX";
        particleRenderer.sortingOrder = 5;

        float spacing = Mathf.Max(width * 0.5f, width);
        int particleCount = Mathf.CeilToInt(length / spacing) + 1;
        Vector3 particleDirection = new Vector3(direction.x, direction.y, 0f);
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
        Color particleTailColor = beamColor;
        particleTailColor.a *= 0.25f;
        for (int index = 0; index < particleCount; index++)
        {
            float progress = particleCount > 1 ? index / (float)(particleCount - 1) : 0f;
            ParticleSystem.EmitParams particle = new ParticleSystem.EmitParams
            {
                position = Vector3.Lerp(start, end, progress),
                velocity = particleDirection * (length * 0.35f / duration)
                    + perpendicular * (UnityEngine.Random.Range(-width, width) / duration),
                startColor = Color.Lerp(beamColor, particleTailColor, progress),
                startSize = width * 0.3f,
                startLifetime = duration
            };
            particles.Emit(particle, 1);
        }

        particles.Play();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float pulse = 0.75f + 0.25f * Mathf.Sin(elapsed / duration * Mathf.PI * 4f);
            beam.startWidth = width * pulse;
            beam.endWidth = width * 0.2f * pulse;
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(beamObject);
        Destroy(sparkObject);
        Destroy(beamMaterial);
    }

    private void BeginIronVanguardSkill()
    {
        ironVanguardReturnPosition = tower.transform.position;
        ironVanguardReturnRotation = tower.transform.rotation;
        if (FindNearestPathSegment(ironVanguardReturnPosition, out Vector2 pathPoint, out Vector2 pathDirection))
        {
            tower.transform.position = new Vector3(pathPoint.x, pathPoint.y, ironVanguardReturnPosition.z);
            ironVanguardFireDirection = pathDirection;
        }
        else
        {
            ironVanguardFireDirection = tower.transform.up;
        }

        ironVanguardSkillActive = true;
        ironVanguardSkillUntil = Time.time + Definition.abilityDuration;
        nextIronVanguardShotTime = Time.time;
        nextIronVanguardFieldPulse = Time.time + Mathf.Max(0.05f, Definition.abilityPulseInterval);
        tower.transform.up = -ironVanguardFireDirection;
        towerRange?.SetIronVanguardSkillConeVisible(true);
        ApplyIronVanguardFieldEffects(true);
        visual?.SetIdleState("shield_idle");
    }

    private bool FindNearestPathSegment(Vector2 position, out Vector2 nearestPoint, out Vector2 directionToEarlierCheckpoint)
    {
        nearestPoint = position;
        directionToEarlierCheckpoint = Vector2.zero;
        Transform[] points = EnemyManager.main != null ? EnemyManager.main.checkpoints : null;
        if (points == null || points.Length < 2)
        {
            points = Path.path != null ? Path.path.point : null;
        }
        if (points == null || points.Length < 2)
        {
            return false;
        }

        float closestDistanceSquared = float.MaxValue;
        for (int index = 0; index < points.Length - 1; index++)
        {
            if (points[index] == null || points[index + 1] == null)
            {
                continue;
            }

            Vector2 start = points[index].position;
            Vector2 end = points[index + 1].position;
            Vector2 segment = end - start;
            float progress = segment.sqrMagnitude > 0f
                ? Mathf.Clamp01(Vector2.Dot(position - start, segment) / segment.sqrMagnitude)
                : 0f;
            Vector2 candidate = start + segment * progress;
            float distanceSquared = (position - candidate).sqrMagnitude;
            if (distanceSquared >= closestDistanceSquared)
            {
                continue;
            }

            closestDistanceSquared = distanceSquared;
            nearestPoint = candidate;
            directionToEarlierCheckpoint = start - candidate;
            if (directionToEarlierCheckpoint.sqrMagnitude <= Mathf.Epsilon)
            {
                directionToEarlierCheckpoint = start - end;
            }
        }

        if (closestDistanceSquared == float.MaxValue)
        {
            return false;
        }
        directionToEarlierCheckpoint.Normalize();
        return directionToEarlierCheckpoint.sqrMagnitude > Mathf.Epsilon;
    }

    private void ApplyIronVanguardFieldEffects(bool applyKnockback)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(tower.transform.position,
            Definition.abilityRadius, LayerMask.GetMask("Enemy"));
        HashSet<Enemy> affected = new HashSet<Enemy>();
        foreach (Collider2D collider in colliders)
        {
            Enemy enemy = collider.GetComponent<Enemy>() ?? collider.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.health <= 0 || !affected.Add(enemy))
            {
                continue;
            }

            enemy.GetComponent<EnemyStatusManager>()?.AddOrRefreshEffect(StatusTagType.Dazzled,
                SpecialTowerType.IronVanguard, Definition.abilityDazzleDuration, 0f,
                "Iron Vanguard shock field");
            if (applyKnockback)
            {
                Vector2 awayFromVanguard = ((Vector2)enemy.transform.position - (Vector2)tower.transform.position).normalized;
                CombatMathEngine.ApplyKnockback(enemy, Definition.abilityKnockback, awayFromVanguard);
            }
        }
    }

    private void FireIronVanguardShot()
    {
        List<Enemy> enemies = CombatMathEngine.GetEnemiesInCone(tower.transform.position,
            ironVanguardFireDirection, Definition.secondaryRange, Definition.coneAngle,
            LayerMask.GetMask("Enemy"));
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.health <= 0)
            {
                continue;
            }

            DealDamage(enemy, tower.damage);
        }
        visual?.PlaySpriteStateWithNextIdle("shield_fire", 0.06f, "shield_idle");
    }

    private void EndIronVanguardSkill()
    {
        ironVanguardSkillActive = false;
        ironVanguardSkillUntil = 0f;
        tower.transform.position = ironVanguardReturnPosition;
        tower.transform.rotation = ironVanguardReturnRotation;
        towerRange?.SetIronVanguardSkillConeVisible(false);
        visual?.SetIdleState("idle");
    }

    private void ApplyKnockback(Enemy enemy, float distance)
    {
        if (enemy != null && distance > 0f)
        {
            Vector2 direction = ((Vector2)enemy.transform.position - (Vector2)tower.transform.position).normalized;
            CombatMathEngine.ApplyKnockback(enemy, distance, direction);
        }
    }

    private void DealDamage(Enemy enemy, int damage)
    {
        if (enemy == null || enemy.health <= 0)
        {
            return;
        }

        enemy.TakeDamage(damage);
        if (enemy.health <= 0)
        {
            HandleKill(enemy);
        }
    }

    private void PushEnemies(Vector2 center, float radius, float distance)
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius, LayerMask.GetMask("Enemy"));
        HashSet<Enemy> pushedEnemies = new HashSet<Enemy>();
        foreach (Collider2D hitCollider in colliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>() ?? hitCollider.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.health <= 0 || !pushedEnemies.Add(enemy))
            {
                continue;
            }
            Vector2 direction = ((Vector2)enemy.transform.position - center).normalized;
            CombatMathEngine.ApplyKnockback(enemy, distance, direction);
        }
    }

    private void PlayState(string fireKey, string idleKey, float duration)
    {
        visual?.PlaySpriteStateWithNextIdle(fireKey, duration, idleKey);
    }

    private void ThrowGrenade(Vector2 targetPosition)
    {
        if (grenadeSprite == null)
        {
            return;
        }
        GameObject grenade = new GameObject("GunnerCommanderGrenadeDummy");
        SpriteRenderer renderer = grenade.AddComponent<SpriteRenderer>();
        renderer.sprite = grenadeSprite;
        renderer.sortingLayerName = "VFX";
        grenade.transform.localScale = Vector3.one * Definition.grenadeVisualScaleMultiplier;
        nextAbilityTime = Time.time + Definition.abilityCooldown;
        gunnerReadyVisual = false;
        gunnerModeInitialized = true;
        Vector2 direction = (targetPosition - (Vector2)tower.transform.position).normalized;
        Vector2 explosionCenter = targetPosition + direction * Definition.projectileExplosionForwardOffset;
        StartCoroutine(CombatMathEngine.GrenadeTrajectoryRoutine(grenade, tower.transform.position,
            targetPosition, Definition, () => HitArea(explosionCenter, Definition.abilityRadius, Definition.abilityDamage,
                0f, StatusTagType.Slowed, Definition.abilitySlow, Definition.abilitySlowDuration)));
    }

    private void OnDestroy()
    {
        foreach (Tower ally in captainAuraTargets)
        {
            if (ally == null)
            {
                continue;
            }

            TowerStatusManager statusManager = ally.GetComponent<TowerStatusManager>();
            statusManager?.RemoveEffectsFromSource(StatusTagType.Damage, SpecialTowerType.TacticalCaptain,
                $"Tactical Captain +{Definition.auraDamageBonus:P0} damage");
            statusManager?.RemoveEffectsFromSource(StatusTagType.Range, SpecialTowerType.TacticalCaptain,
                $"Tactical Captain +{Definition.auraRangeBonus:P0} range");
        }

    }
}

[DisallowMultipleComponent]
public class GuerillaLandmine : MonoBehaviour
{
    private float triggerRadius;
    private float blastRadius;
    private float damage;
    private float knockback;
    private float slow;
    private float slowDuration;
    private bool configured;

    public void Configure(float triggerRadius, float blastRadius, float damage, float knockback,
        float slow, float slowDuration)
    {
        this.triggerRadius = Mathf.Max(0f, triggerRadius);
        this.blastRadius = Mathf.Max(0f, blastRadius);
        this.damage = Mathf.Max(0f, damage);
        this.knockback = Mathf.Max(0f, knockback);
        this.slow = Mathf.Max(0f, slow);
        this.slowDuration = Mathf.Max(0f, slowDuration);
        configured = true;
    }

    private void Update()
    {
        if (!configured)
        {
            return;
        }

        Collider2D[] triggers = Physics2D.OverlapCircleAll(transform.position, triggerRadius,
            LayerMask.GetMask("Enemy"));
        if (triggers.Length == 0)
        {
            return;
        }

        Vector2 center = transform.position;
        Destroy(gameObject);
        HashSet<Enemy> enemies = new HashSet<Enemy>();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(center, blastRadius, LayerMask.GetMask("Enemy")))
        {
            Enemy enemy = hit.GetComponent<Enemy>() ?? hit.GetComponentInParent<Enemy>();
            if (enemy == null || enemy.health <= 0 || !enemies.Add(enemy))
            {
                continue;
            }

            enemy.TakeDamage(Mathf.RoundToInt(damage));
            Vector2 direction = ((Vector2)enemy.transform.position - center).normalized;
            CombatMathEngine.ApplyKnockback(enemy, knockback, direction);
            enemy.GetComponent<EnemyStatusManager>()?.AddEffect(StatusTagType.Slowed,
                SpecialTowerType.GuerillaEliminator, slowDuration, slow, "Guerilla landmine slow");
        }
    }
}