using UnityEngine;
using TMPro;

public class PortfolioHoldingRow : MonoBehaviour
{
    [Header("UI Text References (In Horizontal Order)")]
    public TMP_Text tickerText;
    public TMP_Text buyPriceText;
    public TMP_Text totalInvestedText;
    public TMP_Text change24hText;
    public TMP_Text shareAmountText;
    public TMP_Text totalValueText;
    public TMP_Text profitLossText;

    public void SetupRow(string ticker, float buyPrice, float invested, float change24h, int shares, float currentPrice, float totalValue, float profitLoss)
    {
        if (tickerText != null) tickerText.text = ticker;
        if (buyPriceText != null) buyPriceText.text = $"Rp{buyPrice:N2}";
        if (totalInvestedText != null) totalInvestedText.text = $"Rp{invested:N2}";
        
        if (change24hText != null)
        {
            string sign = change24h >= 0 ? "+" : "";
            string color = change24h >= 0 ? "green" : "red";
            change24hText.text = $"<color={color}>{sign}{change24h:N2}%</color>";
        }

        if (shareAmountText != null) shareAmountText.text = shares.ToString("N0");
        if (totalValueText != null) totalValueText.text = $"Rp{totalValue:N2}";

        if (profitLossText != null)
        {
            string sign = profitLoss >= 0 ? "+" : "";
            string color = profitLoss >= 0 ? "green" : "red";
            float roi = invested > 0 ? (profitLoss / invested) * 100f : 0f;
            profitLossText.text = $"<color={color}>{sign}Rp{profitLoss:N2} ({sign}{roi:N2}%)</color>";
        }
    }
}