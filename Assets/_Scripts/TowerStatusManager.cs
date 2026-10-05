using System;
using System.Collections.Generic;
using UnityEngine;

public class TowerStatusManager : MonoBehaviour
{
    private readonly List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();
    public event Action StatusChanged;

    public IReadOnlyList<StatusEffectInstance> ActiveEffects => activeEffects;

    private void Update()
    {
        bool changed = false;
        for (int index = activeEffects.Count - 1; index >= 0; index--)
        {
            StatusEffectInstance effect = activeEffects[index];
            if (effect == null || effect.durationRemaining <= 0f)
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
            StatusChanged?.Invoke();
        }
    }

    public void AddEffect(StatusTagType type, SpecialTowerType source, float duration, float intensity, string description = "")
    {
        activeEffects.Add(new StatusEffectInstance(type, source, duration, intensity, description));
        StatusChanged?.Invoke();
    }

    public void AddOrRefreshEffect(StatusTagType type, SpecialTowerType source, float duration, float intensity, string description)
    {
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && effect.tagType == type && effect.sourceTower == source && effect.description == description)
            {
                effect.durationRemaining = duration;
                effect.intensity = intensity;
                StatusChanged?.Invoke();
                return;
            }
        }
        AddEffect(type, source, duration, intensity, description);
    }

    public bool RemoveEffectsFromSource(StatusTagType type, SpecialTowerType source, string description)
    {
        bool removed = activeEffects.RemoveAll(effect => effect != null && effect.tagType == type &&
            effect.sourceTower == source && effect.description == description) > 0;
        if (removed)
        {
            StatusChanged?.Invoke();
        }
        return removed;
    }

    public bool HasEffectFromSource(StatusTagType type, SpecialTowerType source, string description)
    {
        return activeEffects.Exists(effect => effect != null && effect.tagType == type &&
            effect.sourceTower == source && effect.description == description);
    }

    public float GetDamageMultiplier()
    {
        float multiplier = 1f;
        float shockTrooperComboBonus = 0f;
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && effect.tagType == StatusTagType.Damage)
            {
                if (effect.sourceTower == SpecialTowerType.ShockTrooper)
                {
                    shockTrooperComboBonus += Mathf.Max(0f, effect.intensity);
                }
                else
                {
                    multiplier *= 1f + effect.intensity;
                }
            }
        }
        return multiplier * (1f + shockTrooperComboBonus);
    }

    public float GetAttackIntervalMultiplier()
    {
        float multiplier = 1f;
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && effect.tagType == StatusTagType.Haste)
            {
                multiplier *= Mathf.Max(0.01f, 1f - effect.intensity);
            }
        }
        return multiplier;
    }

    public float GetRangeMultiplier()
    {
        float multiplier = 1f;
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && effect.tagType == StatusTagType.Range)
            {
                multiplier *= 1f + effect.intensity;
            }
        }
        return multiplier;
    }

    public string GetBuffDescription()
    {
        List<string> descriptions = new List<string>();
        foreach (StatusEffectInstance effect in activeEffects)
        {
            if (effect != null && !string.IsNullOrEmpty(effect.description))
            {
                descriptions.Add(effect.description);
            }
        }
        return string.Join("\n", descriptions);
    }
}