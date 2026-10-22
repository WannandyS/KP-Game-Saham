using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MarketOverviewController : MonoBehaviour
{
    public enum SortColumn
    {
        Name,
        Price,
        Change,
        Volume,
        MarketCap,
        Value
    }

    // NEW: Struct to hold references to the Header Text components
    [Serializable]
    public struct ColumnHeaderUI
    {
        public SortColumn columnType;
        public TMP_Text headerText;
        [HideInInspector] public string originalText; // Stores the base text (e.g., "Price")
    }

    [Header("CSV Data Settings")]
    [Tooltip("Assign CSV text files for all 45 stocks. Name files as 'SYMBOL.csv' (e.g., AADI.csv)")]
    public TextAsset[] csvFiles;

    [Header("Day Selection")]
    public int currentDayIndex = 1;
    public TMP_Text dayHeaderLabel;

    [Header("UI Prefab & Container")]
    public GameObject stockRowPrefab;
    public Transform rowContainer;

    [Header("Sorting State & UI")]
    public SortColumn currentSortColumn = SortColumn.Name;
    public bool isAscending = true;
    public ColumnHeaderUI[] columnHeaders; // Array to hold the header text references

    // NEW: Strings for the visual indicators
    private readonly string arrowUp = "D";
    private readonly string arrowDown = "A";

    [Header("Stock Metadata Mock")]
    public float defaultTotalShares = 5000000000f;

    [Header("Target Scene")]
    public string detailSceneName = "StockDetailScene";

    private List<StockRowData> currentMarketData = new List<StockRowData>();

    private void OnEnable()
    {
        SubscribeToDayManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromDayManager();
    }

    private void SubscribeToDayManager()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
        }
    }

    private void UnsubscribeFromDayManager()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
        }
    }

    private void Start()
    {
        SubscribeToDayManager();

        if (DaySimulationManager.Instance != null)
        {
            UpdateMarketOverview(DaySimulationManager.Instance.currentDay);
        }
        else
        {
            UpdateMarketOverview(currentDayIndex);
        }
    }

    private void Awake()
    {
        // Awake runs before OnEnable and Start. 
        // This guarantees we save the clean original text before any arrows are added.
        for (int i = 0; i < columnHeaders.Length; i++)
        {
            if (columnHeaders[i].headerText != null)
            {
                columnHeaders[i].originalText = columnHeaders[i].headerText.text;
            }
        }
    }

    private void HandleDayChanged(int newDay)
    {
        UpdateMarketOverview(newDay);
    }

    public void UpdateMarketOverview(int dayIndex)
    {
        currentDayIndex = Mathf.Max(1, dayIndex);
        if (dayHeaderLabel != null)
        {
            dayHeaderLabel.text = $"Day {currentDayIndex}";
        }

        currentMarketData.Clear();

        foreach (TextAsset csvAsset in csvFiles)
        {
            if (csvAsset == null) continue;

            string stockSymbol = csvAsset.name;
            StockRowData data = ParseCSVForDay(csvAsset.text, stockSymbol, currentDayIndex);

            if (data != null)
            {
                currentMarketData.Add(data);
            }
        }

        ApplySort();
        RebuildRowUI();
        UpdateHeaderVisuals(); // Update arrows when data loads
    }

    public void OnColumnHeaderClicked(int columnIndex)
    {
        SortColumn clickedColumn = (SortColumn)columnIndex;

        if (currentSortColumn == clickedColumn)
        {
            isAscending = !isAscending;
        }
        else
        {
            currentSortColumn = clickedColumn;
            isAscending = clickedColumn == SortColumn.Name;
        }

        ApplySort();
        RebuildRowUI();
        UpdateHeaderVisuals(); // Update arrows when clicked
    }

    public void ApplySort()
    {
        switch (currentSortColumn)
        {
            case SortColumn.Name:
                currentMarketData = isAscending
                    ? currentMarketData.OrderBy(s => s.stockSymbol).ToList()
                    : currentMarketData.OrderByDescending(s => s.stockSymbol).ToList();
                break;
            case SortColumn.Price:
                currentMarketData = isAscending
                    ? currentMarketData.OrderBy(s => s.price).ToList()
                    : currentMarketData.OrderByDescending(s => s.price).ToList();
                break;
            case SortColumn.Change:
                currentMarketData = isAscending
                    ? currentMarketData.OrderBy(s => s.changePercent).ToList()
                    : currentMarketData.OrderByDescending(s => s.changePercent).ToList();
                break;
            case SortColumn.Volume:
                currentMarketData = isAscending
                    ? currentMarketData.OrderBy(s => s.volume).ToList()
                    : currentMarketData.OrderByDescending(s => s.volume).ToList();
                break;
            case SortColumn.MarketCap:
                currentMarketData = isAscending
                    ? currentMarketData.OrderBy(s => s.marketCap).ToList()
                    : currentMarketData.OrderByDescending(s => s.marketCap).ToList();
                break;
            case SortColumn.Value:
                currentMarketData = isAscending
                    ? currentMarketData.OrderBy(s => s.value).ToList()
                    : currentMarketData.OrderByDescending(s => s.value).ToList();
                break;
        }
    }

    // --- NEW: UI VISUAL UPDATER ---
    private void UpdateHeaderVisuals()
    {
        for (int i = 0; i < columnHeaders.Length; i++)
        {
            if (columnHeaders[i].headerText == null) continue;

            
            columnHeaders[i].headerText.text = columnHeaders[i].originalText;

            if (columnHeaders[i].columnType == currentSortColumn)
            {
                string arrow = isAscending ? arrowUp : arrowDown;
                columnHeaders[i].headerText.text = columnHeaders[i].headerText.text + arrow;
            }
        }
    }
    // ------------------------------

    private void RebuildRowUI()
    {
        foreach (Transform child in rowContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (StockRowData data in currentMarketData)
        {
            InstantiateRowUI(data);
        }
    }

    private StockRowData ParseCSVForDay(string csvContent, string symbol, int day)
    {
        string[] lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return null;

        if (day >= lines.Length) day = lines.Length - 1;

        string[] currentCols = lines[day].Split(',');
        if (currentCols.Length < 7) return null;

        float currentClose = float.Parse(currentCols[2]);
        float currentVolume = float.Parse(currentCols[6]);

        float closePercentChange = 0f;
        if (day > 1)
        {
            string[] prevCols = lines[day - 1].Split(',');
            float prevClose = float.Parse(prevCols[2]);
            if (prevClose > 0)
            {
                closePercentChange = ((currentClose - prevClose) / prevClose) * 100f;
            }
        }

        float calculatedValue = currentClose * currentVolume;
        float calculatedMarketCap = currentClose * defaultTotalShares;

        return new StockRowData
        {
            stockSymbol = symbol,
            date = currentCols[0],
            price = currentClose,
            volume = currentVolume,
            changePercent = closePercentChange,
            value = calculatedValue,
            marketCap = calculatedMarketCap
        };
    }

    private void InstantiateRowUI(StockRowData data)
    {
        GameObject newRow = Instantiate(stockRowPrefab, rowContainer);
        StockRowUI rowUI = newRow.GetComponent<StockRowUI>();

        if (rowUI != null)
        {
            rowUI.SetupRow(data, OnDetailsButtonClicked);
        }
    }

    private void OnDetailsButtonClicked(StockRowData data)
    {
        PlayerPrefs.SetString("SelectedStockSymbol", data.stockSymbol);

        int activeDay = DaySimulationManager.Instance != null ? DaySimulationManager.Instance.currentDay : currentDayIndex;
        PlayerPrefs.SetInt("SelectedDayIndex", activeDay);
        PlayerPrefs.Save();

        SceneManager.LoadScene(detailSceneName);
    }

    public void NextDay()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.NextDay();
        }
        else
        {
            UpdateMarketOverview(currentDayIndex + 1);
        }
    }

    public void PreviousDay()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.PreviousDay();
        }
        else
        {
            UpdateMarketOverview(currentDayIndex - 1);
        }
    }
}