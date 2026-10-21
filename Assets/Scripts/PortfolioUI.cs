using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Globalization;

public class PortfolioUI : MonoBehaviour
{
    [Header("Account Summary")]
    public TMP_Text cashText;
    public TMP_Text totalNetWorthText;

    [Header("Holdings List")]
    public Transform holdingsContainer;          // The Vertical Layout Group holding the rows
    public GameObject holdingRowPrefab;        // Prefab with Horizontal Layout Group + PortfolioHoldingRow script

    [Header("Dataset References for Pricing Lookup")]
    public List<TextAsset> csvDatasets = new List<TextAsset>(); // Assign your stock CSVs here too

    private void OnEnable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated += RefreshUI;
        }

        // Subscribe to day changes so portfolio updates live with the simulation
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
        RefreshUI(); // Automatically recalculate and redraw rows when day changes
    }

    public void RefreshUI()
    {
        if (PortfolioManager.Instance == null) return;

        float cash = PortfolioManager.Instance.currentCash;
        float totalHoldingsValue = 0f;

        // Clear existing rows
        foreach (Transform child in holdingsContainer)
        {
            Destroy(child.gameObject);
        }

        // Loop through all current holdings and build rows
        foreach (var kvp in PortfolioManager.Instance.GetAllHoldings())
        {
            var pos = kvp.Value;
            string ticker = pos.datasetName;

            GetMarketData(ticker, out float currentPrice, out float change24h);

            if (currentPrice <= 0f) currentPrice = pos.AverageBuyPrice;

            float totalValue = pos.totalShares * currentPrice;
            float profitLoss = totalValue - pos.totalInvestedCost;
            totalHoldingsValue += totalValue;

            if (holdingRowPrefab != null && holdingsContainer != null)
            {
                GameObject rowObj = Instantiate(holdingRowPrefab, holdingsContainer);
                PortfolioHoldingRow rowComponent = rowObj.GetComponent<PortfolioHoldingRow>();

                if (rowComponent != null)
                {
                    rowComponent.SetupRow(
                        ticker,
                        pos.AverageBuyPrice,
                        pos.totalInvestedCost,
                        change24h,
                        pos.totalShares,
                        currentPrice,
                        totalValue,
                        profitLoss
                    );
                }
            }
        }

        float netWorth = cash + totalHoldingsValue;
        if (cashText != null) cashText.text = $"Cash: Rp{cash:N2}";
        if (totalNetWorthText != null) totalNetWorthText.text = $"Net Worth: Rp{netWorth:N2}";

        // --- ADD THIS TO FORCE UI LAYOUT TO REFRESH PROPERLY ---
        if (holdingsContainer != null)
        {
            Canvas.ForceUpdateCanvases();
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(holdingsContainer as RectTransform);
        }
    }

    // Helper to extract the latest price and 24h change percentage from the matching CSV file
    private void GetMarketData(string datasetName, out float latestPrice, out float change24h)
    {
        latestPrice = 0f;
        change24h = 0f;

        // Clean name to match asset names securely (removes case sensitivity or extension issues)
        string searchName = datasetName.Trim().ToLower();

        TextAsset targetCsv = csvDatasets.Find(csv => csv != null && csv.name.Trim().ToLower() == searchName);
        
        if (targetCsv == null)
        {
            Debug.LogWarning($"PortfolioUI: Could not find CSV dataset matching ticker '{datasetName}'. Check your csvDatasets list assignment!");
            return;
        }

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
}