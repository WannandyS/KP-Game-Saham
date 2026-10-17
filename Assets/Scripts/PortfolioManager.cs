using System;
using System.Collections.Generic;
using UnityEngine;

public class PortfolioManager : MonoBehaviour
{
    public static PortfolioManager Instance { get; private set; }

    [Header("Wallet & Capital")]
    public float startingCash = 100000f;
    public float currentCash { get; private set; }

    [Serializable]
    public class Position
    {
        public string datasetName;
        public int totalShares;
        public float totalInvestedCost; // Sum of (shares * purchase price)

        public float AverageBuyPrice => totalShares > 0 ? totalInvestedCost / totalShares : 0f;
    }

    [Serializable]
    public class TransactionRecord
    {
        public string date;
        public int dayIndex;
        public string datasetName;
        public bool isBuy; // true = Buy, false = Sell
        public int shares;
        public float pricePerShare;
        public float totalAmount;
    }

    // Key: Dataset/Stock Name
    private readonly Dictionary<string, Position> holdings = new Dictionary<string, Position>();
    private readonly List<TransactionRecord> transactionHistory = new List<TransactionRecord>();

    // Events for UI updating
    public event Action OnPortfolioUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentCash = startingCash;
    }

    public bool ExecuteBuy(string datasetName, string date, int dayIndex, int sharesToBuy, float pricePerShare)
    {
        if (sharesToBuy <= 0 || pricePerShare <= 0f) return false;

        float totalCost = sharesToBuy * pricePerShare;
        if (currentCash < totalCost)
        {
            Debug.LogWarning("PortfolioManager: Insufficient cash to complete buy order.");
            return false;
        }

        currentCash -= totalCost;

        if (!holdings.ContainsKey(datasetName))
        {
            holdings[datasetName] = new Position { datasetName = datasetName, totalShares = 0, totalInvestedCost = 0f };
        }

        Position pos = holdings[datasetName];
        pos.totalShares += sharesToBuy;
        pos.totalInvestedCost += totalCost;

        transactionHistory.Add(new TransactionRecord
        {
            date = date,
            dayIndex = dayIndex,
            datasetName = datasetName,
            isBuy = true,
            shares = sharesToBuy,
            pricePerShare = pricePerShare,
            totalAmount = totalCost
        });

        OnPortfolioUpdated?.Invoke();
        return true;
    }

    public bool ExecuteSell(string datasetName, string date, int dayIndex, int sharesToSell, float pricePerShare)
    {
        if (sharesToSell <= 0 || pricePerShare <= 0f) return false;

        if (!holdings.ContainsKey(datasetName) || holdings[datasetName].totalShares < sharesToSell)
        {
            Debug.LogWarning("PortfolioManager: Not enough shares owned to complete sell order.");
            return false;
        }

        Position pos = holdings[datasetName];
        
        // Calculate proportional cost basis being removed
        float costBasisPerShare = pos.AverageBuyPrice;
        float revenue = sharesToSell * pricePerShare;

        pos.totalShares -= sharesToSell;
        pos.totalInvestedCost -= (sharesToSell * costBasisPerShare);
        currentCash += revenue;

        // Clean up empty positions
        if (pos.totalShares <= 0)
        {
            holdings.Remove(datasetName);
        }

        transactionHistory.Add(new TransactionRecord
        {
            date = date,
            dayIndex = dayIndex,
            datasetName = datasetName,
            isBuy = false,
            shares = sharesToSell,
            pricePerShare = pricePerShare,
            totalAmount = revenue
        });

        OnPortfolioUpdated?.Invoke();
        return true;
    }

    public Dictionary<string, Position> GetAllHoldings() => holdings;
    public List<TransactionRecord> GetTransactionHistory() => transactionHistory;

    public int GetOwnedShares(string datasetName)
    {
        if (holdings.ContainsKey(datasetName))
        {
            return holdings[datasetName].totalShares;
        }
        return 0;
    }
}