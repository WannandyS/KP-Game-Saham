using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class PortfolioManager : MonoBehaviour
{
    public static PortfolioManager Instance { get; private set; }

    [Header("Wallet & Capital")]
    public float startingCash = 10000000f;
    public float currentCash { get; private set; }

    [Header("Trading Fees")]
    [Tooltip("0.002 = 0.2% fee when buying")]
    public float buyFeeRate = 0.002f;
    [Tooltip("0.003 = 0.3% fee when selling")]
    public float sellFeeRate = 0.003f;

    public const int SHARES_PER_LOT = 100;

    [Serializable]
    public class Position
    {
        public string stockSymbol;
        public int totalShares;
        public float totalInvestedCost; // Includes buy fees (for accurate P/L)
        public float totalBaseCost;     // Pure stock cost (for accurate UI Base Price)

        // UPDATED: Now calculates using the pure base cost, ignoring fees
        public float AverageBuyPrice => totalShares > 0 ? totalBaseCost / totalShares : 0f;
        public int TotalLots => totalShares / SHARES_PER_LOT;
    }

    [Serializable]
    public class TransactionRecord
    {
        public string date;
        public int dayIndex;
        public string stockSymbol;
        public bool isBuy;
        public int lots;
        public float pricePerShare;
        public float feeAmount;
        public float totalAmount;
    }

    private readonly Dictionary<string, Position> holdings = new Dictionary<string, Position>();
    private readonly List<TransactionRecord> transactionHistory = new List<TransactionRecord>();

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

    public string CleanTicker(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        string clean = input.Trim();
        clean = Regex.Replace(clean, @"(?i)(\.csv|_csv)$", "");
        clean = Regex.Replace(clean, @"\s+", "");
        return clean.ToUpper();
    }

    public bool ExecuteBuy(string stockSymbol, string date, int dayIndex, int lotsToBuy, float pricePerShare)
    {
        if (lotsToBuy <= 0 || pricePerShare <= 0f) return false;

        int totalShares = lotsToBuy * SHARES_PER_LOT;
        float grossCost = totalShares * pricePerShare; // Pure base cost
        float fee = grossCost * buyFeeRate;
        float totalCost = grossCost + fee;             // Total out-of-pocket

        if (currentCash < totalCost)
        {
            Debug.LogWarning("PortfolioManager: Insufficient cash to complete buy order with fees.");
            return false;
        }

        string cleanSymbol = CleanTicker(stockSymbol);
        currentCash -= totalCost;

        if (!holdings.ContainsKey(cleanSymbol))
        {
            holdings[cleanSymbol] = new Position
            {
                stockSymbol = cleanSymbol,
                totalShares = 0,
                totalInvestedCost = 0f,
                totalBaseCost = 0f
            };
        }

        Position pos = holdings[cleanSymbol];
        pos.totalShares += totalShares;
        pos.totalInvestedCost += totalCost; // Track money spent
        pos.totalBaseCost += grossCost;     // Track pure market value

        transactionHistory.Add(new TransactionRecord
        {
            date = date,
            dayIndex = dayIndex,
            stockSymbol = cleanSymbol,
            isBuy = true,
            lots = lotsToBuy,
            pricePerShare = pricePerShare,
            feeAmount = fee,
            totalAmount = totalCost
        });

        OnPortfolioUpdated?.Invoke();
        return true;
    }

    public bool ExecuteSell(string stockSymbol, string date, int dayIndex, int lotsToSell, float pricePerShare)
    {
        if (lotsToSell <= 0 || pricePerShare <= 0f) return false;

        int totalSharesToSell = lotsToSell * SHARES_PER_LOT;
        string cleanSymbol = CleanTicker(stockSymbol);

        if (!holdings.ContainsKey(cleanSymbol) || holdings[cleanSymbol].totalShares < totalSharesToSell)
        {
            Debug.LogWarning($"PortfolioManager: Not enough shares owned for '{cleanSymbol}' to complete sell order.");
            return false;
        }

        Position pos = holdings[cleanSymbol];

        float grossRevenue = totalSharesToSell * pricePerShare;
        float fee = grossRevenue * sellFeeRate;
        float netRevenue = grossRevenue - fee;

        // Calculate both cost bases before reducing shares
        float baseCostBasisPerShare = pos.AverageBuyPrice;
        float investedCostBasisPerShare = pos.totalInvestedCost / pos.totalShares;

        pos.totalShares -= totalSharesToSell;
        pos.totalBaseCost -= (totalSharesToSell * baseCostBasisPerShare);
        pos.totalInvestedCost -= (totalSharesToSell * investedCostBasisPerShare);

        currentCash += netRevenue;

        if (pos.totalShares <= 0)
        {
            holdings.Remove(cleanSymbol);
        }

        transactionHistory.Add(new TransactionRecord
        {
            date = date,
            dayIndex = dayIndex,
            stockSymbol = cleanSymbol,
            isBuy = false,
            lots = lotsToSell,
            pricePerShare = pricePerShare,
            feeAmount = fee,
            totalAmount = netRevenue
        });

        OnPortfolioUpdated?.Invoke();
        return true;
    }

    public Dictionary<string, Position> GetAllHoldings() => holdings;

    public int GetOwnedLots(string stockSymbol)
    {
        string cleanSymbol = CleanTicker(stockSymbol);
        if (holdings.ContainsKey(cleanSymbol))
        {
            return holdings[cleanSymbol].TotalLots;
        }
        return 0;
    }
}