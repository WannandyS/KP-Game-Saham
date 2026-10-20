using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MarketOverviewController : MonoBehaviour
{
    public enum SortOption
    {
        NameAZ,             // Name: A to Z
        NameZA,             // Name: Z to A
        PriceHighToLow,     // Price: High to Low
        PriceLowToHigh,     // Price: Low to High
        ChangeHighToLow,    // % Change: Highest Gainers First
        ChangeLowToHigh,    // % Change: Highest Losers First
        VolumeHighToLow,    // Volume: Highest to Lowest
        MarketCapHighToLow  // Market Cap: Highest to Lowest
    }

    [Header("CSV Data Settings")]
    [Tooltip("Assign CSV text files for all 45 stocks. Name files as 'SYMBOL.csv' (e.g., AADI.csv)")]
    public TextAsset[] csvFiles;
    
    [Header("Day Selection")]
    public int currentDayIndex = 1; // Updated automatically by DaySimulationManager
    public TMP_Text dayHeaderLabel; // Reference to "Day 2" header text in top right

    [Header("UI Prefab & Container")]
    public GameObject stockRowPrefab; // Row Prefab containing UI text components and 'See More' button
    public Transform rowContainer;   // Content transform inside ScrollRect ScrollView

    [Header("Sorting Controls")]
    public TMP_Dropdown sortDropdown;
    public SortOption currentSortOption = SortOption.NameAZ;

    [Header("Stock Metadata Mock")]
    [Tooltip("Default total outstanding shares used to calculate Market Cap if not present in CSV")]
    public float defaultTotalShares = 5000000000f; // 5 Billion shares placeholder

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

        // Setup dropdown listener if assigned
        if (sortDropdown != null)
        {
            sortDropdown.onValueChanged.RemoveAllListeners();
            sortDropdown.onValueChanged.AddListener(OnSortDropdownChanged);
        }

        // Load data for initial persistent day
        if (DaySimulationManager.Instance != null)
        {
            UpdateMarketOverview(DaySimulationManager.Instance.currentDay);
        }
        else
        {
            UpdateMarketOverview(currentDayIndex);
        }
    }

    /// <summary>
    /// Called automatically whenever DaySimulationManager changes the global simulation day.
    /// </summary>
    private void HandleDayChanged(int newDay)
    {
        UpdateMarketOverview(newDay);
    }

    /// <summary>
    /// Loads, sorts, and updates all 45 stocks for the specified trading day.
    /// </summary>
    public void UpdateMarketOverview(int dayIndex)
    {
        currentDayIndex = Mathf.Max(1, dayIndex);
        if (dayHeaderLabel != null)
        {
            dayHeaderLabel.text = $"Day {currentDayIndex}";
        }

        currentMarketData.Clear();

        // 1. Process and parse each CSV file
        foreach (TextAsset csvAsset in csvFiles)
        {
            if (csvAsset == null) continue;

            string stockSymbol = csvAsset.name; // Uses file name (e.g. "AADI") as stock ticker
            StockRowData data = ParseCSVForDay(csvAsset.text, stockSymbol, currentDayIndex);

            if (data != null)
            {
                currentMarketData.Add(data);
            }
        }

        // 2. Apply sorting before displaying UI
        ApplySort();

        // 3. Render UI rows
        RebuildRowUI();
    }

    /// <summary>
    /// Sorts currentMarketData list based on currentSortOption.
    /// </summary>
    public void ApplySort()
    {
        switch (currentSortOption)
        {
            case SortOption.NameAZ:
                currentMarketData = currentMarketData.OrderBy(s => s.stockSymbol).ToList();
                break;
            case SortOption.NameZA:
                currentMarketData = currentMarketData.OrderByDescending(s => s.stockSymbol).ToList();
                break;
            case SortOption.PriceHighToLow:
                currentMarketData = currentMarketData.OrderByDescending(s => s.price).ToList();
                break;
            case SortOption.PriceLowToHigh:
                currentMarketData = currentMarketData.OrderBy(s => s.price).ToList();
                break;
            case SortOption.ChangeHighToLow:
                currentMarketData = currentMarketData.OrderByDescending(s => s.changePercent).ToList();
                break;
            case SortOption.ChangeLowToHigh:
                currentMarketData = currentMarketData.OrderBy(s => s.changePercent).ToList();
                break;
            case SortOption.VolumeHighToLow:
                currentMarketData = currentMarketData.OrderByDescending(s => s.volume).ToList();
                break;
            case SortOption.MarketCapHighToLow:
                currentMarketData = currentMarketData.OrderByDescending(s => s.marketCap).ToList();
                break;
        }
    }

    /// <summary>
    /// Clears container and instantiates sorted row prefabs.
    /// </summary>
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

    /// <summary>
    /// Call this when changing dropdown filter selection.
    /// </summary>
    public void OnSortDropdownChanged(int index)
    {
        currentSortOption = (SortOption)index;
        ApplySort();
        RebuildRowUI();
    }

    // Public helper methods for UI buttons
    public void SetSortMode(int sortOptionIndex) => OnSortDropdownChanged(sortOptionIndex);
    public void SortByNameAZ() => OnSortDropdownChanged((int)SortOption.NameAZ);
    public void SortByPriceHighToLow() => OnSortDropdownChanged((int)SortOption.PriceHighToLow);
    public void SortByChangeHighToLow() => OnSortDropdownChanged((int)SortOption.ChangeHighToLow);

    /// <summary>
    /// Parses a standard stock CSV file and extracts data for a specific day row.
    /// </summary>
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
        
        // Pass current day from DaySimulationManager if available
        int activeDay = DaySimulationManager.Instance != null ? DaySimulationManager.Instance.currentDay : currentDayIndex;
        PlayerPrefs.SetInt("SelectedDayIndex", activeDay);
        PlayerPrefs.Save();

        SceneManager.LoadScene(detailSceneName);
    }

    // Method to call from UI Next/Prev buttons if using DaySimulationManager directly
    public void NextDay()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.NextDay(); // Assuming DaySimulationManager has a NextDay method
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