using UnityEngine;

public class ShockTrooperCombat : MonoBehaviour
{
    [SerializeField] private float baseDamage = 85f;
    [SerializeField] private float attackInterval = 0.35f;
    [SerializeField] private float attackRange;
    [SerializeField] private float firingStateDuration = 0.18f;
    [SerializeField] private float damagePerComboStack = 0.05f;
    [SerializeField] private int maximumComboStacks = 10;
    [SerializeField] private float shockwaveRadius = 1.5f;
    [SerializeField] private float stunDuration = 0.5f;
    [SerializeField] private GameObject shockwavePrefab;

    private TowerVisualController visualController;
    private Tower tower;
    private Enemy comboTarget;
    private int comboCount;
    private bool isLeftGun;
    private float nextAttackTime;

    public Enemy ComboTarget => comboTarget;
    public int ComboCount => comboCount;
    public float AttackInterval => attackInterval;

    private void Awake()
    {
        tower = GetComponent<Tower>();
        visualController = GetComponentInChildren<TowerVisualController>();
    }

    private void Update()
    {
        if (comboTarget == null || comboTarget.health <= 0)
        {
            ResetCombo();
            return;
        }

        float currentRange = tower != null ? tower.range : attackRange;
        if (currentRange > 0f && Vector2.Distance(transform.position, comboTarget.transform.position) > currentRange)
        {
            ResetCombo();
            return;
        }

        if (tower != null && tower.target != comboTarget.gameObject)
        {
            ResetCombo();
        }
    }

    public void SetAttackRange(float range)
    {
        attackRange = Mathf.Max(0f, range);
    }

    public void Configure(float damage, float interval, float range, float damagePerStack, int maximumStacks,
        float shockRadius, float stunTime)
    {
        baseDamage = damage;
        attackInterval = Mathf.Max(GameBalanceSettings.Instance.MinimumAttackInterval, interval);
        attackRange = Mathf.Max(0f, range);
        damagePerComboStack = Mathf.Max(0f, damagePerStack);
        maximumComboStacks = Mathf.Max(1, maximumStacks);
        shockwaveRadius = Mathf.Max(0f, shockRadius);
        stunDuration = Mathf.Max(0f, stunTime);
    }

    public float GetDamageForCurrentCombo()
    {
        return baseDamage * (1f + Mathf.Min(comboCount * damagePerComboStack, maximumComboStacks * damagePerComboStack));
    }

    public bool TryAttack(Enemy target)
    {
        if (target == null || target.health <= 0 || Time.time < nextAttackTime)
        {
            return false;
        }

        float currentRange = tower != null ? tower.range : attackRange;
        if (currentRange > 0f && Vector2.Distance(transform.position, target.transform.position) > currentRange)
        {
            ResetCombo();
            return false;
        }

        if (comboTarget != target)
        {
            SetComboTarget(target);
        }

        bool superconductive = comboCount >= 10;
        string firingKey = superconductive
            ? (isLeftGun ? "skill_fire1" : "skill_fire2")
            : (isLeftGun ? "fire1" : "fire2");
        visualController?.PlaySpriteStateWithNextIdle(firingKey, firingStateDuration, "idle");

        int damage = Mathf.RoundToInt(GetDamageForCurrentCombo());
        target.TakeDamage(damage);

        nextAttackTime = Time.time + attackInterval;
        isLeftGun = !isLeftGun;

        if (target.health <= 0)
        {
            TriggerShockwave(target.transform.position);
            ResetCombo();
        }
        else
        {
            comboCount = Mathf.Min(comboCount + 1, 10);
            if (tower != null && comboCount > 0)
            {
                tower.AddStatusEffect(StatusTagType.Damage, SpecialTowerType.ShockTrooper, float.MaxValue,
                    damagePerComboStack, "Shock Trooper +5% combo damage");
            }
        }
        return true;
    }

    private void SetComboTarget(Enemy target)
    {
        ResetCombo();
        comboTarget = target;
        comboTarget.Died += OnComboTargetDied;
    }

    private void OnComboTargetDied(Enemy enemy)
    {
        if (enemy == comboTarget)
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        if (comboTarget != null)
        {
            comboTarget.Died -= OnComboTargetDied;
        }
        comboTarget = null;
        comboCount = 0;
        isLeftGun = false;
        tower?.GetComponent<TowerStatusManager>()?.RemoveEffectsFromSource(StatusTagType.Damage,
            SpecialTowerType.ShockTrooper, "Shock Trooper +5% combo damage");
    }

    private void TriggerShockwave(Vector2 center)
    {
        if (shockwavePrefab != null)
        {
            Instantiate(shockwavePrefab, center, Quaternion.identity);
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(center, shockwaveRadius, LayerMask.GetMask("Enemy"));
        foreach (Collider2D hitCollider in colliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = hitCollider.GetComponentInParent<Enemy>();
            }
            if (enemy == null || enemy.health <= 0)
            {
                continue;
            }

            EnemyStatusManager statusManager = enemy.GetComponent<EnemyStatusManager>();
            if (statusManager != null)
            {
                statusManager.AddEffect(StatusTagType.Dazzled, SpecialTowerType.ShockTrooper, stunDuration, 0f, "Electric shockwave stun");
            }
        }
    }

    private void OnDestroy()
    {
        ResetCombo();
    }
}