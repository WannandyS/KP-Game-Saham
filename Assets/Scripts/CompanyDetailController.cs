using UnityEngine;
using TMPro;

public class CompanyDetailController : MonoBehaviour
{
    [Header("UI Text References")]
    public TMP_Text tickerText;          
    public TMP_Text companyNameText;     

    [Header("Managers")]
    public CandlestickChart candlestickChart; 
    public NewsManager newsManager;           
    public TabsControl tabsControl;           

    private void OnEnable()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
        }

        LoadStockData();
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

    public void LoadStockData()
    {
        if (tickerText != null && !string.IsNullOrEmpty(SelectedStockData.StockTicker)) 
            tickerText.text = SelectedStockData.StockTicker;
            
        if (companyNameText != null && !string.IsNullOrEmpty(SelectedStockData.CompanyName)) 
            companyNameText.text = SelectedStockData.CompanyName;

        if (candlestickChart != null && SelectedStockData.PriceCsvAsset != null)
        {
            candlestickChart.SetCSV(SelectedStockData.PriceCsvAsset);
        }

        if (newsManager != null && SelectedStockData.PriceCsvAsset != null)
        {
            newsManager.priceCsvAsset = SelectedStockData.PriceCsvAsset;
            newsManager.InitializeNewsData();
        }

        if (tabsControl != null)
        {
            if (SelectedStockData.OpenOnGraphTab)
            {
                tabsControl.SelectGraphTab();
            }
            else
            {
                tabsControl.SelectNewsTab();
            }
        }
    }
}