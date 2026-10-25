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

    private bool isHovering = false;

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
        isHovering = true;
        if (infoText != null)
        {
            infoText.text = text;
        }
    }

    public void ClearTooltip()
    {
        isHovering = false;
        if (infoText != null)
        {
            infoText.text = defaultText;
        }
    }

    // New method to update the default day info when simulation day changes
    public void SetDefaultText(string newDefaultText)
    {
        defaultText = newDefaultText;
        if (!isHovering)
        {
            ClearTooltip();
        }
    }
}