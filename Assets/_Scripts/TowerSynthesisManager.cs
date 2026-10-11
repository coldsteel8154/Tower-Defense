using System.Collections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerSynthesisManager : MonoBehaviour
{
    [Serializable]
    private class SpecialTowerPrefabEntry
    {
        public SpecialTowerType towerType;
        public GameObject prefab;
    }

    private const float PlacementClearanceRadius = 0.6f;
    private static TowerSynthesisManager instance;
    private static AudioClip fallbackRecycleSound;

    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip recycleSound;
    [SerializeField] private List<SpecialTowerPrefabEntry> specialTowerPrefabs = new List<SpecialTowerPrefabEntry>();

    private Canvas synthesisCostCanvas;
    private RectTransform synthesisCostPanel;
    private TMP_Text synthesisCostText;

    public static TowerSynthesisManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = UnityEngine.Object.FindAnyObjectByType<TowerSynthesisManager>();
                if (instance == null)
                {
                    GameObject managerObject = new GameObject("TowerSynthesisManager");
                    instance = managerObject.AddComponent<TowerSynthesisManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
        }
    }

    private void OnDestroy()
    {
        if (synthesisCostCanvas != null)
        {
            Destroy(synthesisCostCanvas.gameObject);
        }
        if (instance == this)
        {
            instance = null;
        }
    }

    public void UpdateSynthesisCostDisplay(Tower incoming, Vector2 screenPosition, Vector3 worldPosition)
    {
        Tower target = FindTowerAt(worldPosition, incoming);
        if (incoming == null || target == null || GameManager.instance == null ||
            incoming.tier < 1 || target.tier < 1 || incoming.tier + target.tier > 5)
        {
            HideSynthesisCostDisplay();
            return;
        }

        int fee = GameBalanceSettings.Instance.CalculateSynthesisFee(incoming.accumulatedValue, target.tier);
        bool canAfford = CanPurchaseAndSynthesize(incoming, target, 0);
        EnsureSynthesisCostDisplay();
        synthesisCostText.text = "+$" + fee;
        synthesisCostText.color = canAfford
            ? new Color(0.3f, 1f, 0.45f, 1f)
            : new Color(1f, 0.3f, 0.3f, 1f);

        Vector2 panelScreenPosition = screenPosition + new Vector2(18f, -22f);
        panelScreenPosition.x = Mathf.Clamp(panelScreenPosition.x, 0f, Screen.width - synthesisCostPanel.rect.width);
        panelScreenPosition.y = Mathf.Clamp(panelScreenPosition.y, synthesisCostPanel.rect.height, Screen.height);
        RectTransform canvasRect = synthesisCostCanvas.transform as RectTransform;
        if (canvasRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, panelScreenPosition, null, out Vector2 panelLocalPosition))
        {
            synthesisCostPanel.anchoredPosition = panelLocalPosition;
        }
        synthesisCostPanel.gameObject.SetActive(true);
    }

    public static void HideSynthesisCostDisplay()
    {
        if (instance != null && instance.synthesisCostPanel != null)
        {
            instance.synthesisCostPanel.gameObject.SetActive(false);
        }
    }

    private void EnsureSynthesisCostDisplay()
    {
        if (synthesisCostPanel != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("SynthesisCostCanvas");
        synthesisCostCanvas = canvasObject.AddComponent<Canvas>();
        synthesisCostCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        synthesisCostCanvas.sortingOrder = 10000;
        canvasObject.AddComponent<CanvasScaler>();

        GameObject panelObject = new GameObject("SynthesisCost");
        panelObject.transform.SetParent(canvasObject.transform, false);
        synthesisCostPanel = panelObject.AddComponent<RectTransform>();
        synthesisCostPanel.sizeDelta = new Vector2(160f, 44f);
        synthesisCostPanel.pivot = new Vector2(0f, 1f);

        GameObject textObject = new GameObject("Price");
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(5f, 2f);
        textRect.offsetMax = new Vector2(-5f, -2f);
        synthesisCostText = textObject.AddComponent<TextMeshProUGUI>();
        synthesisCostText.font = TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        synthesisCostText.fontSize = 24f;
        synthesisCostText.enableAutoSizing = true;
        synthesisCostText.fontSizeMin = 16f;
        synthesisCostText.fontSizeMax = 24f;
        synthesisCostText.alignment = TextAlignmentOptions.Midline;
        synthesisCostText.fontStyle = FontStyles.Bold;
        synthesisCostText.raycastTarget = false;
        Outline outline = textObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    public bool HandleDrop(Tower source, Vector2 screenPosition, Vector3 worldPosition)
    {
        HideSynthesisCostDisplay();
        if (!CanDrop(source, screenPosition, worldPosition))
        {
            return false;
        }

        if (IsOverRecycleBin(screenPosition))
        {
            return RecycleTower(source);
        }

        Tower target = FindTowerAt(worldPosition, source);
        if (target != null)
        {
            return Synthesize(source, target);
        }

        return true;
    }

    public bool CanDrop(Tower source, Vector2 screenPosition, Vector3 worldPosition)
    {
        if (source == null)
        {
            return false;
        }

        if (IsOverRecycleBin(screenPosition))
        {
            return GameManager.instance != null;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        Tower target = FindTowerAt(worldPosition, source);
        if (target != null)
        {
            return CanPurchaseAndSynthesize(source, target, 0);
        }

        int pathLayerMask = LayerMask.GetMask("Path");
        if (Physics2D.OverlapCircle(worldPosition, PlacementClearanceRadius, pathLayerMask) != null ||
            IsNearPathSegment(worldPosition, PlacementClearanceRadius))
        {
            return false;
        }

        int towerLayer = LayerMask.NameToLayer("Tower");
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(worldPosition, PlacementClearanceRadius);
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == null || IsRangeCollider(overlap))
            {
                continue;
            }

            Tower overlappedTower = overlap.GetComponent<Tower>();
            if (overlappedTower == source)
            {
                continue;
            }
            if (overlappedTower != null)
            {
                return false;
            }
            if ((towerLayer >= 0 && overlap.gameObject.layer == towerLayer) || overlap.CompareTag("Tower"))
            {
                return false;
            }
        }

        return true;
    }

    public Tower FindTowerAtPosition(Vector2 worldPosition, Tower ignoredSource = null)
    {
        return FindTowerAt(worldPosition, ignoredSource);
    }

    public bool CanPurchaseAndSynthesize(Tower incoming, Tower target, int purchaseCost)
    {
        if (incoming == null || target == null || incoming == target || GameManager.instance == null)
        {
            return false;
        }

        incoming.InitializeRecipeCounts();
        target.InitializeRecipeCounts();
        int resultingTier = incoming.tier + target.tier;
        if (resultingTier > 5)
        {
            return false;
        }

        int fee = GameBalanceSettings.Instance.CalculateSynthesisFee(incoming.accumulatedValue, target.tier);
        return GameManager.instance.IsMoneyInfinite || GameManager.instance.playerMoney >= purchaseCost + fee;
    }

    public bool PurchaseAndSynthesize(Tower incoming, Tower target, int purchaseCost)
    {
        if (!CanPurchaseAndSynthesize(incoming, target, purchaseCost))
        {
            return false;
        }

        if (!GameManager.instance.IsMoneyInfinite)
        {
            GameManager.instance.playerMoney -= purchaseCost;
        }
        if (Synthesize(incoming, target))
        {
            return true;
        }

        if (!GameManager.instance.IsMoneyInfinite)
        {
            GameManager.instance.playerMoney += purchaseCost;
        }
        GameManager.instance.UpdateMoneyUI();
        return false;
    }

    private Tower FindTowerAt(Vector2 worldPosition, Tower source)
    {
        Collider2D[] overlaps = Physics2D.OverlapCircleAll(worldPosition, PlacementClearanceRadius);
        Tower closestTower = null;
        float closestDistance = float.MaxValue;
        foreach (Collider2D overlap in overlaps)
        {
            if (overlap == null || IsRangeCollider(overlap))
            {
                continue;
            }

            Tower candidate = overlap.GetComponent<Tower>();
            if (candidate == null || candidate == source)
            {
                continue;
            }

            float distance = Vector2.Distance(worldPosition, candidate.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestTower = candidate;
            }
        }
        return closestTower;
    }

    private static bool IsRangeCollider(Collider2D collider)
    {
        return collider.GetComponent<TowerRange>() != null || collider.gameObject.name == "Range" ||
            collider.gameObject.name == "RangeVisualizer";
    }

    private bool IsNearPathSegment(Vector2 position, float clearance)
    {
        Transform[] points = Path.path != null ? Path.path.point : null;
        if (points == null)
        {
            return false;
        }

        for (int index = 0; index < points.Length - 1; index++)
        {
            if (points[index] == null || points[index + 1] == null)
            {
                continue;
            }

            Vector2 start = points[index].position;
            Vector2 segment = (Vector2)points[index + 1].position - start;
            float segmentLengthSquared = segment.sqrMagnitude;
            float progress = segmentLengthSquared > 0f
                ? Mathf.Clamp01(Vector2.Dot(position - start, segment) / segmentLengthSquared)
                : 0f;
            if (Vector2.Distance(position, start + segment * progress) <= clearance)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsOverRecycleBin(Vector2 screenPosition)
    {
        RectTransform[] rectTransforms = Resources.FindObjectsOfTypeAll<RectTransform>();
        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (rectTransform == null || rectTransform.name != "RecycleBin" ||
                !rectTransform.gameObject.activeInHierarchy || !rectTransform.gameObject.scene.IsValid())
            {
                continue;
            }

            Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition, eventCamera))
            {
                return true;
            }
        }
        return false;
    }

    private bool RecycleTower(Tower tower)
    {
        if (GameManager.instance == null)
        {
            return false;
        }

        int refund = GameBalanceSettings.Instance.CalculateRecycleRefund(tower.accumulatedValue);
        GameManager.instance.playerMoney += refund;
        GameManager.instance.UpdateMoneyUI();
        PlayRecycleSound();
        Destroy(tower.gameObject);
        return true;
    }

    private bool Synthesize(Tower source, Tower target)
    {
        int resultingTier = source.tier + target.tier;
        if (source == target || source.tier < 1 || target.tier < 1 || resultingTier > 5 || GameManager.instance == null)
        {
            return false;
        }

        int fee = GameBalanceSettings.Instance.CalculateSynthesisFee(source.accumulatedValue, target.tier);
        if (!GameManager.instance.IsMoneyInfinite && GameManager.instance.playerMoney < fee)
        {
            return false;
        }

        if (!GameManager.instance.IsMoneyInfinite)
        {
            GameManager.instance.playerMoney -= fee;
        }
        target.MergeRecipeFrom(source);
        GameManager.instance.UpdateMoneyUI();

        if (resultingTier == 5)
        {
            GameBalanceSettings.SpecialTowerEvolutionStats definition =
                GameBalanceSettings.Instance.GetSpecialTowerForRecipe(target.soldierCount, target.assaultCount, target.sniperCount);
            GameObject specialPrefab = definition != null ? FindSpecialTowerPrefab(definition.towerType) : null;
            if (specialPrefab != null)
            {
                GameObject evolvedObject = Instantiate(specialPrefab, target.transform.position, target.transform.rotation);
                Tower evolvedTower = evolvedObject.GetComponent<Tower>();
                if (evolvedTower != null)
                {
                    evolvedTower.specialTowerType = definition.towerType;
                    evolvedTower.soldierCount = target.soldierCount;
                    evolvedTower.assaultCount = target.assaultCount;
                    evolvedTower.sniperCount = target.sniperCount;
                    evolvedTower.componentCount = target.componentCount;
                    evolvedTower.tier = target.tier;
                    evolvedTower.accumulatedValue = target.accumulatedValue;
                    evolvedTower.cost = target.accumulatedValue;
                    evolvedTower.ApplySynthesisTier(target.tier);
                    Destroy(target.gameObject);
                }
                else
                {
                    Destroy(evolvedObject);
                }
            }
        }

        Destroy(source.gameObject);
        return true;
    }

    private GameObject FindSpecialTowerPrefab(SpecialTowerType towerType)
    {
        foreach (SpecialTowerPrefabEntry entry in specialTowerPrefabs)
        {
            if (entry != null && entry.towerType == towerType && entry.prefab != null)
            {
                return entry.prefab;
            }
        }
        return GameBalanceSettings.Instance.GetSpecialTowerPrefab(towerType);
    }

    private void PlayRecycleSound()
    {
        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
                uiAudioSource.playOnAwake = false;
                uiAudioSource.spatialBlend = 0f;
            }
        }

        AudioClip clip = recycleSound != null ? recycleSound : GetFallbackRecycleSound();
        if (clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }

    private static AudioClip GetFallbackRecycleSound()
    {
        if (fallbackRecycleSound != null)
        {
            return fallbackRecycleSound;
        }

        const int sampleRate = 22050;
        const float duration = 0.09f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;
        for (int index = 0; index < sampleCount; index++)
        {
            float progress = (float)index / sampleCount;
            float frequency = Mathf.Lerp(980f, 640f, progress);
            phase += 2f * Mathf.PI * frequency / sampleRate;
            samples[index] = Mathf.Sin(phase) * (1f - progress) * 0.12f;
        }

        fallbackRecycleSound = AudioClip.Create("TowerRecycleUI", sampleCount, 1, sampleRate, false);
        fallbackRecycleSound.SetData(samples, 0);
        return fallbackRecycleSound;
    }
}