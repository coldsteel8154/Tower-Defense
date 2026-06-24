using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonAnimate : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float pressScale = 0.9f;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float animationSpeed = 15f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    private void OnDisable()
    {
        transform.localScale = originalScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * pressScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}