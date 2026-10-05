using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LinebreakerVortexController : MonoBehaviour
{
    [SerializeField] private GameObject vortexPrefab;
    [SerializeField] private float vortexRadius = 2.5f;
    [SerializeField] private float vortexDuration = 3.0f;
    [SerializeField] private float slowIntensity = 0.5f;
    [SerializeField] private float pullInterval = 0.5f;
    [SerializeField] private float pullDistance = 0.4f;
    [SerializeField] private Sprite[] vortexFrames;
    [SerializeField] private float vortexFramesPerSecond = 15f;

    private Coroutine activeVortexRoutine;

    public void Configure(float radius, float duration, float slow, float pulseInterval, float pullAmount,
        Sprite[] visualFrames, float visualFrameRate)
    {
        vortexRadius = Mathf.Max(0f, radius);
        vortexDuration = Mathf.Max(0f, duration);
        slowIntensity = Mathf.Max(0f, slow);
        pullInterval = Mathf.Max(0f, pulseInterval);
        pullDistance = Mathf.Max(0f, pullAmount);
        vortexFrames = visualFrames;
        vortexFramesPerSecond = Mathf.Max(1f, visualFrameRate);
    }

    public void ActivateVortex()
    {
        ActivateVortex(transform.position);
    }

    public void ActivateVortex(Vector2 center)
    {
        if (activeVortexRoutine == null)
        {
            activeVortexRoutine = StartCoroutine(GravityVortexRoutine(center, vortexRadius, vortexDuration));
        }
    }

    public IEnumerator GravityVortexRoutine(Vector2 center, float radius, float duration)
    {
        if (duration <= 0f || radius <= 0f)
        {
            activeVortexRoutine = null;
            yield break;
        }

        GameObject vortexVisual = vortexPrefab != null
            ? Instantiate(vortexPrefab, center, Quaternion.identity)
            : null;
        if (vortexVisual != null)
        {
            Destroy(vortexVisual, duration);
            Transform vortexCore = FindChild(vortexVisual.transform, "VortexCore");
            SpriteRenderer coreRenderer = vortexCore != null ? vortexCore.GetComponent<SpriteRenderer>() : null;
            if (coreRenderer != null && vortexFrames != null && vortexFrames.Length > 0)
            {
                StartCoroutine(AnimateVortexCore(coreRenderer, duration));
            }
        }

        int enemyLayerMask = LayerMask.GetMask("Enemy");
        HashSet<Enemy> affectedEnemies = new HashSet<Enemy>();
        float elapsed = 0f;
        float timeUntilPull = pullInterval;

        while (elapsed < duration)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius, enemyLayerMask);
            foreach (Collider2D hitCollider in colliders)
            {
                Enemy enemy = hitCollider.GetComponent<Enemy>();
                if (enemy == null)
                {
                    enemy = hitCollider.GetComponentInParent<Enemy>();
                }

                if (enemy == null || !affectedEnemies.Add(enemy))
                {
                    continue;
                }

                EnemyStatusManager statusManager = enemy.GetComponent<EnemyStatusManager>();
                float effectDuration = duration - elapsed;
                if (statusManager != null && effectDuration > 0f)
                {
                    statusManager.AddEffect(
                        StatusTagType.Slowed,
                        SpecialTowerType.LinebreakerScout,
                        effectDuration,
                        slowIntensity,
                        "Gravity vortex slow");
                }
            }

            if (pullInterval > 0f && timeUntilPull <= 0f)
            {
                foreach (Collider2D hitCollider in colliders)
                {
                    Enemy enemy = hitCollider.GetComponent<Enemy>();
                    if (enemy == null)
                    {
                        enemy = hitCollider.GetComponentInParent<Enemy>();
                    }
                    if (enemy == null)
                    {
                        continue;
                    }

                    Vector2 pulledPosition = Vector2.MoveTowards(enemy.transform.position, center, pullDistance);
                    enemy.transform.position = new Vector3(pulledPosition.x, pulledPosition.y, enemy.transform.position.z);
                }
                timeUntilPull += pullInterval;
            }

            yield return null;
            float deltaTime = Time.deltaTime;
            elapsed += deltaTime;
            timeUntilPull -= deltaTime;
        }

        activeVortexRoutine = null;
    }

    private IEnumerator AnimateVortexCore(SpriteRenderer renderer, float duration)
    {
        float frameDuration = 1f / vortexFramesPerSecond;
        float elapsed = 0f;
        int frameIndex = 0;
        while (elapsed < duration && renderer != null)
        {
            renderer.sprite = vortexFrames[frameIndex];
            yield return new WaitForSeconds(frameDuration);
            elapsed += frameDuration;
            frameIndex = (frameIndex + 1) % vortexFrames.Length;
        }
    }

    private static Transform FindChild(Transform parent, string childName)
    {
        if (parent.name == childName)
        {
            return parent;
        }
        foreach (Transform child in parent)
        {
            Transform found = FindChild(child, childName);
            if (found != null)
            {
                return found;
            }
        }
        return null;
    }
}