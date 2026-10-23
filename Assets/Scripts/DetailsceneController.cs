using UnityEngine;

public class DetailSceneController : MonoBehaviour
{
    [Header("Chart Components")]
    public CandlestickChart candlestickChart;
    public VolumeChartCSV volumeChart;

    [Header("News Components")]
    public NewsManager newsManager;

    [Header("Default Fallback Dataset")]
    public TextAsset defaultFallbackCsv;

    private void Start()
    {
        // 1. Determine which stock ticker/CSV to load
        string ticker = PlayerPrefs.GetString("SelectedStockSymbol", SelectedStockData.StockTicker);
        TextAsset stockCsv = SelectedStockData.PriceCsvAsset;

        if (stockCsv == null)
        {
            stockCsv = defaultFallbackCsv;
        }

        int targetDay = PlayerPrefs.GetInt("SelectedDayIndex", 1);
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.currentDay = targetDay;
        }

        Debug.Log($"[DetailSceneController] Initializing scene for stock: {ticker} on Day {targetDay}");

        // 2. Load dataset into Candlestick Chart
        if (candlestickChart != null && stockCsv != null)
        {
            candlestickChart.LoadNewDataset(stockCsv);
        }

        // 3. Load dataset into Volume Chart
        if (volumeChart != null && stockCsv != null)
        {
            volumeChart.LoadNewDataset(stockCsv);
        }

        // 4. Initialize News
        if (newsManager != null)
        {
            if (stockCsv != null && newsManager.priceCsvAsset == null)
            {
                newsManager.priceCsvAsset = stockCsv;
            }
            newsManager.InitializeNewsData();
        }
    }

    public void OnBackButtonClicked(string marketOverviewSceneName)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(marketOverviewSceneName);
    }
}