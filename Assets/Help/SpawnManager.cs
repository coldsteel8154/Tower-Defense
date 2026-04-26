using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    [SerializeField] private GameObject enemy;
    public static List<GameObject> enemy_list = new();
    private float time_delay = 0;

    private void FixedUpdate()
    {
        time_delay += Time.fixedDeltaTime;
        if (time_delay >= InitData.level.delay)
        {
            time_delay = 0f;
            GameObject spawn = Instantiate(enemy);
            EnemyData data = FindRate();
            enemy_list.Add(spawn);
            spawn.GetComponent<SpriteRenderer>().sprite = data.sprite;
            spawn.GetComponent<EnemyLocalData>().speed = data.speed;
        }
    }

    private EnemyData FindRate()
    {
        float random = Random.Range(0.0f, 1.0f);
        for (int i = 0; i < InitData.level.Count; i++)
        {
            if (InitData.level.enemyDatas[i].rate >= random)
            {
                return InitData.level.enemyDatas[i];
            }
        }
        return InitData.level.enemyDatas[InitData.level.Count - 1];
    }
}
