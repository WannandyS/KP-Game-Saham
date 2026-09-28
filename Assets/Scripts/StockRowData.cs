using System;

[System.Serializable]
public class StockRowData
{
    public string stockSymbol;
    public string date;
    public float price;
    public float volume;
    public float changePercent; // 24H Change %
    public float value;         // Price * Volume
    public float marketCap;     // Price * Total Outstanding Shares
}