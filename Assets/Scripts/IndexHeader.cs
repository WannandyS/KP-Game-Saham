using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Globalization;

public class IndexHeaderUI : MonoBehaviour
{
    [Header("CSV Dataset")]
    public TextAsset financeCsvAsset;     // Drag your indeks.csv here in the Inspector

    [Header("UI Text References")]
    public TMP_Text openText;
    public TMP_Text lowText;
    public TMP_Text highText;
    public TMP_Text closeText;
    public TMP_Text change24hText;
    public TMP_Text volumeText;

    private List<IndexRecord> indexRecords = new List<IndexRecord>();

    public struct IndexRecord
    {
        public float open;
        public float high;
        public float low;
        public float close;
        public string volumeStr;
        public double volumeVal;
    }

    private void Awake()
    {
        ParseCsv();
    }

    private void OnEnable()
    {
        SubscribeToManager();
    }

    private void Start()
    {
        SubscribeToManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromManager();
    }

    private void SubscribeToManager()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;

            UpdateHeader(DaySimulationManager.Instance.currentDay);
        }
        else
        {
            UpdateHeader(1);
        }
    }

    private void UnsubscribeFromManager()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
        }
    }

    private void HandleDayChanged(int currentDay)
    {
        UpdateHeader(currentDay);
    }

    private void ParseCsv()
    {
        indexRecords.Clear();
        if (financeCsvAsset == null)
        {
            Debug.LogError("IndexHeaderUI: financeCsvAsset belum di-assign di Inspector!");
            return;
        }

        string[] lines = financeCsvAsset.text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            List<string> cols = ParseCsvLine(line);

            if (cols.Count >= 6)
            {
                float.TryParse(cols[1].Replace("$", "").Replace(",", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float open);
                float.TryParse(cols[2].Replace("$", "").Replace(",", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float high);
                float.TryParse(cols[3].Replace("$", "").Replace(",", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float low);
                float.TryParse(cols[4].Replace("$", "").Replace(",", "").Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float close);

                string volStr = cols[5].Replace("\"", "").Trim();
                double.TryParse(volStr.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double volVal);

                indexRecords.Add(new IndexRecord
                {
                    open = open,
                    high = high,
                    low = low,
                    close = close,
                    volumeStr = volStr,
                    volumeVal = volVal
                });
            }
        }
    }

    private List<string> ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        string currentField = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentField.Trim());
                currentField = "";
            }
            else
            {
                currentField += c;
            }
        }
        result.Add(currentField.Trim());
        return result;
    }

    public void UpdateHeader(int dayIndex)
    {
        if (indexRecords.Count == 0) ParseCsv();
        if (indexRecords.Count == 0) return;

        int dataIndex = Mathf.Clamp(dayIndex - 1, 0, indexRecords.Count - 1);
        IndexRecord current = indexRecords[dataIndex];

        float changePercent = 0f;
        if (dataIndex > 0)
        {
            float prevClose = indexRecords[dataIndex - 1].close;
            if (prevClose > 0f)
            {
                changePercent = ((current.close - prevClose) / prevClose) * 100f;
            }
        }

        if (openText != null) openText.text = $"{current.open:N0}";
        if (lowText != null) lowText.text = $"{current.low:N0}";
        if (highText != null) highText.text = $"{current.high:N0}";
        if (closeText != null) closeText.text = $"{current.close:N0}";
        if (volumeText != null) volumeText.text = FormatVolumeAbbreviated(current.volumeVal);

        if (change24hText != null)
        {
            string sign = changePercent >= 0 ? "+" : "";
            string color = changePercent >= 0 ? "green" : "red";
            change24hText.text = $"<color={color}>{sign}{changePercent:F2}%</color>";
        }
    }

    private string FormatVolumeAbbreviated(double value)
    {
        // Using CultureInfo.InvariantCulture or standard "F2" to format with 2 decimal places
        if (value >= 1_000_000_000_000)
            return $"{value / 1_000_000_000_000.0:F2}T";
        if (value >= 1_000_000_000)
            return $"{value / 1_000_000_000.0:F2}B";
        if (value >= 1_000_000)
            return $"{value / 1_000_000.0:F2}M";
        if (value >= 1_000)
            return $"{value / 1_000.0:F2}K";
        
        return $"{value:N0}";
    }
}