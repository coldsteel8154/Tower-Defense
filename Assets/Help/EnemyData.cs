using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyData
{
    public Sprite sprite;
    public float rate, speed, health;

    public EnemyData(Sprite sprite, float rate, float speed)
    {
        this.sprite = sprite;
        this.rate = rate;
        this.speed = speed;
    }
}
