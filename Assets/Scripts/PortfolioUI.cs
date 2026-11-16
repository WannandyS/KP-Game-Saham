using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using System.Globalization;

public class PortfolioUI : MonoBehaviour
{
    public enum SortColumn
    {
        Name,
        Price,
        Value,
        AverageBuyPrice,
        Volume,
        TransactionTotal,
        GainsLoss,
        ChangePercent
    }

    [Serializable]
    public struct ColumnHeaderUI
    {
        public SortColumn columnType;
        public TMP_Text headerText;
        [HideInInspector] public string originalText;
    }

    [Header("Account Summary")]
    public TMP_Text cashText;
    public TMP_Text totalNetWorthText;
    public TMP_Text totalProfitLossText;
    public TMP_Text profitLossPercentageText;
    public TMP_Text equityText;

    [Header("Holdings List")]
    public Transform holdingsContainer;
    public GameObject holdingRowPrefab;

    [Header("Sorting State & UI")]
    public SortColumn currentSortColumn = SortColumn.Name;
    public bool isAscending = true;
    public ColumnHeaderUI[] columnHeaders;

    private readonly string arrowUp = " ▲";
    private readonly string arrowDown = " ▼";

    [Header("Dataset References for Pricing Lookup")]
    public List<TextAsset> csvDatasets = new List<TextAsset>();

    private List<PortfolioItemData> currentPortfolioData = new List<PortfolioItemData>();

