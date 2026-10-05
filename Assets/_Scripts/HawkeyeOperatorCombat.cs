using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HawkeyeOperatorCombat : MonoBehaviour
{
    [SerializeField] private float vulnerabilityDuration = 4f;
    [SerializeField] private float slowIntensity = 0.3f;
    [SerializeField] private float allyCritChance = 0.25f;
    [SerializeField] private float allySearchRadius = 5f;
    [SerializeField] private float allyRangeBonus = 0.2f;
    [SerializeField] private float allyRangeBonusDuration = 4f;
    [SerializeField] private float dataBeamWidth = 0.04f;
    [SerializeField] private Material dataBeamMaterial;

    private readonly HashSet<Enemy> observedEnemies = new HashSet<Enemy>();
    private TowerVisualController visualController;
    private float uplinkUntil;

    public bool IsUplinkActive => Time.time < uplinkUntil;

    private void Awake()
    {
        visualController = GetComponentInChildren<TowerVisualController>();
    }

    private void Update()
    {
        if (uplinkUntil > 0f && Time.time >= uplinkUntil)
        {
            uplinkUntil = 0f;
            visualController?.SetIdleState("idle");
        }
    }

    public void Configure(float effectDuration, float slow, float criticalChance, float searchRadius,
        float rangeBonus, float rangeBonusDuration)
    {
        vulnerabilityDuration = Mathf.Max(0f, effectDuration);
        slowIntensity = Mathf.Max(0f, slow);
        allyCritChance = Mathf.Clamp01(criticalChance);
        allySearchRadius = Mathf.Max(0f, searchRadius);
        allyRangeBonus = Mathf.Max(0f, rangeBonus);
        allyRangeBonusDuration = Mathf.Max(0f, rangeBonusDuration);
    }

    public void ApplyAttackEffect(Enemy enemy)
    {
        if (enemy == null || enemy.health <= 0)
        {
            return;
        }

        EnemyStatusManager statusManager = enemy.GetComponent<EnemyStatusManager>();
        if (statusManager == null)
        {
            return;
        }

        statusManager.AddOrRefreshEffect(
            StatusTagType.Slowed,
            SpecialTowerType.HawkeyeOperator,
            vulnerabilityDuration,
            slowIntensity,
            "Hawkeye vulnerability slow");
        statusManager.AddOrRefreshEffect(
            StatusTagType.Vulnerable,
            SpecialTowerType.HawkeyeOperator,
            vulnerabilityDuration,
            allyCritChance,
            "Allies have an increased critical chance");

        if (observedEnemies.Add(enemy))
        {
            enemy.Died += OnEnemyDied;
        }
    }

    private void OnEnemyDied(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.Died -= OnEnemyDied;
        observedEnemies.Remove(enemy);

        EnemyStatusManager statusManager = enemy.GetComponent<EnemyStatusManager>();
        if (statusManager == null || !statusManager.HasEffectFromSource(StatusTagType.Vulnerable, SpecialTowerType.HawkeyeOperator))
        {
            return;
        }

        Tower hawkeyeTower = GetComponent<Tower>();
        if (hawkeyeTower == null)
        {
            return;
        }

        List<Tower> nearestTowers = FindNearestTowers(hawkeyeTower.transform.position);
        hawkeyeTower.ApplyTemporaryRangeBonus(gameObject, allyRangeBonus, allyRangeBonusDuration);
        foreach (Tower tower in nearestTowers)
        {
            tower.ApplyTemporaryRangeBonus(gameObject, allyRangeBonus, allyRangeBonusDuration);
            StartCoroutine(DataBeamRoutine(tower, allyRangeBonusDuration));
        }
        uplinkUntil = Time.time + allyRangeBonusDuration;
        visualController?.SetIdleState("skill_idle");
    }

    private List<Tower> FindNearestTowers(Vector2 center)
    {
        List<Tower> towers = new List<Tower>();
        Tower[] allTowers = FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
        foreach (Tower tower in allTowers)
        {
            if (tower == null || tower.transform.root == transform.root ||
                Vector2.Distance(center, tower.transform.position) > allySearchRadius)
            {
                continue;
            }
            towers.Add(tower);
        }

        towers.Sort((first, second) =>
            Vector2.Distance(center, first.transform.position).CompareTo(Vector2.Distance(center, second.transform.position)));
        if (towers.Count > 2)
        {
            towers.RemoveRange(2, towers.Count - 2);
        }
        return towers;
    }

    private IEnumerator DataBeamRoutine(Tower tower, float duration)
    {
        if (tower == null)
        {
            yield break;
        }

        GameObject beamObject = new GameObject("HawkeyeDataBeam");
        LineRenderer beam = beamObject.AddComponent<LineRenderer>();
        beam.useWorldSpace = true;
        beam.positionCount = 2;
        beam.startWidth = dataBeamWidth;
        beam.endWidth = dataBeamWidth;
        beam.startColor = new Color(0.25f, 0.9f, 1f, 0.9f);
        beam.endColor = new Color(0.25f, 0.9f, 1f, 0.35f);
        beam.sortingLayerName = "VFX";
        Material runtimeMaterial = null;
        if (dataBeamMaterial != null)
        {
            beam.sharedMaterial = dataBeamMaterial;
        }
        else
        {
            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
            {
                runtimeMaterial = new Material(spriteShader);
                beam.sharedMaterial = runtimeMaterial;
            }
        }

        float elapsed = 0f;
        while (elapsed < duration && tower != null)
        {
            beam.SetPosition(0, transform.position);
            beam.SetPosition(1, tower.transform.position);
            yield return null;
            elapsed += Time.deltaTime;
        }

        Destroy(beamObject);
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }

    private void OnDestroy()
    {
        foreach (Enemy enemy in observedEnemies)
        {
            if (enemy != null)
            {
                enemy.Died -= OnEnemyDied;
            }
        }
        observedEnemies.Clear();
    }
}