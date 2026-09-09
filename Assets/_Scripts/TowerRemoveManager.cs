using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TowerRemoveManager : MonoBehaviour
{
    public static TowerRemoveManager instance;

    [Header("Visual Feedback")]
    public Color hoverRemoveColor = new Color(1.0f, 0.3f, 0.3f, 0.8f);

    private bool isRemoveMode = false;
    public bool IsRemoveMode => isRemoveMode;

    private Tower lastHoveredTower;
    private Color lastOriginalColor;

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
        // Bind button at runtime
        GameObject btnObj = GameObject.Find("RemoveTowerButton");
        if (btnObj != null)
        {
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(ToggleRemoveMode);
            }
        }
    }

    private void Update()
    {
        // Don't trigger remove hotkey while the command input is active or focused
        bool isChatFocused = false;
        if (CheatCommandSystem.instance != null)
        {
            if (CheatCommandSystem.instance.commandPanel != null && CheatCommandSystem.instance.commandPanel.activeSelf)
            {
                isChatFocused = true;
            }
            if (CheatCommandSystem.instance.commandInput != null && CheatCommandSystem.instance.commandInput.isFocused)
            {
                isChatFocused = true;
            }
        }

        if (isChatFocused)
        {
            // Allow escape handling below but skip hotkey toggles
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ToggleRemoveMode();
            }
        }

        if (!isRemoveMode) return;

        // Follow mouse cursor to highlight hovered tower
        Tower currentHovered = FindHoveredTower();

        if (currentHovered != lastHoveredTower)
        {
            // Restore previous tower's color
            RestoreLastTowerColor();

            // Store new tower and color it red
            if (currentHovered != null)
            {
                lastHoveredTower = currentHovered;
                SpriteRenderer sr = lastHoveredTower.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    lastOriginalColor = sr.color;
                    sr.color = hoverRemoveColor;
                }
            }
        }

        // Cancel on Escape or Right Click
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            ExitRemoveMode();
            return;
        }

        // Click to remove
        if (Input.GetMouseButtonDown(0))
        {
            bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!isOverUI && currentHovered != null)
            {
                RemoveTower(currentHovered);
            }
        }
    }

    public void ToggleRemoveMode()
    {
        if (TowerPlacementManager.instance != null && TowerPlacementManager.instance.IsPlacing)
        {
            TowerPlacementManager.instance.CancelPlacement();
        }

        isRemoveMode = !isRemoveMode;
        if (!isRemoveMode)
        {
            RestoreLastTowerColor();
        }
        Debug.Log("Remove Mode Active: " + isRemoveMode);
    }

    public void ExitRemoveMode()
    {
        isRemoveMode = false;
        RestoreLastTowerColor();
    }

    private void RemoveTower(Tower tower)
    {
        // Refund half cost
        if (GameManager.instance != null)
        {
            int refund = tower.cost / 2;
            GameManager.instance.playerMoney += refund;
            GameManager.instance.UpdateMoneyUI();
            Debug.Log("Refunded $" + refund + " for removing " + tower.name);
        }

        // Play an explosion or effect if any (could use same particles)
        if (GameManager.instance != null && GameManager.instance.deathParticlePrefab != null)
        {
            GameObject particles = Instantiate(GameManager.instance.deathParticlePrefab, tower.transform.position, Quaternion.identity);
            Destroy(particles, 1.5f);
        }

        // Clean up hover tracking references
        if (tower == lastHoveredTower)
        {
            lastHoveredTower = null;
        }

        Destroy(tower.gameObject);
        ExitRemoveMode();
    }

    private Tower FindHoveredTower()
    {
        if (Camera.main == null) return null;

        bool isOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        if (isOverUI) return null;

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        Tower[] allTowers = Object.FindObjectsByType<Tower>(FindObjectsInactive.Exclude);
        foreach (var t in allTowers)
        {
            if (t != null && Vector2.Distance(t.transform.position, mouseWorldPos) <= 1.2f)
            {
                return t;
            }
        }

        return null;
    }

    private void RestoreLastTowerColor()
    {
        if (lastHoveredTower != null)
        {
            SpriteRenderer sr = lastHoveredTower.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = lastOriginalColor;
            }
            lastHoveredTower = null;
        }
    }

    private SpriteRenderer cachedSpriteRenderer;
}
