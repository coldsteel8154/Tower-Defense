using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TowerBuyButtonUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text costText;
    public Button button;

    public void Setup(Sprite icon, int cost, System.Action onClickAction)
    {
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
        }

        if (costText != null)
        {
            costText.text = "$" + cost;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClickAction?.Invoke());
        }
    }
}