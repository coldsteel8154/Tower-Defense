using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private int health = 50;
    [SerializeField] private float movespeed = 2.0f;

    private Rigidbody2D rb;
    private Transform checkpoint;
    private int index = 0;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    void Start()
    {
        if (EnemyManager.main != null && EnemyManager.main.checkpoints != null && EnemyManager.main.checkpoints.Length > 0)
        {
            checkpoint = EnemyManager.main.checkpoints[index];
        }
    }

    void FixedUpdate()
    {
        if (checkpoint == null) return;

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
        transform.right = checkpoint.position - transform.position;
        if (rb != null)
        {
            rb.linearVelocity = direction * movespeed;
        }
    }
    
    public void damage(int damage)
    {
        TakeDamage(damage);
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.playerMoney += 50;
            GameManager.instance.UpdateMoneyUI();
        }
        if (SpawnManager.enemy_list != null)
        {
            SpawnManager.enemy_list.Remove(gameObject);
        }
        Destroy(gameObject);
    }
}
