using UnityEngine;
using UnityEngine.EventSystems;

public class ChartHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string infoText;

    public void Init(string text)
    {
        infoText = text;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ChartTooltip.Instance != null)
        {
            ChartTooltip.Instance.ShowTooltip(infoText);
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
