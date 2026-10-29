using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Globalization;

public class TradeController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text stockNameText;
    public TMP_Text currentPriceText;
    public TMP_Text change24hText;          // <-- Added reference for 24h Change UI
    public TMP_InputField lotInputAmount;
    public TMP_Text liveBuyPreviewText;
    public TMP_Text feedbackText;

    [Header("Confirmation PopUps")]
    public GameObject buyConfirmPopUp;
    public GameObject sellConfirmPopUp;      
    public TMP_Text buyConfirmText;
    public TMP_Text sellConfirmText;         
    public Button sellConfirmYesButton;      

    [Header("Result PopUps")]
    public GameObject buyPopUp;
    public GameObject sellPopUp;             
    public GameObject insufficientCashPopUp;
    public GameObject noSharesPopUp;         

    private string currentStockSymbol = "";
    private float currentClosePrice = 0f;
    private string currentDate = "";
    private int currentDayIndex = 1;

    private int pendingLots = 1;

    private void Start()
    {
        CloseAllPopUps();

        if (lotInputAmount != null)
        {
            if (string.IsNullOrEmpty(lotInputAmount.text))
            {
                lotInputAmount.text = "1";
            }
            lotInputAmount.onValueChanged.AddListener(delegate { UpdateLivePreview(); });
        }

        Invoke(nameof(SyncWithChart), 0.05f);
    }

    private void OnEnable()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
        }
        Invoke(nameof(SyncWithChart), 0.05f);
    }

    private void OnDisable()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
        }
    }

    private void HandleDayChanged(int newDay) => SyncWithChart();

    private string CleanTicker(string input)
    {
        if (PortfolioManager.Instance != null) return PortfolioManager.Instance.CleanTicker(input);
        if (string.IsNullOrEmpty(input)) return "";
        return Regex.Replace(input.Trim(), @"(?i)(\.csv|_csv)$", "").Replace(" ", "").ToUpper();
    }

    public void OnStockSelected(string selectedTicker)
    {
        string clean = CleanTicker(selectedTicker);
        SelectedStockData.StockTicker = clean;
        if (stockNameText != null) stockNameText.text = clean;

        CancelInvoke(nameof(SyncWithChart));
        Invoke(nameof(SyncWithChart), 0.05f);
    }

    public void SyncWithChart()
    {
        string activeTicker = CleanTicker(SelectedStockData.StockTicker);
        if (CandlestickChart.ActiveLatestCandle(out string assetName, out string date, out float closePrice))
        {
            int activeDay = DaySimulationManager.Instance != null ? DaySimulationManager.Instance.currentDay : 1;
            if (string.IsNullOrEmpty(activeTicker)) activeTicker = CleanTicker(assetName);
            SetMarketPrice(activeTicker, date, activeDay, closePrice);
        }
        else if (!string.IsNullOrEmpty(activeTicker))
        {
            if (stockNameText != null) stockNameText.text = activeTicker;
        }
    }

    public void SetMarketPrice(string stockSymbol, string date, int dayIndex, float close)
    {
        currentStockSymbol = CleanTicker(!string.IsNullOrEmpty(SelectedStockData.StockTicker) ? SelectedStockData.StockTicker : stockSymbol);
        currentDate = date;
        currentDayIndex = dayIndex;
        currentClosePrice = close;

        if (stockNameText != null) stockNameText.text = currentStockSymbol;
        if (currentPriceText != null) currentPriceText.text = $"{close:N0}";

        // --- Calculate and Display 24H Change ---
        CalculateAndDisplay24HChange(currentStockSymbol, dayIndex, close);

        UpdateLivePreview();
    }

    private void CalculateAndDisplay24HChange(string symbol, int dayIndex, float currentPrice)
    {
        if (change24hText == null) return;

        float changePercent = 0f;
        bool found = false;

        // Try getting CSV asset from SelectedStockData or fallback
        TextAsset csv = SelectedStockData.PriceCsvAsset;
        if (csv == null)
        {
            // Fallback search in Resources or active scene if needed
            csv = Resources.Load<TextAsset>(symbol);
        }

        if (csv != null)
        {
            string[] lines = csv.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length > 1)
            {
                string[] headers = lines[0].Split(',');
                int closeIndex = -1;
                for (int i = 0; i < headers.Length; i++)
                {
                    if (headers[i].Trim().Trim('"').Equals("Close", System.StringComparison.OrdinalIgnoreCase))
                    {
                        closeIndex = i;
                        break;
                    }
                }

                if (closeIndex != -1 && dayIndex > 1 && dayIndex < lines.Length)
                {
                    string[] prevCols = lines[dayIndex - 1].Split(','); // dayIndex is 1-based, lines[1] is Day 1
                    string[] currentCols = lines[dayIndex].Split(',');

                    if (prevCols.Length > closeIndex && currentCols.Length > closeIndex)
                    {
                        if (float.TryParse(prevCols[closeIndex].Trim().Trim('"'), NumberStyles.Float, CultureInfo.InvariantCulture, out float prevClose))
                        {
                            if (prevClose > 0f)
                            {
                                changePercent = ((currentPrice - prevClose) / prevClose) * 100f;
                                found = true;
                            }
                        }
                    }
                }
            }
        }

        if (!found && dayIndex <= 1)
        {
            changePercent = 0f; // Day 1 has no previous close
            found = true;
        }

        if (found)
        {
            string sign = changePercent >= 0 ? "+" : "";
            string color = changePercent >= 0 ? "green" : "red";
            change24hText.text = $"<color={color}>{sign}{changePercent:N2}%</color>";
        }
        else
        {
            change24hText.text = "0.00%";
        }
    }

    public void UpdateLivePreview()
    {
        if (lotInputAmount != null && int.TryParse(lotInputAmount.text, out int lots) && lots > 0)
        {
            float grossCost = lots * PortfolioManager.SHARES_PER_LOT * currentClosePrice;
            float fee = grossCost * (PortfolioManager.Instance != null ? PortfolioManager.Instance.buyFeeRate : 0f);
            float totalCost = grossCost + fee;
            if (liveBuyPreviewText != null) liveBuyPreviewText.text = $"Estimasi Beli: {totalCost:N0}";
        }
        else
        {
            if (liveBuyPreviewText != null) liveBuyPreviewText.text = "Estimasi Beli: Rp0";
        }
    }

    // --- BUYING LOGIC ---
    public void OnClickBuy()
    {
        CloseAllPopUps();
        if (lotInputAmount != null && (!int.TryParse(lotInputAmount.text, out pendingLots) || pendingLots <= 0))
        {
            SetFeedback("Jumlah lot saham tidak valid.");
            return;
        }

        float grossCost = pendingLots * PortfolioManager.SHARES_PER_LOT * currentClosePrice;
        float fee = grossCost * (PortfolioManager.Instance != null ? PortfolioManager.Instance.buyFeeRate : 0f);
        float totalCost = grossCost + fee;

        if (buyConfirmText != null)
        {
            buyConfirmText.text = $"Beli {pendingLots} lot ({pendingLots * PortfolioManager.SHARES_PER_LOT} lembar) {currentStockSymbol}?\n\nHarga Dasar: {grossCost:N0}\nBiaya Beli: {fee:N0}\n\nTotal: {totalCost:N0}";
        }
        ShowPopUp(buyConfirmPopUp);
    }

    public void ConfirmBuyYes()
    {
        CloseAllPopUps();
        if (PortfolioManager.Instance == null) return;

        bool success = PortfolioManager.Instance.ExecuteBuy(currentStockSymbol, currentDate, currentDayIndex, pendingLots, currentClosePrice);
        if (success)
        {
            SetFeedback($"Berhasil membeli {pendingLots} lot {currentStockSymbol}");
            ShowPopUp(buyPopUp);
        }
        else
        {
            SetFeedback("Gagal beli: Saldo tidak cukup.");
            ShowPopUp(insufficientCashPopUp);
        }
    }

    // --- SELLING LOGIC ---

    public void OnClickSell()
    {
        CloseAllPopUps();
        int lots = 0;

        if (lotInputAmount != null && (!int.TryParse(lotInputAmount.text, out lots) || lots <= 0))
        {
            SetFeedback("Jumlah lot saham tidak valid.");
            return;
        }

        TriggerSellConfirmation(currentStockSymbol, lots, currentClosePrice);
    }

    public void OnClickSellPartialFromPortfolio(string stockSymbol, int lotsToSell, float sellPrice)
    {
        CloseAllPopUps();
        float priceToUse = sellPrice > 0 ? sellPrice : currentClosePrice;
        TriggerSellConfirmation(stockSymbol, lotsToSell, priceToUse);
    }

    private void TriggerSellConfirmation(string stockSymbol, int lotsToSell, float priceToUse)
    {
        if (PortfolioManager.Instance == null) return;
        string cleanSymbol = CleanTicker(stockSymbol);
        int ownedLots = PortfolioManager.Instance.GetOwnedLots(cleanSymbol);

        if (lotsToSell > ownedLots || ownedLots <= 0)
        {
            SetFeedback($"Gagal jual: Lot {cleanSymbol} tidak mencukupi (diminta: {lotsToSell}, dimiliki: {ownedLots}).");
            ShowPopUp(noSharesPopUp);
            return;
        }

        float grossRevenue = lotsToSell * PortfolioManager.SHARES_PER_LOT * priceToUse;
        float fee = grossRevenue * PortfolioManager.Instance.sellFeeRate;
        float netRevenue = grossRevenue - fee;

        if (sellConfirmText != null)
        {
            sellConfirmText.text = $"Jual {lotsToSell} lot ({lotsToSell * PortfolioManager.SHARES_PER_LOT} lembar) {cleanSymbol}?\n\nNilai Jual: {grossRevenue:N0}\nBiaya Jual: -{fee:N0}\n\nTotal Diterima: {netRevenue:N0}";
        }

        if (sellConfirmYesButton != null)
        {
            sellConfirmYesButton.onClick.RemoveAllListeners();
            sellConfirmYesButton.onClick.AddListener(() => ExecutePortfolioSellDirect(cleanSymbol, lotsToSell, priceToUse));
        }

        ShowPopUp(sellConfirmPopUp);
    }

    private void ExecutePortfolioSellDirect(string symbol, int lots, float price)
    {
        CloseAllPopUps();
        if (PortfolioManager.Instance == null) return;

        string execDate = !string.IsNullOrEmpty(currentDate) ? currentDate : "Portfolio Sale";
        int execDay = DaySimulationManager.Instance != null ? DaySimulationManager.Instance.currentDay : currentDayIndex;

        bool success = PortfolioManager.Instance.ExecuteSell(symbol, execDate, execDay, lots, price);
        if (success)
        {
            SetFeedback($"Berhasil menjual {lots} lot {symbol}");
            ShowPopUp(sellPopUp);
        }
        else
        {
            SetFeedback("Gagal jual: Terjadi kesalahan eksekusi.");
            ShowPopUp(noSharesPopUp);
        }
    }

    private void ShowPopUp(GameObject popUp)
    {
        if (popUp != null)
        {
            popUp.SetActive(true);
            popUp.transform.SetAsLastSibling();
        }
    }

    public void CloseAllPopUps()
    {
        if (buyConfirmPopUp != null) buyConfirmPopUp.SetActive(false);
        if (sellConfirmPopUp != null) sellConfirmPopUp.SetActive(false);
        if (buyPopUp != null) buyPopUp.SetActive(false);
        if (sellPopUp != null) sellPopUp.SetActive(false);
        if (insufficientCashPopUp != null) insufficientCashPopUp.SetActive(false);
        if (noSharesPopUp != null) noSharesPopUp.SetActive(false);
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
    }
}