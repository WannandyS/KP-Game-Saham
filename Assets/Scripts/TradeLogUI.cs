using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class TradeLogUI : MonoBehaviour
{
    [Header("UI References")]
    public Transform rowContainer;          // ContentContainer with Vertical Layout Group
    public GameObject tradeRowPrefab;     // Prefab with TradeLogRowUI script

    [Header("Pagination Controls")]
    public Button nextButton;
    public Button prevButton;
    public TMP_Text pageInfoText;
    public int rowsPerPage = 8;

    private int currentPage = 0;

    private void OnEnable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated += RefreshLog;
        }
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
        }

        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (prevButton != null) prevButton.onClick.AddListener(PrevPage);

        currentPage = 0;
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
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
        }

        if (nextButton != null) nextButton.onClick.RemoveListener(NextPage);
        if (prevButton != null) prevButton.onClick.RemoveListener(PrevPage);
    }

    private void HandleDayChanged(int day)
    {
        RefreshLog();
    }

    public void NextPage()
    {
        if (PortfolioManager.Instance == null) return;
        int totalHistory = PortfolioManager.Instance.GetTransactionHistory().Count;
        int maxPages = Mathf.CeilToInt((float)totalHistory / rowsPerPage);

        if (currentPage < maxPages - 1)
        {
            currentPage++;
            RefreshLog();
        }
    }

    public void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            RefreshLog();
        }
    }

    public void RefreshLog()
    {
        if (PortfolioManager.Instance == null || rowContainer == null || tradeRowPrefab == null) return;

        foreach (Transform child in rowContainer)
        {
            Destroy(child.gameObject);
        }

        List<PortfolioManager.TransactionRecord> history = PortfolioManager.Instance.GetTransactionHistory();
        int totalHistory = history.Count;
        int maxPages = Mathf.Max(1, Mathf.CeilToInt((float)totalHistory / rowsPerPage));
        
        currentPage = Mathf.Clamp(currentPage, 0, maxPages - 1);

        int startIndex = currentPage * rowsPerPage;
        int endIndex = Mathf.Min(startIndex + rowsPerPage, totalHistory);

        // Display current page slice
        for (int i = startIndex; i < endIndex; i++)
        {
            var record = history[i];
            GameObject rowObj = Instantiate(tradeRowPrefab, rowContainer);
            TradeLogRowUI rowUI = rowObj.GetComponent<TradeLogRowUI>();

            if (rowUI != null)
            {
                if (record.isBuy)
                {
                    rowUI.SetupBuyRow(
                        record.lots,
                        record.stockSymbol,
                        record.pricePerShare,
                        record.grossAmount,
                        record.feeAmount,
                        record.totalAmount
                    );
                }
                else
                {
                    // Calculate estimated gain/loss for sell records
                    float costBasisPerShare = record.grossAmount / (record.lots * PortfolioManager.SHARES_PER_LOT); // approximate or track
                    float gainLoss = record.totalAmount - (record.grossAmount * 0.997f); 
                    float gainLossPercent = record.grossAmount > 0 ? (gainLoss / record.grossAmount) * 100f : 0f;

                    rowUI.SetupSellRow(
                        record.lots,
                        record.stockSymbol,
                        record.pricePerShare,
                        record.grossAmount,
                        record.feeAmount,
                        record.totalAmount,
                        gainLoss,
                        gainLossPercent
                    );
                }
            }
        }

        // Update pagination UI text & buttons
        if (pageInfoText != null)
        {
            pageInfoText.text = $"Page {currentPage + 1} of {maxPages}";
        }

        if (prevButton != null) prevButton.interactable = (currentPage > 0);
        if (nextButton != null) nextButton.interactable = (currentPage < maxPages - 1);
    }
}