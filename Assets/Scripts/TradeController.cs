using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Text.RegularExpressions;

public class TradeController : MonoBehaviour
{
    [Header("UI References (Optional in Portfolio Scene)")]
    public TMP_Text stockNameText;
    public TMP_Text currentPriceText;
    public TMP_InputField shareInputAmount;
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

    private string currentDatasetName = "";
    private float currentClosePrice = 0f;
    private string currentDate = "";
    private int currentDayIndex = 1;

    private int pendingShares = 1;

    private void Start()
    {
        if (shareInputAmount != null && string.IsNullOrEmpty(shareInputAmount.text))
        {
            shareInputAmount.text = "1";
        }

        CloseAllPopUps();
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

    private void HandleDayChanged(int newDay)
    {
        SyncWithChart();
    }

    private string CleanTicker(string input)
    {
        if (PortfolioManager.Instance != null)
            return PortfolioManager.Instance.CleanTicker(input);

        if (string.IsNullOrEmpty(input)) return "";
        string clean = input.Trim();
        clean = Regex.Replace(clean, @"(?i)(\.csv|_csv)$", "");
        clean = Regex.Replace(clean, @"\s+", "");
        return clean.ToUpper();
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

            if (string.IsNullOrEmpty(activeTicker)) 
            {
                activeTicker = CleanTicker(assetName);
            }

            SetMarketPrice(activeTicker, date, activeDay, closePrice);
        }
        else if (!string.IsNullOrEmpty(activeTicker))
        {
            if (stockNameText != null) stockNameText.text = activeTicker;
        }
    }

    public void SetMarketPrice(string datasetName, string date, int dayIndex, float close)
    {
        string cleanName = CleanTicker(!string.IsNullOrEmpty(SelectedStockData.StockTicker) ? SelectedStockData.StockTicker : datasetName);

        currentDatasetName = cleanName;
        currentDate = date;
        currentDayIndex = dayIndex;
        currentClosePrice = close;

        if (stockNameText != null) 
            stockNameText.text = currentDatasetName;
            
        if (currentPriceText != null) 
            currentPriceText.text = $"Rp{close:N2}";
    }

    public void OnClickBuy()
    {
        CloseAllPopUps();

        if (shareInputAmount != null)
        {
            if (!int.TryParse(shareInputAmount.text, out pendingShares) || pendingShares <= 0)
            {
                SetFeedback("Jumlah lembar saham tidak valid.");
                return;
            }
        }

        float totalCost = pendingShares * currentClosePrice;
        if (buyConfirmText != null)
        {
            buyConfirmText.text = $"Apakah Anda yakin ingin membeli {pendingShares} lot {currentDatasetName} seharga Rp{totalCost:N2}?";
        }

        ShowPopUp(buyConfirmPopUp);
    }

    public void ConfirmBuyYes()
    {
        CloseAllPopUps();

        if (PortfolioManager.Instance == null)
        {
            SetFeedback("PortfolioManager tidak ditemukan!");
            return;
        }

        bool success = PortfolioManager.Instance.ExecuteBuy(currentDatasetName, currentDate, currentDayIndex, pendingShares, currentClosePrice);
        
        if (success)
        {
            SetFeedback($"Berhasil membeli {pendingShares}x {currentDatasetName}");
            ShowPopUp(buyPopUp);
        }
        else
        {
            SetFeedback("Gagal beli: Saldo tidak cukup.");
            ShowPopUp(insufficientCashPopUp);
        }
    }

    public void OnClickSell()
    {
        CloseAllPopUps();

        if (shareInputAmount != null)
        {
            if (!int.TryParse(shareInputAmount.text, out pendingShares) || pendingShares <= 0)
            {
                SetFeedback("Jumlah lembar saham tidak valid.");
                return;
            }
        }

        if (PortfolioManager.Instance == null)
        {
            SetFeedback("PortfolioManager tidak ditemukan!");
            return;
        }

        int ownedShares = PortfolioManager.Instance.GetOwnedShares(currentDatasetName);

        if (ownedShares < pendingShares || ownedShares <= 0)
        {
            SetFeedback($"Gagal jual: Saham tidak mencukupi (dimiliki: {ownedShares}).");
            ShowPopUp(noSharesPopUp);
            return;
        }

        float totalRevenue = pendingShares * currentClosePrice;
        if (sellConfirmText != null)
        {
            sellConfirmText.text = $"Apakah Anda yakin ingin menjual {pendingShares} lot {currentDatasetName} seharga Rp{totalRevenue:N2}?";
        }

        if (sellConfirmYesButton != null)
        {
            sellConfirmYesButton.onClick.RemoveAllListeners();
            sellConfirmYesButton.onClick.AddListener(ConfirmSellYes);
        }

        ShowPopUp(sellConfirmPopUp);
    }

