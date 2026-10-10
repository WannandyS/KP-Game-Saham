using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SearchableDropdown : MonoBehaviour
{
    [Header("UI References")]
    public Button mainButton;
    public TMP_Text mainButtonText;
    public GameObject dropdownPanel;
    public TMP_InputField searchInput;
    public Transform contentContainer;
    public GameObject itemPrefab;

    [Header("Chart Integration")]
    public CandlestickChart chartController;
    public List<TextAsset> csvDatasets = new List<TextAsset>();
    private List<GameObject> itemGameObjects = new List<GameObject>();

    void Start()
    {
        dropdownPanel.SetActive(false);
        mainButton.onClick.AddListener(ToggleDropdown);
        searchInput.onValueChanged.AddListener(FilterList);
        

        GenerateAllItems();

        // Optional: Auto-load the first CSV in the list when the game starts
        if (csvDatasets.Count > 0)
        {
            OnItemSelected(csvDatasets[0]);
        }
    }

    public void ToggleDropdown()
    {
        bool isActive = !dropdownPanel.activeSelf;
        dropdownPanel.SetActive(isActive);

        if (isActive)
        {
            searchInput.text = "";
            FilterList("");
            searchInput.Select();
        }
    }

    private void GenerateAllItems()
    {
        foreach (TextAsset csvFile in csvDatasets)
        {
            if (csvFile == null) continue;

            GameObject newItem = Instantiate(itemPrefab, contentContainer);

            // Set the button text to the name of the CSV file
            newItem.GetComponentInChildren<TMP_Text>().text = csvFile.name;

            // Pass the actual TextAsset to the click event
            newItem.GetComponent<Button>().onClick.AddListener(() => OnItemSelected(csvFile));

            itemGameObjects.Add(newItem);
        }
    }

    private void FilterList(string query)
    {
        query = query.ToLower();

        for (int i = 0; i < csvDatasets.Count; i++)
        {
            if (csvDatasets[i] == null) continue;

            // Check if the CSV file name contains the search query
            bool match = csvDatasets[i].name.ToLower().Contains(query);
            itemGameObjects[i].SetActive(match);
        }
    }

    private void OnItemSelected(TextAsset selectedCsv)
    {
        // Update the Dropdown UI label
        searchInput.text = selectedCsv.name;
        dropdownPanel.SetActive(false);

        // Send the selected file to the chart
        if (chartController != null)
        {
            chartController.LoadNewDataset(selectedCsv);
        }
        else
        {
            Debug.LogWarning("SearchableDropdown: Chart Controller is not assigned in the Inspector!");
        }
    }
}