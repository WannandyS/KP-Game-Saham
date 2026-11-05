using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class TradeLogUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform rowContainer;          // The Vertical Layout Group holding transaction rows
    public GameObject tradeRowPrefab;     // Prefab with TradeLogRowUI script

    [Header("Day Navigation Controls")]
    public Button nextDayButton;
    public Button prevDayButton;
    public TMP_Text dayIndicatorText;     // Shows "Day 1", "Day 2", etc.

    private int viewingDay = 1;

    private void OnEnable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated += RefreshLog;
        }

        if (DaySimulationManager.Instance != null)
        {
            // Always default to the current active simulation day when opening the log
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
        // Optional: if you want the log to automatically follow the simulation when time advances, 
        // uncomment the line below. Otherwise, it remembers what day you were inspecting.
        // viewingDay = currentSimDay;
        
        RefreshLog();
    }

    public void RefreshLog()
    {
        if (PortfolioManager.Instance == null || rowContainer == null || tradeRowPrefab == null) return;

        // Ensure viewingDay doesn't exceed the current simulation day
        int currentSimDay = DaySimulationManager.Instance != null ? DaySimulationManager.Instance.currentDay : 1;
        if (viewingDay > currentSimDay)
        {
            viewingDay = currentSimDay;
        }

        // Clear existing rows
        foreach (Transform child in rowContainer)
        {
            Destroy(child.gameObject);
        }

        List<PortfolioManager.TransactionRecord> allHistory = PortfolioManager.Instance.GetTransactionHistory();

        // Filter transactions for the day we are currently viewing
        List<PortfolioManager.TransactionRecord> dayHistory = allHistory.FindAll(t => t.dayIndex == viewingDay);

        foreach (var record in dayHistory)
        {
            GameObject rowObj = Instantiate(tradeRowPrefab, rowContainer);
            TradeLogRowUI rowUI = rowObj.GetComponent<TradeLogRowUI>();

            if (rowUI != null)
            {
                if (record.isBuy)
                {
                    rowUI.SetupBuyRow(record.lots, record.stockSymbol, record.pricePerShare, record.grossAmount, record.feeAmount, record.totalAmount);
                }
                else
                {
                    float gainLoss = record.totalAmount - (record.grossAmount * 0.997f); 
                    float gainLossPercent = 0f; 

                    rowUI.SetupSellRow(record.lots, record.stockSymbol, record.pricePerShare, record.grossAmount, record.feeAmount, record.totalAmount, gainLoss, gainLossPercent);
                }
            }
        }

        // Update Day Indicator Label
        if (dayIndicatorText != null)
        {
            dayIndicatorText.text = $"Day {viewingDay}";
        }

        // Manage Button Interactivity
        if (prevDayButton != null)
        {
            prevDayButton.interactable = (viewingDay > 1);
        }

        if (nextDayButton != null)
        {
            // Capped at currentSimDay instead of totalDays
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