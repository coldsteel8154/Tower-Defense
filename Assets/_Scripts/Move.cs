using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Move : MonoBehaviour
{
    private int point = 1;
    private Vector2 pos;
    private Quaternion rot;
    [SerializeField] private float speed = 3f;
    private void Start()
    {
        transform.position = Path.path.point[0].position;
        pos = transform.position;
    }

    private void FixedUpdate()
    {
        Vector2 pp = Path.path.point[point].position;
        pos = Vector2.MoveTowards(
                transform.position,
                pp,
                speed * Time.fixedDeltaTime
            );
        Vector2 pr = pp - (Vector2)transform.position;
        rot = Quaternion.Euler(0, 0, Mathf.Atan2(pr.y, pr.x) * Mathf.Rad2Deg + 90);
        transform.SetPositionAndRotation(
            pos,
            rot
        );
        if (Vector2.Distance(Path.path.point[point].position, transform.position) <= 0.01f) point++;
        if (point >= Path.path.point.Count())
        {
            SpawnManager.enemy_list.Remove(gameObject);
            Destroy(gameObject);
        }
    }
}
