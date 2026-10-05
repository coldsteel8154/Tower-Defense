using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerVisualController : MonoBehaviour
{
    [Serializable]
    public class SpriteStateEntry
    {
        public string key;
        public Sprite sprite;
        public Sprite[] frames;
        public float framesPerSecond = 12f;
        [Min(0f)] public float visualScaleMultiplier = 1f;
    }

    [SerializeField] private Transform visualRoot;
    [SerializeField] private float visualScaleMultiplier = 1.0f;
    [SerializeField] private string idleSpriteKey;
    [SerializeField] private List<SpriteStateEntry> spriteStates = new List<SpriteStateEntry>();

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Sprite cachedIdleSprite;
    private string currentIdleKey;
    private Coroutine spriteStateCoroutine;
    private Coroutine animationCoroutine;
    private float lastAppliedScale;
    private float currentStateScaleMultiplier = 1f;

    private void Awake()
    {
        if (visualRoot == null)
        {
            visualRoot = transform.name == "VisualRoot" ? transform : transform.Find("VisualRoot");
        }

        if (visualRoot == null)
        {
            Debug.LogError("TowerVisualController requires a child named VisualRoot.", this);
            enabled = false;
            return;
        }

        spriteRenderer = visualRoot.GetComponent<SpriteRenderer>();
        animator = visualRoot.GetComponent<Animator>();
        cachedIdleSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        currentIdleKey = string.IsNullOrEmpty(idleSpriteKey) && cachedIdleSprite != null
            ? cachedIdleSprite.name
            : idleSpriteKey;
        Tower owner = GetComponentInParent<Tower>();
        if (owner != null && owner.specialTowerType != SpecialTowerType.None)
        {
            visualScaleMultiplier *= GameBalanceSettings.Instance.GetTowerVisualScale(owner.specialTowerType);
        }
        SpriteStateEntry idleState = FindSpriteState(currentIdleKey);
        if (idleState != null)
        {
            currentStateScaleMultiplier = idleState.visualScaleMultiplier > 0f
                ? idleState.visualScaleMultiplier
                : 1f;
        }
        ApplyVisualScale(visualScaleMultiplier);
    }

    private void Update()
    {
        if (visualRoot != null && !Mathf.Approximately(lastAppliedScale, visualScaleMultiplier))
        {
            ApplyVisualScale(visualScaleMultiplier);
        }
    }

    public void ApplyVisualScale(float scale)
    {
        visualScaleMultiplier = scale;
        ApplyCombinedVisualScale();
    }

    private void ApplyStateVisualScale(float scale)
    {
        currentStateScaleMultiplier = scale > 0f ? scale : 1f;
        ApplyCombinedVisualScale();
    }

    private void ApplyCombinedVisualScale()
    {
        if (visualRoot == null)
        {
            return;
        }

        visualRoot.localScale = Vector3.one * visualScaleMultiplier * currentStateScaleMultiplier;
        lastAppliedScale = visualScaleMultiplier;
    }

    public void PlaySpriteState(string spriteKey, float duration)
    {
        StartSpriteState(spriteKey, duration);
    }

    public void PlaySpriteStateWithNextIdle(string fireKey, float duration, string nextIdleKey)
    {
        currentIdleKey = nextIdleKey;
        StartSpriteState(fireKey, duration);
    }

    public void SetIdleState(string idleKey)
    {
        StopAnimationPlayback();
        currentIdleKey = idleKey;
        if (spriteStateCoroutine != null)
        {
            StopCoroutine(spriteStateCoroutine);
            spriteStateCoroutine = null;
        }
        SetSpriteByKey(currentIdleKey);
    }

    public void PlayAnimation(string clipName)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            StopSpriteCoroutines();
            animator.enabled = true;
            animator.Play(clipName, 0, 0f);
            return;
        }

        SpriteStateEntry state = FindSpriteState(clipName);
        if (state != null && state.frames != null && state.frames.Length > 0)
        {
            if (spriteStateCoroutine != null)
            {
                StopCoroutine(spriteStateCoroutine);
                spriteStateCoroutine = null;
            }
            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }
            animationCoroutine = StartCoroutine(LoopSpriteFrames(state));
        }
    }

    private void StopSpriteCoroutines()
    {
        if (spriteStateCoroutine != null)
        {
            StopCoroutine(spriteStateCoroutine);
            spriteStateCoroutine = null;
        }
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    public void PlayOneShotEffect(string spriteKey, Vector3 position, float duration, float worldWidth = 0f,
        int sortingOrder = 3)
    {
        SpriteStateEntry state = FindSpriteState(spriteKey);
        if (state == null)
        {
            return;
        }

        Sprite[] frames = state.frames != null && state.frames.Length > 0
            ? state.frames
            : state.sprite != null ? new[] { state.sprite } : null;
        if (frames == null || frames.Length == 0 || frames[0] == null)
        {
            return;
        }

        GameObject effect = new GameObject(spriteKey + "Effect");
        effect.transform.position = position;
        if (worldWidth > 0f && frames[0].bounds.size.x > 0f)
        {
            effect.transform.localScale = Vector3.one * (worldWidth / frames[0].bounds.size.x);
        }

        SpriteRenderer effectRenderer = effect.AddComponent<SpriteRenderer>();
        effectRenderer.sprite = frames[0];
        effectRenderer.sortingLayerName = "VFX";
        effectRenderer.sortingOrder = sortingOrder;
        StartCoroutine(PlayOneShotEffectRoutine(effect, effectRenderer, frames, Mathf.Max(0.01f, duration)));
    }

    private IEnumerator PlayOneShotEffectRoutine(GameObject effect, SpriteRenderer effectRenderer,
        Sprite[] frames, float duration)
    {
        float frameDuration = duration / frames.Length;
        foreach (Sprite frame in frames)
        {
            if (effect == null || effectRenderer == null)
            {
                yield break;
            }
            if (frame != null)
            {
                effectRenderer.sprite = frame;
            }
            yield return new WaitForSeconds(frameDuration);
        }
        if (effect != null)
        {
            Destroy(effect);
        }
    }

    public void StopAnimation()
    {
        if (spriteStateCoroutine != null)
        {
            StopCoroutine(spriteStateCoroutine);
            spriteStateCoroutine = null;
        }
        if (animator != null)
        {
            animator.enabled = false;
        }
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        if (!SetSpriteByKey(currentIdleKey) && spriteRenderer != null)
        {
            spriteRenderer.sprite = cachedIdleSprite;
        }
    }

    private void StartSpriteState(string spriteKey, float duration)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        StopAnimationPlayback();

        if (spriteStateCoroutine != null)
        {
            StopCoroutine(spriteStateCoroutine);
        }
        spriteStateCoroutine = StartCoroutine(SpriteStateRoutine(spriteKey, duration));
    }

    private void StopAnimationPlayback()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        if (animator != null)
        {
            animator.enabled = false;
        }
    }

    private IEnumerator SpriteStateRoutine(string spriteKey, float duration)
    {
        SpriteStateEntry state = FindSpriteState(spriteKey);
        ApplyStateVisualScale(state != null ? state.visualScaleMultiplier : 1f);
        if (state != null && state.frames != null && state.frames.Length > 1)
        {
            float elapsed = 0f;
            float frameDuration = 1f / Mathf.Max(1f, state.framesPerSecond);
            while (elapsed < duration)
            {
                for (int frameIndex = 0; frameIndex < state.frames.Length && elapsed < duration; frameIndex++)
                {
                    if (state.frames[frameIndex] != null)
                    {
                        spriteRenderer.sprite = state.frames[frameIndex];
                    }
                    float wait = Mathf.Min(frameDuration, duration - elapsed);
                    yield return new WaitForSeconds(wait);
                    elapsed += wait;
                }
            }
        }
        else
        {
            SetSpriteByKey(spriteKey);
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
        }
        if (!SetSpriteByKey(currentIdleKey) && spriteRenderer != null)
        {
            spriteRenderer.sprite = cachedIdleSprite;
        }
        spriteStateCoroutine = null;
    }

    private bool SetSpriteByKey(string key)
    {
        if (spriteRenderer == null || string.IsNullOrEmpty(key))
        {
            return false;
        }

        SpriteStateEntry stateEntry = FindSpriteState(key);
        if (stateEntry != null)
        {
            ApplyStateVisualScale(stateEntry.visualScaleMultiplier);
            Sprite stateSprite = stateEntry.sprite;
            if (stateSprite == null && stateEntry.frames != null && stateEntry.frames.Length > 0)
            {
                stateSprite = stateEntry.frames[0];
            }
            if (stateSprite != null)
            {
                spriteRenderer.sprite = stateSprite;
                return true;
            }
        }

        if (spriteRenderer.sprite != null && string.Equals(spriteRenderer.sprite.name, key, StringComparison.Ordinal))
        {
            ApplyStateVisualScale(1f);
            return true;
        }

        Sprite resourceSprite = Resources.Load<Sprite>(key);
        if (resourceSprite != null)
        {
            spriteRenderer.sprite = resourceSprite;
            ApplyStateVisualScale(1f);
            return true;
        }
        return false;
    }

    private SpriteStateEntry FindSpriteState(string key)
    {
        foreach (SpriteStateEntry state in spriteStates)
        {
            if (state != null && string.Equals(state.key, key, StringComparison.Ordinal))
            {
                return state;
            }
        }
        return null;
    }

    public Sprite[] GetSpriteStateFrames(string key)
    {
        SpriteStateEntry state = FindSpriteState(key);
        if (state == null)
        {
            return null;
        }
        if (state.frames != null && state.frames.Length > 0)
        {
            return state.frames;
        }
        return state.sprite != null ? new[] { state.sprite } : null;
    }

    public Sprite GetGuideIdleSprite()
    {
        string[] preferredKeys = { "idle_ready", "idle", idleSpriteKey, "idle_empty" };
        foreach (string key in preferredKeys)
        {
            SpriteStateEntry state = FindSpriteState(key);
            Sprite sprite = GetFirstSprite(state);
            if (sprite != null)
            {
                return sprite;
            }
        }

        foreach (SpriteStateEntry state in spriteStates)
        {
            if (state != null && state.key != null &&
                state.key.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0 &&
                state.key.IndexOf("skill", StringComparison.OrdinalIgnoreCase) < 0 &&
                state.key.IndexOf("shield", StringComparison.OrdinalIgnoreCase) < 0)
            {
                Sprite sprite = GetFirstSprite(state);
                if (sprite != null)
                {
                    return sprite;
                }
            }
        }

        Transform root = visualRoot != null
            ? visualRoot
            : transform.name == "VisualRoot" ? transform : transform.Find("VisualRoot");
        SpriteRenderer renderer = root != null ? root.GetComponent<SpriteRenderer>() : null;
        return renderer != null ? renderer.sprite : null;
    }

    private static Sprite GetFirstSprite(SpriteStateEntry state)
    {
        if (state == null)
        {
            return null;
        }
        if (state.sprite != null)
        {
            return state.sprite;
        }
        return state.frames != null && state.frames.Length > 0 ? state.frames[0] : null;
    }

    public float GetSpriteStateFrameRate(string key)
    {
        SpriteStateEntry state = FindSpriteState(key);
        return state != null ? Mathf.Max(1f, state.framesPerSecond) : 12f;
    }

    private IEnumerator LoopSpriteFrames(SpriteStateEntry state)
    {
        ApplyStateVisualScale(state.visualScaleMultiplier);
        float frameDuration = 1f / Mathf.Max(1f, state.framesPerSecond);
        while (state.frames != null && state.frames.Length > 0)
        {
            foreach (Sprite frame in state.frames)
            {
                if (frame != null && spriteRenderer != null)
                {
                    spriteRenderer.sprite = frame;
                }
                yield return new WaitForSeconds(frameDuration);
            }
        }
    }
}