    public void OnClickSellAllFromPortfolio(string stockSymbol, float sellPrice)
    {
        CloseAllPopUps();

        if (PortfolioManager.Instance == null)
        {
            SetFeedback("PortfolioManager tidak ditemukan!");
            return;
        }

        string cleanSymbol = CleanTicker(stockSymbol);
        int ownedShares = PortfolioManager.Instance.GetOwnedShares(cleanSymbol);

        Debug.Log($"[Portfolio Sell Attempt] Raw Symbol: '{stockSymbol}' -> Cleaned: '{cleanSymbol}', Owned Shares: {ownedShares}");

        if (ownedShares <= 0)
        {
            SetFeedback($"Gagal jual: Saham {cleanSymbol} tidak mencukupi (dimiliki: 0).");
            ShowPopUp(noSharesPopUp);
            return;
        }

        float priceToUse = sellPrice > 0 ? sellPrice : currentClosePrice;
        float totalRevenue = ownedShares * priceToUse;

        if (sellConfirmText != null)
        {
            sellConfirmText.text = $"Apakah Anda yakin ingin menjual SELURUH ({ownedShares} lot) {cleanSymbol} seharga Rp{totalRevenue:N2}?";
        }

        if (sellConfirmYesButton != null)
        {
            sellConfirmYesButton.onClick.RemoveAllListeners();
            sellConfirmYesButton.onClick.AddListener(() =>
            {
                ExecutePortfolioSellDirect(cleanSymbol, ownedShares, priceToUse);
            });
        }

        ShowPopUp(sellConfirmPopUp);
    }

    public void OnClickSellAllFromPortfolio(string stockSymbol)
    {
        OnClickSellAllFromPortfolio(stockSymbol, currentClosePrice);
    }

    private void ExecutePortfolioSellDirect(string symbol, int shares, float price)
    {
        CloseAllPopUps();

        if (PortfolioManager.Instance == null) return;

        string execDate = !string.IsNullOrEmpty(currentDate) ? currentDate : "Portfolio Sale";
        int execDay = DaySimulationManager.Instance != null ? DaySimulationManager.Instance.currentDay : currentDayIndex;

        bool success = PortfolioManager.Instance.ExecuteSell(symbol, execDate, execDay, shares, price);

        if (success)
        {
            SetFeedback($"Berhasil menjual {shares}x {symbol}");
            ShowPopUp(sellPopUp);
        }
        else
        {
            SetFeedback("Gagal jual: Terjadi kesalahan eksekusi.");
            ShowPopUp(noSharesPopUp);
        }
    }

    public void ConfirmSellYes()
    {
        CloseAllPopUps();

        if (PortfolioManager.Instance == null)
        {
            SetFeedback("PortfolioManager tidak ditemukan!");
            return;
        }

        bool success = PortfolioManager.Instance.ExecuteSell(currentDatasetName, currentDate, currentDayIndex, pendingShares, currentClosePrice);
        
        if (success)
        {
            SetFeedback($"Berhasil menjual {pendingShares}x {currentDatasetName}");
            ShowPopUp(sellPopUp);
        }
        else
        {
            SetFeedback("Gagal jual: Saham tidak cukup.");
            ShowPopUp(noSharesPopUp);
        }
    }

    // --- DITAMBAHKAN: SetAsLastSibling untuk memastikan PopUp tampil di urutan terdepan UI ---
    private void ShowPopUp(GameObject popUp)
    {
        if (popUp != null)
        {
            popUp.SetActive(true);
            popUp.transform.SetAsLastSibling();
        }
        else
        {
            Debug.LogError("TradeController: Referensi PopUp bernilai NULL di Inspector!");
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
        Debug.Log(message);
    }
}