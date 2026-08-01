using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    private TMP_Text textMesh;
    private float lifetime = 1.0f;
    private float elapsed = 0f;
    private Vector3 velocity;

    public void Setup(string text, Color color, float speed = 1.5f)
    {
        textMesh = GetComponentInChildren<TMP_Text>();
        if (textMesh != null)
        {
            textMesh.text = text;
            textMesh.color = color;
        }
        velocity = new Vector3(Random.Range(-0.5f, 0.5f), speed, 0);
    }

    private void Update()
    {
        transform.position += velocity * Time.deltaTime;
        elapsed += Time.deltaTime;
        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
        else
        {
            if (textMesh != null)
            {
                Color c = textMesh.color;
                c.a = Mathf.Lerp(1f, 0f, elapsed / lifetime);
                textMesh.color = c;
            }
        }
    }

    public static void Create(Vector3 position, string text, Color color)
    {
        GameObject go = new GameObject("FloatingText_Canvas");
        go.transform.position = position + new Vector3(0, 0.5f, 0);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Scale Canvas down so we can use reasonable font sizes
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(5, 2);
        go.transform.localScale = new Vector3(0.12f, 0.12f, 1f);

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);

        TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        FloatingText ft = go.AddComponent<FloatingText>();
        ft.Setup(text, color);
    }
}
