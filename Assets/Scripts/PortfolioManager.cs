using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class PortfolioManager : MonoBehaviour
{
    public static PortfolioManager Instance { get; private set; }

    public const int SHARES_PER_LOT = 100;

    [Header("Wallet & Capital")]
    public float startingCash = 100000000f; // Cash awal
    public float currentCash { get; private set; }

    [Header("Trading Fees")]
    public float buyFeeRate = 0.002f;  // 0.2% fee beli
    public float sellFeeRate = 0.003f; // 0.3% fee jual

    [Serializable]
    public class Position
    {
        public string stockSymbol;
        public int totalShares;
        public float totalInvestedCost; // Termasuk Fee Beli

        public int TotalLots => totalShares / SHARES_PER_LOT;
        public float AverageBuyPrice => totalShares > 0 ? totalInvestedCost / totalShares : 0f;
    }

    [Serializable]
    public class TransactionRecord
    {
        public string date;
        public int dayIndex;
        public string datasetName;
        public bool isBuy;
        public int shares;
        public float pricePerShare;
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

        int sharesToBuy = lotsToBuy * SHARES_PER_LOT;
        float grossCost = sharesToBuy * pricePerShare;
        float fee = grossCost * buyFeeRate;
        float totalCostWithFee = grossCost + fee;

        if (currentCash < totalCostWithFee)
        {
            Debug.LogWarning("PortfolioManager: Saldo tidak mencukupi untuk membeli.");
            return false;
        }

        string cleanName = CleanTicker(stockSymbol);
        currentCash -= totalCostWithFee;

        if (!holdings.ContainsKey(cleanName))
        {
            holdings[cleanName] = new Position { stockSymbol = cleanName, totalShares = 0, totalInvestedCost = 0f };
        }

        Position pos = holdings[cleanName];
        pos.totalShares += sharesToBuy;
        pos.totalInvestedCost += totalCostWithFee;

        transactionHistory.Add(new TransactionRecord
        {
            date = date,
            dayIndex = dayIndex,
            datasetName = cleanName,
            isBuy = true,
            shares = sharesToBuy,
            pricePerShare = pricePerShare,
            totalAmount = totalCostWithFee
        });

        OnPortfolioUpdated?.Invoke();
        return true;
    }

    public bool ExecuteSell(string stockSymbol, string date, int dayIndex, int lotsToSell, float pricePerShare)
    {
        if (lotsToSell <= 0 || pricePerShare <= 0f) return false;

        string cleanName = CleanTicker(stockSymbol);
        int sharesToSell = lotsToSell * SHARES_PER_LOT;

        if (!holdings.ContainsKey(cleanName) || holdings[cleanName].totalShares < sharesToSell)
        {
            Debug.LogWarning($"PortfolioManager: Lembar saham '{cleanName}' tidak mencukupi untuk dijual.");
            return false;
        }

        Position pos = holdings[cleanName];
        
        float costBasisPerShare = pos.AverageBuyPrice;
        float grossRevenue = sharesToSell * pricePerShare;
        float fee = grossRevenue * sellFeeRate;
        float netRevenue = grossRevenue - fee;

        pos.totalShares -= sharesToSell;
        pos.totalInvestedCost -= (sharesToSell * costBasisPerShare);
        currentCash += netRevenue;

        if (pos.totalShares <= 0)
        {
            holdings.Remove(cleanName);
        }

        transactionHistory.Add(new TransactionRecord
        {
            date = date,
            dayIndex = dayIndex,
            datasetName = cleanName,
            isBuy = false,
            shares = sharesToSell,
            pricePerShare = pricePerShare,
            totalAmount = netRevenue
        });

        OnPortfolioUpdated?.Invoke();
        return true;
    }

    public Dictionary<string, Position> GetAllHoldings() => holdings;
    public List<TransactionRecord> GetTransactionHistory() => transactionHistory;

    public int GetOwnedShares(string stockSymbol)
    {
        string cleanName = CleanTicker(stockSymbol);
        if (holdings.ContainsKey(cleanName))
        {
            return holdings[cleanName].totalShares;
        }
        return 0;
    }

    public int GetOwnedLots(string stockSymbol)
    {
        return GetOwnedShares(stockSymbol) / SHARES_PER_LOT;
    }
}