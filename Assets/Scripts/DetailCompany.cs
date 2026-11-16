using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

public class DetailCompany : MonoBehaviour
{
    [Header("Company Detail CSV File")]
    public TextAsset companyDetailCsv;

    [Header("Right Panel UI References")]
    public TMP_Text companyNameText;
    public TMP_Text stockCodeText;
    public TMP_Text descriptionText;
    public TMP_Text sectorText;
    public TMP_Text foundedYearText;

    [Header("Default Settings")]
    public string fallbackTicker = "AADI"; //if the scene is launched directly without Market

    private class CompanyProfile
    {
        public string ticker;
        public string name;
        public string sector;
        public string foundedYear;
        public string description;
    }

    private Dictionary<string, CompanyProfile> companyDatabase = new Dictionary<string, CompanyProfile>();
    private bool isParsed = false;

    private void Awake()
    {
        ParseCsvIfNeeded();
    }

    private void Start()
    {
        string chosenTicker = CleanTicker(SelectedStockData.StockTicker);
        if (string.IsNullOrEmpty(chosenTicker))
        {
            chosenTicker = CleanTicker(fallbackTicker);
        }

        DisplayCompanyDetail(chosenTicker);
    }

    public void DisplayCompanyDetail(string ticker)
    {
        ParseCsvIfNeeded();

        string clean = CleanTicker(ticker);
        if (companyDatabase.TryGetValue(clean, out CompanyProfile profile))
        {
            if (companyNameText != null) companyNameText.text = profile.name;
            if (stockCodeText != null) stockCodeText.text = profile.ticker;
            if (descriptionText != null) descriptionText.text = profile.description;
            if (sectorText != null) sectorText.text = profile.sector;
            if (foundedYearText != null) foundedYearText.text = profile.foundedYear;
        }
        else
        {
            if (companyNameText != null) companyNameText.text = clean;
            if (stockCodeText != null) stockCodeText.text = clean;
            if (descriptionText != null) descriptionText.text = "Company details are not available.";
            if (sectorText != null) sectorText.text = "-";
            if (foundedYearText != null) foundedYearText.text = "-";
        }
    }

    private void ParseCsvIfNeeded()
    {
        if (isParsed || companyDetailCsv == null) return;

        companyDatabase.Clear();
        List<List<string>> rows = ParseCsvRecords(companyDetailCsv.text);

        foreach (var cols in rows)
        {
            if (cols.Count < 4) continue;

            string rawTicker = CleanTicker(cols[0]);
            if (string.IsNullOrEmpty(rawTicker) || rawTicker == "CODE" || rawTicker == "KODE" || rawTicker == "TICKER" || rawTicker == "SAHAM" || rawTicker == "STOCK")
                continue;

            string rawDesc = cols.Count >= 5 ? cols[4].Trim() : "";

            CompanyProfile profile = new CompanyProfile
            {
                ticker = rawTicker,
                name = cols[1].Trim(),
                sector = cols[2].Trim(),
                foundedYear = cols[3].Trim(),
                description = rawDesc
            };

            companyDatabase[rawTicker] = profile;
        }

        isParsed = true;
    }

    private List<List<string>> ParseCsvRecords(string csvText)
    {
        List<List<string>> records = new List<List<string>>();
        List<string> currentRow = new List<string>();
        StringBuilder currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csvText.Length; i++)
        {
            char c = csvText[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    currentRow.Add(currentField.ToString());
                    currentField.Clear();
                }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && i + 1 < csvText.Length && csvText[i + 1] == '\n')
                    {
                        i++;
                    }
                    currentRow.Add(currentField.ToString());
                    currentField.Clear();

                    if (currentRow.Count > 1 || !string.IsNullOrWhiteSpace(currentRow[0]))
                    {
                        records.Add(currentRow);
                    }
                    currentRow = new List<string>();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        if (currentField.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentField.ToString());
            records.Add(currentRow);
        }

        return records;
    }

    private string CleanTicker(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return Regex.Replace(input.Trim(), @"(?i)(\.csv|_csv)$", "").Replace(" ", "").ToUpper();
    }
}