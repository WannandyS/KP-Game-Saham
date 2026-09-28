using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class MarketOverviewController : MonoBehaviour
{
    [Header("CSV Data Settings")]
    [Tooltip("Assign CSV text files for all 45 stocks. Name files as 'SYMBOL.csv' (e.g., AADI.csv)")]
    public TextAsset[] csvFiles;
    
    [Header("Day Selection")]
    public int currentDayIndex = 1; // Day 1 = 1st trading row, Day 2 = 2nd trading row, etc.
    public TMP_Text dayHeaderLabel; // Reference to "Day 2" header text in top right

    [Header("UI Prefab & Container")]
    public GameObject stockRowPrefab; // Row Prefab containing UI text components and 'See More' button
    public Transform rowContainer;   // Content transform inside ScrollRect ScrollView

    [Header("Stock Metadata Mock")]
    [Tooltip("Default total outstanding shares used to calculate Market Cap if not present in CSV")]
    public float defaultTotalShares = 5000000000f; // 5 Billion shares placeholder

    [Header("Target Scene")]
    public string detailSceneName = "StockDetailScene";

    private List<StockRowData> currentMarketData = new List<StockRowData>();

    private void Start()
    {
        UpdateMarketOverview(currentDayIndex);
    }

    /// <summary>
    /// Loads and updates all 45 stocks for the specified trading day.
    /// </summary>
    public void UpdateMarketOverview(int dayIndex)
    {
        currentDayIndex = Mathf.Max(1, dayIndex);
        if (dayHeaderLabel != null)
        {
            dayHeaderLabel.text = $"Day {currentDayIndex}";
        }

        currentMarketData.Clear();

        // Clear existing instantiated UI rows
        foreach (Transform child in rowContainer)
        {
            Destroy(child.gameObject);
        }

        // Process each CSV file
        foreach (TextAsset csvAsset in csvFiles)
        {
            if (csvAsset == null) continue;

            string stockSymbol = csvAsset.name; // Uses file name (e.g. "AADI") as stock ticker
            StockRowData data = ParseCSVForDay(csvAsset.text, stockSymbol, currentDayIndex);

            if (data != null)
            {
                currentMarketData.Add(data);
                InstantiateRowUI(data);
            }
        }
    }

    /// <summary>
    /// Parses a standard stock CSV file and extracts data for a specific day row.
    /// </summary>
    private StockRowData ParseCSVForDay(string csvContent, string symbol, int day)
    {
        string[] lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return null; // Header only or empty

        // Header: Date, Adj Close, Close, High, Low, Open, Volume
        // Target index row (day 1 corresponds to line 1, day 2 to line 2, etc.)
        if (day >= lines.Length) day = lines.Length - 1; // Clamp to available data

        string[] currentCols = lines[day].Split(',');
        if (currentCols.Length < 7) return null;

        float currentClose = float.Parse(currentCols[2]); // Close price
        float currentVolume = float.Parse(currentCols[6]); // Volume

        // Calculate 24H Percentage Change relative to previous day
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

        // Required Formulae
        float calculatedValue = currentClose * currentVolume;
        float calculatedMarketCap = currentClose * defaultTotalShares; // Price * Total Outstanding Shares

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

    /// <summary>
    /// Instantiates and populates the UI Row prefab in the scrollable view.
    /// </summary>
    private void InstantiateRowUI(StockRowData data)
    {
        GameObject newRow = Instantiate(stockRowPrefab, rowContainer);
        StockRowUI rowUI = newRow.GetComponent<StockRowUI>();

        if (rowUI != null)
        {
            rowUI.SetupRow(data, OnDetailsButtonClicked);
        }
    }

    /// <summary>
    /// Handles click event for the "See More" detail button.
    /// </summary>
    private void OnDetailsButtonClicked(StockRowData data)
    {
        // Store selected stock symbol and date to pass to detail candlestick scene
        PlayerPrefs.SetString("SelectedStockSymbol", data.stockSymbol);
        PlayerPrefs.SetInt("SelectedDayIndex", currentDayIndex);
        PlayerPrefs.Save();

        // Load the Detail Scene showing Candlesticks & Volume
        SceneManager.LoadScene(detailSceneName);
    }

    // Call this from a UI Button or Slider to switch days dynamically
    public void NextDay() => UpdateMarketOverview(currentDayIndex + 1);
    public void PreviousDay() => UpdateMarketOverview(currentDayIndex - 1);
}