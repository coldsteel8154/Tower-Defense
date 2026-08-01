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
    }

    public Image displayImage;
    public DisplayCharacter[] characters;
    public float cycleDuration = 4.0f;
    public float spinDuration = 1.0f;
    public float swaySpeed = 2.0f;
    public float swayAmplitude = 8.0f;

    private int currentIndex = 0;
    private float cycleTimer = 0f;
    private float fireTimer = 0f;
    private bool isTransitioning = false;

    private void Start()
    {
        if (characters != null && characters.Length > 0 && displayImage != null)
        {
            displayImage.sprite = characters[currentIndex].normalSprite;
            displayImage.preserveAspect = true;
        }
    }

    private void Update()
    {
        if (characters == null || characters.Length == 0 || displayImage == null) return;

        if (!isTransitioning)
        {
            // Sway left & right
            float swayAngle = swayAmplitude * Mathf.Sin(Time.time * swaySpeed);
            transform.localRotation = Quaternion.Euler(0, 0, swayAngle);

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
            yield return new WaitForSeconds(0.15f);
            if (!isTransitioning && currentIndex < characters.Length && characters[currentIndex] == character)
            {
                displayImage.sprite = character.normalSprite;
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
            transform.localRotation = Quaternion.Euler(0, 0, targetAngle);

            // Swap sprite at exactly the mid-point (180 degrees spin, when "back-facing")
            if (t >= 0.5f && !spriteSwapped)
            {
                spriteSwapped = true;
                currentIndex = nextIndex;
                displayImage.sprite = characters[currentIndex].normalSprite;
                fireTimer = 0f;
            }

            yield return null;
        }

        // Snap precisely to 0 after finishing spin
        transform.localRotation = Quaternion.identity;
        isTransitioning = false;
    }
}
