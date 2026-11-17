using UnityEngine;
using TMPro;

public class GlossaryUI : MonoBehaviour
{
    [Header("Glossary Panel & List")]
    public GameObject glossaryPanel;
    public Transform listContainer;
    public GameObject glossaryRowPrefab; // Drag from Project folder, NOT the scene Hierarchy

    [Header("Empty State & Display Options")]
    [Tooltip("Optional text/object shown when 0 terms have been discovered yet")]
    public GameObject emptyStateObject;

    [Tooltip("Keep unchecked so undiscovered terms do not appear until clicked")]
    public bool showLockedPlaceholders = false;

    private void OnEnable()
    {
        if (GlossaryManager.Instance != null)
        {
            GlossaryManager.Instance.OnTermClicked += HandleTermClicked;
            GlossaryManager.Instance.OnTermDiscovered += HandleTermDiscovered;
        }
        RefreshGlossary();
    }

    private void OnDisable()
    {
        if (GlossaryManager.Instance != null)
        {
            GlossaryManager.Instance.OnTermClicked -= HandleTermClicked;
            GlossaryManager.Instance.OnTermDiscovered -= HandleTermDiscovered;
        }
    }

    private void HandleTermDiscovered(GlossaryDatabase.GlossaryEntry entry)
    {
        RefreshGlossary();
    }

    private void HandleTermClicked(GlossaryDatabase.GlossaryEntry entry)
    {
        if (glossaryPanel != null) glossaryPanel.SetActive(true);
        RefreshGlossary();
    }

    public void RefreshGlossary()
    {
        if (GlossaryManager.Instance == null || listContainer == null || glossaryRowPrefab == null) return;

        foreach (Transform child in listContainer)
        {
            Destroy(child.gameObject);
        }

        var entries = showLockedPlaceholders
            ? GlossaryManager.Instance.GetAllEntries()
            : GlossaryManager.Instance.GetDiscoveredEntries();

        if (emptyStateObject != null)
        {
            emptyStateObject.SetActive(entries.Count == 0);
        }

        foreach (var entry in entries)
        {
            bool unlocked = GlossaryManager.Instance.IsTermDiscovered(entry.termId);
            GameObject row = Instantiate(glossaryRowPrefab, listContainer);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = unlocked ? entry.displayTitle : "??? (Undiscovered)";
                texts[1].text = unlocked ? entry.definition : "Click highlighted terms while learning or trading to unlock.";
            }
        }
    }
}