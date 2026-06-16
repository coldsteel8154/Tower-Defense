using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public float range = 8f;
    public int damage =8 ;
    public float fireRate =1f;

    public GameObject target;
    private float cooldown = 0f;
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(target)
        {
            if(cooldown >= fireRate)
            {
                transform.right = target.transform.position - transform.position;
                Debug.Log("防禦塔攻擊了：" + target.name); 

                target.GetComponent<EnemyLocalData>().TakeDamage(damage); // 注意：確認你的敵人腳本名稱是 EnemyLocalData 還是 Enemy
                cooldown = 0f;
            }
            else
            {
                cooldown += 1 * Time.deltaTime;
            }
        }
    }
    
}
