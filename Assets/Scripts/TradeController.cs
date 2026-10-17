using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TradeController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text stockNameText;       // Displays active stock name
    public TMP_Text currentPriceText;    // Displays current close price
    public TMP_InputField shareInputAmount; // Input field for shares
    public TMP_Text feedbackText;        // Status messages (success/failure)

    private string currentDatasetName = "DefaultStock";
    private float currentClosePrice = 0f;
    private string currentDate = "";
    private int currentDayIndex = 1;

    private void Start()
    {
        if (shareInputAmount != null && string.IsNullOrEmpty(shareInputAmount.text))
        {
            shareInputAmount.text = "1";
        }
        SyncWithChart();
    }

    private void OnEnable()
    {
        // Automatically sync up the price whenever the trading panel is opened/enabled
        SyncWithChart();
    }

    public void SyncWithChart()
    {
        CandlestickChart chart = FindAnyObjectByType<CandlestickChart>();
        if (chart != null)
        {
            // Trigger a refresh or force the chart to re-feed its current day state
            if (DaySimulationManager.Instance != null)
            {
                // This triggers HandleDayChanged on the chart, which will push the latest price back here
                // (Alternatively, you can read public data if exposed, but pushing via simulation day is seamless)
            }
        }
    }

    // Called by CandlestickChart whenever data or day changes
    public void SetMarketPrice(string datasetName, string date, int dayIndex, float close)
    {
        currentDatasetName = datasetName;
        currentDate = date;
        currentDayIndex = dayIndex;
        currentClosePrice = close;

        // Update Panel UI labels safely
        if (stockNameText != null) stockNameText.text = datasetName;
        if (currentPriceText != null) currentPriceText.text = $"${close:N2}";
    }

    public void OnClickBuy()
    {
        if (!int.TryParse(shareInputAmount.text, out int shares) || shares <= 0)
        {
            SetFeedback("Please enter a valid share amount.");
            return;
        }

        if (PortfolioManager.Instance == null)
        {
            SetFeedback("PortfolioManager not found! Make sure it's persistent.");
            return;
        }

        bool success = PortfolioManager.Instance.ExecuteBuy(currentDatasetName, currentDate, currentDayIndex, shares, currentClosePrice);
        if (success)
        {
            SetFeedback($"Bought {shares}x {currentDatasetName} @ ${currentClosePrice:N2}");
        }
        else
        {
            SetFeedback("Buy failed: Insufficient cash.");
        }
    }

    public void OnClickSell()
    {
        if (!int.TryParse(shareInputAmount.text, out int shares) || shares <= 0)
        {
            SetFeedback("Please enter a valid share amount.");
            return;
        }

        if (PortfolioManager.Instance == null)
        {
            SetFeedback("PortfolioManager not found!");
            return;
        }

        bool success = PortfolioManager.Instance.ExecuteSell(currentDatasetName, currentDate, currentDayIndex, shares, currentClosePrice);
        if (success)
        {
            SetFeedback($"Sold {shares}x {currentDatasetName} @ ${currentClosePrice:N2}");
        }
        else
        {
            SetFeedback("Sell failed: Not enough shares owned.");
        }
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
        Debug.Log(message);
    }
}