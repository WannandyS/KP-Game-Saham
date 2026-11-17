using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class GlossaryManager : MonoBehaviour
{
    private static GlossaryManager _instance;
    public static GlossaryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                EnsureInitialized();
            }
            return _instance;
        }
    }

    [Header("Database & Styling")]
    public GlossaryDatabase database;
    public string linkColorHex = "#faf4c4";
    public bool underlineLinks = true;

    public event Action<GlossaryDatabase.GlossaryEntry> OnTermDiscovered;
    public event Action<GlossaryDatabase.GlossaryEntry> OnTermClicked;

    private readonly Dictionary<string, GlossaryDatabase.GlossaryEntry> entryById = new Dictionary<string, GlossaryDatabase.GlossaryEntry>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> keywordToId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> discoveredIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private Regex autoLinkRegex;
    private bool isInitialized;
    private const string SAVE_KEY = "DiscoveredGlossaryTerms";

    // Runs BEFORE any scene's Awake() or OnEnable()
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInitialized();
    }

    private static void EnsureInitialized()
    {
        if (_instance != null) return;

        // Check if one already exists in the loaded scene (even if inactive)
        GlossaryManager existing = FindAnyObjectByType<GlossaryManager>(FindObjectsInactive.Include);
        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            existing.enabled = true;
            existing.Initialize();
            return;
        }

        // Otherwise spawn a persistent manager automatically
        GameObject go = new GameObject("GlossaryManager (Auto)");
        GlossaryManager created = go.AddComponent<GlossaryManager>();
        created.Initialize();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }

        Initialize();
    }

private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        _instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        if (database == null)
        {
            database = Resources.Load<GlossaryDatabase>("GlossaryDatabase");
        }

        // If no ScriptableObject asset exists yet, create an in-memory instance from JSON
        if (database == null)
        {
            TextAsset fallbackJson = Resources.Load<TextAsset>("glossary_data");
            if (fallbackJson != null)
            {
                database = ScriptableObject.CreateInstance<GlossaryDatabase>();
                database.LoadFromJson(fallbackJson);
            }
        }
        else if (database.jsonSource != null)
        {
            // Keep the ScriptableObject synced with the assigned JSON file
            database.LoadFromJson(database.jsonSource);
        }

        BuildLookupAndRegex();
        LoadDiscoveredTerms();
        Debug.Log($"[GlossaryManager] Ready on '{gameObject.name}'. Loaded {entryById.Count} terms.");
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            Debug.LogError($"[GlossaryManager] '{gameObject.name}' was destroyed! Call stack:\n{Environment.StackTrace}");
            _instance = null;
            isInitialized = false;
        }
    }

    private void BuildLookupAndRegex()
    {
        entryById.Clear();
        keywordToId.Clear();

        if (database == null || database.entries == null || database.entries.Count == 0)
        {
            Debug.LogWarning("[GlossaryManager] Database is missing or empty! Place your asset at 'Assets/Resources/GlossaryDatabase.asset' or assign it in the Inspector.");
            return;
        }

        List<string> allKeywords = new List<string>();

        foreach (var entry in database.entries)
        {
            if (string.IsNullOrWhiteSpace(entry.termId)) continue;

            string cleanId = entry.termId.Trim().ToUpper();
            entryById[cleanId] = entry;

            RegisterKeyword(entry.termId, cleanId, allKeywords);
            RegisterKeyword(entry.termId.Replace("_", " "), cleanId, allKeywords);

            if (!string.IsNullOrWhiteSpace(entry.displayTitle))
            {
                RegisterKeyword(entry.displayTitle, cleanId, allKeywords);
            }

            if (entry.matchKeywords != null)
            {
                foreach (string kw in entry.matchKeywords)
                {
                    RegisterKeyword(kw, cleanId, allKeywords);
                }
            }
        }

        if (allKeywords.Count == 0) return;

        string joinedPatterns = string.Join("|", allKeywords
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(k => k.Length)
            .Select(Regex.Escape));

        string pattern = $@"(<[^>]+>)|\b({joinedPatterns})\b";
        autoLinkRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private void RegisterKeyword(string keyword, string termId, List<string> allKeywords)
    {
        string trimmed = keyword.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        if (!keywordToId.ContainsKey(trimmed))
        {
            keywordToId[trimmed] = termId;
            allKeywords.Add(trimmed);
        }
    }

    public string AutoLinkText(string rawText)
    {
        if (string.IsNullOrEmpty(rawText) || autoLinkRegex == null)
            return rawText;

        return autoLinkRegex.Replace(rawText, match =>
        {
            if (match.Groups[1].Success)
                return match.Value;

            string matchedWord = match.Groups[2].Value;
            if (keywordToId.TryGetValue(matchedWord, out string termId))
            {
                string formatted = $"<color={linkColorHex}>{matchedWord}</color>";
                if (underlineLinks) formatted = $"<u>{formatted}</u>";
                return $"<link=\"{termId}\">{formatted}</link>";
            }

            return match.Value;
        });
    }

    public void HandleTermClicked(string termId)
    {
        if (string.IsNullOrWhiteSpace(termId)) return;

        string cleanId = termId.Trim().ToUpper();
        if (!entryById.TryGetValue(cleanId, out var entry)) return;

        bool isNewDiscovery = discoveredIds.Add(cleanId);
        if (isNewDiscovery)
        {
            SaveDiscoveredTerms();
            OnTermDiscovered?.Invoke(entry);
            Debug.Log($"[Glossary] New term discovered: {entry.displayTitle}");
        }

        OnTermClicked?.Invoke(entry);
    }

    public bool IsTermDiscovered(string termId) => discoveredIds.Contains(termId);

    public List<GlossaryDatabase.GlossaryEntry> GetDiscoveredEntries()
    {
        if (database == null) return new List<GlossaryDatabase.GlossaryEntry>();
        return database.entries.Where(e => discoveredIds.Contains(e.termId)).ToList();
    }

    public List<GlossaryDatabase.GlossaryEntry> GetAllEntries()
    {
        return database != null ? database.entries : new List<GlossaryDatabase.GlossaryEntry>();
    }

    private void SaveDiscoveredTerms()
    {
        PlayerPrefs.SetString(SAVE_KEY, string.Join(",", discoveredIds));
        PlayerPrefs.Save();
    }

    private void LoadDiscoveredTerms()
    {
        discoveredIds.Clear();
        string saved = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(saved)) return;

        foreach (string id in saved.Split(','))
        {
            if (!string.IsNullOrWhiteSpace(id))
                discoveredIds.Add(id.Trim().ToUpper());
        }
    }

    [ContextMenu("Reset Discovered Terms")]
    public void ResetProgress()
    {
        discoveredIds.Clear();
        PlayerPrefs.DeleteKey(SAVE_KEY);
    }
}