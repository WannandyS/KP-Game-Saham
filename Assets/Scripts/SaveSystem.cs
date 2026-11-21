using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

public class SaveSystemManager : MonoBehaviour
{
    public static SaveSystemManager Instance { get; private set; }

    [Serializable]
    public class AccountRecord
    {
        public string username;
        public string salt;
        public string passwordHash;
        public List<string> saveSlotIds = new List<string>();
    }

    [Serializable]
    public class AccountDatabase
    {
        public List<AccountRecord> accounts = new List<AccountRecord>();
    }

    [Serializable]
    public class SavedPositionData
    {
        public string stockSymbol;
        public int totalShares;
        public float totalInvestedCost;
    }

    [Serializable]
    public class SavedTransactionData
    {
        public string date;
        public int dayIndex;
        public string stockSymbol;
        public bool isBuy;
        public int lots;
        public float pricePerShare;
        public float grossAmount;
        public float feeAmount;
        public float totalAmount;
        public float costBasisPerShare;
    }

    [Serializable]
    public class PlayerSaveGameData
    {
        public string saveId;
        public string saveName;
        public string ownerUsername;
        public string savedAtUtc;

        // Day & Spoof state
        public int spoofedDay;
        public int startDatasetDay;
        public int maxTradingDays;

        // Portfolio & Wallet state
        public float startingCash;
        public float currentCash;
        public List<SavedPositionData> holdings = new List<SavedPositionData>();
        public List<SavedTransactionData> transactionHistory = new List<SavedTransactionData>();
    }

    public string CurrentLoggedInUser { get; private set; }
    public event Action<string> OnAccountChanged;
    public event Action<string> OnSaveSystemMessage;

    private string RootSaveFolder => Path.Combine(Application.persistentDataPath, "PlayerSaves");
    private string AccountsFilePath => Path.Combine(RootSaveFolder, "accounts.json");

