using UnityEngine;

public class TacticalMarkController : MonoBehaviour
{
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float markScaleMultiplier = 1f;

    private SpriteRenderer spriteRenderer;
    private Vector3 baseVisualScale = Vector3.one;

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = transform.name == "VisualRoot" ? transform : transform.Find("VisualRoot");
        }

        if (visualRoot != null)
        {
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
            baseVisualScale = visualRoot.localScale;
            string markName = gameObject.name.Replace("(Clone)", string.Empty).Trim();
            markScaleMultiplier *= GameBalanceSettings.Instance.GetMarkVisualScale(markName);
            ApplyVisualScale();
        }
        else
        {
            Debug.LogError("TacticalMarkController requires a child named VisualRoot.", this);
            enabled = false;
        }
    }

    public void InitializeMark(Sprite sprite, float scaleMultiplier)
    {
        if (visualRoot == null)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }

        markScaleMultiplier = scaleMultiplier;
        ApplyVisualScale();
    }

    private void ApplyVisualScale()
    {
        if (visualRoot != null)
        {
            visualRoot.localScale = baseVisualScale * markScaleMultiplier;
        }
    }
}