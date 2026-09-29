using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CompanyDetailController : MonoBehaviour
{
    [Header("UI Text Displays")]
    public TextMeshProUGUI companyTickerText;
    public TextMeshProUGUI companyNameText;
    public TextMeshProUGUI companyDescriptionText;
    public TextMeshProUGUI currentDayText;

    [Header("Chart References")]
    public CandlestickChart candlestickChart;
    public VolumeChartCSV volumeChart;

    [Header("News Feed UI")]
    public Transform newsContainer;
    public GameObject newsItemPrefab; // Simple UI object containing TextMeshProUGUI components

    private void Start()
    {
        CompanyData data = CompanySelectionManager.SelectedCompany;
        int currentDay = CompanySelectionManager.CurrentSimulationDay;

        if (data == null)
        {
            Debug.LogWarning("No Company Data selected. Load from selection screen first.");
            return;
        }

        PopulateCompanyInfo(data, currentDay);
        InitializeCharts(data);
        PopulateNewsFeed(data, currentDay);
    }

    private void PopulateCompanyInfo(CompanyData data, int day)
    {
        if (companyTickerText != null) companyTickerText.text = data.companyTicker;
        if (companyNameText != null) companyNameText.text = data.companyName;
        if (companyDescriptionText != null) companyDescriptionText.text = data.companyDescription;
        if (currentDayText != null) currentDayText.text = $"Day {day}";
    }

    private void InitializeCharts(CompanyData data)
    {
        if (candlestickChart != null && data.priceCSV != null)
        {
            candlestickChart.csvFile = data.priceCSV;
            candlestickChart.autoUpdate = false; // Set to false to control via Day system
        }

        if (volumeChart != null && data.priceCSV != null)
        {
            volumeChart.csvFile = data.priceCSV;
            volumeChart.autoUpdate = false;
        }
    }

    private void PopulateNewsFeed(CompanyData data, int currentDay)
    {
        if (newsContainer == null || newsItemPrefab == null) return;

        // Clear existing news items
        foreach (Transform child in newsContainer)
        {
            Destroy(child.gameObject);
        }

        List<CompanyData.NewsItem> newsList = data.GetNews();

        foreach (var news in newsList)
        {
            // Only show news published up to the current simulation day
            if (news.dayIndex <= currentDay)
            {
                GameObject itemObj = Instantiate(newsItemPrefab, newsContainer);
                
                // Expecting prefab to have Day Text, Headline Text, and Content Text
                TextMeshProUGUI[] texts = itemObj.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 2)
                {
                    texts[0].text = $"Day {news.dayIndex}";
                    texts[1].text = news.headline;
                    if (texts.Length >= 3) texts[2].text = news.content;
                }
            }
        }
    }
}
