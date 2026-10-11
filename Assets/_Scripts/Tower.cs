using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Tower : MonoBehaviour
{
    // Base-tower prefabs use a 0.5 root scale; special tower ranges use that same world-unit scale.
    private const float SpecialTowerRangeUnitScale = 0.5f;

    public enum TowerClass { Unknown, Soldier, Assault, Sniper }

    public float range;
    public int damage;
    public float fireRate;
    public float turnSpeed = 360f; // Turn speed in degrees per second
    public int cost;
    public bool isSniper = false;
    public int tier = 1;
    public int accumulatedValue;
    public int componentCount = 1;
    public int soldierCount;
    public int assaultCount;
    public int sniperCount;
    public SpecialTowerType specialTowerType = SpecialTowerType.None;

    public event Action<Tower> TierEvolved;

    [Header("Type")]
    public TowerClass towerClass = TowerClass.Unknown;

    [Header("Visual Settings")]
    public Sprite normalSprite;
    public Sprite fireSprite;
    public float fireSpriteDuration = 0.15f;

    public GameObject target;
    private float cooldown = 0f;

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer rangeRenderer;
    private TowerVisualController visualController;
    private TowerRange towerRange;
    private TowerStatusManager statusManager;
    private SpecialTowerCombatController specialCombat;
    private ShockTrooperCombat shockTrooperCombat;
    private bool isHovered = false;
    private bool lastIsPlacing = false;
    private bool lastIsHovered = false;
    private Coroutine flashCoroutine;
    private Coroutine pushCoroutine;
    private float baseRange;
    private float baseDamage;
    private float baseFireRate;
    private readonly Dictionary<GameObject, float> temporaryRangeBonuses = new Dictionary<GameObject, float>();
    private readonly Dictionary<GameObject, Coroutine> temporaryRangeCoroutines = new Dictionary<GameObject, Coroutine>();

    void Awake()
    {
        baseRange = range;
        baseDamage = damage;
        baseFireRate = fireRate;
        spriteRenderer = GetComponent<SpriteRenderer>();
        visualController = GetComponentInChildren<TowerVisualController>();
        specialCombat = GetComponent<SpecialTowerCombatController>();
        shockTrooperCombat = GetComponent<ShockTrooperCombat>();
        if (spriteRenderer == null && visualController != null)
        {
            spriteRenderer = visualController.GetComponent<SpriteRenderer>();
        }
        if (spriteRenderer != null && normalSprite == null)
        {
            normalSprite = spriteRenderer.sprite;
        }

        if (GetComponent<TowerPlacementController>() == null)
        {
            gameObject.AddComponent<TowerPlacementController>();
        }

        if (specialTowerType != SpecialTowerType.None)
        {
            specialCombat = GetComponent<SpecialTowerCombatController>();
            if (specialCombat == null)
            {
                specialCombat = gameObject.AddComponent<SpecialTowerCombatController>();
            }
            if (specialTowerType == SpecialTowerType.ShockTrooper && GetComponent<ShockTrooperCombat>() == null)
            {
                shockTrooperCombat = gameObject.AddComponent<ShockTrooperCombat>();
            }
            if (specialTowerType == SpecialTowerType.HawkeyeOperator && GetComponent<HawkeyeOperatorCombat>() == null)
            {
                gameObject.AddComponent<HawkeyeOperatorCombat>();
            }
            if (specialTowerType == SpecialTowerType.LinebreakerScout && GetComponent<LinebreakerVortexController>() == null)
            {
                gameObject.AddComponent<LinebreakerVortexController>();
            }
        }

        statusManager = GetComponent<TowerStatusManager>();
        if (statusManager == null)
        {
            statusManager = gameObject.AddComponent<TowerStatusManager>();
        }
        statusManager.StatusChanged += RefreshEffectiveStats;

        Transform rangeChild = transform.Find("Range");
        if (rangeChild == null)
        {
            rangeChild = transform.Find("RangeVisualizer");
        }
        if (rangeChild != null)
        {
            rangeRenderer = rangeChild.GetComponent<SpriteRenderer>();
            if (rangeChild.name == "RangeVisualizer")
            {
                CircleCollider2D rangeCollider = rangeChild.GetComponent<CircleCollider2D>();
                if (rangeCollider == null)
                {
                    rangeCollider = rangeChild.gameObject.AddComponent<CircleCollider2D>();
                }
                rangeCollider.radius = 0.5f;
                rangeCollider.isTrigger = true;

                towerRange = rangeChild.GetComponent<TowerRange>();
                if (towerRange == null)
                {
                    towerRange = rangeChild.gameObject.AddComponent<TowerRange>();
                }
                towerRange.Initialize(this);
            }
            
            // Set very low alpha so overlaps don't create dark areas
            // 0.1 + 0.1 overlap = 0.19 (still very light)
            if (rangeRenderer != null)
            {
                Color rangeColor = rangeRenderer.color;
                rangeColor.a = 0.1f;
                rangeRenderer.color = rangeColor;
                rangeRenderer.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : rangeRenderer.sortingLayerID;
                rangeRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : -1;
            }
        }
    }
    
    void Start()
    {
        // Update range visibility on start
        UpdateRangeVisibility();
    }

    void OnEnable()
    {
        // Ensure that whenever the component becomes enabled at runtime it has correct stats
        ApplyClassDamage();
        if (accumulatedValue <= 0)
        {
            accumulatedValue = cost;
        }
        InitializeRecipeCounts();
        DifficultySettings.OnShowAllTowerRangesChanged += UpdateRangeVisibility;
    }

    void OnDisable()
    {
        DifficultySettings.OnShowAllTowerRangesChanged -= UpdateRangeVisibility;
        if (statusManager != null)
        {
            statusManager.StatusChanged -= RefreshEffectiveStats;
        }
    }

    public void ApplyClassDamage()
    {
        if (specialTowerType != SpecialTowerType.None)
        {
            GameBalanceSettings.SpecialTowerEvolutionStats specialStats =
                GameBalanceSettings.Instance.GetSpecialTowerEvolutionStats(specialTowerType);
            damage = specialStats.level5Stats.damage;
            range = GetConfiguredWorldRange(specialStats.level5Stats.range);
            fireRate = specialStats.level5Stats.fireRate;
            cost = specialStats.level5Stats.cost;
            baseRange = range;
            baseDamage = damage;
            baseFireRate = fireRate;
            RefreshEffectiveStats();
            return;
        }

        towerClass = ResolveTowerClass();
        GameBalanceSettings.TowerStats stats =
            GameBalanceSettings.Instance.GetBaseTowerStats(towerClass, Mathf.Clamp(tier, 1, 4));
        damage = stats.damage;
        range = stats.range;
        fireRate = stats.fireRate;
        cost = stats.cost;

        baseRange = range;
        baseDamage = damage;
        baseFireRate = fireRate;
        RefreshEffectiveStats();
    }

    public int GetBalanceConfiguredCost()
    {
        GameBalanceSettings settings = GameBalanceSettings.Instance;
        return specialTowerType != SpecialTowerType.None
            ? settings.GetSpecialTowerEvolutionStats(specialTowerType).level5Stats.cost
            : settings.GetBaseTowerStats(ResolveTowerClass(), 1).cost;
    }

    public void ApplyTemporaryRangeBonus(GameObject source, float bonusFraction, float duration)
    {
        if (source == null || duration <= 0f)
        {
            return;
        }
        Tower sourceTower = source.GetComponent<Tower>();
        SpecialTowerType sourceType = sourceTower != null ? sourceTower.specialTowerType : SpecialTowerType.HawkeyeOperator;
        statusManager?.AddOrRefreshEffect(StatusTagType.Range, sourceType, duration, bonusFraction, "Hawkeye uplink +20% range");
    }

    private void RecalculateRange()
    {
        float multiplier = 1f;
        foreach (float sourceMultiplier in temporaryRangeBonuses.Values)
        {
            multiplier *= sourceMultiplier;
        }
        range = baseRange * multiplier;
        if (statusManager != null)
        {
            range *= statusManager.GetRangeMultiplier();
        }

        Transform rangeVisualizer = transform.Find("RangeVisualizer");
        if (rangeVisualizer != null)
        {
            rangeVisualizer.localScale = new Vector3(range * 2f, range * 2f, rangeVisualizer.localScale.z);
            TowerRange towerRange = rangeVisualizer.GetComponent<TowerRange>();
            if (towerRange != null)
            {
                towerRange.UpdateRange();
            }
        }
    }

    private void RefreshEffectiveStats()
    {
        damage = statusManager != null
            ? Mathf.RoundToInt(baseDamage * statusManager.GetDamageMultiplier())
            : Mathf.RoundToInt(baseDamage);
        fireRate = statusManager != null
            ? GameBalanceSettings.Instance.ClampAttackInterval(baseFireRate * statusManager.GetAttackIntervalMultiplier())
            : baseFireRate;
        if (specialCombat != null)
        {
            fireRate = GameBalanceSettings.Instance.ClampAttackInterval(fireRate * specialCombat.GetAttackIntervalMultiplier());
        }
        RecalculateRange();
    }

    public float GetEffectiveAttackInterval()
    {
        float interval = statusManager != null ? baseFireRate * statusManager.GetAttackIntervalMultiplier() : baseFireRate;
        if (specialCombat != null)
        {
            interval *= specialCombat.GetAttackIntervalMultiplier();
        }
        return GameBalanceSettings.Instance.ClampAttackInterval(interval);
    }

    public string GetBuffDescription()
    {
        return statusManager != null ? statusManager.GetBuffDescription() : string.Empty;
    }

    public void AddStatusEffect(StatusTagType type, SpecialTowerType source, float duration, float intensity, string description = "")
    {
        statusManager?.AddEffect(type, source, duration, intensity, description);
    }

    public void AddOrRefreshStatusEffect(StatusTagType type, SpecialTowerType source, float duration, float intensity, string description)
    {
        statusManager?.AddOrRefreshEffect(type, source, duration, intensity, description);
    }

    public void ResetAttackCooldown()
    {
        cooldown = 0f;
    }

    public void ApplySynthesisTier(int newTier)
    {
        tier = Mathf.Clamp(newTier, 1, 5);
        if (tier < 5)
        {
            TowerClass effectiveTowerClass = ResolveTowerClass();
            if (effectiveTowerClass != TowerClass.Unknown)
            {
                GameBalanceSettings.TowerStats stats = GameBalanceSettings.Instance.GetBaseTowerStats(effectiveTowerClass, tier);
                damage = stats.damage;
                baseRange = stats.range;
                fireRate = stats.fireRate;
                baseDamage = stats.damage;
                baseFireRate = stats.fireRate;
                RecalculateRange();
                RefreshEffectiveStats();
            }
            return;
        }

        if (tier5Evolved)
        {
            return;
        }

        if (specialTowerType == SpecialTowerType.None)
        {
            specialTowerType = ResolveSpecialTowerType();
        }
        if (specialTowerType != SpecialTowerType.None)
        {
            GameBalanceSettings.SpecialTowerEvolutionStats evolution =
                GameBalanceSettings.Instance.GetSpecialTowerEvolutionStats(specialTowerType);
            damage = evolution.level5Stats.damage;
            baseDamage = damage;
            baseRange = GetConfiguredWorldRange(evolution.level5Stats.range);
            baseFireRate = evolution.level5Stats.fireRate;
            RefreshEffectiveStats();
        }

        tier5Evolved = true;
        TierEvolved?.Invoke(this);
    }

    public float GetConfiguredWorldRange(float configuredRange)
    {
        return GetConfiguredWorldRange(specialTowerType, configuredRange);
    }

    public static float GetConfiguredWorldRange(SpecialTowerType type, float configuredRange)
    {
        return type == SpecialTowerType.None
            ? configuredRange
            : configuredRange * SpecialTowerRangeUnitScale;
    }

    public void MergeRecipeFrom(Tower incoming)
    {
        if (incoming == null)
        {
            return;
        }

        InitializeRecipeCounts();
        incoming.InitializeRecipeCounts();
        soldierCount += incoming.soldierCount;
        assaultCount += incoming.assaultCount;
        sniperCount += incoming.sniperCount;
        componentCount = soldierCount + assaultCount + sniperCount;
        tier = componentCount;
        accumulatedValue += incoming.accumulatedValue;
        cost = accumulatedValue;

        if (tier >= 5)
        {
            GameBalanceSettings.SpecialTowerEvolutionStats definition =
                GameBalanceSettings.Instance.GetSpecialTowerForRecipe(soldierCount, assaultCount, sniperCount);
            if (definition != null)
            {
                specialTowerType = definition.towerType;
            }
            ApplySynthesisTier(5);
            return;
        }

        ApplyCompositionStats();
    }

    public void InitializeRecipeCounts()
    {
        if (soldierCount + assaultCount + sniperCount > 0)
        {
            componentCount = soldierCount + assaultCount + sniperCount;
            tier = Mathf.Max(tier, componentCount);
            return;
        }

        if (specialTowerType == SpecialTowerType.None)
        {
            switch (ResolveTowerClass())
            {
                case TowerClass.Soldier:
                    soldierCount = 1;
                    break;
                case TowerClass.Assault:
                    assaultCount = 1;
                    break;
                case TowerClass.Sniper:
                    sniperCount = 1;
                    break;
            }
        }
        else
        {
            GameBalanceSettings.SpecialTowerEvolutionStats definition =
                GameBalanceSettings.Instance.GetSpecialTowerEvolutionStats(specialTowerType);
            soldierCount = definition.soldierCount;
            assaultCount = definition.assaultCount;
            sniperCount = definition.sniperCount;
        }

        int recipeCount = soldierCount + assaultCount + sniperCount;
        if (recipeCount > 0)
        {
            componentCount = recipeCount;
            tier = Mathf.Max(tier, recipeCount);
        }
        else
        {
            componentCount = Mathf.Max(1, componentCount);
            tier = Mathf.Max(1, tier);
        }
    }

    private void ApplyCompositionStats()
    {
        int totalCount = soldierCount + assaultCount + sniperCount;
        if (totalCount <= 0 || totalCount >= 5)
        {
            return;
        }

        TowerClass baseClass = soldierCount >= assaultCount && soldierCount >= sniperCount
            ? TowerClass.Soldier
            : assaultCount >= sniperCount ? TowerClass.Assault : TowerClass.Sniper;
        GameBalanceSettings settings = GameBalanceSettings.Instance;
        GameBalanceSettings.TowerStats baseStats = settings.GetBaseTowerStats(baseClass, 1);
        GameBalanceSettings.TowerTierDelta soldierDelta = settings.GetBaseTowerTierDelta(TowerClass.Soldier);
        GameBalanceSettings.TowerTierDelta assaultDelta = settings.GetBaseTowerTierDelta(TowerClass.Assault);
        GameBalanceSettings.TowerTierDelta sniperDelta = settings.GetBaseTowerTierDelta(TowerClass.Sniper);
        damage = baseStats.damage +
             (soldierCount - (baseClass == TowerClass.Soldier ? 1 : 0)) * soldierDelta.damageDelta +
             (assaultCount - (baseClass == TowerClass.Assault ? 1 : 0)) * assaultDelta.damageDelta +
             (sniperCount - (baseClass == TowerClass.Sniper ? 1 : 0)) * sniperDelta.damageDelta;
        range = baseStats.range +
            (soldierCount - (baseClass == TowerClass.Soldier ? 1 : 0)) * soldierDelta.rangeDelta +
            (assaultCount - (baseClass == TowerClass.Assault ? 1 : 0)) * assaultDelta.rangeDelta +
            (sniperCount - (baseClass == TowerClass.Sniper ? 1 : 0)) * sniperDelta.rangeDelta;
        float attackInterval = baseStats.fireRate +
                       (soldierCount - (baseClass == TowerClass.Soldier ? 1 : 0)) * soldierDelta.intervalDelta +
                       (assaultCount - (baseClass == TowerClass.Assault ? 1 : 0)) * assaultDelta.intervalDelta +
                       (sniperCount - (baseClass == TowerClass.Sniper ? 1 : 0)) * sniperDelta.intervalDelta;
        fireRate = settings.ClampAttackInterval(attackInterval);
        towerClass = baseClass;
        ApplyBaseClassSprite();
        baseRange = range;
        RecalculateRange();
    }

    private void ApplyBaseClassSprite()
    {
        GameBalanceSettings settings = GameBalanceSettings.Instance;
        Sprite baseSprite = settings.GetBaseTowerSprite(towerClass);
        if (baseSprite == null)
        {
            return;
        }

        normalSprite = baseSprite;
        Sprite baseFireSprite = settings.GetBaseTowerFireSprite(towerClass);
        if (baseFireSprite != null)
        {
            fireSprite = baseFireSprite;
        }
        if (flashCoroutine == null && spriteRenderer != null)
        {
            spriteRenderer.sprite = baseSprite;
        }
    }

    private TowerClass ResolveTowerClass()
    {
        if (towerClass != TowerClass.Unknown)
        {
            return towerClass;
        }

        string lowerName = gameObject.name.ToLowerInvariant();
        if (isSniper || lowerName.Contains("sniper"))
        {
            return TowerClass.Sniper;
        }
        if (lowerName.Contains("assault"))
        {
            return TowerClass.Assault;
        }
        if (lowerName.Contains("soldier"))
        {
            return TowerClass.Soldier;
        }
        return TowerClass.Unknown;
    }

    private SpecialTowerType ResolveSpecialTowerType()
    {
        string normalizedName = gameObject.name.ToLowerInvariant()
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty);
        foreach (SpecialTowerType towerType in Enum.GetValues(typeof(SpecialTowerType)))
        {
            if (towerType == SpecialTowerType.None)
            {
                continue;
            }

            if (normalizedName.Contains(towerType.ToString().ToLowerInvariant()))
            {
                return towerType;
            }
        }
        return SpecialTowerType.None;
    }

    [SerializeField] private bool tier5Evolved;

    // Update is called once per frame
    void Update()
    {
        // If game is frozen (via cheat command), stop everything
        if (CheatCommandSystem.isFrozen) return;

        // Robust manual mouse hover detection (independent of Unity physics raycasting/input issues)
        bool currentIsHovered = false;
        if (Camera.main != null)
        {
            bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (!isOverUI)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0;
                
                // Detection radius of 1.2f matches the tower visual bounds perfectly
                if (Vector2.Distance(transform.position, mouseWorldPos) <= 1.2f)
                {
                    currentIsHovered = true;
                }
            }
        }
        isHovered = currentIsHovered;

        // Update range visibility dynamically when placement mode or hover changes
        bool currentIsPlacing = TowerPlacementManager.instance != null && TowerPlacementManager.instance.IsPlacing;
        if (currentIsPlacing != lastIsPlacing || isHovered != lastIsHovered)
        {
            lastIsPlacing = currentIsPlacing;
            lastIsHovered = isHovered;
            UpdateRangeVisibility();
        }

        if(target)
        {
            // Smoothly rotate towards the target
            Vector3 dir = target.transform.position - transform.position;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            if(cooldown >= GetEffectiveAttackInterval())
            {
                Debug.Log("防禦塔攻擊了：" + target.name); 

                Enemy specialTarget = target.GetComponent<Enemy>();
                if (specialCombat != null && specialTarget != null && specialTowerType != SpecialTowerType.None)
                {
                    bool attackResolved = specialTowerType == SpecialTowerType.ShockTrooper && shockTrooperCombat != null
                        ? shockTrooperCombat.TryAttack(specialTarget)
                        : specialCombat.TryAttack(this, specialTarget);
                    if (attackResolved && GameManager.instance != null)
                    {
                        GameManager.instance.shotsFired++;
                    }
                    cooldown = 0f;
                    return;
                }

                TriggerFireFlash();

                // Increment shots fired in GameManager
                if (GameManager.instance != null)
                {
                    GameManager.instance.shotsFired++;
                }

                EnemyLocalData enemyLocal = target.GetComponent<EnemyLocalData>();
                if (enemyLocal != null)
                {
                    enemyLocal.TakeDamage(damage);
                }
                else
                {
                    Enemy enemy = target.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage);
                        
                        // If Sniper, apply push!
                        if (isSniper || gameObject.name.Contains("Sniper"))
                        {
                            enemy.ApplyPush(transform.position);
                        }
                    }
                }
                cooldown = 0f;
            }
            else
            {
                cooldown += 1 * Time.deltaTime;
            }
        }
    }

    private void TriggerFireFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashFireSprite());
    }

    private IEnumerator FlashFireSprite()
    {
        if (spriteRenderer != null && fireSprite != null)
        {
            spriteRenderer.sprite = fireSprite;
        }
        yield return new WaitForSeconds(fireSpriteDuration);
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
        flashCoroutine = null;
    }

    public void UpdateRangeVisibility()
    {
        if (rangeRenderer == null && towerRange == null) return;

        bool isPlacing = TowerPlacementManager.instance != null && TowerPlacementManager.instance.IsPlacing;
        bool shouldShow = isHovered || (isPlacing && DifficultySettings.showAllTowerRangesWhenPlacing);
        if (rangeRenderer != null && rangeRenderer.enabled != shouldShow)
        {
            rangeRenderer.enabled = shouldShow;
        }
        towerRange?.SetVisible(shouldShow);
    }
}
