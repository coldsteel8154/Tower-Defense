using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerRange : MonoBehaviour
{
    [SerializeField] private Tower Tower;
    private List<GameObject> targets = new List<GameObject>();
    void Start()
    {
        UpdateRange();
    }

    // Update is called once per frame
    void Update()
    {
        if(targets.Count > 0)
        {
            Tower.target = targets[0];
        }
    }
    private void OTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Enemy")
        {
            targets.Add(collision.gameObject);
        }
    }

    private void OTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Enemy")
        {
            targets.Remove(collision.gameObject);
        }
    }

    public void UpdateRange()
    {
        transform.localScale = new Vector3(Tower.range, Tower.range, Tower.range);
    }
}
