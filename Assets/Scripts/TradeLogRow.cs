using UnityEngine;
using TMPro;

public class TradeLogRowUI : MonoBehaviour
{
    [Header("Buy Columns")]
    public TMP_Text buyNoText;       // Lots
    public TMP_Text buySahamText;    // Ticker
    public TMP_Text buyAvgText;      // Price per share
    public TMP_Text buyValueText;    // Gross Value
    public TMP_Text buyFeeText;      // Fee
    public TMP_Text buyTotalText;    // Total Cost

    [Header("Sell Columns")]
    public TMP_Text sellNoText;      // Lots
    public TMP_Text sellSahamText;   // Ticker
    public TMP_Text sellAvgText;     // Price per share
    public TMP_Text sellValueText;   // Gross Revenue
    public TMP_Text sellFeeText;     // Fee
    public TMP_Text sellTotalText;   // Net Revenue

    [Header("Gain / Loss Columns")]
    public TMP_Text glText;          // G / L
    public TMP_Text glPercentText;   // G / L %

    // Dipanggil saat baris pertama kali dibuat untuk mengosongkan semua kolom awal
    public void InitEmptyRow()
    {
        if (buyNoText) buyNoText.text = "";
        if (buySahamText) buySahamText.text = "";
        if (buyAvgText) buyAvgText.text = "";
        if (buyValueText) buyValueText.text = "";
        if (buyFeeText) buyFeeText.text = "";
        if (buyTotalText) buyTotalText.text = "";

        if (sellNoText) sellNoText.text = "";
        if (sellSahamText) sellSahamText.text = "";
        if (sellAvgText) sellAvgText.text = "";
        if (sellValueText) sellValueText.text = "";
        if (sellFeeText) sellFeeText.text = "";
        if (sellTotalText) sellTotalText.text = "";

        if (glText) glText.text = "";
        if (glPercentText) glPercentText.text = "";

        gameObject.SetActive(true);
    }

    // Mengisi khusus kolom BUY (tanpa menghapus kolom SELL)
    public void SetupBuyColumns(int lots, string symbol, float avgPrice, float grossVal, float fee, float total)
    {
        if (buyNoText != null) buyNoText.text = lots.ToString("N0");
        if (buySahamText != null) buySahamText.text = symbol;
        if (buyAvgText != null) buyAvgText.text = avgPrice.ToString("N0");
        if (buyValueText != null) buyValueText.text = grossVal.ToString("N0");
        if (buyFeeText != null) buyFeeText.text = fee.ToString("N0");
        if (buyTotalText != null) buyTotalText.text = total.ToString("N0");
    }

    // Mengisi khusus kolom SELL dan Profit/Loss (tanpa menghapus kolom BUY)
    public void SetupSellColumns(int lots, string symbol, float avgPrice, float grossVal, float fee, float netTotal, float gainLoss, float gainLossPercent)
    {
        if (sellNoText != null) sellNoText.text = lots.ToString("N0");
        if (sellSahamText != null) sellSahamText.text = symbol;
        if (sellAvgText != null) sellAvgText.text = avgPrice.ToString("N0");
        if (sellValueText != null) sellValueText.text = grossVal.ToString("N0");
        if (sellFeeText != null) sellFeeText.text = fee.ToString("N0");
        if (sellTotalText != null) sellTotalText.text = netTotal.ToString("N0");

        if (glText != null)
        {
            string sign = gainLoss >= 0 ? "+" : "";
            string color = gainLoss >= 0 ? "green" : "red";
            glText.text = $"<color={color}>{sign}{gainLoss:N0}</color>";
        }

        if (glPercentText != null)
        {
            string sign = gainLossPercent >= 0 ? "+" : "";
            string color = gainLossPercent >= 0 ? "green" : "red";
            glPercentText.text = $"<color={color}>{sign}{gainLossPercent:F2}%</color>";
        }
    }
}