using UnityEngine;
using UnityEngine.EventSystems;

public class ChartHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string infoText;
    private RectTransform targetRect;

    private void Awake()
    {
        targetRect = GetComponent<RectTransform>();
    }

    public void Init(string text)
    {
        infoText = text;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ChartTooltip.Instance != null)
        {
            ChartTooltip.Instance.ShowTooltip(infoText, targetRect);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ChartTooltip.Instance != null)
        {
            ChartTooltip.Instance.HideTooltip();
        }
    }
}