    private void Awake()
    {
        // Store original header texts before appending sorting arrows
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
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated += RefreshUI;
        }

        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated -= RefreshUI;
        }

        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
        }
    }

    private void HandleDayChanged(int day)
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (PortfolioManager.Instance == null) return;

        float cash = PortfolioManager.Instance.currentCash;
        float totalHoldingsGrossValue = 0f;

        currentPortfolioData.Clear();

        foreach (var kvp in PortfolioManager.Instance.GetAllHoldings())
        {
            var pos = kvp.Value;
            string symbol = pos.stockSymbol;

            GetMarketData(symbol, out float currentPrice, out float change24h);

            if (currentPrice <= 0f) currentPrice = pos.AverageBuyPrice;

            float grossValue = pos.totalShares * currentPrice;
            float profitLoss = grossValue - pos.totalInvestedCost;
            float profitLossPercent = pos.totalInvestedCost > 0f ? (profitLoss / pos.totalInvestedCost) * 100f : 0f;
            totalHoldingsGrossValue += grossValue;

            currentPortfolioData.Add(new PortfolioItemData
            {
                symbol = symbol,
                averageBuyPrice = pos.AverageBuyPrice,
                totalInvested = pos.totalInvestedCost,
                totalShares = pos.totalShares,
                currentPrice = currentPrice,
                totalValue = grossValue,
                profitLoss = profitLoss,
                profitLossPercent = profitLossPercent,
                change24h = change24h
            });
        }

        // Apply sorting and build UI rows
        ApplySort();
        RebuildRowUI();
        UpdateHeaderVisuals();

        // Update Account Summary Header
        float netWorth = cash + totalHoldingsGrossValue;
        float initialCash = PortfolioManager.Instance.startingCash;
        float totalProfitLoss = netWorth - initialCash;
        float profitLossPercentage = initialCash > 0f ? (totalProfitLoss / initialCash) * 100f : 0f;

        if (cashText != null) cashText.text = $"{cash:N0}";
        if (totalNetWorthText != null) totalNetWorthText.text = $"{netWorth:N0}";
        if (equityText != null) equityText.text = $"{totalHoldingsGrossValue:N0}";

        if (totalProfitLossText != null)
        {
            string sign = totalProfitLoss >= 0 ? "+" : "";
            string color = totalProfitLoss >= 0 ? "green" : "red";
            totalProfitLossText.text = $"<color={color}>{sign}{totalProfitLoss:N0}</color>";
        }

        if (profitLossPercentageText != null)
        {
            string sign = profitLossPercentage >= 0 ? "+" : "";
            string color = profitLossPercentage >= 0 ? "green" : "red";
            profitLossPercentageText.text = $"<color={color}>{sign}{profitLossPercentage:F2}%</color>";
        }
    }

    /// <summary>
    /// Called from OnClick() of Column Header buttons in UI
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
            isAscending = clickedColumn == SortColumn.Name; // Default ascending for Name, descending for numbers if preferred
        }

        ApplySort();
        RebuildRowUI();
        UpdateHeaderVisuals();
    }

    private void ApplySort()
    {
        switch (currentSortColumn)
        {
            case SortColumn.Name:
                currentPortfolioData = isAscending
                    ? currentPortfolioData.OrderBy(s => s.symbol).ToList()
                    : currentPortfolioData.OrderByDescending(s => s.symbol).ToList();
                break;
            case SortColumn.Price:
                currentPortfolioData = isAscending
                    ? currentPortfolioData.OrderBy(s => s.currentPrice).ToList()
                    : currentPortfolioData.OrderByDescending(s => s.currentPrice).ToList();
                break;
            case SortColumn.Value:
                currentPortfolioData = isAscending
                    ? currentPortfolioData.OrderBy(s => s.totalValue).ToList()
                    : currentPortfolioData.OrderByDescending(s => s.totalValue).ToList();
                break;
            case SortColumn.AverageBuyPrice:
                currentPortfolioData = isAscending
                    ? currentPortfolioData.OrderBy(s => s.averageBuyPrice).ToList()
                    : currentPortfolioData.OrderByDescending(s => s.averageBuyPrice).ToList();
                break;
            case SortColumn.Volume:
                currentPortfolioData = isAscending
                    ? currentPortfolioData.OrderBy(s => s.totalShares).ToList()
                    : currentPortfolioData.OrderByDescending(s => s.totalShares).ToList();
                break;
            case SortColumn.TransactionTotal:
                currentPortfolioData = isAscending
                    ? currentPortfolioData.OrderBy(s => s.totalInvested).ToList()
                    : currentPortfolioData.OrderByDescending(s => s.totalInvested).ToList();
                break;
            case SortColumn.GainsLoss:
                currentPortfolioData = isAscending
                    ? currentPortfolioData.OrderBy(s => s.profitLoss).ToList()
                    : currentPortfolioData.OrderByDescending(s => s.profitLoss).ToList();
                break;
            case SortColumn.ChangePercent:
                currentPortfolioData = isAscending
                    ? currentPortfolioData.OrderBy(s => s.profitLossPercent).ToList()
                    : currentPortfolioData.OrderByDescending(s => s.profitLossPercent).ToList();
                break;
        }
    }

    private void UpdateHeaderVisuals()
    {
        if (columnHeaders == null) return;

        for (int i = 0; i < columnHeaders.Length; i++)
        {
            if (columnHeaders[i].headerText == null) continue;

            columnHeaders[i].headerText.text = columnHeaders[i].originalText;

            if (columnHeaders[i].columnType == currentSortColumn)
            {
                string arrow = isAscending ? arrowUp : arrowDown;
                columnHeaders[i].headerText.text = columnHeaders[i].originalText + arrow;
            }
        }
    }

    private void RebuildRowUI()
    {
        if (holdingsContainer == null) return;

        foreach (Transform child in holdingsContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var data in currentPortfolioData)
        {
            if (holdingRowPrefab != null)
            {
                GameObject rowObj = Instantiate(holdingRowPrefab, holdingsContainer);
                PortfolioHoldingRow rowComponent = rowObj.GetComponent<PortfolioHoldingRow>();

                if (rowComponent != null)
                {
                    rowComponent.SetupRow(
                        data.symbol,
                        data.averageBuyPrice,
                        data.totalInvested,
                        data.change24h,
                        data.totalShares / PortfolioManager.SHARES_PER_LOT, // Lots owned
                        data.currentPrice,
                        data.totalValue,
                        data.profitLoss
                    );
                }
            }
        }
    }

    private void GetMarketData(string stockSymbol, out float latestPrice, out float change24h)
    {
        latestPrice = 0f;
        change24h = 0f;

        string searchName = stockSymbol.Trim().ToLower().Replace(".csv", "").Replace("_csv", "");
        TextAsset targetCsv = csvDatasets.Find(csv => csv != null && csv.name.Trim().ToLower().Replace(".csv", "").Replace("_csv", "") == searchName);

        if (targetCsv == null) return;

        string[] lines = targetCsv.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return;

        string[] headers = lines[0].Split(',');
        int closeIndex = FindColumnIndex(headers, "Close");
        if (closeIndex == -1) return;

        List<float> closes = new List<float>();
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length > closeIndex)
            {
                string cleanVal = values[closeIndex].Trim().Trim('"');
                if (float.TryParse(cleanVal, NumberStyles.Float, CultureInfo.InvariantCulture, out float close))
                {
                    closes.Add(close);
                }
            }
        }

        if (closes.Count > 0)
        {
            int currentDayLimit = (DaySimulationManager.Instance != null) ? Mathf.Min(DaySimulationManager.Instance.currentDay, closes.Count) : closes.Count;

            if (currentDayLimit > 0)
            {
                latestPrice = closes[currentDayLimit - 1];

                if (currentDayLimit > 1)
                {
                    float prevPrice = closes[currentDayLimit - 2];
                    change24h = ((latestPrice - prevPrice) / prevPrice) * 100f;
                }
            }
        }
    }

    private int FindColumnIndex(string[] headers, string columnName)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            if (string.Equals(headers[i].Trim().Trim('"'), columnName, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private class PortfolioItemData
    {
        public string symbol;
        public float averageBuyPrice;
        public float totalInvested;
        public int totalShares;
        public float currentPrice;
        public float totalValue;
        public float profitLoss;
        public float profitLossPercent;
        public float change24h;
    }
}