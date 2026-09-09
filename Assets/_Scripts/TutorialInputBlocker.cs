using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialInputBlocker : MonoBehaviour, ICanvasRaycastFilter
{
    private RectTransform allowedTarget;
    private RectTransform allowedEndTutorialButton;

    public void SetAllowedTarget(RectTransform target, RectTransform endTutorialButton)
    {
        allowedTarget = target;
        allowedEndTutorialButton = endTutorialButton;
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // Keep the tutorial overlay completely transparent to regular UI events.
        // Otherwise it steals hover/click events from the active menu buttons and
        // breaks normal button animations and click behavior.
        return true;
    }

    private bool IsInside(RectTransform rect, Vector2 screenPoint, Camera eventCamera)
    {
        return rect != null && rect.gameObject != null && rect.gameObject.activeInHierarchy &&
               RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, eventCamera);
    }
}