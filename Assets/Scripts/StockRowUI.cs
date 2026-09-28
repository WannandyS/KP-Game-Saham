using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StockRowUI : MonoBehaviour
{
    [Header("UI Text Fields")]
    public TMP_Text stockText;
    public TMP_Text priceText;
    public TMP_Text changeText;
    public TMP_Text volumeText;
    public TMP_Text valueText;
    public TMP_Text marketCapText;

    [Header("Action Controls")]
    public Button detailsButton;

    public void SetupRow(StockRowData data, Action<StockRowData> onDetailsClicked)
    {
        stockText.text = data.stockSymbol;
        priceText.text = data.price.ToString("N0");
        
        // 24H Change % formatting with color indicators
        string sign = data.changePercent >= 0 ? "+" : "";
        changeText.text = $"{sign}{data.changePercent:F2}%";
        changeText.color = data.changePercent >= 0 ? Color.green : Color.red;

        volumeText.text = FormatLargeNumber(data.volume, false);
        valueText.text = FormatLargeNumber(data.value, true);
        marketCapText.text = FormatLargeNumber(data.marketCap, true);

        detailsButton.onClick.RemoveAllListeners();
        detailsButton.onClick.AddListener(() => onDetailsClicked?.Invoke(data));
    }

    /// <summary>
    /// Formats large currency/volume values into readable Millions (M) or Billions (B) suffixes.
    /// </summary>
    private string FormatLargeNumber(float number, bool appendM)
    {
        if (number >= 1_000_000_000_000f)
            return (number / 1_000_000_000_000f).ToString("F2") + " T";
        if (number >= 1_000_000_000f)
            return (number / 1_000_000_000f).ToString("F2") + " B";
        if (number >= 1_000_000f)
            return (number / 1_000_000f).ToString("F2") + " M";

        return number.ToString("N0") + (appendM ? " M" : "");
    }
}