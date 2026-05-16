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
        while (targets.Count > 0 && targets[0] == null)
        {
            targets.RemoveAt(0);
        }

        if (targets.Count > 0)
        {
            Tower.target = targets[0]; // 鎖定第一個進來且還活著的敵人
        }
        else
        {
            Tower.target = null; // 範圍內沒敵人時，清空目標
        }
    }

        private void OnTriggerEnter2D(Collider2D collision)
    {
        // 建議改用 CompareTag，效能比 == "Enemy" 更好，且不會產生垃圾記憶體 (GC)
        if (collision.CompareTag("Enemy"))
        {
            // 防止重複加入同一個物件
            if (!targets.Contains(collision.gameObject))
            {
                targets.Add(collision.gameObject);
            }
        }
    }

    // 【修復拼字】確保 Unity 能正確觸發
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Enemy"))
        {
            targets.Remove(collision.gameObject);
        }
    }

    public void UpdateRange()
    {
        if (Tower != null)
        {
            transform.localScale = new Vector3(Tower.range, Tower.range, 1f); // 2D 遊戲的 Z 軸維持 1 即可
        }
    }
}
