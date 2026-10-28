using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PortfolioHoldingRow : MonoBehaviour
{
    [Header("UI Text References (Asset Info)")]
    public TMP_Text tickerText;

    [Header("UI Text References (Financials)")]
    public TMP_Text buyPriceText;
    public TMP_Text totalInvestedText;
    public TMP_Text change24hText;
    public TMP_Text shareAmountText;
    public TMP_Text totalValueText;
    public TMP_Text profitLossText;         // Rp Profit/Loss
    public TMP_Text profitLossPercentText;  // ROI %

    [Header("UI Action Button")]
    public Button sellButton; // Slot untuk tombol Jual di prefab baris portofolio

    public void SetupRow(string ticker, float buyPrice, float invested, float change24h, int shares, float currentPrice, float totalValue, float profitLoss)
    {
        // Set nama ticker saham
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

        // Profit/Loss Amount (Rp)
        if (profitLossText != null)
        {
            string sign = profitLoss >= 0 ? "+" : "";
            string color = profitLoss >= 0 ? "green" : "red";
            profitLossText.text = $"<color={color}>{sign}Rp{profitLoss:N2}</color>";
        }

        // ROI Percentage (%)
        if (profitLossPercentText != null)
        {
            string sign = profitLoss >= 0 ? "+" : "";
            string color = profitLoss >= 0 ? "green" : "red";
            float roi = invested > 0 ? (profitLoss / (invested * 100)) * 100f : 0f;
            profitLossPercentText.text = $"<color={color}>{sign}{roi:N2}%</color>";
        }

        // Listener tombol Jual langsung dari Portofolio
        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() =>
            {
                TradeController trade = Object.FindAnyObjectByType<TradeController>();
                if (trade != null)
                {
                    trade.OnClickSellAllFromPortfolio(ticker, currentPrice);
                }
                else
                {
                    Debug.LogError("TradeController tidak ditemukan di scene!");
                }
            });
        }
    }
}