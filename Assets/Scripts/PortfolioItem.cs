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
    public TMP_Text profitLossText;
    public TMP_Text profitLossPercentText;

    [Header("UI Action Controls")]
    public TMP_InputField sellLotInput; // Make sure this is assigned in the Prefab
    public Button sellButton;

    public void SetupRow(string stockSymbol, float buyPrice, float invested, float change24h, int lotsOwned, float currentPrice, float totalValue, float profitLoss)
    {
        if (tickerText != null) tickerText.text = stockSymbol;
        if (buyPriceText != null) buyPriceText.text = $"Rp{buyPrice:N2}";
        if (totalInvestedText != null) totalInvestedText.text = $"Rp{invested:N2}";

        if (change24hText != null)
        {
            string sign = change24h >= 0 ? "+" : "";
            string color = change24h >= 0 ? "green" : "red";
            change24hText.text = $"<color={color}>{sign}{change24h:N2}%</color>";
        }

        if (shareAmountText != null) shareAmountText.text = lotsOwned.ToString("N0");
        if (totalValueText != null) totalValueText.text = $"Rp{totalValue:N2}";

        if (profitLossText != null)
        {
            string sign = profitLoss >= 0 ? "+" : "";
            string color = profitLoss >= 0 ? "green" : "red";
            profitLossText.text = $"<color={color}>{sign}Rp{profitLoss:N2}</color>";
        }

        if (profitLossPercentText != null)
        {
            string sign = profitLoss >= 0 ? "+" : "";
            string color = profitLoss >= 0 ? "green" : "red";
            float roi = invested > 0 ? (profitLoss / invested) * 100f : 0f;
            profitLossPercentText.text = $"<color={color}>{sign}{roi:N2}%</color>";
        }

        // Set default text to maximum lots owned
        if (sellLotInput != null)
        {
            sellLotInput.text = lotsOwned.ToString();
        }

        // --- LISTENER RESTORED ---
        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(() =>
            {
                TradeController trade = Object.FindAnyObjectByType<TradeController>();
                if (trade != null)
                {
                    int lotsToSell = lotsOwned;

                    if (sellLotInput != null && int.TryParse(sellLotInput.text, out int parsedLots) && parsedLots > 0)
                    {
                        lotsToSell = parsedLots;
                    }

                    trade.OnClickSellPartialFromPortfolio(stockSymbol, lotsToSell, currentPrice);
                }
                else
                {
                    Debug.LogError("TradeController tidak ditemukan di scene!");
                }
            });
        }
    }
}