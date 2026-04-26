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
                target.GetComponent<Enemy>().damage(damage);
                cooldown = 0f;
            }
            else
            {
                cooldown += 1 * Time.deltaTime;
            }
        }
    }
}
