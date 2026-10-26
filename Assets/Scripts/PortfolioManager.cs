using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        public float totalInvestedCost;

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

    // PEMBERSIHAN TICKER TOTAL: Menghapus .csv, _csv, spasi, dan karakter tak terlihat
    public string CleanTicker(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        
        string clean = input.Trim();
        clean = Regex.Replace(clean, @"(?i)(\.csv|_csv)$", ""); // Hapus .csv / _csv di akhir
        clean = Regex.Replace(clean, @"\s+", "");             // Hapus semua spasi
        
        return clean.ToUpper();
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

        string cleanName = CleanTicker(datasetName);
        currentCash -= totalCost;

        if (!holdings.ContainsKey(cleanName))
        {
            holdings[cleanName] = new Position { datasetName = cleanName, totalShares = 0, totalInvestedCost = 0f };
        }

        Position pos = holdings[cleanName];
        pos.totalShares += sharesToBuy;
        pos.totalInvestedCost += totalCost;

        transactionHistory.Add(new TransactionRecord
        {
            date = date,
            dayIndex = dayIndex,
            datasetName = cleanName,
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

        string cleanName = CleanTicker(datasetName);

        if (!holdings.ContainsKey(cleanName) || holdings[cleanName].totalShares < sharesToSell)
        {
            Debug.LogWarning($"PortfolioManager: Not enough shares owned for '{cleanName}' to complete sell order.");
            return false;
        }

        Position pos = holdings[cleanName];
        
        float costBasisPerShare = pos.AverageBuyPrice;
        float revenue = sharesToSell * pricePerShare;

        pos.totalShares -= sharesToSell;
        pos.totalInvestedCost -= (sharesToSell * costBasisPerShare);
        currentCash += revenue;

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
            totalAmount = revenue
        });

        OnPortfolioUpdated?.Invoke();
        return true;
    }

    public Dictionary<string, Position> GetAllHoldings() => holdings;
    public List<TransactionRecord> GetTransactionHistory() => transactionHistory;

    public int GetOwnedShares(string datasetName)
    {
        string cleanName = CleanTicker(datasetName);
        if (holdings.ContainsKey(cleanName))
        {
            return holdings[cleanName].totalShares;
        }
        return 0;
    }
}