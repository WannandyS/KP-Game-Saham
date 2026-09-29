using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCompanyData", menuName = "Trading Simulator/Company Data")]
public class CompanyData : ScriptableObject
{
    public string companyTicker;        // e.g., "AADI"
    public string companyName;          // e.g., "PT Adaro Andalan Indonesia Tbk"
    [TextArea(3, 5)]
    public string companyDescription;   // Details about the company

    [Header("CSV References")]
    public TextAsset priceCSV;          // Price CSV (Date, Open, High, Low, Close, Volume)
    public TextAsset newsCSV;           // News CSV for this company

    [Serializable]
    public class NewsItem
    {
        public int dayIndex;            // Associated trading day (Day 1, Day 2, etc.)
        public string headline;
        public string content;
    }

    // Helper method to parse the news CSV on demand
    public List<NewsItem> GetNews()
    {
        List<NewsItem> newsList = new List<NewsItem>();
        if (newsCSV == null) return newsList;

        string[] lines = newsCSV.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return newsList;

        // Assuming News CSV columns: [Trading Day / Row Index], [Headline], [Content]
        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length < 2) continue;

            newsList.Add(new NewsItem
            {
                dayIndex = i, // Row index mapping to Trading Day
                headline = values[0].Trim().Trim('"'),
                content = values.Length > 1 ? values[1].Trim().Trim('"') : ""
            });
        }
        return newsList;
    }
}