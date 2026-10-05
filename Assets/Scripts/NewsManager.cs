using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;

[System.Serializable]
public class NewsItem
{
    public DateTime Date;
    public string Title;
    public string Ticker;
    public string Description;
    public int MappedDay = -1;
}

public class NewsManager : MonoBehaviour
{
    [Header("CSV Text Assets")]
    public TextAsset priceCsvAsset;
    public TextAsset newsCsvAsset;

    [Header("UI References")]
    public NewsRowUI[] newsRows = new NewsRowUI[4];
    public TextMeshProUGUI headerDateText; // Optional: Displays current date/time on top bar

    private List<NewsItem> allNewsList = new List<NewsItem>();
    private Dictionary<string, int> dateToDayMap = new Dictionary<string, int>();
    private Dictionary<int, string> dayToDateMap = new Dictionary<int, string>();

    private List<NewsItem> activeAvailableNews = new List<NewsItem>();
    private int currentNewsPageIndex = 0;

    private void Start()
{
    ParsePriceDataset();
    ParseNewsDataset();

    if (DaySimulationManager.Instance != null)
    {
        // Register listener for day change events
        DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;

        // Sync immediately with current persisted day state
        HandleDayChanged(DaySimulationManager.Instance.currentDay);
    }
}

    private void OnDestroy()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
        }
    }

    private void ParsePriceDataset()
    {
        if (priceCsvAsset == null) return;

        using (StringReader reader = new StringReader(priceCsvAsset.text))
        {
            string line;
            bool isHeader = true;
            int dayCounter = 1;

            while ((line = reader.ReadLine()) != null)
            {
                if (isHeader) { isHeader = false; continue; }

                string[] columns = line.Split(',');
                if (columns.Length > 0)
                {
                    string dateStr = columns[0].Trim();
                    if (!dateToDayMap.ContainsKey(dateStr))
                    {
                        dateToDayMap.Add(dateStr, dayCounter);
                        dayToDateMap.Add(dayCounter, dateStr);
                    }
                    dayCounter++;
                }
            }
        }
    }

    private void ParseNewsDataset()
    {
        if (newsCsvAsset == null) return;

        List<string[]> rows = ParseCsvWithQuotes(newsCsvAsset.text);

        foreach (var columns in rows)
        {
            if (columns.Length < 4) continue;

            string rawDateTime = columns[0].Trim();
            string title = columns[1].Trim().Replace("[]", "").Trim();
            string ticker = columns[2].Trim();
            string description = columns[3].Trim();

            string cleanedDateStr = Regex.Replace(rawDateTime, @"\s+", " ");

            if (DateTime.TryParseExact(cleanedDateStr, "d MMM yyyy HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                string keyDate = parsedDate.ToString("yyyy-MM-dd");

                NewsItem item = new NewsItem
                {
                    Date = parsedDate,
                    Title = title,
                    Ticker = ticker,
                    Description = description
                };

                if (dateToDayMap.TryGetValue(keyDate, out int mappedDay))
                {
                    item.MappedDay = mappedDay;
                }
                else
                {
                    item.MappedDay = FindNextTradingDay(parsedDate);
                }

                if (item.MappedDay != -1)
                {
                    allNewsList.Add(item);
                }
            }
        }

        // Sort news chronologically descending (newest first)
        allNewsList.Sort((a, b) => b.Date.CompareTo(a.Date));
    }

    private int FindNextTradingDay(DateTime newsDate)
    {
        for (int i = 0; i < 7; i++)
        {
            string candidate = newsDate.AddDays(i).ToString("yyyy-MM-dd");
            if (dateToDayMap.TryGetValue(candidate, out int dayIndex))
            {
                return dayIndex;
            }
        }
        return -1;
    }

    private void HandleDayChanged(int currentDay)
    {
        if (dayToDateMap.TryGetValue(currentDay, out string dateStr) && headerDateText != null)
        {
            headerDateText.text = dateStr;
        }

        // Get all news published on or before current simulation day
        activeAvailableNews = allNewsList.FindAll(n => n.MappedDay <= currentDay);
        
        // Reset news page to 0 whenever day changes
        currentNewsPageIndex = 0;
        RefreshNewsUI();
    }

    public void RefreshNewsUI()
    {
        // Clear all 4 rows
        for (int i = 0; i < newsRows.Length; i++)
        {
            if (newsRows[i] != null) newsRows[i].Clear();
        }

        int startIndex = currentNewsPageIndex * newsRows.Length;

        for (int i = 0; i < newsRows.Length; i++)
        {
            int newsIndex = startIndex + i;
            if (newsIndex < activeAvailableNews.Count && newsRows[i] != null)
            {
                var news = activeAvailableNews[newsIndex];
                newsRows[i].SetNews(news.Title, news.Description, news.Date.ToString("dd MMM yyyy HH:mm"));
            }
        }
    }

    /// <summary>
    /// Call this from UI Button OnClick() for Newer/Next News Page
    /// </summary>
    public void PreviousNewsPage()
    {
        if (currentNewsPageIndex > 0)
        {
            currentNewsPageIndex--;
            RefreshNewsUI();
        }
    }

    /// <summary>
    /// Call this from UI Button OnClick() for Older/Previous News Page
    /// </summary>
    public void NextNewsPage()
    {
        if ((currentNewsPageIndex + 1) * newsRows.Length < activeAvailableNews.Count)
        {
            currentNewsPageIndex++;
            RefreshNewsUI();
        }
    }

    // Handles multi-line CSV entries inside quotes
    private List<string[]> ParseCsvWithQuotes(string csvText)
    {
        List<string[]> rows = new List<string[]>();
        List<string> currentFields = new List<string>();
        string currentField = "";
        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                currentFields.Add(currentField);
                currentField = "";
            }
            else if ((c == '\n' || c == '\r') && !inQuotes)
            {
                if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                {
                    i++;
                }
                currentFields.Add(currentField);
                if (currentFields.Count > 1 || !string.IsNullOrWhiteSpace(currentFields[0]))
                {
                    rows.Add(currentFields.ToArray());
                }
                currentFields = new List<string>();
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }

        if (!string.IsNullOrEmpty(currentField) || currentFields.Count > 0)
        {
            currentFields.Add(currentField);
            rows.Add(currentFields.ToArray());
        }

        return rows;
    }
}