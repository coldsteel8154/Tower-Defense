using System;

[Serializable]
public class StatusEffectInstance
{
    public StatusTagType tagType;
    public SpecialTowerType sourceTower;
    public float durationRemaining;
    public float intensity;
    public string description;

    public StatusEffectInstance(StatusTagType tagType, SpecialTowerType sourceTower, float durationRemaining, float intensity, string description)
    {
        this.tagType = tagType;
        this.sourceTower = sourceTower;
        this.durationRemaining = durationRemaining;
        this.intensity = intensity;
        this.description = description;
    }
}