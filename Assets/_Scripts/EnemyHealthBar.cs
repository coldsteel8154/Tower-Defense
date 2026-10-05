using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyHealthBar : MonoBehaviour
{
    private Enemy enemy;
    private GameObject canvasGo;
    private Image fillImage;
    private TMP_Text healthText;
    private int lastHealth = -1;

    private void Start()
    {
        enemy = GetComponent<Enemy>();
        if (enemy == null) return;

        // Create Canvas
        canvasGo = new GameObject("HealthBar_Canvas");
        canvasGo.transform.SetParent(transform, false);
        canvasGo.transform.localPosition = new Vector3(0, 1.2f, 0); // Position above the sprite head

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(2.0f, 0.35f);
        canvasGo.transform.localScale = Vector3.one;

        // 1. Background image
        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        bgImg.raycastTarget = false;

        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 2. Fill Area
        GameObject fillContainer = new GameObject("FillArea");
        fillContainer.transform.SetParent(canvasGo.transform, false);
        RectTransform fillContainerRect = fillContainer.AddComponent<RectTransform>();
        fillContainerRect.anchorMin = new Vector2(0.3f, 0.15f); // Leave 30% on the left for the health number
        fillContainerRect.anchorMax = new Vector2(0.95f, 0.85f);
        fillContainerRect.sizeDelta = Vector2.zero;

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillContainer.transform, false);
        fillImage = fillGo.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.9f, 0.2f, 1.0f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.raycastTarget = false;

        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // 3. Health Number on the left
        GameObject textGo = new GameObject("HealthNumber");
        textGo.transform.SetParent(canvasGo.transform, false);
        healthText = textGo.AddComponent<TextMeshProUGUI>();
        healthText.fontSize = 0.25f; // Perfectly proportioned to canvas height of 0.35f!
        healthText.alignment = TextAlignmentOptions.Center;
        healthText.color = Color.white;
        healthText.raycastTarget = false;

        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.02f, 0f);
        textRect.anchorMax = new Vector2(0.28f, 1f);
        textRect.sizeDelta = Vector2.zero;

        // 4. Enemy Tag Container (above health fill)
        int uiLayer = LayerMask.NameToLayer("UI");

        GameObject tagContainerGo = new GameObject("TagContainer");
        tagContainerGo.transform.SetParent(canvasGo.transform, false);
        if (uiLayer >= 0) tagContainerGo.layer = uiLayer;

        RectTransform tcRect = tagContainerGo.AddComponent<RectTransform>();
        tcRect.anchorMin = new Vector2(0f, 1f);
        tcRect.anchorMax = new Vector2(1f, 1f);
        tcRect.pivot = new Vector2(0f, 0f);
        tcRect.anchoredPosition = new Vector2(0f, 0.05f); // Positioned above the health bar
        tcRect.sizeDelta = new Vector2(0f, 0.3f);

        HorizontalLayoutGroup hlg = tagContainerGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.LowerLeft;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.spacing = 0.02f;

        // Child Template: TagItemTemplate (Image 24x24 + TextMeshPro-UGUI superscript "^N")
        GameObject templateGo = new GameObject("TagItemTemplate");
        templateGo.transform.SetParent(tagContainerGo.transform, false);
        if (uiLayer >= 0) templateGo.layer = uiLayer;

        RectTransform tplRect = templateGo.AddComponent<RectTransform>();
        tplRect.sizeDelta = new Vector2(0.34f, 0.34f);

        Image img = templateGo.AddComponent<Image>();
        img.raycastTarget = false;

        GameObject superTextGo = new GameObject("SuperscriptText");
        superTextGo.transform.SetParent(templateGo.transform, false);
        if (uiLayer >= 0) superTextGo.layer = uiLayer;

        RectTransform stRect = superTextGo.AddComponent<RectTransform>();
        stRect.anchorMin = new Vector2(1f, 1f);
        stRect.anchorMax = new Vector2(1f, 1f);
        stRect.pivot = new Vector2(0f, 1f); // Top-right of tag icon
        stRect.anchoredPosition = new Vector2(0f, 0f);
        stRect.sizeDelta = new Vector2(0.2f, 0.2f);

        TextMeshProUGUI tmp = superTextGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "^N";
        tmp.fontSize = 0.18f;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        // Deactivate template as specified
        templateGo.SetActive(false);
    }

    private void Update()
    {
        if (enemy == null || canvasGo == null) return;

        // Crucial: Reset canvas rotation so health bar stays horizontal when the enemy rotates!
        canvasGo.transform.rotation = Quaternion.identity;
        // Keep it offset above head
        canvasGo.transform.position = transform.position + new Vector3(0, 1.2f, 0);

        int currentHealth = enemy.health;
        int maxHealth = enemy.maxHealth;

        if (currentHealth < 0) currentHealth = 0;

        if (currentHealth != lastHealth)
        {
            lastHealth = currentHealth;
            
            // Update Text
            if (healthText != null)
            {
                healthText.text = currentHealth.ToString();
            }

            // Update Fill Amount
            if (fillImage != null && maxHealth > 0)
            {
                fillImage.fillAmount = (float)currentHealth / maxHealth;
                
                // Color grading: Green -> Yellow -> Red
                float pct = fillImage.fillAmount;
                if (pct > 0.5f)
                {
                    fillImage.color = Color.Lerp(Color.yellow, new Color(0.2f, 0.9f, 0.2f), (pct - 0.5f) * 2f);
                }
                else
                {
                    fillImage.color = Color.Lerp(Color.red, Color.yellow, pct * 2f);
                }
            }
        }
    }
}
