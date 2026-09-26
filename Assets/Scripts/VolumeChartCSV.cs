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

    [Tooltip("Zero-based column index for Volume data in CSV. (Column 7 = Index 6)")]
    public int volumeColumnIndex = 6;

    [Header("Chart Area")]
    public RectTransform chartArea;

    [Tooltip("Number of volume bars visible on screen.")]
    public int maxBars = 30;

    [Tooltip("Percentage of each slot occupied by the volume bar.")]
    [Range(0.1f, 1f)]
    public float barWidthPercent = 0.8f;

    [Header("Simulation")]
    [Tooltip("Seconds between each new volume bar.")]
    public float updateInterval = 5f;

    [Tooltip("Automatically advance through the CSV.")]
    public bool autoUpdate = true;

    [Header("Volume Scale (Right Side)")]
    [Tooltip("Space reserved on the right for volume labels.")]
    public float labelWidth = 70f;

    [Tooltip("Space between volume labels and the chart.")]
    public float chartRightPadding = 5f;

    [Header("Footer Axis")]
    [Tooltip("Space reserved at the bottom for Day labels.")]
    public float footerHeight = 30f;

    [Tooltip("Font size for day labels at footer.")]
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

    private int nextIndex = 0;
    private Coroutine updateCoroutine;

    private void Start()
    {
        LoadCSV();
        InitializeChart();

        if (autoUpdate)
        {
            StartUpdating();
        }
    }

    private void LoadCSV()
    {
        allVolume.Clear();

        if (csvFile == null)
        {
            Debug.LogError("VolumeChartCSV: No CSV file assigned.");
            return;
        }

        string[] lines = csvFile.text.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (lines.Length <= 1)
        {
            Debug.LogWarning("VolumeChartCSV: CSV contains no data.");
            return;
        }

        string[] headers = SplitCSVLine(lines[0]);
        int dateIndex = FindColumnIndex(headers, "Date");
        if (dateIndex == -1) dateIndex = 0;

        int currentDayCounter = 1;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = SplitCSVLine(line);

            if (values.Length <= volumeColumnIndex)
            {
                Debug.LogWarning($"VolumeChartCSV: Skipping CSV line {i + 1} (Insufficient columns).");
                continue;
            }

            string date = values[dateIndex].Trim().Trim('"');

            if (!TryParseFloat(values[volumeColumnIndex], out float volume))
            {
                Debug.LogWarning($"VolumeChartCSV: Invalid volume data on CSV line {i + 1}.");
                continue;
            }

            allVolume.Add(new VolumeData(date, volume, currentDayCounter));
            currentDayCounter++;
        }

        Debug.Log($"VolumeChartCSV: Loaded {allVolume.Count} volume entries.");
    }

    private void InitializeChart()
    {
        displayedVolume.Clear();
        nextIndex = 0;

        if (allVolume.Count == 0)
        {
            RedrawChart();
            return;
        }

        int initialCount = Mathf.Min(maxBars, allVolume.Count);

        for (int i = 0; i < initialCount; i++)
        {
            displayedVolume.Add(allVolume[i]);
        }

        nextIndex = initialCount;
        RedrawChart();
    }

    public void StartUpdating()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }

        updateCoroutine = StartCoroutine(UpdateRoutine());
    }

    public void StopUpdating()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
            updateCoroutine = null;
        }
    }

    private IEnumerator UpdateRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);

            AdvanceOneBar();

            if (nextIndex >= allVolume.Count)
            {
                updateCoroutine = null;
                Debug.Log("VolumeChartCSV: Reached end of CSV.");
                yield break;
            }
        }
    }

    public void AdvanceOneBar()
    {
        if (allVolume.Count == 0 || nextIndex >= allVolume.Count)
        {
            return;
        }

        if (displayedVolume.Count >= maxBars)
        {
            displayedVolume.RemoveAt(0);
        }

        displayedVolume.Add(allVolume[nextIndex]);
        nextIndex++;

        RedrawChart();
    }

    public void RedrawChart()
    {
        ClearChart();

        if (chartArea == null || displayedVolume.Count == 0)
        {
            return;
        }

        float maxVolume = float.MinValue;

        for (int i = 0; i < displayedVolume.Count; i++)
        {
            maxVolume = Mathf.Max(maxVolume, displayedVolume[i].volume);
        }

        if (maxVolume <= 0f) maxVolume = 1f;

        float totalWidth = chartArea.rect.width;
        float barAreaWidth = totalWidth - labelWidth - chartRightPadding;
        barAreaWidth = Mathf.Max(1f, barAreaWidth);

        DrawGrid(maxVolume, barAreaWidth);

        float slotWidth = barAreaWidth / displayedVolume.Count;

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

        // Create Bar Image with Raycasting enabled
        RectTransform bar = CreateImage("VolumeBar", barColor, true);
        bar.SetParent(chartArea, false);
        bar.anchorMin = Vector2.zero;
        bar.anchorMax = Vector2.zero;
        bar.pivot = new Vector2(0.5f, 0f);

        bar.anchoredPosition = new Vector2(x, footerHeight);
        bar.sizeDelta = new Vector2(width, barHeight);

        string tooltipText = $"<b>Day {data.dayIndex}</b> ({data.date})\n" +
                            $"Volume: {data.volume:#,##0} ({FormatVolume(data.volume)})";

        ChartHoverTrigger barHover = bar.gameObject.AddComponent<ChartHoverTrigger>();
        barHover.Init(tooltipText);
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
        text.overflowMode = TextOverflowModes.Overflow;
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
        if (vol >= 1_000_000_000f)
            return (vol / 1_000_000_000f).ToString("0.##") + "B";
        if (vol >= 1_000_000f)
            return (vol / 1_000_000f).ToString("0.##") + "M";
        if (vol >= 1_000f)
            return (vol / 1_000f).ToString("0.##") + "K";

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
        {
            Destroy(chartArea.GetChild(i).gameObject);
        }
    }

    private string[] SplitCSVLine(string line)
    {
        return line.Split(',');
    }

    private int FindColumnIndex(string[] headers, string columnName)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            string header = headers[i].Trim().Trim('"');
            if (string.Equals(header, columnName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    private bool TryParseFloat(string value, out float result)
    {
        value = value.Trim().Trim('"');
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }
}
