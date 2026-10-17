using UnityEngine;
using TMPro;

public class PortfolioItemUI : MonoBehaviour
{
    [Header("Horizontal Columns")]
    public TMP_Text tickerText;
    public TMP_Text priceWhenBoughtText; // Average Buy Price
    public TMP_Text totalInvestedText;
    public TMP_Text change24hText;       // Current price vs previous day / 24h change
    public TMP_Text shareAmountText;
    public TMP_Text totalValueText;
    public TMP_Text profitLossText;      // Projected Profit / Loss

    public void Setup(string ticker, float avgBuyPrice, float totalInvested, int shares, float currentPrice, float prevPrice)
    {
        if (tickerText != null) tickerText.text = ticker;
        if (priceWhenBoughtText != null) priceWhenBoughtText.text = $"${avgBuyPrice:N2}";
        if (totalInvestedText != null) totalInvestedText.text = $"${totalInvested:N2}";
        if (shareAmountText != null) shareAmountText.text = shares.ToString("N0");

        // Calculate Current Total Value
        float totalValue = shares * currentPrice;
        if (totalValueText != null) totalValueText.text = $"${totalValue:N2}";

        // Calculate 24h Change (Percentage or dollar difference from previous candle)
        float change = currentPrice - prevPrice;
        float changePercent = prevPrice > 0 ? (change / prevPrice) * 100f : 0f;
        if (change24hText != null)
        {
            string colorHex = change >= 0 ? "#00FF00" : "#FF0000";
            change24hText.text = $"<color={colorHex}>{changePercent:+0.00;-0.00;0.00}%</color>";
        }

        // Calculate Projected Profit / Loss: Total Value - Total Invested Cost
        float profitLoss = totalValue - totalInvested;
        if (profitLossText != null)
        {
            string plColorHex = profitLoss >= 0 ? "#00FF00" : "#FF0000";
            profitLossText.text = $"<color={plColorHex}>${profitLoss:+N2;-N2;0.00}</color>";
        }
    }
}