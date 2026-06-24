using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public float range = 8f;
    public int damage = 8;
    public float fireRate = 1f;
    public float turnSpeed = 360f; // Turn speed in degrees per second

    [Header("Visual Settings")]
    public Sprite normalSprite;
    public Sprite fireSprite;
    public float fireSpriteDuration = 0.15f;

    public GameObject target;
    private float cooldown = 0f;

    private SpriteRenderer spriteRenderer;
    private SpriteRenderer rangeRenderer;
    private bool isHovered = false;
    private bool lastIsPlacing = false;
    private bool lastIsHovered = false;
    private Coroutine flashCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && normalSprite == null)
        {
            normalSprite = spriteRenderer.sprite;
        }

        Transform rangeChild = transform.Find("Range");
        if (rangeChild != null)
        {
            rangeRenderer = rangeChild.GetComponent<SpriteRenderer>();
        }
    }
    
    void Start()
    {
        UpdateRangeVisibility();
    }

    // Update is called once per frame
    void Update()
    {
        // Robust manual mouse hover detection (independent of Unity physics raycasting/input issues)
        bool currentIsHovered = false;
        if (Camera.main != null)
        {
            bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null && 
                            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (!isOverUI)
            {
                Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = 0;
                
                // Detection radius of 1.2f matches the tower visual bounds perfectly
                if (Vector2.Distance(transform.position, mouseWorldPos) <= 1.2f)
                {
                    currentIsHovered = true;
                }
            }
        }
        isHovered = currentIsHovered;

        // Update range visibility dynamically when placement mode or hover changes
        bool currentIsPlacing = TowerPlacementManager.instance != null && TowerPlacementManager.instance.IsPlacing;
        if (currentIsPlacing != lastIsPlacing || isHovered != lastIsHovered)
        {
            lastIsPlacing = currentIsPlacing;
            lastIsHovered = isHovered;
            UpdateRangeVisibility();
        }

        if(target)
        {
            // Smoothly rotate towards the target
            Vector3 dir = target.transform.position - transform.position;
            float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + 90f;
            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

            if(cooldown >= fireRate)
            {
                Debug.Log("防禦塔攻擊了：" + target.name); 

                TriggerFireFlash();

                EnemyLocalData enemyLocal = target.GetComponent<EnemyLocalData>();
                if (enemyLocal != null)
                {
                    enemyLocal.TakeDamage(damage);
                }
                else
                {
                    Enemy enemy = target.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.TakeDamage(damage);
                    }
                }
                cooldown = 0f;
            }
            else
            {
                cooldown += 1 * Time.deltaTime;
            }
        }
    }

    private void TriggerFireFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashFireSprite());
    }

    private IEnumerator FlashFireSprite()
    {
        if (spriteRenderer != null && fireSprite != null)
        {
            spriteRenderer.sprite = fireSprite;
        }
        yield return new WaitForSeconds(fireSpriteDuration);
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }
        flashCoroutine = null;
    }

    public void UpdateRangeVisibility()
    {
        if (rangeRenderer == null) return;

        bool shouldShow = isHovered || (TowerPlacementManager.instance != null && TowerPlacementManager.instance.IsPlacing);
        if (rangeRenderer.enabled != shouldShow)
        {
            rangeRenderer.enabled = shouldShow;
        }
    }
}
