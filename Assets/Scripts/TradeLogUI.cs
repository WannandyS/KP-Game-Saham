using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TradeLogUI : MonoBehaviour
{
    [Header("Scroll Container")]
    public Transform dayTableContainer;  

    [Header("Prefabs")]
    public GameObject dayTablePrefab;   
    public GameObject tradeRowPrefab;     

    private void OnEnable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated += RefreshLog;
        }

        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged += HandleDaySimulationChanged;
        }

        RefreshLog();
    }

    private void OnDisable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated -= RefreshLog;
        }

        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDaySimulationChanged;
        }
    }

    private void HandleDaySimulationChanged(int currentSimDay)
    {
        RefreshLog();
    }

    public void RefreshLog()
    {
        if (PortfolioManager.Instance == null || dayTableContainer == null || dayTablePrefab == null || tradeRowPrefab == null) return;

        foreach (Transform child in dayTableContainer)
        {
            Destroy(child.gameObject);
        }

        List<PortfolioManager.TransactionRecord> allHistory = PortfolioManager.Instance.GetTransactionHistory();

        if (allHistory == null || allHistory.Count == 0) return;

        // Kelompokkan per hari, hari terbaru di paling atas
        var groupedByDay = allHistory
            .GroupBy(t => t.dayIndex)
            .OrderByDescending(g => g.Key); 

        foreach (var dayGroup in groupedByDay)
        {
            int dayNumber = dayGroup.Key;
            List<PortfolioManager.TransactionRecord> dayHistory = dayGroup.ToList();

            GameObject dayTableObj = Instantiate(dayTablePrefab, dayTableContainer);
            
            TMP_Text dayHeader = FindDayHeaderText(dayTableObj);
            if (dayHeader != null)
            {
                dayHeader.text = $"Day {dayNumber}";
            }

            Transform rowContainer = FindRowContainerInTable(dayTableObj);

            // Balik urutan transaksi di hari ini agar transaksi terbaru selalu muncul di baris paling atas Panel
            dayHistory.Reverse();

            List<PortfolioManager.TransactionRecord> buyRecords = new List<PortfolioManager.TransactionRecord>(dayHistory.FindAll(t => t.isBuy));
            List<PortfolioManager.TransactionRecord> sellRecords = new List<PortfolioManager.TransactionRecord>(dayHistory.FindAll(t => !t.isBuy));

            while (buyRecords.Count > 0 || sellRecords.Count > 0)
            {
                PortfolioManager.TransactionRecord buyToUse = null;
                PortfolioManager.TransactionRecord sellToUse = null;

                if (sellRecords.Count > 0)
                {
                    sellToUse = sellRecords[0];
                    sellRecords.RemoveAt(0);

                    buyToUse = buyRecords.FirstOrDefault(b => b.stockSymbol == sellToUse.stockSymbol);
                    if (buyToUse != null)
                    {
                        buyRecords.Remove(buyToUse);
                    }
                    else
                    {
                        buyToUse = allHistory.LastOrDefault(b => b.isBuy && b.stockSymbol == sellToUse.stockSymbol && b.dayIndex <= dayNumber);
                    }
                }
                else if (buyRecords.Count > 0)
                {
                    buyToUse = buyRecords[0];
                    buyRecords.RemoveAt(0);
                }

                // Semua transaksi pada hari yang sama dimasukkan ke rowContainer (Panel) yang sama
                GameObject rowObj = Instantiate(tradeRowPrefab, rowContainer);
                TradeLogRowUI rowUI = rowObj.GetComponent<TradeLogRowUI>();

                if (rowUI != null)
                {
                    rowUI.InitEmptyRow();

                    if (buyToUse != null && buyToUse.dayIndex == dayNumber)
                    {
                        rowUI.SetupBuyColumns(
                            buyToUse.lots,
                            buyToUse.stockSymbol,
                            buyToUse.pricePerShare,
                            buyToUse.grossAmount,
                            buyToUse.feeAmount,
                            buyToUse.totalAmount
                        );
                    }

                    if (sellToUse != null)
                    {
                        float totalSellRevenue = sellToUse.totalAmount;
                        float totalBuyCost = 0f;

                        if (sellToUse.costBasisPerShare > 0f)
                        {
                            int totalSellShares = sellToUse.lots * PortfolioManager.SHARES_PER_LOT;
                            float grossBuyCost = totalSellShares * sellToUse.costBasisPerShare;
                            float buyFeeRate = PortfolioManager.Instance != null ? PortfolioManager.Instance.buyFeeRate : 0.002f;
                            float proportionalBuyFee = grossBuyCost * buyFeeRate;
                            totalBuyCost = grossBuyCost + proportionalBuyFee;
                        }
                        else if (buyToUse != null)
                        {
                            int totalBuyShares = buyToUse.lots * PortfolioManager.SHARES_PER_LOT;
                            float costPerShare = totalBuyShares > 0 ? buyToUse.totalAmount / totalBuyShares : 0f;
                            int totalSellShares = sellToUse.lots * PortfolioManager.SHARES_PER_LOT;
                            totalBuyCost = costPerShare * totalSellShares;
                        }
                        else
                        {
                            float estimatedGrossBuy = sellToUse.lots * PortfolioManager.SHARES_PER_LOT * sellToUse.pricePerShare;
                            float estimatedBuyFee = estimatedGrossBuy * 0.002f;
                            totalBuyCost = estimatedGrossBuy + estimatedBuyFee;
                        }

                        float gainLoss = totalSellRevenue - totalBuyCost;
                        float gainLossPercent = totalBuyCost > 0f ? (gainLoss / totalBuyCost) * 100f : 0f;

                        rowUI.SetupSellColumns(
                            sellToUse.lots,
                            sellToUse.stockSymbol,
                            sellToUse.pricePerShare,
                            sellToUse.grossAmount,
                            sellToUse.feeAmount,
                            sellToUse.totalAmount,
                            gainLoss,
                            gainLossPercent
                        );
                    }
                }
            }

            // Rebuild layout rowContainer terlebih dahulu agar ukuran tiap baris valid sebelum BuildBorders()
            if (rowContainer is RectTransform rowContainerRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rowContainerRect);
            }

            TableBorder tableBorder = dayTableObj.GetComponent<TableBorder>();
            if (tableBorder != null)
            {
                tableBorder.BuildBorders();
            }
        }

        Canvas.ForceUpdateCanvases();
        if (dayTableContainer is RectTransform rectTransform)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, 0f);
        }
    }

    private TMP_Text FindDayHeaderText(GameObject dayTableObj)
    {
        Transform target = dayTableObj.transform.Find("DayHistory");
        if (target != null) return target.GetComponent<TMP_Text>();

        Transform headerArea = dayTableObj.transform.Find("Header");
        if (headerArea == null) headerArea = dayTableObj.transform.Find("HeaderArea");
        if (headerArea != null)
        {
            Transform nestedTarget = headerArea.Find("DayHistory");
            if (nestedTarget != null) return nestedTarget.GetComponent<TMP_Text>();
        }

        return dayTableObj.GetComponentInChildren<TMP_Text>();
    }

    private Transform FindRowContainerInTable(GameObject dayTableObj)
    {
        Transform container = dayTableObj.transform.Find("Panel");
        if (container == null) container = dayTableObj.transform.Find("RowContainer");
        return container != null ? container : dayTableObj.transform;
    }
}