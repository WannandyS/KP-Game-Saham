using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PortfolioUI : MonoBehaviour
{
    [Header("UI Summary Labels")]
    public TMP_Text cashText;
    public TMP_Text totalPortfolioValueText;

    [Header("Holdings List Container")]
    public Transform holdingsContainer;
    public GameObject holdingItemPrefab; // Simple prefab with Text component

    [Header("Transaction History Container")]
    public Transform transactionContainer;
    public GameObject transactionItemPrefab; // Simple prefab with Text component

    private void OnEnable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated += RefreshUI;
        }
        RefreshUI();
    }

    private void OnDisable()
    {
        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.OnPortfolioUpdated -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        if (PortfolioManager.Instance == null) return;

        // Update Cash & Net Worth
        float cash = PortfolioManager.Instance.currentCash;
        float holdingsCost = 0f;

        foreach (var kvp in PortfolioManager.Instance.GetAllHoldings())
        {
            holdingsCost += kvp.Value.totalInvestedCost;
        }

        float totalNetWorth = cash + holdingsCost;

        if (cashText != null) cashText.text = $"Cash: ${cash:N2}";
        if (totalPortfolioValueText != null) totalPortfolioValueText.text = $"Total Net Worth: ${totalNetWorth:N2}";

        // Populate Holdings
        PopulateHoldings();

        // Populate Transactions
        PopulateTransactions();
    }

    private void PopulateHoldings()
    {
        if (holdingsContainer == null || holdingItemPrefab == null) return;

        // Clear existing UI elements in container
        foreach (Transform child in holdingsContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var kvp in PortfolioManager.Instance.GetAllHoldings())
        {
            var pos = kvp.Value;
            GameObject itemObj = Instantiate(holdingItemPrefab, holdingsContainer);
            TMP_Text label = itemObj.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                label.text = $"<b>{pos.datasetName}</b> | Shares: {pos.totalShares:N0} | Avg Price: ${pos.AverageBuyPrice:N2} | Cost Basis: ${pos.totalInvestedCost:N2}";
            }
        }
    }

    private void PopulateTransactions()
    {
        if (transactionContainer == null || transactionItemPrefab == null) return;

        // Clear existing transaction UI items
        foreach (Transform child in transactionContainer)
        {
            Destroy(child.gameObject);
        }

        List<PortfolioManager.TransactionRecord> history = PortfolioManager.Instance.GetTransactionHistory();

        // Loop in reverse to show newest transactions first
        for (int i = history.Count - 1; i >= 0; i--)
        {
            var tx = history[i];
            GameObject itemObj = Instantiate(transactionItemPrefab, transactionContainer);
            TMP_Text label = itemObj.GetComponentInChildren<TMP_Text>();

            if (label != null)
            {
                string actionType = tx.isBuy ? "<color=green>BUY</color>" : "<color=red>SELL</color>";
                label.text = $"[{tx.date} (Day {tx.dayIndex})] {actionType} {tx.shares}x {tx.datasetName} @ ${tx.pricePerShare:N2} = ${tx.totalAmount:N2}";
            }
        }
    }
}