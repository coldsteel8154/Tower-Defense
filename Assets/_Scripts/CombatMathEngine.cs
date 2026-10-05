using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CombatMathEngine
{
    public static RaycastHit2D[] PerformHitscan(Vector2 origin, Vector2 direction, float range, float width, LayerMask mask)
    {
        if (range <= 0f || direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return new RaycastHit2D[0];
        }

        direction.Normalize();
        RaycastHit2D[] hits = width > 0f
            ? Physics2D.CircleCastAll(origin, width * 0.5f, direction, range, mask)
            : Physics2D.RaycastAll(origin, direction, range, mask);

        Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));
        return hits;
    }

    public static List<Enemy> GetEnemiesInCone(Vector2 origin, Vector2 direction, float range, float angle, LayerMask mask)
    {
        List<Enemy> enemies = new List<Enemy>();
        if (range <= 0f || direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return enemies;
        }

        Vector2 forward = direction.normalized;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(origin, range, mask);
        HashSet<Enemy> seenEnemies = new HashSet<Enemy>();

        foreach (Collider2D hitCollider in colliders)
        {
            Enemy enemy = hitCollider.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = hitCollider.GetComponentInParent<Enemy>();
            }
            if (enemy == null || !seenEnemies.Add(enemy))
            {
                continue;
            }

            Vector2 directionToEnemy = (Vector2)enemy.transform.position - origin;
            if (Vector2.Angle(forward, directionToEnemy) <= angle * 0.5f)
            {
                enemies.Add(enemy);
            }
        }

        return enemies;
    }

    public static IEnumerator GrenadeTrajectoryRoutine(GameObject grenadeObj, Vector2 startPos, Vector2 targetPos,
        GameBalanceSettings.SpecialTowerEvolutionStats settings, Action onExplode)
    {
        if (grenadeObj == null)
        {
            onExplode?.Invoke();
            yield break;
        }

        Transform grenadeTransform = grenadeObj.transform;
        float grenadeZ = grenadeTransform.position.z;
        float rotation = grenadeTransform.eulerAngles.z;
        Vector2 forward = targetPos - startPos;
        forward = forward.sqrMagnitude > Mathf.Epsilon ? forward.normalized : Vector2.right;

        float elapsed = 0f;
        while (elapsed < settings.projectileAirDuration)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, settings.projectileAirDuration);
            float progress = elapsed / settings.projectileAirDuration;
            Vector2 groundPosition = Vector2.Lerp(startPos, targetPos, progress);
            float height = 4f * settings.projectileAirPeakHeight * progress * (1f - progress);
            grenadeTransform.position = new Vector3(groundPosition.x, groundPosition.y + height, grenadeZ);
            grenadeTransform.rotation = Quaternion.Euler(0f, 0f, rotation + settings.projectileAirRotationSpeed * elapsed);
            yield return null;
        }

        rotation += settings.projectileAirRotationSpeed * settings.projectileAirDuration;
        Vector2 bounceStart = targetPos;
        Vector2 bounceEnd = bounceStart + forward * settings.projectileBounceDistance;
        elapsed = 0f;
        while (elapsed < settings.projectileBounceDuration)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, settings.projectileBounceDuration);
            float progress = elapsed / settings.projectileBounceDuration;
            Vector2 groundPosition = Vector2.Lerp(bounceStart, bounceEnd, progress);
            float height = 4f * settings.projectileBouncePeakHeight * progress * (1f - progress);
            grenadeTransform.position = new Vector3(groundPosition.x, groundPosition.y + height, grenadeZ);
            grenadeTransform.rotation = Quaternion.Euler(0f, 0f, rotation + settings.projectileBounceRotationSpeed * elapsed);
            yield return null;
        }

        rotation += settings.projectileBounceRotationSpeed * settings.projectileBounceDuration;
        Vector2 rollStart = bounceEnd;
        Vector2 rollEnd = rollStart + forward * settings.projectileRollDistance;
        elapsed = 0f;
        while (elapsed < settings.projectileRollDuration)
        {
            elapsed = Mathf.Min(elapsed + Time.deltaTime, settings.projectileRollDuration);
            float progress = elapsed / settings.projectileRollDuration;
            Vector2 groundPosition = Vector2.Lerp(rollStart, rollEnd, progress);
            float dampedRotation = settings.projectileRollRotationSpeed * settings.projectileRollDuration
                * (progress - 0.5f * progress * progress);
            grenadeTransform.position = new Vector3(groundPosition.x, groundPosition.y, grenadeZ);
            grenadeTransform.rotation = Quaternion.Euler(0f, 0f, rotation + dampedRotation);
            yield return null;
        }

        UnityEngine.Object.Destroy(grenadeObj);
        onExplode?.Invoke();
    }

    public static void ApplyKnockback(Enemy enemy, float distance, Vector2 direction)
    {
        if (enemy == null || distance <= 0f || direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 displacement = (Vector3)(direction.normalized * distance);
        enemy.transform.position += displacement;
    }
}