using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MainMenuAnimatedChart : MonoBehaviour
{
    [Serializable]
    public class SimulatedCandle
    {
        public float open;
        public float high;
        public float low;
        public float close;
        public float volume;

        public SimulatedCandle(float open, float high, float low, float close, float volume)
        {
            this.open = open;
            this.high = high;
            this.low = low;
            this.close = close;
            this.volume = volume;
        }
    }

    [Header("Chart Containers (From Your UI Layout)")]
    [Tooltip("Top-left dark panel for Candlestick chart")]
    public RectTransform candlestickArea;
    [Tooltip("Bottom-left dark panel for Volume chart")]
    public RectTransform volumeArea;

    [Header("Animation & Slide Settings")]
    [Tooltip("Duration in seconds for a single candle to smoothly animate its price movement")]
    public float candleAnimationDuration = 3.0f;
    [Tooltip("Pause duration in seconds after a candle finishes before spawning the next one")]
    public float delayBetweenCandles = 2.0f;
    [Tooltip("Duration in seconds for the smooth horizontal scroll when chart reaches max capacity")]
    public float slideAnimationDuration = 0.6f;
    [Tooltip("Refresh interval while animating (lower = smoother movement)")]
    public float tickInterval = 0.02f;

    [Header("Capacity & Sizing")]
    public int maxBars = 32;
    [Range(0.1f, 1f)]
    public float barWidthPercent = 0.75f;
    public float wickWidth = 2f;
    public float minimumBodyHeight = 2f;

    [Header("Simulation Algorithm")]
    public float startingPrice = 5000f;
    [Range(0.005f, 0.1f)]
    public float priceVolatility = 0.025f;
    public float baseVolume = 1500000f;

    [Header("Right Scale & Padding")]
    public float rightLabelWidth = 65f;
    public float chartRightPadding = 8f;
    public float verticalPadding = 12f;

    [Header("Colors & Styling")]
    [Tooltip("Hex #00FF00")]
    public Color bullishColor = new Color(0f, 1f, 0f, 1f);
    [Tooltip("Hex #FF0000")]
    public Color bearishColor = new Color(1f, 0f, 0f, 1f);
    public Color dojiColor = Color.gray;
    [Tooltip("Hex #3399FF for all volume bars")]
    public Color volumeBarColor = new Color(0.2f, 0.6f, 1f, 0.8f);
    public Color gridColor = new Color(1f, 1f, 1f, 0.15f);
    public int candlestickGridLines = 4;
    public int volumeGridLines = 2;
    public float gridLineWidth = 1f;

    [Header("Labels & Typography")]
    public bool showRightScaleLabels = true;
    public Color textColor = new Color(1f, 1f, 1f, 0.8f);
    public int labelFontSize = 12;
    public TMP_FontAsset labelFont;

    private readonly List<SimulatedCandle> candles = new List<SimulatedCandle>();
    private float currentGridMin = 0f;
    private float currentGridMax = 1f;
    private float lastClosePrice;
    private float horizontalShiftSlots = 0f; // 0 = normal, 0 -> 1 = smooth shift left by 1 slot
    private Coroutine animationCoroutine;

    private void Start()
    {
        lastClosePrice = startingPrice;
        candles.Clear();
        horizontalShiftSlots = 0f;
        StartSimulation();
    }

    private void OnEnable()
    {
        if (animationCoroutine == null && Application.isPlaying)
        {
            StartSimulation();
        }
    }

    private void OnDisable()
    {
        StopSimulation();
    }

    private void StartSimulation()
    {
        StopSimulation();
        animationCoroutine = StartCoroutine(SimulationLoop());
    }

    private void StopSimulation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    private IEnumerator SimulationLoop()
    {
        // Draw initial empty grid
        yield return null;
        RedrawAllCharts();

        while (true)
        {
            // If chart is already full (at maxBars), smoothly slide all bars 1 slot to the left first
            if (candles.Count >= maxBars)
            {
                float slideElapsed = 0f;
                while (slideElapsed < slideAnimationDuration)
                {
                    slideElapsed += tickInterval;
                    float t = Mathf.Clamp01(slideElapsed / slideAnimationDuration);
                    // Smoothstep easing for smooth horizontal slide
                    horizontalShiftSlots = t * t * (3f - 2f * t);
                    RedrawAllCharts();
                    yield return new WaitForSeconds(tickInterval);
                }

                // Remove the oldest bar that just slid off the left edge and reset offset
                candles.RemoveAt(0);
                horizontalShiftSlots = 0f;
            }

            SimulatedCandle nextCandle = GenerateNextCandle(lastClosePrice);
            float targetClose = nextCandle.close;
            float targetHigh = nextCandle.high;
            float targetLow = nextCandle.low;
            float targetVol = nextCandle.volume;

            SimulatedCandle liveCandle = new SimulatedCandle(
                nextCandle.open,
                nextCandle.open,
                nextCandle.open,
                nextCandle.open,
                targetVol * 0.15f
            );
            candles.Add(liveCandle);

            // Animate the new candle slowly over candleAnimationDuration (3 seconds)
            float elapsed = 0f;
            float phaseOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

            while (elapsed < candleAnimationDuration)
            {
                elapsed += tickInterval;
                float rawProgress = Mathf.Clamp01(elapsed / candleAnimationDuration);
                float easedProgress = rawProgress * rawProgress * (3f - 2f * rawProgress);

                float swingAmplitude = (targetHigh - targetLow) * 0.25f * Mathf.Sin(rawProgress * Mathf.PI);
                float gentleWave = Mathf.Sin((rawProgress * Mathf.PI * 2.5f) + phaseOffset) * swingAmplitude;

                float currentPrice = Mathf.Lerp(liveCandle.open, targetClose, easedProgress) + gentleWave;
                currentPrice = Mathf.Max(10f, currentPrice);

                liveCandle.close = currentPrice;
                liveCandle.high = Mathf.Max(liveCandle.high, currentPrice);
                liveCandle.low = Mathf.Min(liveCandle.low, currentPrice);
                liveCandle.volume = Mathf.Lerp(targetVol * 0.15f, targetVol, easedProgress);

                RedrawAllCharts();
                yield return new WaitForSeconds(tickInterval);
            }

            liveCandle.high = Mathf.Max(liveCandle.high, targetHigh);
            liveCandle.low = Mathf.Min(liveCandle.low, targetLow);
            liveCandle.close = targetClose;
            liveCandle.volume = targetVol;
            lastClosePrice = targetClose;
            RedrawAllCharts();

            // Pause between candles (2 seconds)
            if (delayBetweenCandles > 0f)
            {
                yield return new WaitForSeconds(delayBetweenCandles);
            }
        }
    }

    private SimulatedCandle GenerateNextCandle(float openPrice)
    {
        float reversion = (startingPrice - openPrice) * 0.04f;
        float percentChange = UnityEngine.Random.Range(-priceVolatility, priceVolatility);
        float closePrice = Mathf.Max(50f, openPrice * (1f + percentChange) + reversion);

        float bodyHigh = Mathf.Max(openPrice, closePrice);
        float bodyLow = Mathf.Min(openPrice, closePrice);

        float upperWick = openPrice * UnityEngine.Random.Range(0.002f, priceVolatility * 0.6f);
        float lowerWick = openPrice * UnityEngine.Random.Range(0.002f, priceVolatility * 0.6f);

        float highPrice = bodyHigh + upperWick;
        float lowPrice = Mathf.Max(10f, bodyLow - lowerWick);

        float moveIntensity = Mathf.Abs(closePrice - openPrice) / Mathf.Max(1f, openPrice);
        float volumeMultiplier = UnityEngine.Random.Range(0.5f, 1.4f) + (moveIntensity * 25f);
        float volume = baseVolume * volumeMultiplier;

        return new SimulatedCandle(openPrice, highPrice, lowPrice, closePrice, volume);
    }

    public void RedrawAllCharts()
    {
        if (candlestickArea != null && candlestickArea.rect.width > 0 && candlestickArea.rect.height > 0)
        {
            DrawCandlestickChart();
        }

        if (volumeArea != null && volumeArea.rect.width > 0 && volumeArea.rect.height > 0)
        {
            DrawVolumeChart();
        }
    }

    private void DrawCandlestickChart()
    {
        ClearContainer(candlestickArea);

        float highestPrice = startingPrice * 1.02f;
        float lowestPrice = startingPrice * 0.98f;

        if (candles.Count > 0)
        {
            highestPrice = float.MinValue;
            lowestPrice = float.MaxValue;

            for (int i = 0; i < candles.Count; i++)
            {
                highestPrice = Mathf.Max(highestPrice, candles[i].high);
                lowestPrice = Mathf.Min(lowestPrice, candles[i].low);
            }
        }

        if (Mathf.Approximately(highestPrice, lowestPrice))
        {
            highestPrice += 10f;
            lowestPrice -= 10f;
        }

        float priceRange = highestPrice - lowestPrice;
        float roughStep = priceRange / Mathf.Max(1, candlestickGridLines);
        float magnitude = Mathf.Pow(10, Mathf.Floor(Mathf.Log10(roughStep > 0 ? roughStep : 1f)));
        float residual = roughStep / magnitude;
        float cleanStep;

        if (residual < 1.5f) cleanStep = 1f * magnitude;
        else if (residual < 3f) cleanStep = 2f * magnitude;
        else if (residual < 7f) cleanStep = 5f * magnitude;
        else cleanStep = 10f * magnitude;

        currentGridMin = Mathf.Floor(lowestPrice / cleanStep) * cleanStep;
        currentGridMax = Mathf.Ceil(highestPrice / cleanStep) * cleanStep;

        if (Mathf.Approximately(currentGridMin, currentGridMax))
        {
            currentGridMin -= cleanStep;
            currentGridMax += cleanStep;
        }

        float totalWidth = candlestickArea.rect.width;
        float reservedRight = showRightScaleLabels ? (rightLabelWidth + chartRightPadding) : chartRightPadding;
        float plotWidth = Mathf.Max(1f, totalWidth - reservedRight);

        DrawPriceGrid(plotWidth);

        if (candles.Count == 0) return;

        float slotWidth = plotWidth / maxBars;
        for (int i = 0; i < candles.Count; i++)
        {
            SimulatedCandle candle = candles[i];
            float candleWidth = slotWidth * barWidthPercent;
            float x = ((i - horizontalShiftSlots) * slotWidth) + (slotWidth * 0.5f);

            // Skip drawing bars that have slid completely past the left border
            if (x + (candleWidth * 0.5f) < 0f) continue;

            DrawSingleCandle(candle, x, candleWidth);
        }
    }

    private void DrawSingleCandle(SimulatedCandle candle, float x, float candleWidth)
    {
        Color color = candle.close > candle.open ? bullishColor : (candle.close < candle.open ? bearishColor : dojiColor);

        float highY = PriceToY(candle.high);
        float lowY = PriceToY(candle.low);
        float openY = PriceToY(candle.open);
        float closeY = PriceToY(candle.close);

        float wickHeight = Mathf.Max(Mathf.Abs(highY - lowY), 1f);
        RectTransform wick = CreateUIImage("Wick", color);
        wick.SetParent(candlestickArea, false);
        wick.anchorMin = Vector2.zero;
        wick.anchorMax = Vector2.zero;
        wick.pivot = new Vector2(0.5f, 0.5f);
        wick.anchoredPosition = new Vector2(x, Mathf.Min(highY, lowY) + wickHeight * 0.5f);
        wick.sizeDelta = new Vector2(wickWidth, wickHeight);

        float bodyHeight = Mathf.Max(Mathf.Abs(openY - closeY), minimumBodyHeight);
        RectTransform body = CreateUIImage("Body", color);
        body.SetParent(candlestickArea, false);
        body.anchorMin = Vector2.zero;
        body.anchorMax = Vector2.zero;
        body.pivot = new Vector2(0.5f, 0.5f);
        body.anchoredPosition = new Vector2(x, Mathf.Min(openY, closeY) + bodyHeight * 0.5f);
        body.sizeDelta = new Vector2(candleWidth, bodyHeight);
    }

    private void DrawVolumeChart()
    {
        ClearContainer(volumeArea);

        float maxVolume = baseVolume * 1.5f;
        if (candles.Count > 0)
        {
            maxVolume = float.MinValue;
            for (int i = 0; i < candles.Count; i++)
            {
                maxVolume = Mathf.Max(maxVolume, candles[i].volume);
            }
            if (maxVolume <= 0f) maxVolume = 1f;
        }

        float totalWidth = volumeArea.rect.width;
        float reservedRight = showRightScaleLabels ? (rightLabelWidth + chartRightPadding) : chartRightPadding;
        float plotWidth = Mathf.Max(1f, totalWidth - reservedRight);

        DrawVolumeGrid(maxVolume, plotWidth);

        if (candles.Count == 0) return;

        float slotWidth = plotWidth / maxBars;
        float printableHeight = Mathf.Max(1f, volumeArea.rect.height - (verticalPadding * 2f));

        for (int i = 0; i < candles.Count; i++)
        {
            SimulatedCandle candle = candles[i];
            float width = slotWidth * barWidthPercent;
            float x = ((i - horizontalShiftSlots) * slotWidth) + (slotWidth * 0.5f);

            if (x + (width * 0.5f) < 0f) continue;

            float normalized = Mathf.Clamp01(candle.volume / maxVolume);
            float barHeight = Mathf.Max(2f, normalized * printableHeight);

            RectTransform bar = CreateUIImage("VolumeBar", volumeBarColor);
            bar.SetParent(volumeArea, false);
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = Vector2.zero;
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(x, verticalPadding);
            bar.sizeDelta = new Vector2(width, barHeight);
        }
    }

    private void DrawPriceGrid(float plotWidth)
    {
        if (candlestickGridLines <= 0) return;
        float printableHeight = candlestickArea.rect.height - (verticalPadding * 2f);

        for (int i = 0; i <= candlestickGridLines; i++)
        {
            float normalized = i / (float)candlestickGridLines;
            float y = verticalPadding + (normalized * printableHeight);
            float price = Mathf.Lerp(currentGridMin, currentGridMax, normalized);

            RectTransform line = CreateUIImage("PriceGridLine", gridColor);
            line.SetParent(candlestickArea, false);
            line.anchorMin = Vector2.zero;
            line.anchorMax = Vector2.zero;
            line.pivot = new Vector2(0f, 0.5f);
            line.anchoredPosition = new Vector2(0f, y);
            line.sizeDelta = new Vector2(plotWidth, gridLineWidth);

            if (showRightScaleLabels)
            {
                CreateRightLabel(candlestickArea, FormatPrice(price), y, plotWidth);
            }
        }
    }

    private void DrawVolumeGrid(float maxVolume, float plotWidth)
    {
        if (volumeGridLines <= 0) return;
        float printableHeight = volumeArea.rect.height - (verticalPadding * 2f);

        for (int i = 0; i <= volumeGridLines; i++)
        {
            float normalized = i / (float)volumeGridLines;
            float y = verticalPadding + (normalized * printableHeight);
            float volValue = maxVolume * normalized;

            RectTransform line = CreateUIImage("VolumeGridLine", gridColor);
            line.SetParent(volumeArea, false);
            line.anchorMin = Vector2.zero;
            line.anchorMax = Vector2.zero;
            line.pivot = new Vector2(0f, 0.5f);
            line.anchoredPosition = new Vector2(0f, y);
            line.sizeDelta = new Vector2(plotWidth, gridLineWidth);

            if (showRightScaleLabels && i > 0)
            {
                CreateRightLabel(volumeArea, FormatVolume(volValue), y, plotWidth);
            }
        }
    }

    private void CreateRightLabel(RectTransform parent, string textContent, float y, float plotWidth)
    {
        GameObject labelObj = new GameObject("ScaleLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(parent, false);

        RectTransform rect = labelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(plotWidth + chartRightPadding, y);
        rect.sizeDelta = new Vector2(rightLabelWidth, 22f);

        TextMeshProUGUI text = labelObj.GetComponent<TextMeshProUGUI>();
        text.text = textContent;
        text.color = textColor;
        text.fontSize = labelFontSize;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        if (labelFont != null) text.font = labelFont;
    }

    private float PriceToY(float price)
    {
        float normalized = Mathf.InverseLerp(currentGridMin, currentGridMax, price);
        float printableHeight = candlestickArea.rect.height - (verticalPadding * 2f);
        return verticalPadding + (normalized * printableHeight);
    }

    private string FormatPrice(float price) => Mathf.RoundToInt(price).ToString("#,##0", CultureInfo.InvariantCulture);

    private string FormatVolume(float vol)
    {
        if (vol >= 1_000_000_000f) return (vol / 1_000_000_000f).ToString("0.#") + "B";
        if (vol >= 1_000_000f) return (vol / 1_000_000f).ToString("0.#") + "M";
        if (vol >= 1_000f) return (vol / 1_000f).ToString("0.#") + "K";
        return vol.ToString("#,##0", CultureInfo.InvariantCulture);
    }

    private RectTransform CreateUIImage(string objectName, Color color)
    {
        GameObject imgObj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Image image = imgObj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return imgObj.GetComponent<RectTransform>();
    }

    private void ClearContainer(RectTransform container)
    {
        if (container == null) return;
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
}