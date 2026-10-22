using System.Collections.Generic;
using UnityEngine;

public static class SelectedStockData
{
    public static string StockTicker = "ADMR";
    public static string CompanyName = "Adaro Minerals Indonesia";
    public static TextAsset PriceCsvAsset;
    public static bool OpenOnGraphTab = false;

    // Dictionary untuk mencocokkan kode saham dengan nama lengkap perusahaan
    private static readonly Dictionary<string, string> companyNames = new Dictionary<string, string>()
    {
        { "ADMR", "Adaro Minerals Indonesia Tbk" },
        { "ASII", "Astra International Tbk" },
        { "BBRI", "Bank Rakyat Indonesia (Persero) Tbk" },
        { "BMRI", "Bank Mandiri (Persero) Tbk" },
        { "CPIN", "Charoen Pokphand Indonesia Tbk" },
        { "EXCL", "XL Axiata Tbk" },
        { "GOTO", "GoTo Gojek Tokopedia Tbk" },
        { "INDF", "Indofood Sukses Makmur Tbk" },
        { "INKP", "Indah Kiat Pulp & Paper Tbk" },
        { "KLBF", "Kalbe Farma Tbk" },
        { "MDKA", "Merdeka Copper Gold Tbk" },
        { "MEDC", "Medco Energi Internasional Tbk" },
        { "PGEO", "Pertamina Geothermal Energy Tbk" },
        { "TLKM", "Telkom Indonesia (Persero) Tbk" }
    };

    public static string GetCompanyName(string ticker)
    {
        if (companyNames.TryGetValue(ticker.ToUpper(), out string fullName))
        {
            return fullName;
        }
        return ticker;
    }
}