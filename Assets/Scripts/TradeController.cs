using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class TradeController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text stockNameText;
    public TMP_Text currentPriceText;
    public TMP_InputField lotInputAmount;
    public TMP_Text liveBuyPreviewText;
    public TMP_Text feedbackText;

    [Header("Confirmation PopUps")]
    public GameObject buyConfirmPopUp;
    public GameObject sellConfirmPopUp;      // RESTORED
    public TMP_Text buyConfirmText;
    public TMP_Text sellConfirmText;         // RESTORED
    public Button sellConfirmYesButton;      // RESTORED

    [Header("Result PopUps")]
    public GameObject buyPopUp;
    public GameObject sellPopUp;             // RESTORED
    public GameObject insufficientCashPopUp;
    public GameObject noSharesPopUp;         // RESTORED

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
        if (currentPriceText != null) currentPriceText.text = $"Rp{close:N2}";

        UpdateLivePreview();
    }

    public void UpdateLivePreview()
    {
        if (lotInputAmount != null && int.TryParse(lotInputAmount.text, out int lots) && lots > 0)
        {
            float grossCost = lots * PortfolioManager.SHARES_PER_LOT * currentClosePrice;
            float fee = grossCost * (PortfolioManager.Instance != null ? PortfolioManager.Instance.buyFeeRate : 0f);
            float totalCost = grossCost + fee;
            if (liveBuyPreviewText != null) liveBuyPreviewText.text = $"Estimasi Beli: Rp{totalCost:N2}";
        }
        else
        {
            if (liveBuyPreviewText != null) liveBuyPreviewText.text = "Estimasi Beli: Rp0.00";
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
            buyConfirmText.text = $"Beli {pendingLots} lot ({pendingLots * PortfolioManager.SHARES_PER_LOT} lembar) {currentStockSymbol}?\n\nHarga Dasar: Rp{grossCost:N2}\nBiaya Beli: Rp{fee:N2}\n\nTotal: Rp{totalCost:N2}";
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

    // --- SELLING LOGIC (RESTORED) ---

    // Called by the main trading screen Sell Button
    public void OnClickSell()
    {
        CloseAllPopUps();

        int lots = 0; // Declare the variable first to satisfy the compiler

        if (lotInputAmount != null && (!int.TryParse(lotInputAmount.text, out lots) || lots <= 0))
        {
            SetFeedback("Jumlah lot saham tidak valid.");
            return;
        }

        TriggerSellConfirmation(currentStockSymbol, lots, currentClosePrice);
    }

    // Called dynamically from the Portfolio UI rows
    public void OnClickSellPartialFromPortfolio(string stockSymbol, int lotsToSell, float sellPrice)
    {
        CloseAllPopUps();
        float priceToUse = sellPrice > 0 ? sellPrice : currentClosePrice;
        TriggerSellConfirmation(stockSymbol, lotsToSell, priceToUse);
    }

    // Centralized popup generation for selling
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
            sellConfirmText.text = $"Jual {lotsToSell} lot ({lotsToSell * PortfolioManager.SHARES_PER_LOT} lembar) {cleanSymbol}?\n\nNilai Jual: Rp{grossRevenue:N2}\nBiaya Jual: -Rp{fee:N2}\n\nTotal Diterima: Rp{netRevenue:N2}";
        }

        if (sellConfirmYesButton != null)
        {
            sellConfirmYesButton.onClick.RemoveAllListeners();
            sellConfirmYesButton.onClick.AddListener(() => ExecutePortfolioSellDirect(cleanSymbol, lotsToSell, priceToUse));
        }

        ShowPopUp(sellConfirmPopUp);
    }

    // Executes the final sale
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

    // --- UTILITIES ---
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