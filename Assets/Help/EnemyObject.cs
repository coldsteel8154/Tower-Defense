using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;

public class EnemyLocalData : MonoBehaviour
{
    public float speed;
    private int endpoint;
    private int p = 1;
    private Vector2 pos;

    private int health = 8;
    private void Awake()
    {
        transform.position = Path.path.point[0].position;
        endpoint = Path.path.point.Count();
        pos = transform.position;
    }

    void Update()
    {
        if(health <= 0)
        {
            Destroy(gameObject);
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        Transform[] point = Path.path.point;
        if(Vector2.Distance(pos, point[p].position) <= 0.01f) p++;
        if(p >= endpoint)
        {
            SpawnManager.enemy_list.Remove(gameObject);
            Destroy(gameObject);
        }
        pos = Vector2.MoveTowards(pos, point[p].position, speed * Time.fixedDeltaTime);
        Vector2 dp = (Vector2)point[p].position - pos;
        Vector3 tr = Vector3.zero;
        tr.z = Mathf.Atan2(dp.y, dp.x) * Mathf.Rad2Deg + 90;
        transform.SetPositionAndRotation(pos, Quaternion.Euler(tr));

    }
    
    public void damage(int damage)
    {
        health -= damage; //  正確：扣減血量
    }

}
