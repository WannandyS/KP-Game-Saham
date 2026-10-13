using UnityEngine;
using TMPro;

public class ChartTooltip : MonoBehaviour
{
    public static ChartTooltip Instance { get; private set; }

    [Header("UI Text Reference")]
    [Tooltip("Assign your pre-setup TextMeshProUGUI component here.")]
    public TextMeshProUGUI infoText;

    [Header("Default Text")]
    [Tooltip("Text to display when no candle/bar is being hovered.")]
    public string defaultText = "Hover over a candle for details.";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        ClearTooltip();
    }

    public void ShowTooltip(string text)
    {
        if (infoText != null)
        {
            infoText.text = text;
        }
    }

    public void ClearTooltip()
    {
        if (infoText != null)
        {
            infoText.text = defaultText;
        }
    }
}