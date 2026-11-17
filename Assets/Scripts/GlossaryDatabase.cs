using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GlossaryDatabase", menuName = "StockSim/Glossary Database")]
public class GlossaryDatabase : ScriptableObject
{
    [Serializable]
    public class GlossaryEntry
    {
        [Tooltip("Unique ID, e.g., MARKET_CAP, CANDLESTICK, LOT")]
        public string termId;

        [Tooltip("Title shown in the Glossary UI, e.g., Market Capitalization")]
        public string displayTitle;

        [Tooltip("All word variations to auto-detect in text (case-insensitive)")]
        public List<string> matchKeywords = new List<string>();

        [TextArea(3, 6)]
        public string definition;
    }

    [Header("JSON Auto-Population")]
    [Tooltip("Drag glossary_data.json here to populate the entries list automatically")]
    public TextAsset jsonSource;

    [Header("Glossary Entries")]
    public List<GlossaryEntry> entries = new List<GlossaryEntry>();

    public void LoadFromJson(TextAsset jsonAsset)
    {
        if (jsonAsset == null || string.IsNullOrWhiteSpace(jsonAsset.text)) return;
        JsonUtility.FromJsonOverwrite(jsonAsset.text, this);
    }

    [ContextMenu("Import From Assigned JSON")]
    public void ImportFromAssignedJson()
    {
        if (jsonSource == null)
        {
            Debug.LogWarning("[GlossaryDatabase] Assign glossary_data.json to 'Json Source' first!");
            return;
        }

        LoadFromJson(jsonSource);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
#endif
        Debug.Log($"[GlossaryDatabase] Imported {entries.Count} terms from {jsonSource.name}.");
    }
}