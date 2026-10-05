using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyStatusManager : MonoBehaviour
{
    [SerializeField] private List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();
    [SerializeField] private Sprite slowedIcon;
    [SerializeField] private Sprite dazzledIcon;
    [SerializeField] private Sprite vulnerableIcon;
    [SerializeField] private Sprite hasteIcon;
    [SerializeField] private Sprite damageIcon;
    [SerializeField] private Sprite rangeIcon;

    private readonly List<GameObject> spawnedTagItems = new List<GameObject>();
    private Transform tagContainer;
    private GameObject tagItemTemplate;

    public IReadOnlyList<StatusEffectInstance> ActiveEffects => activeEffects;

    private void Update()
    {
        bool changed = false;
        for (int index = activeEffects.Count - 1; index >= 0; index--)
        {
            StatusEffectInstance effect = activeEffects[index];
            if (effect == null)
            {
                activeEffects.RemoveAt(index);
                changed = true;
                continue;
            }

            effect.durationRemaining -= Time.deltaTime;
            if (effect.durationRemaining <= 0f)
            {
                activeEffects.RemoveAt(index);
                changed = true;
            }
        }

        if (changed)
        {
            RefreshTagUI();
        }
        else if (tagContainer == null || tagItemTemplate == null)
        {
            RefreshTagUI();
        }
    }

    public void AddEffect(StatusTagType type, SpecialTowerType source, float duration, float intensity, string description = "")
    {
        activeEffects.Add(new StatusEffectInstance(type, source, duration, intensity, description));
        RefreshTagUI();
    }

    public void AddOrRefreshEffect(StatusTagType type, SpecialTowerType source, float duration, float intensity, string description)
    {
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && effect.tagType == type && effect.sourceTower == source && effect.description == description)
            {
                effect.durationRemaining = duration;
                effect.intensity = intensity;
                RefreshTagUI();
                return;
            }
        }
        AddEffect(type, source, duration, intensity, description);
    }

    public void SetTagIcons(Sprite slowed, Sprite dazzled, Sprite vulnerable, Sprite haste, Sprite damage, Sprite range)
    {
        slowedIcon = slowed;
        dazzledIcon = dazzled;
        vulnerableIcon = vulnerable;
        hasteIcon = haste;
        damageIcon = damage;
        rangeIcon = range;
        RefreshTagUI();
    }

    public float GetVulnerabilityDamageMultiplier()
    {
        float multiplier = 1f;
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && effect.tagType == StatusTagType.Vulnerable && effect.sourceTower == SpecialTowerType.Spotter)
            {
                multiplier *= 1f + effect.intensity;
            }
        }
        return multiplier;
    }

    public float GetAllyCriticalChance()
    {
        float chance = 0f;
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && effect.tagType == StatusTagType.Vulnerable && effect.sourceTower == SpecialTowerType.HawkeyeOperator)
            {
                chance = Mathf.Max(chance, effect.intensity);
            }
        }
        return Mathf.Clamp01(chance);
    }

    public bool RemoveEffect(StatusEffectInstance effect)
    {
        bool removed = activeEffects.Remove(effect);
        if (removed)
        {
            RefreshTagUI();
        }
        return removed;
    }

    public float GetCurrentSpeedMultiplier()
    {
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && effect.tagType == StatusTagType.Dazzled)
            {
                return 0.0f;
            }
        }

        float totalSlow = 0f;
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && effect.tagType == StatusTagType.Slowed)
            {
                totalSlow += Mathf.Max(0f, effect.intensity);
            }
        }

        float slowCap = GameBalanceSettings.Instance.maxSlowCap;
        totalSlow = Mathf.Min(totalSlow, slowCap);
        return Mathf.Max(0.01f, 1.0f - totalSlow);
    }

    public bool HasEffectFromSource(StatusTagType type, SpecialTowerType source)
    {
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && effect.tagType == type && effect.sourceTower == source)
            {
                return true;
            }
        }
        return false;
    }

    public void RefreshTagUI()
    {
        if (!ResolveTagTemplate())
        {
            return;
        }

        foreach (GameObject item in spawnedTagItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedTagItems.Clear();

        List<StatusTagType> orderedTypes = new List<StatusTagType>();
        Dictionary<StatusTagType, int> counts = new Dictionary<StatusTagType, int>();
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect == null)
            {
                continue;
            }

            if (!counts.ContainsKey(effect.tagType))
            {
                orderedTypes.Add(effect.tagType);
                counts.Add(effect.tagType, 0);
            }
            counts[effect.tagType]++;
        }

        foreach (StatusTagType type in orderedTypes)
        {
            GameObject tagItem = Instantiate(tagItemTemplate, tagContainer, false);
            tagItem.name = "TagItem_" + type;
            tagItem.SetActive(true);

            Image iconImage = tagItem.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = GetIcon(type);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                iconImage.color = Color.white;
            }

            Transform countTransform = tagItem.transform.Find("SuperscriptText");
            TMP_Text countText = countTransform != null ? countTransform.GetComponent<TMP_Text>() : null;
            if (countText != null)
            {
                int count = counts[type];
                countText.text = count > 1 ? "^" + count : string.Empty;
                countText.gameObject.SetActive(count > 1);
            }

            spawnedTagItems.Add(tagItem);
        }
    }

    private bool ResolveTagTemplate()
    {
        if (tagContainer == null || tagItemTemplate == null)
        {
            Transform healthBar = transform.Find("HealthBar_Canvas");
            if (healthBar == null)
            {
                return false;
            }

            tagContainer = healthBar.Find("TagContainer");
            if (tagContainer == null)
            {
                return false;
            }

            tagItemTemplate = tagContainer.Find("TagItemTemplate")?.gameObject;
        }
        return tagContainer != null && tagItemTemplate != null;
    }

    private Sprite GetIcon(StatusTagType type)
    {
        return type switch
        {
            StatusTagType.Slowed => slowedIcon,
            StatusTagType.Dazzled => dazzledIcon,
            StatusTagType.Vulnerable => vulnerableIcon,
            StatusTagType.Haste => hasteIcon,
            StatusTagType.Damage => damageIcon,
            StatusTagType.Range => rangeIcon,
            _ => null
        };
    }
}