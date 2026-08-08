using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class TowerPlacementManager : MonoBehaviour
{
    public static TowerPlacementManager instance;

    [Header("Tower Settings")]
    [SerializeField] private GameObject towerPrefab;
    [SerializeField] private int towerCost = 100;

    public GameObject TowerPrefab => towerPrefab;
    public int TowerCost => towerCost;

    [Header("Placement Rules")]
    [SerializeField] private float mapMinX = -11.5f;
    [SerializeField] private float mapMaxX = 11.5f;
    [SerializeField] private float mapMinY = -4.5f;
    [SerializeField] private float mapMaxY = 4.5f;
    [SerializeField] private float minPathDistance = 1.0f;
    [SerializeField] private float minTowerDistance = 1.2f;

    [Header("Visual Feedback")]
    [SerializeField] private Color validColor = new Color(0.3f, 1.0f, 0.3f, 0.7f);
    [SerializeField] private Color invalidColor = new Color(1.0f, 0.3f, 0.3f, 0.7f);
    [SerializeField] private Color validRangeColor = new Color(0.3f, 1.0f, 0.3f, 0.2f);
    [SerializeField] private Color invalidRangeColor = new Color(1.0f, 0.3f, 0.3f, 0.2f);

    private GameObject previewInstance;
    private SpriteRenderer previewMainRenderer;
    private SpriteRenderer previewRangeRenderer;
    private TowerRange previewTowerRange;
    private bool isPlacing = false;
    public bool IsPlacing => isPlacing;

    private GameObject currentTowerPrefab;
    private int currentTowerCost;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Automatically bind the click event at runtime to prevent any inspector setup issues
        GameObject btnObj = GameObject.Find("BuyTowerButton");
        if (btnObj != null)
        {
            UnityEngine.UI.Button btn = btnObj.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(StartPlacement);
                Debug.Log("Successfully bound BuyTowerButton to TowerPlacementManager.StartPlacement() at runtime.");
            }
        }
    }

    private void Update()
    {
        if (!isPlacing) return;

        // Follow mouse cursor
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        previewInstance.transform.position = mouseWorldPos;

        // Check if cursor is over any UI element to prevent placement when clicking buttons
        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // Check placement validity
        bool hasMoney = GameManager.instance != null ? GameManager.instance.playerMoney >= currentTowerCost : false;
        bool isValid = !isOverUI && IsValidPlacement(mouseWorldPos) && hasMoney;

        // Update preview colors
        UpdatePreviewColors(isValid);

        // Cancel placement
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        // Place tower
        if (Input.GetMouseButtonDown(0))
        {
            if (isValid)
            {
                PlaceTower(mouseWorldPos);
            }
            else if (isOverUI)
            {
                // Just let the UI click happen, don't cancel or force placement
            }
            else
            {
                Debug.LogWarning("Invalid placement position or insufficient funds!");
            }
        }
    }

    public void StartPlacement()
    {
        if (TowerRemoveManager.instance != null && TowerRemoveManager.instance.IsRemoveMode)
        {
            TowerRemoveManager.instance.ExitRemoveMode();
        }

        if (isPlacing)
        {
            CancelPlacement();
            Debug.Log("Cancelled tower placement via BuyTowerButton.");
            return;
        }

        StartPlacement(towerPrefab, towerCost);
    }

    public void StartPlacement(GameObject prefab, int cost)
    {
        if (TowerRemoveManager.instance != null && TowerRemoveManager.instance.IsRemoveMode)
        {
            TowerRemoveManager.instance.ExitRemoveMode();
        }

        if (isPlacing)
        {
            // If the player presses a buy button again while already placing,
            // cancel the current placement instead of silently ignoring.
            if (currentTowerPrefab == prefab && currentTowerCost == cost)
            {
                CancelPlacement();
                Debug.Log("Cancelled tower placement by pressing the same tower button again.");
                return;
            }

            // Switch placement to the new tower prefab if a different tower is selected.
            CancelPlacement();
        }

        if (prefab == null)
        {
            Debug.LogError("Cannot start placement: tower prefab is null!");
            return;
        }

        if (GameManager.instance == null)
        {
            Debug.LogError("Cannot start placement: GameManager.instance is null.");
            return;
        }

        if (GameManager.instance.playerMoney < cost)
        {
            Debug.LogWarning("Insufficient money to buy a tower!");
            return;
        }

        currentTowerPrefab = prefab;
        currentTowerCost = cost;
        isPlacing = true;

        // Instantiate tower as preview
        previewInstance = Instantiate(currentTowerPrefab);
        previewInstance.name = "Tower_Preview";

        // Disable logic components so it doesn't act as a real tower
        Tower towerComp = previewInstance.GetComponent<Tower>();
        if (towerComp != null) towerComp.enabled = false;

        var rb = previewInstance.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;

        previewMainRenderer = previewInstance.GetComponent<SpriteRenderer>();

        // Disable range trigger logic, but keep range renderer visual
        Transform rangeChild = previewInstance.transform.Find("Range");
        if (rangeChild != null)
        {
            previewTowerRange = rangeChild.GetComponent<TowerRange>();
            if (previewTowerRange != null)
            {
                previewTowerRange.UpdateRange();
                previewTowerRange.enabled = false;
            }

            var col = rangeChild.GetComponent<CircleCollider2D>();
            if (col != null) col.enabled = false;

            previewRangeRenderer = rangeChild.GetComponent<SpriteRenderer>();
            if (previewRangeRenderer != null)
            {
                previewRangeRenderer.enabled = true;
            }
        }
    }

    public void CancelPlacement()
    {
        isPlacing = false;
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }
    }

    private void PlaceTower(Vector3 position)
    {
        isPlacing = false;

        // Deduct money
        if (GameManager.instance != null)
        {
            GameManager.instance.playerMoney -= currentTowerCost;
            GameManager.instance.UpdateMoneyUI();
        }
        else
        {
            Debug.LogError("PlaceTower: GameManager.instance is null; cannot deduct money.");
        }

        // Spawn actual tower
        GameObject realTower = Instantiate(currentTowerPrefab, position, Quaternion.identity);
        realTower.name = currentTowerPrefab.name + "_" + System.DateTime.Now.Ticks;
        
        Tower towerComp = realTower.GetComponent<Tower>();
        if (towerComp != null)
        {
            towerComp.cost = currentTowerCost;
        }


        // Clean up preview
        Destroy(previewInstance);

        Debug.Log("Successfully placed new tower at " + position);
    }

    private void UpdatePreviewColors(bool isValid)
    {
        if (previewMainRenderer != null)
        {
            previewMainRenderer.color = isValid ? validColor : invalidColor;
        }

        if (previewRangeRenderer != null)
        {
            previewRangeRenderer.color = isValid ? validRangeColor : invalidRangeColor;
        }
    }

    public bool IsValidPlacement(Vector2 position)
    {
        // 1. Check map boundaries
        if (position.x < mapMinX || position.x > mapMaxX || position.y < mapMinY || position.y > mapMaxY)
            return false;

        // 2. Check distance from the path segments
        if (Path.path != null && Path.path.point != null)
        {
            Transform[] points = Path.path.point;
            for (int i = 0; i < points.Length - 1; i++)
            {
                if (points[i] != null && points[i + 1] != null)
                {
                    float dist = DistanceToSegment(position, points[i].position, points[i + 1].position);
                    if (dist < minPathDistance)
                    {
                        return false; // Too close to the path
                    }
                }
            }
        }

        // 3. Check overlap with other towers
        Tower[] existingTowers = FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
        foreach (var tower in existingTowers)
        {
            // Skip the preview tower itself!
            if (tower.gameObject == previewInstance) continue;

            if (Vector2.Distance(position, tower.transform.position) < minTowerDistance)
            {
                return false; // Too close to another tower
            }
        }

        return true;
    }

    private float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        Vector2 ap = p - a;
        float t = Vector2.Dot(ap, ab) / Vector2.Dot(ab, ab);
        t = Mathf.Clamp01(t);
        Vector2 closest = a + t * ab;
        return Vector2.Distance(p, closest);
    }
}
