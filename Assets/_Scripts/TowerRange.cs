using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerRange : MonoBehaviour
{
    [SerializeField] private Tower Tower;
    private List<GameObject> targets = new List<GameObject>();
    private readonly List<FilledIndicator> indicators = new List<FilledIndicator>();
    private FilledIndicator primaryRangeIndicator;
    private FilledIndicator ironVanguardConeIndicator;
    private bool rangeVisible;
    private bool ironVanguardSkillConeVisible;
    private bool indicatorsInitialized;
    private static Material sharedLineMaterial;

    private sealed class FilledIndicator
    {
        public Mesh mesh;
        public MeshRenderer renderer;
        public Color baseColor;
        public Color previewColor;
        public bool hasPreviewColor;
    }

    public void Initialize(Tower tower)
    {
        Tower = tower;
        InitializeIndicators();
    }

    void Start()
    {
        UpdateRange();
    }

    // Update is called once per frame
    void Update()
    {
        if (Tower == null) return;
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
        CircleCollider2D rangeCollider = GetComponent<CircleCollider2D>();
        if (rangeCollider != null)
        {
            rangeCollider.radius = 0.5f;
        }

        if (Tower == null)
        {
            return;
        }

        float diameter = Tower.range * 2f;
        transform.localScale = new Vector3(diameter, diameter, transform.localScale.z);
        if (primaryRangeIndicator != null)
        {
            SetCircle(primaryRangeIndicator, Tower.range, new Color(1f, 0.22f, 0.2f, 0.14f));
        }
    }

    public void SetVisible(bool visible)
    {
        rangeVisible = visible;
        foreach (FilledIndicator indicator in indicators)
        {
            if (indicator != null && indicator.renderer != null)
            {
                indicator.renderer.enabled = visible ||
                    (indicator == ironVanguardConeIndicator && ironVanguardSkillConeVisible);
            }
        }
    }

    public void SetIronVanguardSkillConeVisible(bool visible)
    {
        ironVanguardSkillConeVisible = visible;
        if (ironVanguardConeIndicator != null && ironVanguardConeIndicator.renderer != null)
        {
            ironVanguardConeIndicator.renderer.enabled = rangeVisible || visible;
        }
    }

    public void SetPreviewColor(Color color)
    {
        foreach (FilledIndicator indicator in indicators)
        {
            if (indicator == null || indicator.mesh == null)
            {
                continue;
            }

            Color appliedColor = new Color(color.r, color.g, color.b, indicator.baseColor.a);
            if (indicator.hasPreviewColor && indicator.previewColor == appliedColor)
            {
                continue;
            }

            Color[] colors = new Color[indicator.mesh.vertexCount];
            for (int index = 0; index < colors.Length; index++)
            {
                colors[index] = appliedColor;
            }
            indicator.mesh.colors = colors;
            indicator.previewColor = appliedColor;
            indicator.hasPreviewColor = true;
        }
    }

    public void ClearPreviewColor()
    {
        foreach (FilledIndicator indicator in indicators)
        {
            if (indicator == null || indicator.mesh == null || !indicator.hasPreviewColor)
            {
                continue;
            }

            Color[] colors = new Color[indicator.mesh.vertexCount];
            for (int index = 0; index < colors.Length; index++)
            {
                colors[index] = indicator.baseColor;
            }
            indicator.mesh.colors = colors;
            indicator.hasPreviewColor = false;
        }
    }

    private void InitializeIndicators()
    {
        if (indicatorsInitialized || Tower == null)
        {
            return;
        }

        indicatorsInitialized = true;
        primaryRangeIndicator = CreateIndicator("Range_Primary");
        SetCircle(primaryRangeIndicator, Tower.range, new Color(1f, 0.22f, 0.2f, 0.14f));
        if (Tower.specialTowerType == SpecialTowerType.None)
        {
            return;
        }

        GameBalanceSettings.SpecialTowerEvolutionStats definition =
            GameBalanceSettings.Instance.GetSpecialTowerEvolutionStats(Tower.specialTowerType);
        switch (Tower.specialTowerType)
        {
            case SpecialTowerType.Veteran:
                AddCircle("Range_Aura", definition.abilityRadius, new Color(0.2f, 0.65f, 1f, 0.12f));
                AddCircle("Range_Melee", definition.secondaryRange, new Color(1f, 0.82f, 0.15f, 0.14f));
                break;
            case SpecialTowerType.Juggernaut:
                AddCone("Range_Frenzy", definition.secondaryRange, definition.coneAngle,
                    new Color(1f, 0.82f, 0.15f, 0.14f));
                break;
            case SpecialTowerType.DrillSergeant:
            case SpecialTowerType.TacticalCaptain:
                AddCircle("Range_Aura", definition.abilityRadius, new Color(0.2f, 0.65f, 1f, 0.12f));
                if (Tower.specialTowerType == SpecialTowerType.TacticalCaptain)
                {
                    AddCircle("Range_Mortar", definition.secondaryRange, new Color(1f, 0.82f, 0.15f, 0.14f));
                }
                break;
            case SpecialTowerType.GunnerCommander:
            case SpecialTowerType.ShockTrooper:
            case SpecialTowerType.LinebreakerScout:
                AddCircle("Range_Special", definition.abilityRadius, new Color(1f, 0.82f, 0.15f, 0.14f));
                break;
            case SpecialTowerType.IronVanguard:
                AddCircle("Range_Special", definition.abilityRadius, new Color(1f, 0.82f, 0.15f, 0.14f));
                ironVanguardConeIndicator = AddCone("Range_IronVanguardSkill", definition.secondaryRange,
                    definition.coneAngle, new Color(1f, 0.82f, 0.15f, 0.14f));
                break;
            case SpecialTowerType.GuerillaEliminator:
                AddCircle("Range_MineBlast", definition.abilityRadius, new Color(1f, 0.82f, 0.15f, 0.14f));
                AddCircle("Range_LoneWolf", definition.secondaryRange, new Color(1f, 0.82f, 0.15f, 0.14f));
                break;
            case SpecialTowerType.Pathfinder:
                AddCircle("Range_Melee", definition.secondaryRange, new Color(1f, 0.82f, 0.15f, 0.14f));
                break;
            case SpecialTowerType.BarrettOverload:
                AddCircle("Range_Blast", definition.abilityRadius, new Color(1f, 0.82f, 0.15f, 0.14f));
                break;
            case SpecialTowerType.TrenchSweeper:
                AddCone("Range_Scatter", Tower.GetConfiguredWorldRange(definition.level5Stats.range), definition.coneAngle,
                    new Color(1f, 0.82f, 0.15f, 0.14f));
                break;
            case SpecialTowerType.RailgunOperator:
                AddLane("Range_Railgun", Tower.GetConfiguredWorldRange(definition.level5Stats.range), definition.secondaryRange,
                    new Color(1f, 0.82f, 0.15f, 0.14f));
                break;
            case SpecialTowerType.HawkeyeOperator:
                AddCircle("Range_Uplink", definition.abilityRadius, new Color(0.2f, 0.65f, 1f, 0.12f));
                break;
        }
    }

    private void AddCircle(string name, float radius, Color color)
    {
        if (radius <= 0f)
        {
            return;
        }
        FilledIndicator indicator = CreateIndicator(name);
        SetCircle(indicator, radius, color);
    }

    private FilledIndicator AddCone(string name, float radius, float angle, Color color)
    {
        if (radius <= 0f || angle <= 0f)
        {
            return null;
        }
        FilledIndicator indicator = CreateIndicator(name);
        const int segments = 24;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;
        for (int index = 0; index <= segments; index++)
        {
            float degrees = Mathf.Lerp(-angle * 0.5f, angle * 0.5f, index / (float)segments);
            Vector2 direction = Quaternion.Euler(0f, 0f, degrees) * Vector2.down;
            vertices[index + 1] = direction * radius;
            if (index < segments)
            {
                int triangleIndex = index * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = index + 2;
                triangles[triangleIndex + 2] = index + 1;
            }
        }
        SetMesh(indicator, vertices, triangles, color);
        return indicator;
    }

    private void AddLane(string name, float length, float width, Color color)
    {
        if (length <= 0f || width <= 0f)
        {
            return;
        }

        FilledIndicator indicator = CreateIndicator(name);
        float halfWidth = width * 0.5f;
        Vector3[] vertices =
        {
            new Vector3(-halfWidth, 0f, 0f),
            new Vector3(halfWidth, 0f, 0f),
            new Vector3(halfWidth, -length, 0f),
            new Vector3(-halfWidth, -length, 0f)
        };
        SetMesh(indicator, vertices, new[] { 0, 1, 2, 0, 2, 3 }, color);
    }

    private FilledIndicator CreateIndicator(string name)
    {
        GameObject indicatorObject = new GameObject(name);
        indicatorObject.transform.SetParent(Tower.transform, false);
        MeshFilter meshFilter = indicatorObject.AddComponent<MeshFilter>();
        MeshRenderer indicatorRenderer = indicatorObject.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh { name = name + "Mesh" };
        meshFilter.sharedMesh = mesh;
        indicatorRenderer.sortingLayerName = "RangeIndicators";
        indicatorRenderer.sortingOrder = 0;
        if (sharedLineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                sharedLineMaterial = new Material(shader);
            }
        }
        if (sharedLineMaterial != null)
        {
            indicatorRenderer.sharedMaterial = sharedLineMaterial;
        }
        indicatorRenderer.enabled = false;
        FilledIndicator indicator = new FilledIndicator { mesh = mesh, renderer = indicatorRenderer };
        indicators.Add(indicator);
        return indicator;
    }

    private void SetCircle(FilledIndicator indicator, float radius, Color color)
    {
        const int segments = 64;
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;
        for (int index = 0; index < segments; index++)
        {
            float angle = index * Mathf.PI * 2f / segments;
            vertices[index + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            int triangleIndex = index * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = (index + 1) % segments + 1;
            triangles[triangleIndex + 2] = index + 1;
        }
        SetMesh(indicator, vertices, triangles, color);
    }

    private static void SetMesh(FilledIndicator indicator, Vector3[] vertices, int[] triangles, Color color)
    {
        indicator.baseColor = color;
        Color[] colors = new Color[vertices.Length];
        Vector3[] normals = new Vector3[vertices.Length];
        for (int index = 0; index < vertices.Length; index++)
        {
            colors[index] = color;
            normals[index] = Vector3.back;
        }

        indicator.mesh.Clear();
        indicator.mesh.vertices = vertices;
        indicator.mesh.triangles = triangles;
        indicator.mesh.colors = colors;
        indicator.mesh.normals = normals;
        indicator.mesh.RecalculateBounds();
    }
}
