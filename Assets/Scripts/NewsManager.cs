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
    public TextMeshProUGUI headerDateText; // Optional

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
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
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

        bool isHeader = true;

        foreach (var columns in rows)
        {
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            if (columns.Length < 4) continue;

            string rawDateStr = columns[0].Trim();
            string ticker = columns[1].Trim();

            string title = columns[2].Trim().Replace("[]", "").Trim();
            string description = columns[3].Trim();

            string[] formats = new string[]
            {
                "M/d/yyyy", "d/M/yyyy", "M/d/yyyy HH:mm:ss", "d/M/yyyy HH:mm:ss",
                "yyyy-MM-dd", "d MMM yyyy", "dd/MM/yyyy"
            };

            if (DateTime.TryParseExact(rawDateStr, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
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

        allNewsList.Sort((a, b) => b.Date.CompareTo(a.Date));
    }

    private int FindNextTradingDay(DateTime newsDate)
    {
        for (int i = 0; i < 30; i++)
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

        activeAvailableNews = allNewsList.FindAll(n => n.MappedDay <= currentDay);

        currentNewsPageIndex = 0;
        RefreshNewsUI();
    }

    public void RefreshNewsUI()
    {
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

                string formattedDayOnly = $"Day {news.MappedDay}";

                newsRows[i].SetNews(news.Title, news.Description, formattedDayOnly);
            }
        }
    }

    public void PreviousNewsPage()
    {
        if (currentNewsPageIndex > 0)
        {
            currentNewsPageIndex--;
            RefreshNewsUI();
        }
    }

    public void NextNewsPage()
    {
        if ((currentNewsPageIndex + 1) * newsRows.Length < activeAvailableNews.Count)
        {
            currentNewsPageIndex++;
            RefreshNewsUI();
        }
    }

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