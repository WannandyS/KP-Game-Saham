using UnityEngine;
using TMPro;

public class DetailSceneController : MonoBehaviour
{
    [Header("UI Text References")]
    public TMP_Text tickerText;          
    public TMP_Text companyNameText;     

    [Header("Chart Components")]
    public CandlestickChart candlestickChart;
    public VolumeChartCSV volumeChart;

    [Header("News Components")]
    public NewsManager newsManager;

    [Header("Default Fallback Dataset")]
    public TextAsset defaultFallbackCsv;

    private static bool isInitialized = false;

    private void Awake()
    {
        isInitialized = false;
    }

    private void OnEnable()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
        }

        if (!isInitialized)
        {
            InitializeDetailScene();
        }
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
        if (candlestickChart != null)
        {
            candlestickChart.RedrawChart();
        }

        if (newsManager != null)
        {
            newsManager.RefreshNewsUI();
        }
    }

    public void InitializeDetailScene()
    {
        if (isInitialized) return;
        isInitialized = true;

        string ticker = SelectedStockData.StockTicker;
        TextAsset stockCsv = SelectedStockData.PriceCsvAsset;

        if (string.IsNullOrEmpty(ticker))
        {
            ticker = PlayerPrefs.GetString("SelectedStockSymbol", "");
            
            if (!string.IsNullOrEmpty(ticker))
            {
                SelectedStockData.StockTicker = ticker;
                SelectedStockData.CompanyName = SelectedStockData.GetCompanyName(ticker);
            }
        }

        if (stockCsv == null)
        {
            stockCsv = defaultFallbackCsv;
            SelectedStockData.PriceCsvAsset = stockCsv;
            
            if (string.IsNullOrEmpty(SelectedStockData.StockTicker) && defaultFallbackCsv != null)
            {
                SelectedStockData.StockTicker = defaultFallbackCsv.name;
                SelectedStockData.CompanyName = SelectedStockData.GetCompanyName(defaultFallbackCsv.name);
            }
        }

        if (tickerText != null) 
            tickerText.text = SelectedStockData.StockTicker;
            
        if (companyNameText != null) 
        {
            string fullCompanyName = !string.IsNullOrEmpty(SelectedStockData.CompanyName) 
                ? SelectedStockData.CompanyName 
                : SelectedStockData.GetCompanyName(SelectedStockData.StockTicker);

            companyNameText.text = fullCompanyName;
        }

        int currentDay = DaySimulationManager.Instance != null ? DaySimulationManager.Instance.currentDay : 1;
        Debug.Log($"[DetailSceneController] Loading stock: {SelectedStockData.StockTicker} ({SelectedStockData.CompanyName}) on Day {currentDay}");

        if (candlestickChart != null && stockCsv != null)
        {
            candlestickChart.SetCSV(stockCsv);
        }

        if (volumeChart != null && stockCsv != null)
        {
            volumeChart.SetCSV(stockCsv);
        }

        if (newsManager != null)
        {
            newsManager.priceCsvAsset = stockCsv;
            newsManager.InitializeNewsData();
        }
    }

    public void OnBackButtonClicked(string marketOverviewSceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(marketOverviewSceneName);
    }
}