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
        rb=GetComponent<Rigidbody2D>();
    }
    void Start()
    {
        checkpoint = EnemyManager.main.checkpoints[index];
    }

    void Update()
    {
        checkpoint = EnemyManager.main.checkpoints[index];

        if(Vector2.Distance(checkpoint.transform.position,transform.position) <= 0.1f) 
        {
            index++;
            if(index >= EnemyManager.main.checkpoints.Length)
            {
                Destroy(gameObject);
            }
            //Debug.Log("Checkpoint Reached");
        }
        if(health <= 0)
        {
            Destroy(gameObject);
        }
    }
    void FixedUpdate()
    {
        Vector2 direction = (checkpoint.position - transform.position).normalized;
        transform.right = checkpoint.position - transform.position;
        rb.velocity = direction * movespeed;
    }
    public void damage(int damage)
    {
        health = damage;
    }
}
