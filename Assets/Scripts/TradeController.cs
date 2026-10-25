using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TradeController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text stockNameText;
    public TMP_Text currentPriceText;
    public TMP_InputField shareInputAmount;
    public TMP_Text feedbackText;

    [Header("Confirmation PopUps")]
    public GameObject buyConfirmPopUp;       // PopUp Konfirmasi Beli (Yes/No)
    public GameObject sellConfirmPopUp;      // PopUp Konfirmasi Jual (Yes/No)
    public TMP_Text buyConfirmText;          // Text pesan rincian beli
    public TMP_Text sellConfirmText;         // Text pesan rincian jual

    [Header("Result PopUps")]
    public GameObject buyPopUp;              // PopUp Beli Berhasil
    public GameObject sellPopUp;             // PopUp Jual Berhasil
    public GameObject insufficientCashPopUp; // PopUp Cash Tidak Cukup
    public GameObject noSharesPopUp;         // PopUp Tidak Punya Saham / Saham Kurang

    private string currentDatasetName = "DefaultStock";
    private float currentClosePrice = 0f;
    private string currentDate = "";
    private int currentDayIndex = 1;

    private int pendingShares = 0; // Menyimpan lembar/lot saham yang diinput

    private void Start()
    {
        if (shareInputAmount != null && string.IsNullOrEmpty(shareInputAmount.text))
        {
            shareInputAmount.text = "1";
        }
        SyncWithChart();
        CloseAllPopUps();
    }

    private void OnEnable()
    {
        SyncWithChart();
    }

    public void SyncWithChart()
    {
        CandlestickChart chart = FindAnyObjectByType<CandlestickChart>();
        if (chart != null && DaySimulationManager.Instance != null)
        {
            // Logika sync chart
        }
    }

    public void SetMarketPrice(string datasetName, string date, int dayIndex, float close)
    {
        currentDatasetName = datasetName;
        currentDate = date;
        currentDayIndex = dayIndex;
        currentClosePrice = close;

        if (stockNameText != null) stockNameText.text = datasetName;
        if (currentPriceText != null) currentPriceText.text = $"{close:N2}";
    }

    // --- ALUR BELI ---
    public void OnClickBuy()
    {
        CloseAllPopUps();

        if (!int.TryParse(shareInputAmount.text, out pendingShares) || pendingShares <= 0)
        {
            SetFeedback("Jumlah lembar saham tidak valid.");
            return;
        }

        float totalCost = pendingShares * currentClosePrice;
        if (buyConfirmText != null)
        {
            buyConfirmText.text = $"Apakah Anda yakin ingin membeli {pendingShares} lot {currentDatasetName} seharga Rp{totalCost:N2}?";
        }

        ShowPopUp(buyConfirmPopUp);
    }

    public void ConfirmBuyYes()
    {
        CloseAllPopUps();

        if (PortfolioManager.Instance == null)
        {
            SetFeedback("PortfolioManager tidak ditemukan!");
            return;
        }

        bool success = PortfolioManager.Instance.ExecuteBuy(currentDatasetName, currentDate, currentDayIndex, pendingShares, currentClosePrice);
        
        if (success)
        {
            SetFeedback($"Berhasil membeli {pendingShares}x {currentDatasetName}");
            ShowPopUp(buyPopUp);
        }
        else
        {
            SetFeedback("Gagal beli: Saldo tidak cukup.");
            ShowPopUp(insufficientCashPopUp);
        }
    }

    // --- ALUR JUAL ---
    public void OnClickSell()
    {
        CloseAllPopUps();

        if (!int.TryParse(shareInputAmount.text, out pendingShares) || pendingShares <= 0)
        {
            SetFeedback("Jumlah lembar saham tidak valid.");
            return;
        }

        if (PortfolioManager.Instance == null)
        {
            SetFeedback("PortfolioManager tidak ditemukan!");
            return;
        }

        // Cek kepemilikan saham langsung di PortfolioManager
        int ownedShares = PortfolioManager.Instance.GetOwnedShares(currentDatasetName);

        // Jika tidak punya saham sama sekali atau jumlah saham kurang dari input
        if (ownedShares < pendingShares)
        {
            SetFeedback($"Gagal jual: Saham tidak mencukupi (dimiliki: {ownedShares}).");
            ShowPopUp(noSharesPopUp); // LANGSUNG TAMPILKAN POPUP TIDAK ADA SAHAM
            return;
        }

        // Jika saham mencukupi, baru tampilkan pop-up konfirmasi jual
        float totalRevenue = pendingShares * currentClosePrice;
        if (sellConfirmText != null)
        {
            sellConfirmText.text = $"Apakah Anda yakin ingin menjual {pendingShares} lot {currentDatasetName} seharga Rp{totalRevenue:N2}?";
        }

        ShowPopUp(sellConfirmPopUp);
    }

    public void ConfirmSellYes()
    {
        CloseAllPopUps();

        if (PortfolioManager.Instance == null)
        {
            SetFeedback("PortfolioManager tidak ditemukan!");
            return;
        }

        bool success = PortfolioManager.Instance.ExecuteSell(currentDatasetName, currentDate, currentDayIndex, pendingShares, currentClosePrice);
        
        if (success)
        {
            SetFeedback($"Berhasil menjual {pendingShares}x {currentDatasetName}");
            ShowPopUp(sellPopUp);
        }
        else
        {
            SetFeedback("Gagal jual: Saham tidak cukup.");
            ShowPopUp(noSharesPopUp);
        }
    }

    // --- HELPER POPUP ---
    private void ShowPopUp(GameObject popUp)
    {
        if (popUp != null) popUp.SetActive(true);
    }

    public void CloseAllPopUps()
    {
        if (buyConfirmPopUp != null) buyConfirmPopUp.SetActive(false);
        if (sellConfirmPopUp != null) sellConfirmPopUp.SetActive(false);
        if (buyPopUp != null) buyPopUp.SetActive(false);
        if (sellPopUp != null) sellPopUp.SetActive(false);
        if (insufficientCashPopUp != null) insufficientCashPopUp.SetActive(false);
        if (noSharesPopUp != null) noSharesPopUp.SetActive(false);
    }

    private void SetFeedback(string message)
    {
        if (feedbackText != null) feedbackText.text = message;
        Debug.Log(message);
    }
}