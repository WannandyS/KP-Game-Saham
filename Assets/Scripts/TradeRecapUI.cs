using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class TradeRecapUI : MonoBehaviour
{
    [Header("Panel & Tombol")]
    public GameObject recapPopUp;       // Drag objek RecapPopUp ke sini
    public Button openRecapButton;      // Drag tombol RECAP (di pojok kanan bawah) ke sini
    public Button closeButton;          // Drag tombol Close / CancelButton di dalam RecapPopUp ke sini

    [Header("Teks di Dalam RecapPopUp")]
    public TMP_Text titleText;          // Drag objek Title milik RecapPopUp ke sini
    public TMP_Text descriptionText;    // Drag objek Description milik RecapPopUp ke sini

    private void Start()
    {
        if (recapPopUp != null)
        {
            recapPopUp.SetActive(false);
        }

        if (openRecapButton != null)
        {
            openRecapButton.onClick.RemoveListener(OpenRecap);
            openRecapButton.onClick.AddListener(OpenRecap);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseRecap);
            closeButton.onClick.AddListener(CloseRecap);
        }
    }

    private void OnEnable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated += RefreshIfOpen;
        }

        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
        }
    }

    private void OnDisable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated -= RefreshIfOpen;
        }

        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
        }
    }

    private void HandleDayChanged(int newDay)
    {
        if (recapPopUp != null && recapPopUp.activeSelf)
        {
            UpdateRecapData(newDay);
        }
    }

    private void RefreshIfOpen()
    {
        if (recapPopUp != null && recapPopUp.activeSelf)
        {
            UpdateRecapData(GetCurrentDay());
        }
    }

    public void OpenRecap()
    {
        if (recapPopUp == null) return;

        UpdateRecapData(GetCurrentDay());
        recapPopUp.SetActive(true);
        recapPopUp.transform.SetAsLastSibling();
    }

    public void CloseRecap()
    {
        if (recapPopUp != null)
        {
            recapPopUp.SetActive(false);
        }
    }

    private int GetCurrentDay()
    {
        if (DaySimulationManager.Instance != null)
        {
            return DaySimulationManager.Instance.currentDay;
        }

        if (PortfolioManager.Instance != null)
        {
            var history = PortfolioManager.Instance.GetTransactionHistory();
            if (history != null && history.Count > 0)
            {
                return history.Max(t => t.dayIndex);
            }
        }

        return 1;
    }

    public void UpdateRecapData(int targetDay)
    {
        int buyCount = 0;
        int sellCount = 0;
        int totalBuyLots = 0;
        int totalSellLots = 0;
        float totalGainLoss = 0f;

        if (PortfolioManager.Instance != null)
        {
            List<PortfolioManager.TransactionRecord> allHistory = PortfolioManager.Instance.GetTransactionHistory();

            if (allHistory != null)
            {
                List<PortfolioManager.TransactionRecord> todayTransactions = allHistory
                    .Where(t => t.dayIndex == targetDay)
                    .ToList();

                buyCount = todayTransactions.Count(t => t.isBuy);
                sellCount = todayTransactions.Count(t => !t.isBuy);

                totalBuyLots = todayTransactions.Where(t => t.isBuy).Sum(t => t.lots);
                totalSellLots = todayTransactions.Where(t => !t.isBuy).Sum(t => t.lots);

                foreach (var sellRecord in todayTransactions.Where(t => !t.isBuy))
                {
                    float totalSellRevenue = sellRecord.totalAmount;
                    float totalBuyCost = 0f;

                    if (sellRecord.costBasisPerShare > 0f)
                    {
                        int totalSellShares = sellRecord.lots * PortfolioManager.SHARES_PER_LOT;
                        float grossBuyCost = totalSellShares * sellRecord.costBasisPerShare;
                        float buyFeeRate = PortfolioManager.Instance.buyFeeRate;
                        float proportionalBuyFee = grossBuyCost * buyFeeRate;
                        totalBuyCost = grossBuyCost + proportionalBuyFee;
                    }
                    else
                    {
                        var matchingBuy = allHistory.LastOrDefault(b =>
                            b.isBuy &&
                            b.stockSymbol == sellRecord.stockSymbol &&
                            b.dayIndex <= targetDay);

                        if (matchingBuy != null)
                        {
                            int totalBuyShares = matchingBuy.lots * PortfolioManager.SHARES_PER_LOT;
                            float costPerShare = totalBuyShares > 0 ? matchingBuy.totalAmount / totalBuyShares : 0f;
                            int totalSellShares = sellRecord.lots * PortfolioManager.SHARES_PER_LOT;
                            totalBuyCost = costPerShare * totalSellShares;
                        }
                        else
                        {
                            float estimatedGrossBuy = sellRecord.lots * PortfolioManager.SHARES_PER_LOT * sellRecord.pricePerShare;
                            float estimatedBuyFee = estimatedGrossBuy * 0.002f;
                            totalBuyCost = estimatedGrossBuy + estimatedBuyFee;
                        }
                    }

                    totalGainLoss += (totalSellRevenue - totalBuyCost);
                }
            }
        }

        int totalTransactions = buyCount + sellCount;

        string glColorHex = "#FFFFFF";
        string glPrefix = "";
        if (totalGainLoss > 0.5f)
        {
            glColorHex = "#4ADE80";
            glPrefix = "+";
        }
        else if (totalGainLoss < -0.5f)
        {
            glColorHex = "#F87171";
        }

        if (titleText != null)
        {
            titleText.text = $"Recap Transaksi - Day {targetDay}";
        }

        if (descriptionText != null)
        {
            descriptionText.text =
                $"Total Beli (Buy)  : <b>{buyCount}x</b> ({totalBuyLots:N0} Lot)\n" +
                $"Total Jual (Sell) : <b>{sellCount}x</b> ({totalSellLots:N0} Lot)\n" +
                $"Total Transaksi   : <b>{totalTransactions}x</b>\n\n" +
                $"Total Gain / Loss : <color={glColorHex}><b>{glPrefix}{totalGainLoss:N0}</b></color>";
        }
    }
}