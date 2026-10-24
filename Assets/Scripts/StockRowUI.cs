using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class StockRowUI : MonoBehaviour
{
    [Header("UI Text References")]
    public TMP_Text symbolText;
    public TMP_Text priceText;
    public TMP_Text changeText;
    public TMP_Text volumeText;
    public TMP_Text marketCapText;
    public TMP_Text valueText;

    [Header("UI Button References")]
    public Button seeDetailButton;
    public Button seeGraphButton;

    [Header("Data References")]
    private StockRowData currentData;
    private TextAsset currentPriceCsv;
    private string companyFullName;

    /// <summary>
    /// Dipanggil oleh controller untuk mengisi UI baris saham
    /// </summary>
    public void SetupRow(StockRowData data, TextAsset priceCsv, string companyName = "")
    {
        currentData = data;
        currentPriceCsv = priceCsv;

        if (data == null)
        {
            Debug.LogError("[StockRowUI] Data saham null/kosong!");
            return;
        }

        // Jika companyName tidak diisi dari controller, otomatis ambil dari SelectedStockData
        if (string.IsNullOrEmpty(companyName))
        {
            companyFullName = SelectedStockData.GetCompanyName(data.stockSymbol);
        }
        else
        {
            companyFullName = companyName;
        }

        // Tampilkan data ke UI Text
        if (symbolText != null)
        {
            symbolText.text = data.stockSymbol;
        }
        else
        {
            Debug.LogWarning($"[StockRowUI] Slot 'Symbol Text' di Prefab {gameObject.name} belum di-assign di Inspector!");
        }

        // PERBAIKAN: Awalan "Rp" dihapus, langsung menampilkan format angka
        if (priceText != null) priceText.text = data.price.ToString("#,##0");
        
        if (changeText != null)
        {
            string sign = data.changePercent > 0 ? "+" : "";
            changeText.text = $"{sign}{data.changePercent:F2}%";
            changeText.color = data.changePercent >= 0 ? Color.green : Color.red;
        }

        if (volumeText != null) volumeText.text = FormatLargeNumber(data.volume);
        if (marketCapText != null) marketCapText.text = FormatLargeNumber(data.marketCap);
        if (valueText != null) valueText.text = FormatLargeNumber(data.value);

        // Hubungkan Listener Tombol
        if (seeDetailButton != null)
        {
            seeDetailButton.onClick.RemoveAllListeners();
            seeDetailButton.onClick.AddListener(OnOpenDetail);
        }

        if (seeGraphButton != null)
        {
            seeGraphButton.onClick.RemoveAllListeners();
            seeGraphButton.onClick.AddListener(OnOpenGraph);
        }
    }

    private void OnOpenDetail()
    {
        SaveSelectedData(openOnGraph: false);
        SceneManager.LoadScene("Gamemode (Detail Perusahaan)");
    }

    private void OnOpenGraph()
    {
        SaveSelectedData(openOnGraph: true);
        SceneManager.LoadScene("Gamemode (Detail Perusahaan)");
    }

    private void SaveSelectedData(bool openOnGraph)
    {
        if (currentData == null) return;

        SelectedStockData.StockTicker = currentData.stockSymbol;
        SelectedStockData.CompanyName = !string.IsNullOrEmpty(companyFullName) 
            ? companyFullName 
            : SelectedStockData.GetCompanyName(currentData.stockSymbol);
            
        SelectedStockData.PriceCsvAsset = currentPriceCsv;
        SelectedStockData.OpenOnGraphTab = openOnGraph;
    }

    private string FormatLargeNumber(float num)
    {
        if (num >= 1_000_000_000_000f) return (num / 1_000_000_000_000f).ToString("0.##") + " T";
        if (num >= 1_000_000_000f) return (num / 1_000_000_000f).ToString("0.##") + " M";
        if (num >= 1_000_000f) return (num / 1_000_000f).ToString("0.##") + " Jt";
        if (num >= 1_000f) return (num / 1_000f).ToString("0.##") + " K";

        return num.ToString("#,##0");
    }
}