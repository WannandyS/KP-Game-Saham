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

        // Filter transaksi untuk hari yang sedang dilihat dan dibalik agar transaksi TERBARU diproses lebih dulu
        List<PortfolioManager.TransactionRecord> dayHistory = allHistory
            .FindAll(t => t.dayIndex == viewingDay)
            .AsEnumerable()
            .Reverse()
            .ToList();

        // Pisahkan daftar transaksi Buy dan Sell pada hari tersebut
        List<PortfolioManager.TransactionRecord> buyRecords = dayHistory.FindAll(t => t.isBuy);
        List<PortfolioManager.TransactionRecord> sellRecords = dayHistory.FindAll(t => !t.isBuy);

        // Pasangkan (Pair) Buy dan Sell secara rapat tanpa ada baris kosong yang melompat
        while (buyRecords.Count > 0 || sellRecords.Count > 0)
        {
            PortfolioManager.TransactionRecord buyToUse = null;
            PortfolioManager.TransactionRecord sellToUse = null;

            if (buyRecords.Count > 0)
            {
                buyToUse = buyRecords[0];
                buyRecords.RemoveAt(0);

                // Prioritas 1: Cari transaksi Sell dengan kode saham yang SAMA
                sellToUse = sellRecords.FirstOrDefault(s => s.stockSymbol == buyToUse.stockSymbol);

                if (sellToUse != null)
                {
                    sellRecords.Remove(sellToUse);
                }
                else if (sellRecords.Count > 0)
                {
                    // Prioritas 2: Jika kode saham beda, pasangkan dengan transaksi Sell pertama yang ada agar sebaris & rapat
                    sellToUse = sellRecords[0];
                    sellRecords.RemoveAt(0);
                }
            }
            else if (sellRecords.Count > 0)
            {
                // Jika hanya tersisa transaksi Sell
                sellToUse = sellRecords[0];
                sellRecords.RemoveAt(0);
            }

            // Buat 1 Baris UI
            GameObject rowObj = Instantiate(tradeRowPrefab, rowContainer);
            TradeLogRowUI rowUI = rowObj.GetComponent<TradeLogRowUI>();

            if (rowUI != null)
            {
                rowUI.InitEmptyRow();

                // Isi data BUY jika ada
                if (buyToUse != null)
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

                // Isi data SELL jika ada
                if (sellToUse != null)
                {
                    float gainLoss = sellToUse.totalAmount - (sellToUse.grossAmount * 0.997f); 
                    float gainLossPercent = 0f;

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