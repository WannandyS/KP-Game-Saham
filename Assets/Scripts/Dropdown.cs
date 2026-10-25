using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SearchableDropdown : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI References")]
    public Button mainButton;
    public TMP_Text mainButtonText; // Tetap statis (tidak akan diubah nilainya oleh skrip)
    public GameObject dropdownPanel;
    public TMP_InputField searchInput;
    public Transform contentContainer;
    public GameObject itemPrefab;

    [Header("Chart Integration")]
    public CandlestickChart chartController;
    public VolumeChartCSV volumeChartController;
    public TradeController tradeController;
    public List<TextAsset> csvDatasets = new List<TextAsset>();
    private List<GameObject> itemGameObjects = new List<GameObject>();

    private string currentSelectedTicker = "";

    void Start()
    {
        dropdownPanel.SetActive(false);
        
        if (mainButton != null)
        {
            mainButton.onClick.AddListener(ToggleDropdown);
        }

        if (searchInput != null)
        {
            searchInput.onValueChanged.AddListener(FilterList);
        }

        GenerateAllItems();

        if (tradeController == null)
        {
            tradeController = FindAnyObjectByType<TradeController>();
        }

        if (csvDatasets.Count > 0)
        {
            TextAsset initialCsv = csvDatasets[0];

            if (!string.IsNullOrEmpty(SelectedStockData.StockTicker))
            {
                TextAsset found = csvDatasets.Find(x => x != null && x.name.Equals(SelectedStockData.StockTicker, System.StringComparison.OrdinalIgnoreCase));
                if (found != null) initialCsv = found;
            }

            OnItemSelected(initialCsv);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Hanya kosongkan teks pencarian pada input field saat tombol ditekan
        if (searchInput != null) searchInput.text = "";
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!dropdownPanel.activeSelf)
        {
            RestoreText();
        }
    }

    private void RestoreText()
    {
        // Hanya mengembalikan teks saham aktif ke InputField pencarian
        if (searchInput != null) searchInput.text = currentSelectedTicker;
    }

    public void ToggleDropdown()
    {
        bool isActive = !dropdownPanel.activeSelf;
        dropdownPanel.SetActive(isActive);

        if (isActive)
        {
            if (searchInput != null)
            {
                searchInput.text = "";
                searchInput.Select();
            }
        }
        else
        {
            RestoreText();
        }
    }

    private void GenerateAllItems()
    {
        foreach (TextAsset csvFile in csvDatasets)
        {
            if (csvFile == null) continue;

            GameObject newItem = Instantiate(itemPrefab, contentContainer);

            TMP_Text itemText = newItem.GetComponentInChildren<TMP_Text>();
            if (itemText != null) itemText.text = csvFile.name;

            newItem.GetComponent<Button>().onClick.AddListener(() => OnItemSelected(csvFile));

            itemGameObjects.Add(newItem);
        }
    }

    /// <summary>
    /// Memfilter list kode saham secara ketat dari huruf pertama hingga huruf berikutnya (Strict Prefix Match)
    /// </summary>
    private void FilterList(string query)
    {
        query = query.ToLower().Trim();

        for (int i = 0; i < csvDatasets.Count; i++)
        {
            if (csvDatasets[i] == null) continue;

            string stockName = csvDatasets[i].name.ToLower();

            if (string.IsNullOrEmpty(query))
            {
                itemGameObjects[i].SetActive(true);
            }
            else if (stockName.StartsWith(query))
            {
                itemGameObjects[i].SetActive(true);
            }
            else
            {
                itemGameObjects[i].SetActive(false);
            }
        }
    }

    private void OnItemSelected(TextAsset selectedCsv)
    {
        if (selectedCsv == null) return;

        currentSelectedTicker = selectedCsv.name;

        SelectedStockData.StockTicker = selectedCsv.name;
        SelectedStockData.PriceCsvAsset = selectedCsv;

        RestoreText();
        dropdownPanel.SetActive(false);

        if (chartController != null)
        {
            chartController.LoadNewDataset(selectedCsv);
        }

        if (volumeChartController != null)
        {
            volumeChartController.LoadNewDataset(selectedCsv);
        }

        if (tradeController == null)
        {
            tradeController = FindAnyObjectByType<TradeController>();
        }

        if (tradeController != null)
        {
            tradeController.OnStockSelected(selectedCsv.name);
        }
    }
}