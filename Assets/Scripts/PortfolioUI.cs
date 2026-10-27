using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Globalization;

public class PortfolioUI : MonoBehaviour
{
    [Header("Account Summary")]
    public TMP_Text cashText;
    public TMP_Text totalNetWorthText;
    public TMP_Text totalProfitLossText;       // Added for Total P/L
    public TMP_Text profitLossPercentageText; // Added for P/L Percentage

    [Header("Holdings List")]
    public Transform holdingsContainer;          
    public GameObject holdingRowPrefab;        

    [Header("Dataset References for Pricing Lookup")]
    public List<TextAsset> csvDatasets = new List<TextAsset>(); 

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
        float totalHoldingsValue = 0f;

        foreach (Transform child in holdingsContainer)
        {
            Destroy(child.gameObject);
        }

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

        // --- CORRECTED Overall P/L Calculations ---
        float totalInvestedCostSum = 0f;
        foreach (var kvp in PortfolioManager.Instance.GetAllHoldings())
        {
            totalInvestedCostSum += kvp.Value.totalInvestedCost;
        }

        // Access startingCash via PortfolioManager.Instance
        float initialCash = PortfolioManager.Instance.startingCash;
        float totalProfitLoss = netWorth - initialCash;
        float profitLossPercentage = initialCash > 0f ? (totalProfitLoss / initialCash) * 100f : 0f;

        // --- UI Text Assignments ---
        if (cashText != null) cashText.text = $"Rp{cash:N2}";
        if (totalNetWorthText != null) totalNetWorthText.text = $"Rp{netWorth:N2}";
        
        if (totalProfitLossText != null)
        {
            string sign = totalProfitLoss >= 0 ? "+" : "";
            string color = totalProfitLoss >= 0 ? "green" : "red";
            totalProfitLossText.text = $"<color={color}>{sign}Rp{totalProfitLoss:N2}</color>";
        }

        if (profitLossPercentageText != null)
        {
            string sign = profitLossPercentage >= 0 ? "+" : "";
            string color = profitLossPercentage >= 0 ? "green" : "red";
            profitLossPercentageText.text = $"<color={color}>{sign}{profitLossPercentage:F2}%</color>";
        }
    }

    private void GetMarketData(string datasetName, out float latestPrice, out float change24h)
    {
        latestPrice = 0f;
        change24h = 0f;

        string searchName = datasetName.Trim().ToLower().Replace(".csv", "").Replace("_csv", "");
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
}