using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class InitData : MonoBehaviour
{
    public static InitData level;
    public Sprite[] enemy;
    public float[] speed;
    public int Count;
    public List<EnemyData> enemyDatas = new();
    public float delay = 1f;
    private void Awake()
    {
        Count = enemy.Count();
        int sum = Count * (Count + 1) / 2;
        int n = 0;
        for (int i = Count; i > 0; i--)
        {
            n += i;
            enemyDatas.Add(new EnemyData(enemy[i - 1], n * 1.0f / sum, speed[i - 1]));
        }
        level = this;
    }

}
