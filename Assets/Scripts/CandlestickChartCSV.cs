using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CandlestickChart : MonoBehaviour
{
    [Serializable]
    public class CandleData
    {
        public string date;
        public float open;
        public float high;
        public float low;
        public float close;
        public int dayIndex;

        public CandleData(string date, float open, float high, float low, float close, int dayIndex)
        {
            this.date = date;
            this.open = open;
            this.high = high;
            this.low = low;
            this.close = close;
            this.dayIndex = dayIndex;
        }
    }

    [Header("CSV")]
    public TextAsset csvFile;

    [Header("Chart Area")]
    public RectTransform chartArea;

    [Tooltip("Fixed maximum candles capacity visible across the axis.")]
    public int maxCandles = 30;

    [Tooltip("Percentage of each slot occupied by candle body.")]
    [Range(0.1f, 1f)]
    public float candleWidthPercent = 0.8f;

    [Tooltip("Width of high/low wicks.")]
    public float wickWidth = 2f;

    [Tooltip("Minimum visible body height in pixels.")]
    public float minimumBodyHeight = 2f;

    [Header("Price Scale (Right Side)")]
    public float priceLabelWidth = 70f;
    public float chartRightPadding = 5f;

    [Header("Footer Axis")]
    public float footerHeight = 30f;
    public int dayFontSize = 12;

    [Header("Candle Colors")]
    public Color bullishColor = Color.green;
    public Color bearishColor = Color.red;
    public Color dojiColor = Color.gray;

    [Header("Grid")]
    public Color gridColor = new Color(1f, 1f, 1f, 0.15f);
    public int gridLineCount = 5;
    public float gridLineWidth = 1f;

    [Header("Labels & Typography")]
    public Color textColor = Color.white;
    public int priceFontSize = 14;
    public TMP_FontAsset priceFont;

    private readonly List<CandleData> allCandles = new List<CandleData>();
    private readonly List<CandleData> displayedCandles = new List<CandleData>();

    private void OnEnable()
    {
        if (DaySimulationManager.Instance != null)
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        if (DaySimulationManager.Instance != null)
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
    }

    private void Start()
    {
        LoadCSV();
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.totalDays = allCandles.Count;
            HandleDayChanged(DaySimulationManager.Instance.currentDay);
        }
    }

    private void LoadCSV()
    {
        allCandles.Clear();
        if (csvFile == null) return;

        string[] lines = csvFile.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return;

        string[] headers = SplitCSVLine(lines[0]);
        int dateIndex = FindColumnIndex(headers, "Date");
        int openIndex = FindColumnIndex(headers, "Open");
        int highIndex = FindColumnIndex(headers, "High");
        int lowIndex = FindColumnIndex(headers, "Low");
        int closeIndex = FindColumnIndex(headers, "Close");

        int currentDayCounter = 1;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] values = SplitCSVLine(line);
            int maximumIndex = Mathf.Max(dateIndex, openIndex, highIndex, lowIndex, closeIndex);

            if (values.Length <= maximumIndex) continue;

            string date = values[dateIndex].Trim().Trim('"');

            if (!TryParseFloat(values[openIndex], out float open) ||
                !TryParseFloat(values[highIndex], out float high) ||
                !TryParseFloat(values[lowIndex], out float low) ||
                !TryParseFloat(values[closeIndex], out float close))
                continue;

            allCandles.Add(new CandleData(date, open, high, low, close, currentDayCounter));
            currentDayCounter++;
        }
    }

    private void HandleDayChanged(int currentSimDay)
    {
        displayedCandles.Clear();

        int targetIndex = Mathf.Min(currentSimDay, allCandles.Count);
        int startIndex = Mathf.Max(0, targetIndex - maxCandles);

        for (int i = startIndex; i < targetIndex; i++)
        {
            displayedCandles.Add(allCandles[i]);
        }

        RedrawChart();
    }

    public void RedrawChart()
    {
        ClearChart();

        if (chartArea == null || displayedCandles.Count == 0) return;

        float highestPrice = float.MinValue;
        float lowestPrice = float.MaxValue;

        for (int i = 0; i < displayedCandles.Count; i++)
        {
            CandleData candle = displayedCandles[i];
            highestPrice = Mathf.Max(highestPrice, candle.high);
            lowestPrice = Mathf.Min(lowestPrice, candle.low);
        }

        if (Mathf.Approximately(highestPrice, lowestPrice))
        {
            highestPrice += 1f;
            lowestPrice -= 1f;
        }

        float priceRange = highestPrice - lowestPrice;
        float padding = priceRange * 0.05f;

        highestPrice += padding;
        lowestPrice -= padding;

        float totalWidth = chartArea.rect.width;
        float candleAreaWidth = Mathf.Max(1f, totalWidth - priceLabelWidth - chartRightPadding);

        DrawGrid(lowestPrice, highestPrice, candleAreaWidth);

        // Uniform slot sizing: Divides width by fixed maxCandles to prevent oversized elements on early days
        int slotCount = Mathf.Max(maxCandles, displayedCandles.Count);
        float slotWidth = candleAreaWidth / slotCount;

        for (int i = 0; i < displayedCandles.Count; i++)
        {
            CandleData candle = displayedCandles[i];
            float candleWidth = slotWidth * candleWidthPercent;
            float x = (i * slotWidth) + (slotWidth * 0.5f);

            DrawCandle(candle, x, candleWidth, lowestPrice, highestPrice);
            CreateFooterLabel(candle.dayIndex, x, slotWidth);
        }
    }

    private void DrawCandle(CandleData candle, float x, float candleWidth, float lowestPrice, float highestPrice)
    {
        Color candleColor = candle.close > candle.open ? bullishColor : (candle.close < candle.open ? bearishColor : dojiColor);

        float highY = PriceToY(candle.high, lowestPrice, highestPrice);
        float lowY = PriceToY(candle.low, lowestPrice, highestPrice);
        float openY = PriceToY(candle.open, lowestPrice, highestPrice);
        float closeY = PriceToY(candle.close, lowestPrice, highestPrice);

        float wickTop = Mathf.Max(highY, lowY);
        float wickBottom = Mathf.Min(highY, lowY);
        float wickHeight = Mathf.Max(wickTop - wickBottom, 1f);

        string tooltipText = $"<b>Day {candle.dayIndex}</b>\n" +
                            $"Open: {FormatPrice(candle.open)}\n" +
                            $"High: {FormatPrice(candle.high)}\n" +
                            $"Low: {FormatPrice(candle.low)}\n" +
                            $"Close: {FormatPrice(candle.close)}";

        RectTransform wick = CreateImage("Wick", candleColor, true);
        wick.SetParent(chartArea, false);
        wick.anchorMin = Vector2.zero;
        wick.anchorMax = Vector2.zero;
        wick.pivot = new Vector2(0.5f, 0.5f);
        wick.anchoredPosition = new Vector2(x, wickBottom + wickHeight * 0.5f);
        wick.sizeDelta = new Vector2(wickWidth, wickHeight);
        wick.gameObject.AddComponent<ChartHoverTrigger>().Init(tooltipText);

        float bodyTop = Mathf.Max(openY, closeY);
        float bodyBottom = Mathf.Min(openY, closeY);
        float bodyHeight = Mathf.Max(bodyTop - bodyBottom, minimumBodyHeight);

        RectTransform body = CreateImage("CandleBody", candleColor, true);
        body.SetParent(chartArea, false);
        body.anchorMin = Vector2.zero;
        body.anchorMax = Vector2.zero;
        body.pivot = new Vector2(0.5f, 0.5f);
        body.anchoredPosition = new Vector2(x, bodyBottom + bodyHeight * 0.5f);
        body.sizeDelta = new Vector2(candleWidth, bodyHeight);
        body.gameObject.AddComponent<ChartHoverTrigger>().Init(tooltipText);
    }

    private void DrawGrid(float lowestPrice, float highestPrice, float candleAreaWidth)
    {
        if (gridLineCount <= 0) return;
        float printableHeight = chartArea.rect.height - footerHeight;

        for (int i = 0; i <= gridLineCount; i++)
        {
            float normalized = i / (float)gridLineCount;
            float y = footerHeight + (normalized * printableHeight);
            float price = Mathf.Lerp(lowestPrice, highestPrice, normalized);

            RectTransform line = CreateImage("GridLine", gridColor, false);
            line.SetParent(chartArea, false);
            line.anchorMin = Vector2.zero;
            line.anchorMax = Vector2.zero;
            line.pivot = new Vector2(0f, 0.5f);
            line.anchoredPosition = new Vector2(0f, y);
            line.sizeDelta = new Vector2(candleAreaWidth, gridLineWidth);

            CreatePriceLabel(price, y, candleAreaWidth);
        }
    }

    private void CreatePriceLabel(float price, float y, float candleAreaWidth)
    {
        GameObject labelObject = new GameObject("PriceLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(chartArea, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(candleAreaWidth + chartRightPadding, y);
        rect.sizeDelta = new Vector2(priceLabelWidth, 25f);

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.text = FormatPrice(price);
        text.color = textColor;
        text.fontSize = priceFontSize;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        if (priceFont != null) text.font = priceFont;
    }

    private void CreateFooterLabel(int dayNumber, float x, float slotWidth)
    {
        GameObject labelObject = new GameObject($"DayLabel_{dayNumber}", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(chartArea, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, footerHeight * 0.5f);
        rect.sizeDelta = new Vector2(slotWidth, footerHeight);

        TextMeshProUGUI text = labelObject.GetComponent<TextMeshProUGUI>();
        text.text = $"Day {dayNumber}";
        text.color = textColor;
        text.fontSize = dayFontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        if (priceFont != null) text.font = priceFont;
    }

    private string FormatPrice(float price) => price.ToString("#,##0.##", CultureInfo.InvariantCulture);

    private RectTransform CreateImage(string objectName, Color color, bool raycastTarget = false)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return imageObject.GetComponent<RectTransform>();
    }

    private float PriceToY(float price, float lowestPrice, float highestPrice)
    {
        float normalized = Mathf.InverseLerp(lowestPrice, highestPrice, price);
        float printableHeight = chartArea.rect.height - footerHeight;
        return footerHeight + (normalized * printableHeight);
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