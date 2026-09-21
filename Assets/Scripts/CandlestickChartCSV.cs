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

        public CandleData(
            string date,
            float open,
            float high,
            float low,
            float close)
        {
            this.date = date;
            this.open = open;
            this.high = high;
            this.low = low;
            this.close = close;
        }
    }

    [Header("CSV")]
    public TextAsset csvFile;

    [Header("Chart")]
    public RectTransform chartArea;

    [Tooltip("Number of candles visible on screen.")]
    public int maxCandles = 30;

    [Tooltip("Percentage of each candle slot occupied by the candle body.")]
    [Range(0.1f, 1f)]
    public float candleWidthPercent = 0.8f;

    [Tooltip("Width of the high/low wick.")]
    public float wickWidth = 2f;

    [Tooltip("Minimum visible candle body height in pixels.")]
    public float minimumBodyHeight = 2f;

    [Header("Simulation")]
    [Tooltip("Seconds between each new candle.")]
    public float updateInterval = 5f;

    [Tooltip("Automatically advance through the CSV.")]
    public bool autoUpdate = true;

    [Header("Price Scale")]
    [Tooltip("Space reserved on the left for price labels.")]
    public float priceLabelWidth = 70f;

    [Tooltip("Space between price labels and the chart.")]
    public float chartLeftPadding = 5f;

    [Header("Candle Colors")]
    public Color bullishColor = Color.green;
    public Color bearishColor = Color.red;
    public Color dojiColor = Color.gray;

    [Header("Grid")]
    public Color gridColor =
        new Color(1f, 1f, 1f, 0.15f);

    public int gridLineCount = 5;

    public float gridLineWidth = 1f;

    [Header("Price Labels")]
    public Color textColor = Color.white;

    public int priceFontSize = 14;

    [Tooltip("Optional TMP font.")]
    public TMP_FontAsset priceFont;


    private readonly List<CandleData> allCandles =
        new List<CandleData>();

    
    private readonly List<CandleData> displayedCandles =
        new List<CandleData>();


    private int nextCandleIndex = 0;

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
        allCandles.Clear();

        if (csvFile == null)
        {
            Debug.LogError(
                "CandlestickChart: No CSV file assigned."
            );

            return;
        }

        string[] lines =
            csvFile.text.Split(
                new[] { '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries
            );

        if (lines.Length <= 1)
        {
            Debug.LogWarning(
                "CandlestickChart: CSV contains no data."
            );

            return;
        }


        string[] headers =
            SplitCSVLine(lines[0]);

        int dateIndex =
            FindColumnIndex(headers, "Date");

        int openIndex =
            FindColumnIndex(headers, "Open");

        int highIndex =
            FindColumnIndex(headers, "High");

        int lowIndex =
            FindColumnIndex(headers, "Low");

        int closeIndex =
            FindColumnIndex(headers, "Close");

        if (dateIndex == -1 ||
            openIndex == -1 ||
            highIndex == -1 ||
            lowIndex == -1 ||
            closeIndex == -1)
        {
            Debug.LogError(
                "CandlestickChart: Could not find required CSV columns."
            );

            return;
        }

        // ========================================================
        // Read every CSV row.
        // ========================================================

        for (int i = 1; i < lines.Length; i++)
        {
            string line =
                lines[i].Trim();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            string[] values =
                SplitCSVLine(line);

            int maximumIndex =
                Mathf.Max(
                    dateIndex,
                    openIndex,
                    highIndex,
                    lowIndex,
                    closeIndex
                );

            if (values.Length <= maximumIndex)
            {
                Debug.LogWarning(
                    $"CandlestickChart: Skipping CSV line {i + 1}."
                );

                continue;
            }

            string date =
                values[dateIndex]
                    .Trim()
                    .Trim('"');

            bool openValid =
                TryParseFloat(
                    values[openIndex],
                    out float open
                );

            bool highValid =
                TryParseFloat(
                    values[highIndex],
                    out float high
                );

            bool lowValid =
                TryParseFloat(
                    values[lowIndex],
                    out float low
                );

            bool closeValid =
                TryParseFloat(
                    values[closeIndex],
                    out float close
                );

            if (!openValid ||
                !highValid ||
                !lowValid ||
                !closeValid)
            {
                Debug.LogWarning(
                    $"CandlestickChart: Invalid data on CSV line {i + 1}."
                );

                continue;
            }

            allCandles.Add(
                new CandleData(
                    date,
                    open,
                    high,
                    low,
                    close
                )
            );
        }

        Debug.Log(
            $"CandlestickChart: Loaded {allCandles.Count} candles."
        );
    }


    private void InitializeChart()
    {
        displayedCandles.Clear();

        nextCandleIndex = 0;

        if (allCandles.Count == 0)
        {
            RedrawChart();
            return;
        }


        int initialCount =
            Mathf.Min(
                maxCandles,
                allCandles.Count
            );

        for (int i = 0;
             i < initialCount;
             i++)
        {
            displayedCandles.Add(
                allCandles[i]
            );
        }

        nextCandleIndex =
            initialCount;

        RedrawChart();
    }


    public void StartUpdating()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);
        }

        updateCoroutine =
            StartCoroutine(
                UpdateChartRoutine()
            );
    }

    public void StopUpdating()
    {
        if (updateCoroutine != null)
        {
            StopCoroutine(updateCoroutine);

            updateCoroutine = null;
        }
    }


    private IEnumerator UpdateChartRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(
                updateInterval
            );

            AdvanceOneCandle();


            if (nextCandleIndex >= allCandles.Count)
            {
                updateCoroutine = null;

                Debug.Log(
                    "CandlestickChart: Reached end of CSV."
                );

                yield break;
            }
        }
    }


    public void AdvanceOneCandle()
    {
        if (allCandles.Count == 0)
        {
            return;
        }

        if (nextCandleIndex >= allCandles.Count)
        {
            Debug.Log(
                "CandlestickChart: No more candles available."
            );

            return;
        }


        if (displayedCandles.Count >= maxCandles)
        {
            displayedCandles.RemoveAt(0);
        }


        CandleData newCandle =
            allCandles[nextCandleIndex];

        displayedCandles.Add(
            newCandle
        );

        nextCandleIndex++;


        RedrawChart();

        Debug.Log(
            $"CandlestickChart: Added {newCandle.date}"
        );
    }


    public void RedrawChart()
    {
        ClearChart();

        if (chartArea == null)
        {
            Debug.LogError(
                "CandlestickChart: Chart Area is not assigned."
            );

            return;
        }

        if (displayedCandles.Count == 0)
        {
            return;
        }


        float highestPrice =
            float.MinValue;

        float lowestPrice =
            float.MaxValue;

        for (int i = 0;
             i < displayedCandles.Count;
             i++)
        {
            CandleData candle =
                displayedCandles[i];

            highestPrice =
                Mathf.Max(
                    highestPrice,
                    candle.high
                );

            lowestPrice =
                Mathf.Min(
                    lowestPrice,
                    candle.low
                );
        }

        if (Mathf.Approximately(
            highestPrice,
            lowestPrice))
        {
            highestPrice += 1f;
            lowestPrice -= 1f;
        }

        float priceRange =
            highestPrice - lowestPrice;

        float padding =
            priceRange * 0.05f;

        highestPrice += padding;
        lowestPrice -= padding;


        float totalWidth =
            chartArea.rect.width;

        float candleAreaWidth =
            totalWidth
            - priceLabelWidth
            - chartLeftPadding;

        candleAreaWidth =
            Mathf.Max(
                1f,
                candleAreaWidth
            );


        DrawGrid(
            lowestPrice,
            highestPrice,
            candleAreaWidth
        );


        float slotWidth =
            candleAreaWidth /
            displayedCandles.Count;


        for (int i = 0;
             i < displayedCandles.Count;
             i++)
        {
            CandleData candle =
                displayedCandles[i];

            float candleWidth =
                slotWidth *
                candleWidthPercent;

            float x =
                priceLabelWidth
                + chartLeftPadding
                + (i * slotWidth)
                + (slotWidth * 0.5f);

            DrawCandle(
                candle,
                x,
                candleWidth,
                lowestPrice,
                highestPrice
            );
        }
    }

    private void DrawCandle(
        CandleData candle,
        float x,
        float candleWidth,
        float lowestPrice,
        float highestPrice)
    {
        Color candleColor;

        if (candle.close > candle.open)
        {
            candleColor =
                bullishColor;
        }
        else if (candle.close < candle.open)
        {
            candleColor =
                bearishColor;
        }
        else
        {
            candleColor =
                dojiColor;
        }

        float highY =
            PriceToY(
                candle.high,
                lowestPrice,
                highestPrice
            );

        float lowY =
            PriceToY(
                candle.low,
                lowestPrice,
                highestPrice
            );

        float openY =
            PriceToY(
                candle.open,
                lowestPrice,
                highestPrice
            );

        float closeY =
            PriceToY(
                candle.close,
                lowestPrice,
                highestPrice
            );

        float wickTop =
            Mathf.Max(
                highY,
                lowY
            );

        float wickBottom =
            Mathf.Min(
                highY,
                lowY
            );

        float wickHeight =
            Mathf.Max(
                wickTop - wickBottom,
                1f
            );

        RectTransform wick =
            CreateImage(
                "Wick",
                candleColor
            );

        wick.SetParent(
            chartArea,
            false
        );

        wick.anchorMin =
            new Vector2(0f, 0f);

        wick.anchorMax =
            new Vector2(0f, 0f);

        wick.pivot =
            new Vector2(0.5f, 0.5f);

        wick.anchoredPosition =
            new Vector2(
                x,
                wickBottom
                + wickHeight * 0.5f
            );

        wick.sizeDelta =
            new Vector2(
                wickWidth,
                wickHeight
            );

        float bodyTop =
            Mathf.Max(
                openY,
                closeY
            );

        float bodyBottom =
            Mathf.Min(
                openY,
                closeY
            );

        float bodyHeight =
            Mathf.Max(
                bodyTop - bodyBottom,
                minimumBodyHeight
            );

        RectTransform body =
            CreateImage(
                "CandleBody",
                candleColor
            );

        body.SetParent(
            chartArea,
            false
        );

        body.anchorMin =
            new Vector2(0f, 0f);

        body.anchorMax =
            new Vector2(0f, 0f);

        body.pivot =
            new Vector2(0.5f, 0.5f);

        body.anchoredPosition =
            new Vector2(
                x,
                bodyBottom
                + bodyHeight * 0.5f
            );

        body.sizeDelta =
            new Vector2(
                candleWidth,
                bodyHeight
            );
    }

    private void DrawGrid(
        float lowestPrice,
        float highestPrice,
        float candleAreaWidth)
    {
        if (gridLineCount <= 0)
            return;

        float chartHeight =
            chartArea.rect.height;

        for (int i = 0;
             i <= gridLineCount;
             i++)
        {
            float normalized =
                i / (float)gridLineCount;

            float y =
                normalized * chartHeight;

            float price =
                Mathf.Lerp(
                    lowestPrice,
                    highestPrice,
                    normalized
                );

            RectTransform line =
                CreateImage(
                    "GridLine",
                    gridColor
                );

            line.SetParent(
                chartArea,
                false
            );

            line.anchorMin =
                new Vector2(0f, 0f);

            line.anchorMax =
                new Vector2(0f, 0f);

            line.pivot =
                new Vector2(0f, 0.5f);

            line.anchoredPosition =
                new Vector2(
                    priceLabelWidth
                    + chartLeftPadding,
                    y
                );

            line.sizeDelta =
                new Vector2(
                    candleAreaWidth,
                    gridLineWidth
                );

            CreatePriceLabel(
                price,
                y
            );
        }
    }

    private void CreatePriceLabel(
        float price,
        float y)
    {
        GameObject labelObject =
            new GameObject(
                "PriceLabel",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        labelObject.transform.SetParent(
            chartArea,
            false
        );

        RectTransform rect =
            labelObject.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0f, 0f);

        rect.anchorMax =
            new Vector2(0f, 0f);

        rect.pivot =
            new Vector2(1f, 0.5f);

        rect.anchoredPosition =
            new Vector2(
                priceLabelWidth - 5f,
                y
            );

        rect.sizeDelta =
            new Vector2(
                priceLabelWidth,
                25f
            );

        TextMeshProUGUI text =
            labelObject.GetComponent<TextMeshProUGUI>();

        text.text =
            FormatPrice(price);

        text.color =
            textColor;

        text.fontSize =
            priceFontSize;

        text.alignment =
            TextAlignmentOptions.MidlineRight;

        text.enableWordWrapping =
            false;

        text.overflowMode =
            TextOverflowModes.Overflow;

        text.raycastTarget =
            false;

        if (priceFont != null)
        {
            text.font =
                priceFont;
        }
    }

    private string FormatPrice(
        float price)
    {
        return price.ToString(
            "#,##0.##",
            CultureInfo.InvariantCulture
        );
    }

    private RectTransform CreateImage(
        string objectName,
        Color color)
    {
        GameObject imageObject =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        Image image =
            imageObject.GetComponent<Image>();

        image.color =
            color;

        image.raycastTarget =
            false;

        return imageObject.GetComponent<RectTransform>();
    }

    private float PriceToY(
        float price,
        float lowestPrice,
        float highestPrice)
    {
        float normalized =
            Mathf.InverseLerp(
                lowestPrice,
                highestPrice,
                price
            );

        return normalized *
               chartArea.rect.height;
    }

    private void ClearChart()
    {
        if (chartArea == null)
            return;

        for (int i =
                 chartArea.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                chartArea
                    .GetChild(i)
                    .gameObject
            );
        }
    }

    private string[] SplitCSVLine(
        string line)
    {
        return line.Split(',');
    }

    private int FindColumnIndex(
        string[] headers,
        string columnName)
    {
        for (int i = 0;
             i < headers.Length;
             i++)
        {
            string header =
                headers[i]
                    .Trim()
                    .Trim('"');

            if (string.Equals(
                header,
                columnName,
                StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private bool TryParseFloat(
        string value,
        out float result)
    {
        value =
            value
                .Trim()
                .Trim('"');

        return float.TryParse(
            value,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out result
        );
    }
}