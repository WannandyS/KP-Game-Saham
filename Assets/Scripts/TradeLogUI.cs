using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class TradeLogUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform rowContainer;          // Vertical Layout Group untuk menampung baris
    public GameObject tradeRowPrefab;     // Prefab dengan skrip TradeLogRowUI

    [Header("Day Navigation Controls")]
    public Button nextDayButton;
    public Button prevDayButton;
    public TMP_Text dayIndicatorText;     // Label "Day 1", "Day 2", dll.

    private int viewingDay = 1;

    private void OnEnable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated += RefreshLog;
        }

        if (DaySimulationManager.Instance != null)
        {
            viewingDay = DaySimulationManager.Instance.currentDay;
            DaySimulationManager.Instance.OnDayChanged += HandleDaySimulationChanged;
        }

        if (nextDayButton != null) nextDayButton.onClick.AddListener(NextDay);
        if (prevDayButton != null) prevDayButton.onClick.AddListener(PrevDay);

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

        if (nextDayButton != null) nextDayButton.onClick.RemoveListener(NextDay);
        if (prevDayButton != null) prevDayButton.onClick.RemoveListener(PrevDay);
    }

    private void HandleDaySimulationChanged(int currentSimDay)
    {
        RefreshLog();
    }

    public void RefreshLog()
    {
        if (PortfolioManager.Instance == null || rowContainer == null || tradeRowPrefab == null) return;

        int currentSimDay = DaySimulationManager.Instance != null ? DaySimulationManager.Instance.currentDay : 1;
        if (viewingDay > currentSimDay)
        {
            viewingDay = currentSimDay;
        }

        // Hapus semua baris lama di UI
        foreach (Transform child in rowContainer)
        {
            Destroy(child.gameObject);
        }

        List<PortfolioManager.TransactionRecord> allHistory = PortfolioManager.Instance.GetTransactionHistory();

        // Ambil transaksi untuk hari yang sedang dilihat
        List<PortfolioManager.TransactionRecord> dayHistory = allHistory
            .FindAll(t => t.dayIndex == viewingDay)
            .ToList();

        List<PortfolioManager.TransactionRecord> buyRecords = new List<PortfolioManager.TransactionRecord>(dayHistory.FindAll(t => t.isBuy));
        List<PortfolioManager.TransactionRecord> sellRecords = new List<PortfolioManager.TransactionRecord>(dayHistory.FindAll(t => !t.isBuy));

        // Pasangkan secara akurat berdasarkan Ticker/Symbol yang sama
        while (buyRecords.Count > 0 || sellRecords.Count > 0)
        {
            PortfolioManager.TransactionRecord buyToUse = null;
            PortfolioManager.TransactionRecord sellToUse = null;

            if (sellRecords.Count > 0)
            {
                sellToUse = sellRecords[0];
                sellRecords.RemoveAt(0);

                // 1. Cari di buyRecords hari yang sama terlebih dahulu
                buyToUse = buyRecords.FirstOrDefault(b => b.stockSymbol == sellToUse.stockSymbol);
                if (buyToUse != null)
                {
                    buyRecords.Remove(buyToUse);
                }
                else
                {
                    // 2. Jika tidak ada di hari yang sama (misal beli kemarin, jual hari ini), 
                    // cari di SELURUH riwayat transaksi (allHistory) untuk simbol yang sama sebelum hari ini!
                    buyToUse = allHistory.FirstOrDefault(b => b.isBuy && b.stockSymbol == sellToUse.stockSymbol && b.dayIndex <= viewingDay);
                }
            }
            else if (buyRecords.Count > 0)
            {
                buyToUse = buyRecords[0];
                buyRecords.RemoveAt(0);
            }

            GameObject rowObj = Instantiate(tradeRowPrefab, rowContainer);
            TradeLogRowUI rowUI = rowObj.GetComponent<TradeLogRowUI>();

            if (rowUI != null)
            {
                rowUI.InitEmptyRow();

                // Isi data BUY jika transaksi buy terjadi pada hari ini yang sedang dilihat
                if (buyToUse != null && buyToUse.dayIndex == viewingDay)
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

                // Isi data SELL jika ada dengan perhitungan P/L yang akurat
                if (sellToUse != null)
                {
                    float totalSellRevenue = sellToUse.totalAmount; // Net revenue setelah sell fee
                    float totalBuyCost = 0f;

                    if (buyToUse != null)
                    {
                        int totalBuyShares = buyToUse.lots * PortfolioManager.SHARES_PER_LOT;
                        float costPerShare = totalBuyShares > 0 ? buyToUse.totalAmount / totalBuyShares : 0f;
                        int totalSellShares = sellToUse.lots * PortfolioManager.SHARES_PER_LOT;
                        totalBuyCost = costPerShare * totalSellShares;
                    }
                    else
                    {
                        // Fallback mutlak jika tidak ada record buy sama sekali di seluruh history
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

        // Update Label Indikator Hari
        if (dayIndicatorText != null)
        {
            dayIndicatorText.text = $"Day {viewingDay}";
        }

        // Update Status Interaktif Tombol Navigasi
        if (prevDayButton != null)
        {
            prevDayButton.interactable = (viewingDay > 1);
        }

        if (nextDayButton != null)
        {
            nextDayButton.interactable = (viewingDay < currentSimDay);
        }
    }

    public void NextDay()
    {
        int currentSimDay = DaySimulationManager.Instance != null ? DaySimulationManager.Instance.currentDay : viewingDay + 1;
        if (viewingDay < currentSimDay)
        {
            viewingDay++;
            RefreshLog();
        }
    }

    public void PrevDay()
    {
        if (viewingDay > 1)
        {
            viewingDay--;
            RefreshLog();
        }
    }
}