using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Globalization;

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

    [Serializable]
    public struct ColumnHeaderUI
    {
        public SortColumn columnType;
        public TMP_Text headerText;
        [HideInInspector] public string originalText; // Menyimpan teks dasar (misal: "SAHAM")
    }

    [Header("CSV Data Settings")]
    [Tooltip("Assign CSV text files for all stocks. Name files as 'SYMBOL.csv' (e.g., ADMR.csv)")]
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
    public ColumnHeaderUI[] columnHeaders; // Array penampung referensi header kolom

    // Indikator panah visual sorting
    private readonly string arrowUp = " ▲";
    private readonly string arrowDown = " ▼";

    [Header("Stock Metadata Mock")]
    public float defaultTotalShares = 5000000000f;

    [Header("Target Scene")]
    public string detailSceneName = "DetailPerusahaan";

    private List<StockRowData> currentMarketData = new List<StockRowData>();

    private void Awake()
    {
        // Menyimpan teks asli header sebelum ditambahkan panah indikator
        if (columnHeaders != null)
        {
            for (int i = 0; i < columnHeaders.Length; i++)
            {
                if (columnHeaders[i].headerText != null)
                {
                    columnHeaders[i].originalText = columnHeaders[i].headerText.text;
                }
            }
        }
    }

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

        if (csvFiles != null)
        {
            foreach (TextAsset csvAsset in csvFiles)
            {
                if (csvAsset == null) continue;

                string stockSymbol = csvAsset.name; // Nama file CSV sebagai Ticker
                StockRowData data = ParseCSVForDay(csvAsset.text, stockSymbol, currentDayIndex);

                if (data != null)
                {
                    currentMarketData.Add(data);
                }
            }
        }

        ApplySort();
        RebuildRowUI();
        UpdateHeaderVisuals();
    }

    /// <summary>
    /// Dipanggil dari OnClick() tombol Header Kolom di UI
    /// </summary>
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
        UpdateHeaderVisuals();
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

    private void UpdateHeaderVisuals()
    {
        if (columnHeaders == null) return;

        for (int i = 0; i < columnHeaders.Length; i++)
        {
            if (columnHeaders[i].headerText == null) continue;

            // Reset teks ke teks asli tanpa panah
            columnHeaders[i].headerText.text = columnHeaders[i].originalText;

            // Tambahkan panah indikator jika kolom sedang aktif di-sort
            if (columnHeaders[i].columnType == currentSortColumn)
            {
                string arrow = isAscending ? arrowUp : arrowDown;
                columnHeaders[i].headerText.text = columnHeaders[i].originalText + arrow;
            }
        }
    }

    private void RebuildRowUI()
    {
        if (rowContainer == null) return;

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

        // Baris 0 adalah Header: Date,Adj Close,Close,High,Low,Open,Volume,Market Cap
        if (day >= lines.Length) day = lines.Length - 1;

        string[] currentCols = lines[day].Split(',');
        if (currentCols.Length < 7) return null;

        // Gunakan CultureInfo.InvariantCulture untuk mengantisipasi desimal
        float currentClose = float.Parse(currentCols[2], CultureInfo.InvariantCulture);
        float currentVolume = float.Parse(currentCols[6], CultureInfo.InvariantCulture);

        float closePercentChange = 0f;
        if (day > 1)
        {
            string[] prevCols = lines[day - 1].Split(',');
            float prevClose = float.Parse(prevCols[2], CultureInfo.InvariantCulture);
            if (prevClose > 0)
            {
                closePercentChange = ((currentClose - prevClose) / prevClose) * 100f;
            }
        }

        float calculatedValue = currentClose * currentVolume;

        // DIBUAT DYNAMIC: Membaca Market Cap dari kolom ke-8 (index 7) jika tersedia di CSV
        float calculatedMarketCap = 0f;
        if (currentCols.Length >= 8 && float.TryParse(currentCols[7], NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedMarketCap))
        {
            calculatedMarketCap = parsedMarketCap;
        }
        else
        {
            // Fallback jika CSV lama tidak memiliki kolom Market Cap
            calculatedMarketCap = currentClose * defaultTotalShares;
        }

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
        if (stockRowPrefab == null || rowContainer == null) return;

        GameObject newRow = Instantiate(stockRowPrefab, rowContainer);
        StockRowUI rowUI = newRow.GetComponent<StockRowUI>();

        if (rowUI != null)
        {
            TextAsset priceCsv = GetCsvAssetBySymbol(data.stockSymbol);
            string companyName = SelectedStockData.GetCompanyName(data.stockSymbol);

            rowUI.SetupRow(data, priceCsv, companyName);
        }
    }

    private TextAsset GetCsvAssetBySymbol(string symbol)
    {
        if (csvFiles == null) return null;
        foreach (TextAsset csv in csvFiles)
        {
            if (csv != null && csv.name.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            {
                return csv;
            }
        }
        return null;
    }

    public void OnDetailsButtonClicked(StockRowData data)
    {
        if (data == null) return;

        SelectedStockData.StockTicker = data.stockSymbol;
        SelectedStockData.CompanyName = SelectedStockData.GetCompanyName(data.stockSymbol);
        SelectedStockData.PriceCsvAsset = GetCsvAssetBySymbol(data.stockSymbol);
        SelectedStockData.OpenOnGraphTab = false;

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