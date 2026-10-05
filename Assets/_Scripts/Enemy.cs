using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public event Action<Enemy> Died;

    public int health = 50;
    public float movespeed = 2.0f;
    [HideInInspector] public int maxHealth = 50;

    private Rigidbody2D rb;
    private Transform checkpoint;
    private int index = 0;
    private Coroutine pushCoroutine;
    private SpriteRenderer cachedSpriteRenderer;
    private EnemyStatusManager statusManager;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        cachedSpriteRenderer = GetComponent<SpriteRenderer>();
        statusManager = GetComponent<EnemyStatusManager>();
        if (statusManager == null)
        {
            statusManager = gameObject.AddComponent<EnemyStatusManager>();
        }

        GameBalanceSettings settings = GameBalanceSettings.Instance;
        statusManager.SetTagIcons(
            settings.GetStatusTagIcon(StatusTagType.Slowed),
            settings.GetStatusTagIcon(StatusTagType.Dazzled),
            settings.GetStatusTagIcon(StatusTagType.Vulnerable),
            settings.GetStatusTagIcon(StatusTagType.Haste),
            settings.GetStatusTagIcon(StatusTagType.Damage),
            settings.GetStatusTagIcon(StatusTagType.Range));
    }
    
    void Start()
    {
        maxHealth = health;
        gameObject.AddComponent<EnemyHealthBar>();

        if (EnemyManager.main != null && EnemyManager.main.checkpoints != null && EnemyManager.main.checkpoints.Length > 0)
        {
            checkpoint = EnemyManager.main.checkpoints[index];
        }
    }

    void FixedUpdate()
    {
        if (checkpoint == null) return;

        // If game is frozen (via cheat command), stop moving
        if (CheatCommandSystem.isFrozen)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        if (Vector2.Distance(transform.position, checkpoint.position) <= 0.1f)
        {
            index++;
            if (EnemyManager.main != null && EnemyManager.main.checkpoints != null && index < EnemyManager.main.checkpoints.Length)
            {
                checkpoint = EnemyManager.main.checkpoints[index];
            }
            else
            {
                // Reached the end
                if (GameManager.instance != null)
                {
                    GameManager.instance.LoseLife(1);
                }
                if (SpawnManager.enemy_list != null)
                {
                    SpawnManager.enemy_list.Remove(gameObject);
                }
                Destroy(gameObject);
                return;
            }
        }

        Vector2 direction = (checkpoint.position - transform.position).normalized;
        // Fix: Some enemy sprites face the opposite direction; flip Simons by 180 degrees
        if (gameObject.name.ToLower().Contains("simon"))
        {
            transform.up = -direction; // face the other way for Simon-type enemies
        }
        else
        {
            transform.up = direction;
        }

        // Flip sprite to face movement
        if (cachedSpriteRenderer != null && direction.x != 0f)
        {
            cachedSpriteRenderer.flipX = direction.x < 0f;
        }

        if (rb != null)
        {
            float statusSpeedMultiplier = statusManager != null ? statusManager.GetCurrentSpeedMultiplier() : 1f;
            rb.linearVelocity = direction * movespeed * statusSpeedMultiplier;
        }
    }
    
    public void damage(int damage)
    {
        TakeDamage(damage);
    }

    public void TakeDamage(int amount)
    {
        if (health <= 0) return;

        EnemyStatusManager activeStatusManager = statusManager != null ? statusManager : GetComponent<EnemyStatusManager>();
        if (activeStatusManager != null)
        {
            amount = Mathf.RoundToInt(amount * activeStatusManager.GetVulnerabilityDamageMultiplier());
            if (UnityEngine.Random.value < activeStatusManager.GetAllyCriticalChance())
            {
                amount *= 2;
            }
        }

        health -= amount;

        // Register damage dealt
        if (GameManager.instance != null)
        {
            GameManager.instance.damageDealt += amount;
        }

        // Show floating hit indicator
        FloatingText.Create(transform.position, "-" + amount, Color.red);

        if (health <= 0)
        {
            Die();
        }
    }

    public void ApplyPush(Vector3 sniperPosition)
    {
        Vector3 pushDirection = (transform.position - sniperPosition).normalized;
        float pushDistance = 0.8f;
        float pushDuration = 0.2f;

        if (pushCoroutine != null)
        {
            StopCoroutine(pushCoroutine);
        }
        pushCoroutine = StartCoroutine(SlidePush(pushDirection, pushDistance, pushDuration));
    }

    private IEnumerator SlidePush(Vector3 direction, float distance, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction * distance;
        float elapsed = 0f;
        bool hasRb = rb != null;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Min(elapsed / duration, 1f));
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);

            if (hasRb)
            {
                rb.MovePosition(newPos);
            }
            else
            {
                transform.position = newPos;
            }

            yield return null;
        }

        if (hasRb)
        {
            rb.MovePosition(targetPos);
        }
        else
        {
            transform.position = targetPos;
        }

        pushCoroutine = null;
    }

    private void Die()
    {
        Died?.Invoke(this);

        if (GameManager.instance != null)
        {
            if (gameObject.name.Contains("simonking"))
            {
                GameManager.instance.simonKingKills++;
            }
            else if (gameObject.name.Contains("ultrasimon"))
            {
                GameManager.instance.ultraSimonKills++;
            }
            else
            {
                GameManager.instance.regularSimonKills++;
            }

            int reward = GameBalanceSettings.Instance.GetEnemyReward(gameObject.name, DifficultySettings.GetBalanceDifficulty());
            GameManager.instance.playerMoney += reward;
            GameManager.instance.UpdateMoneyUI();
        }

        // Explode death particles!
        if (GameManager.instance != null && GameManager.instance.deathParticlePrefab != null)
        {
            GameObject particles = Instantiate(GameManager.instance.deathParticlePrefab, transform.position, Quaternion.identity);
            
            // Apply particle density option
            var ps = particles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                float alpha = 0.95f;
                if (DifficultySettings.particleSetting == "Less")
                {
                    alpha = 0.55f;
                }
                else if (DifficultySettings.particleSetting == "Least")
                {
                    alpha = 0.2f;
                }
                
                // Slightly bright green color matching Simon's skin/body
                main.startColor = new Color(0.25f, 0.8f, 0.35f, alpha);

                var emission = ps.emission;
                if (DifficultySettings.particleSetting == "Less")
                {
                    emission.rateOverTimeMultiplier = 0.5f;
                    ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
                    emission.GetBursts(bursts);
                    for (int i = 0; i < bursts.Length; i++)
                    {
                        float count = Mathf.Max(1f, bursts[i].count.constant * 0.5f);
                        bursts[i].count = new ParticleSystem.MinMaxCurve(count);
                    }
                    emission.SetBursts(bursts);
                }
                else if (DifficultySettings.particleSetting == "Least")
                {
                    emission.rateOverTimeMultiplier = 0.1f;
                    ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[emission.burstCount];
                    emission.GetBursts(bursts);
                    for (int i = 0; i < bursts.Length; i++)
                    {
                        float count = Mathf.Max(1f, bursts[i].count.constant * 0.1f);
                        bursts[i].count = new ParticleSystem.MinMaxCurve(count);
                    }
                    emission.SetBursts(bursts);
                }
            }

            Destroy(particles, 3f); // Clean up particles
        }

        if (SpawnManager.enemy_list != null)
        {
            SpawnManager.enemy_list.Remove(gameObject);
        }
        Destroy(gameObject);
    }
}

