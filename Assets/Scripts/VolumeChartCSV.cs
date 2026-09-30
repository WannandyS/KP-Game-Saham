using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VolumeChartCSV : MonoBehaviour
{
    [Serializable]
    public class VolumeData
    {
        public string date;
        public float volume;
        public int dayIndex;

        public VolumeData(string date, float volume, int dayIndex)
        {
            this.date = date;
            this.volume = volume;
            this.dayIndex = dayIndex;
        }
    }

    [Header("CSV")]
    public TextAsset csvFile;
    public int volumeColumnIndex = 6;

    [Header("Chart Area")]
    public RectTransform chartArea;

    [Tooltip("Fixed maximum bars capacity visible across the axis.")]
    public int maxBars = 30;

    [Range(0.1f, 1f)]
    public float barWidthPercent = 0.8f;

    [Header("Volume Scale (Right Side)")]
    public float labelWidth = 70f;
    public float chartRightPadding = 5f;

    [Header("Footer Axis")]
    public float footerHeight = 30f;
    public int dayFontSize = 12;

    [Header("Styling")]
    public Color barColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    public Color gridColor = new Color(1f, 1f, 1f, 0.15f);
    public int gridLineCount = 3;
    public float gridLineWidth = 1f;

    [Header("Labels & Typography")]
    public Color textColor = Color.white;
    public int labelFontSize = 12;
    public TMP_FontAsset labelFont;

    private readonly List<VolumeData> allVolume = new List<VolumeData>();
    private readonly List<VolumeData> displayedVolume = new List<VolumeData>();

    private void OnEnable()
    {
        SubscribeToDayManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromDayManager();
    }

    private void SubscribeToDayManager()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
        }
    }

    private void UnsubscribeFromDayManager()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
        }
    }

    private void Start()
    {
        LoadCSV();
        SubscribeToDayManager();

        if (DaySimulationManager.Instance != null)
        {
            HandleDayChanged(DaySimulationManager.Instance.currentDay);
        }
    }

    private void LoadCSV()
    {
        allVolume.Clear();
        if (csvFile == null) return;

        string[] lines = csvFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return;

        string[] headers = SplitCSVLine(lines[0]);
        int dateIndex = FindColumnIndex(headers, "Date");
        if (dateIndex == -1) dateIndex = 0;

        int currentDayCounter = 1;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = SplitCSVLine(line);
            if (values.Length <= volumeColumnIndex) continue;

            string date = values[dateIndex].Trim().Trim('"');

            if (!TryParseFloat(values[volumeColumnIndex], out float volume))
                continue;

            allVolume.Add(new VolumeData(date, volume, currentDayCounter));
            currentDayCounter++;
        }
    }

    private void HandleDayChanged(int currentSimDay)
    {
        displayedVolume.Clear();

        int targetIndex = Mathf.Min(currentSimDay, allVolume.Count);
        int startIndex = Mathf.Max(0, targetIndex - maxBars);

        for (int i = startIndex; i < targetIndex; i++)
        {
            displayedVolume.Add(allVolume[i]);
        }

        RedrawChart();
    }

    public void RedrawChart()
    {
        ClearChart();

        if (chartArea == null || displayedVolume.Count == 0) return;

        float maxVolume = float.MinValue;

        for (int i = 0; i < displayedVolume.Count; i++)
        {
            maxVolume = Mathf.Max(maxVolume, displayedVolume[i].volume);
        }

        if (maxVolume <= 0f) maxVolume = 1f;

        float totalWidth = chartArea.rect.width;
        float barAreaWidth = Mathf.Max(1f, totalWidth - labelWidth - chartRightPadding);

        DrawGrid(maxVolume, barAreaWidth);

        // Uniform slot sizing: Divides width by fixed maxBars to keep bar sizes uniform
        int slotCount = Mathf.Max(maxBars, displayedVolume.Count);
        float slotWidth = barAreaWidth / slotCount;

        for (int i = 0; i < displayedVolume.Count; i++)
        {
            VolumeData data = displayedVolume[i];
            float width = slotWidth * barWidthPercent;
            float x = (i * slotWidth) + (slotWidth * 0.5f);

            DrawBar(data, maxVolume, x, width);
            CreateFooterLabel(data.dayIndex, x, slotWidth);
        }
    }

    private void DrawBar(VolumeData data, float maxVolume, float x, float width)
    {
        float normalized = Mathf.Clamp01(data.volume / maxVolume);
        float printableHeight = chartArea.rect.height - footerHeight;
        float barHeight = Mathf.Max(1f, normalized * printableHeight);

        RectTransform bar = CreateImage("VolumeBar", barColor, true);
        bar.SetParent(chartArea, false);
        bar.anchorMin = Vector2.zero;
        bar.anchorMax = Vector2.zero;
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(x, footerHeight);
        bar.sizeDelta = new Vector2(width, barHeight);

        string tooltipText = $"<b>Day {data.dayIndex}</b>\n" +
                            $"Volume: {data.volume:#,##0} ({FormatVolume(data.volume)})";

        bar.gameObject.AddComponent<ChartHoverTrigger>().Init(tooltipText);
    }

    private void DrawGrid(float maxVolume, float barAreaWidth)
    {
        if (gridLineCount <= 0) return;
        float printableHeight = chartArea.rect.height - footerHeight;

        for (int i = 0; i <= gridLineCount; i++)
        {
            float normalized = i / (float)gridLineCount;
            float y = footerHeight + (normalized * printableHeight);
            float volumeValue = maxVolume * normalized;

            RectTransform line = CreateImage("GridLine", gridColor, false);
            line.SetParent(chartArea, false);
            line.anchorMin = Vector2.zero;
            line.anchorMax = Vector2.zero;
            line.pivot = new Vector2(0f, 0.5f);
            line.anchoredPosition = new Vector2(0f, y);
            line.sizeDelta = new Vector2(barAreaWidth, gridLineWidth);

            CreateLabel(FormatVolume(volumeValue), y, barAreaWidth);
        }
    }

    private void CreateLabel(string textContent, float y, float barAreaWidth)
    {
        GameObject labelObj = new GameObject("VolumeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(chartArea, false);

        RectTransform rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(barAreaWidth + chartRightPadding, y);
        rect.sizeDelta = new Vector2(labelWidth, 20f);

        TextMeshProUGUI text = labelObj.GetComponent<TextMeshProUGUI>();
        text.text = textContent;
        text.color = textColor;
        text.fontSize = labelFontSize;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        if (labelFont != null) text.font = labelFont;
    }

    private void CreateFooterLabel(int dayNumber, float x, float slotWidth)
    {
        GameObject labelObj = new GameObject($"DayLabel_{dayNumber}", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(chartArea, false);

        RectTransform rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, footerHeight * 0.5f);
        rect.sizeDelta = new Vector2(slotWidth, footerHeight);

        TextMeshProUGUI text = labelObj.GetComponent<TextMeshProUGUI>();
        text.text = $"Day {dayNumber}";
        text.color = textColor;
        text.fontSize = dayFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        if (labelFont != null) text.font = labelFont;
    }

    private string FormatVolume(float vol)
    {
        if (vol >= 1_000_000_000f) return (vol / 1_000_000_000f).ToString("0.##") + "B";
        if (vol >= 1_000_000f) return (vol / 1_000_000f).ToString("0.##") + "M";
        if (vol >= 1_000f) return (vol / 1_000f).ToString("0.##") + "K";
        return vol.ToString("#,##0", CultureInfo.InvariantCulture);
    }

    private RectTransform CreateImage(string objectName, Color color, bool raycastTarget = false)
    {
        GameObject imgObj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Image image = imgObj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return imgObj.GetComponent<RectTransform>();
    }

    private void ClearChart()
    {
        if (chartArea == null) return;
        for (int i = chartArea.childCount - 1; i >= 0; i--)
            Destroy(chartArea.GetChild(i).gameObject);
    }

    private string[] SplitCSVLine(string line) => line.Split(',');

    private int FindColumnIndex(string[] headers, string columnName)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            if (string.Equals(headers[i].Trim().Trim('"'), columnName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private bool TryParseFloat(string value, out float result)
    {
        value = value.Trim().Trim('"');
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}