using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuAnimator : MonoBehaviour
{
    [System.Serializable]
    public class DisplayCharacter
    {
        public string name;
        public Sprite normalSprite;
        public Sprite fireSprite;
        public float fireRate;
        public bool isTower;
        [Tooltip("Relative size multiplier for this character in the menu. 0 = auto based on in-game size.")]
        public float scaleMultiplier = 0f;
    }

    public Image displayImage;
    public DisplayCharacter[] characters;
    public float cycleDuration = 4.0f;
    public float spinDuration = 1.0f;
    public float swaySpeed = 2.0f;
    public float swayAmplitude = 8.0f;
    public float towerVerticalOffset = 100f;
    public float simonVerticalOffset = 100f;
    [Tooltip("UI pixels per world unit for scaling sprites in the main menu to match in-game sizes.")]
    public float uiPixelsPerUnit = 180f;

    public int currentIndex = 0;
    private float cycleTimer = 0f;
    private float fireTimer = 0f;
    private bool isTransitioning = false;
    private RectTransform displayRect;

    private void Start()
    {
        if (characters != null && characters.Length > 0 && displayImage != null)
        {
            displayImage.sprite = characters[currentIndex].normalSprite;
            displayImage.preserveAspect = true;

            displayRect = displayImage.rectTransform;
            ApplyCurrentCharacterLayout();
        }
    }

    private void Update()
    {
        if (characters == null || characters.Length == 0 || displayImage == null || displayRect == null) return;

        if (!isTransitioning)
        {
            ApplyCurrentCharacterLayout();

            // Sway left & right
            float swayAngle = swayAmplitude * Mathf.Sin(Time.time * swaySpeed);
            displayRect.localRotation = Quaternion.Euler(0, 0, swayAngle);

            // Handle occasional firing for towers
            var character = characters[currentIndex];
            if (character.isTower)
            {
                fireTimer += Time.deltaTime;
                if (fireTimer >= character.fireRate + Random.Range(0.5f, 2.0f))
                {
                    fireTimer = 0f;
                    StartCoroutine(TriggerFireFlash(character));
                }
            }

            // Cycle timer
            cycleTimer += Time.deltaTime;
            if (cycleTimer >= cycleDuration)
            {
                cycleTimer = 0f;
                StartCoroutine(SpinTransitionRoutine());
            }
        }
    }

    private IEnumerator TriggerFireFlash(DisplayCharacter character)
    {
        if (character.fireSprite != null)
        {
            displayImage.sprite = character.fireSprite;
            ApplyCurrentCharacterLayout();
            yield return new WaitForSeconds(0.15f);
            if (!isTransitioning && currentIndex < characters.Length && characters[currentIndex] == character)
            {
                displayImage.sprite = character.normalSprite;
                ApplyCurrentCharacterLayout();
            }
        }
    }

    private IEnumerator SpinTransitionRoutine()
    {
        isTransitioning = true;
        float elapsed = 0f;

        // Get starting rotation from current sway rotation
        float startAngle = transform.localRotation.eulerAngles.z;
        if (startAngle > 180f) startAngle -= 360f; // normalize to -180 to 180

        bool spriteSwapped = false;
        int nextIndex = (currentIndex + 1) % characters.Length;

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spinDuration;

            // Spin clockwise (decreasing angle)
            // Spin full 360 degrees: from startAngle to (startAngle - 360)
            float targetAngle = startAngle - (t * 360f);
            displayRect.localRotation = Quaternion.Euler(0, 0, targetAngle);

            // Swap sprite at exactly the mid-point (180 degrees spin, when "back-facing")
            if (t >= 0.5f && !spriteSwapped)
            {
                spriteSwapped = true;
                currentIndex = nextIndex;
                displayImage.sprite = characters[currentIndex].normalSprite;
                ApplyCurrentCharacterLayout();
                fireTimer = 0f;
            }

            yield return null;
        }

        // Snap precisely to 0 after finishing spin
        displayRect.localRotation = Quaternion.identity;
        isTransitioning = false;
    }

    public void ApplyCurrentCharacterLayout()
    {
        if (displayImage != null && displayRect == null)
        {
            displayRect = displayImage.rectTransform;
        }

        if (displayRect == null || characters == null || characters.Length == 0) return;

        var character = characters[currentIndex];
        float verticalOffset = character.isTower ? towerVerticalOffset : simonVerticalOffset;

        // Align the RectTransform pivot to the sprite's pivot (so rotation uses the Sprite Editor pivot)
        Sprite s = displayImage != null ? displayImage.sprite : null;
        if (s != null)
        {
            Rect spriteRect = s.rect;
            if (spriteRect.width > 0f && spriteRect.height > 0f)
            {
                Vector2 spritePivotNorm = new Vector2(s.pivot.x / spriteRect.width, s.pivot.y / spriteRect.height);
                SetRectTransformPivot(displayRect, spritePivotNorm);
            }
        }

        float relativeScale = GetCharacterScale(character);
        ApplyDisplaySize(character, relativeScale);

        Vector2 pos = displayRect.anchoredPosition;
        pos.y = verticalOffset;
        displayRect.anchoredPosition = pos;
        displayRect.localRotation = Quaternion.identity;
    }

    private void ApplyDisplaySize(DisplayCharacter character, float relativeScale)
    {
        if (character == null || displayImage == null) return;

        Sprite s = displayImage.sprite != null ? displayImage.sprite : character.normalSprite;
        if (s == null) return;

        // In-game scale factor used in game prefabs:
        // Enemy prefab transform scale = 0.35
        // Tower prefabs transform scale = 0.50
        float inGameScale = character.isTower ? 0.50f : 0.35f;

        // Calculate exact in-game world dimensions (in Unity world units):
        float worldWidth = (s.rect.width / Mathf.Max(s.pixelsPerUnit, 1f)) * inGameScale;
        float worldHeight = (s.rect.height / Mathf.Max(s.pixelsPerUnit, 1f)) * inGameScale;

        // Convert world units to UI pixel dimensions using the UI scale factor
        float mult = relativeScale > 0f ? relativeScale : 1f;
        float w = worldWidth * uiPixelsPerUnit * mult;
        float h = worldHeight * uiPixelsPerUnit * mult;

        displayRect.sizeDelta = new Vector2(w, h);
    }

    private float GetCharacterScale(DisplayCharacter character)
    {
        if (character == null) return 1f;

        if (character.scaleMultiplier > 0f)
        {
            return character.scaleMultiplier;
        }

        return 1.0f;
    }

    // Move the pivot of a RectTransform while compensating anchoredPosition so the visual element doesn't jump.
    private void SetRectTransformPivot(RectTransform rt, Vector2 newPivot)
    {
        if (rt == null) return;

        Vector2 size = rt.rect.size;
        Vector2 deltaPivot = newPivot - rt.pivot;
        Vector2 deltaPosition = new Vector2(deltaPivot.x * size.x, deltaPivot.y * size.y);

        rt.pivot = newPivot;
        rt.anchoredPosition += deltaPosition;
    }
}