    private AccountDatabase accountDb = new AccountDatabase();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Directory.CreateDirectory(RootSaveFolder);
        LoadAccountsDatabase();
    }

    private string NormalizeUsername(string rawUsername)
    {
        return string.IsNullOrWhiteSpace(rawUsername) ? "" : rawUsername.Trim().ToLowerInvariant();
    }

    private string GetUserFolder(string normalizedUsername)
    {
        return Path.Combine(RootSaveFolder, normalizedUsername);
    }

    private string GetSaveFilePath(string normalizedUsername, string saveId)
    {
        return Path.Combine(GetUserFolder(normalizedUsername), $"{saveId}.json");
    }

    private void LoadAccountsDatabase()
    {
        if (!File.Exists(AccountsFilePath))
        {
            accountDb = new AccountDatabase();
            SaveAccountsDatabase();
            return;
        }

        string json = File.ReadAllText(AccountsFilePath);
        accountDb = JsonUtility.FromJson<AccountDatabase>(json) ?? new AccountDatabase();
    }

    private void SaveAccountsDatabase()
    {
        Directory.CreateDirectory(RootSaveFolder);
        string json = JsonUtility.ToJson(accountDb, true);
        File.WriteAllText(AccountsFilePath, json);
    }

    private string GenerateSalt()
    {
        byte[] saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

    private string HashPassword(string password, string salt)
    {
        using (var sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes($"{salt}:{password}");
            byte[] hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    public bool RegisterAccount(string username, string password)
    {
        string normUser = NormalizeUsername(username);
        if (string.IsNullOrEmpty(normUser) || string.IsNullOrEmpty(password))
        {
            Notify("Username and password cannot be empty.");
            return false;
        }

        if (accountDb.accounts.Exists(a => a.username == normUser))
        {
            Notify($"Account '{normUser}' already exists.");
            return false;
        }

        string salt = GenerateSalt();
        string hash = HashPassword(password, salt);

        var newAccount = new AccountRecord
        {
            username = normUser,
            salt = salt,
            passwordHash = hash
        };

        accountDb.accounts.Add(newAccount);
        Directory.CreateDirectory(GetUserFolder(normUser));
        SaveAccountsDatabase();

        CurrentLoggedInUser = normUser;
        OnAccountChanged?.Invoke(CurrentLoggedInUser);
        Notify($"Registered and logged in as '{normUser}'.");
        return true;
    }

    public bool Login(string username, string password)
    {
        string normUser = NormalizeUsername(username);
        AccountRecord account = accountDb.accounts.Find(a => a.username == normUser);

        if (account == null)
        {
            Notify("Invalid username or password.");
            return false;
        }

        string computedHash = HashPassword(password, account.salt);
        if (!string.Equals(computedHash, account.passwordHash, StringComparison.Ordinal))
        {
            Notify("Invalid username or password.");
            return false;
        }

        CurrentLoggedInUser = account.username;
        OnAccountChanged?.Invoke(CurrentLoggedInUser);
        Notify($"Logged in as '{CurrentLoggedInUser}'.");
        return true;
    }

    public void Logout()
    {
        CurrentLoggedInUser = null;
        OnAccountChanged?.Invoke(null);
        Notify("Logged out.");
    }

    public List<PlayerSaveGameData> GetCurrentUserSaves()
    {
        List<PlayerSaveGameData> results = new List<PlayerSaveGameData>();
        if (string.IsNullOrEmpty(CurrentLoggedInUser)) return results;

        AccountRecord account = accountDb.accounts.Find(a => a.username == CurrentLoggedInUser);
        if (account == null) return results;

        foreach (string slotId in account.saveSlotIds)
        {
            string path = GetSaveFilePath(CurrentLoggedInUser, slotId);
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<PlayerSaveGameData>(json);
                if (data != null && data.ownerUsername == CurrentLoggedInUser)
                {
                    results.Add(data);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Creates a new save slot or overwrites an existing saveId for the logged-in player.
    /// Pass null or empty existingSaveId to create a new save slot.
    /// </summary>
    public bool SaveGame(string saveName, string existingSaveId = null)
    {
        if (string.IsNullOrEmpty(CurrentLoggedInUser))
        {
            Notify("You must log in before saving.");
            return false;
        }

        if (PortfolioManager.Instance == null || DaySimulationManager.Instance == null)
        {
            Notify("Missing PortfolioManager or DaySimulationManager.");
            return false;
        }

        AccountRecord account = accountDb.accounts.Find(a => a.username == CurrentLoggedInUser);
        if (account == null) return false;

        string saveId = string.IsNullOrWhiteSpace(existingSaveId)
            ? $"save_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N").Substring(0, 6)}"
            : existingSaveId;

        var saveData = new PlayerSaveGameData
        {
            saveId = saveId,
            saveName = string.IsNullOrWhiteSpace(saveName) ? $"Save ({DateTime.Now:MMM dd HH:mm})" : saveName.Trim(),
            ownerUsername = CurrentLoggedInUser,
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            spoofedDay = DaySimulationManager.Instance.currentDay,
            startDatasetDay = DaySimulationManager.Instance.startDatasetDay,
            maxTradingDays = DaySimulationManager.Instance.maxTradingDays,
            startingCash = PortfolioManager.Instance.startingCash,
            currentCash = PortfolioManager.Instance.currentCash
        };

        foreach (var kvp in PortfolioManager.Instance.GetAllHoldings())
        {
            var pos = kvp.Value;
            saveData.holdings.Add(new SavedPositionData
            {
                stockSymbol = pos.stockSymbol,
                totalShares = pos.totalShares,
                totalInvestedCost = pos.totalInvestedCost
            });
        }

        foreach (var tx in PortfolioManager.Instance.GetTransactionHistory())
        {
            saveData.transactionHistory.Add(new SavedTransactionData
            {
                date = tx.date,
                dayIndex = tx.dayIndex,
                stockSymbol = tx.stockSymbol,
                isBuy = tx.isBuy,
                lots = tx.lots,
                pricePerShare = tx.pricePerShare,
                grossAmount = tx.grossAmount,
                feeAmount = tx.feeAmount,
                totalAmount = tx.totalAmount,
                costBasisPerShare = tx.costBasisPerShare
            });
        }

        Directory.CreateDirectory(GetUserFolder(CurrentLoggedInUser));
        File.WriteAllText(GetSaveFilePath(CurrentLoggedInUser, saveId), JsonUtility.ToJson(saveData, true));

        if (!account.saveSlotIds.Contains(saveId))
        {
            account.saveSlotIds.Add(saveId);
            SaveAccountsDatabase();
        }

        Notify($"Saved '{saveData.saveName}' for '{CurrentLoggedInUser}'.");
        return true;
    }

    public bool LoadGame(string saveId)
    {
        if (string.IsNullOrEmpty(CurrentLoggedInUser))
        {
            Notify("Access denied: please log in first.");
            return false;
        }

        string path = GetSaveFilePath(CurrentLoggedInUser, saveId);
        if (!File.Exists(path))
        {
            Notify("Save file not found for this account.");
            return false;
        }

        string json = File.ReadAllText(path);
        var saveData = JsonUtility.FromJson<PlayerSaveGameData>(json);

        // Strict ownership check so no save file can be loaded outside its owner's session
        if (saveData == null || saveData.ownerUsername != CurrentLoggedInUser)
        {
            Notify("Access denied: save belongs to another account.");
            return false;
        }

        if (PortfolioManager.Instance != null)
        {
            PortfolioManager.Instance.RestoreState(
                saveData.startingCash,
                saveData.currentCash,
                saveData.holdings,
                saveData.transactionHistory
            );
        }

        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.maxTradingDays = saveData.maxTradingDays > 0 ? saveData.maxTradingDays : 50;
            DaySimulationManager.Instance.LoadDayState(saveData.spoofedDay, saveData.startDatasetDay);
        }

        Notify($"Loaded '{saveData.saveName}' (Spoofed Day {saveData.spoofedDay}, Dataset Day {saveData.startDatasetDay + saveData.spoofedDay - 1}).");
        return true;
    }

    private void Notify(string message)
    {
        Debug.Log($"[SaveSystemManager] {message}");
        OnSaveSystemMessage?.Invoke(message);
    }
}