using System;
using UnityEngine;

public class DaySimulationManager : MonoBehaviour
{
    public static DaySimulationManager Instance { get; private set; }

    [Header("Simulation Settings")]
    [Tooltip("Spoofed day shown to the player (1-indexed, 1 to maxTradingDays).")]
    public int currentDay = 1;

    [Tooltip("Maximum trading days allowed per run before final result.")]
    public int maxTradingDays = 50;

    [Tooltip("Actual starting day/row in the CSV dataset (spoofed as Day 1).")]
    public int startDatasetDay = 1;

    [Tooltip("Total days/rows available in the CSV dataset.")]
    public int totalDatasetDays = 236;

    [Tooltip("Randomize startDatasetDay on a new run.")]
    public bool randomizeStartDayOnNewRun = true;

    [Header("Debounce / Cooldown Settings")]
    public float clickCooldown = 0.25f;

    /// <summary>
    /// True dataset day/row to read from CSVs (1-indexed).
    /// </summary>
    public int CurrentDatasetDay => Mathf.Clamp(startDatasetDay + (currentDay - 1), 1, totalDatasetDays);

    public event Action<int> OnDayChanged;
    public event Action OnTradingSessionCompleted;

    private float lastClickTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (randomizeStartDayOnNewRun)
        {
            StartNewRandomRun();
        }
    }

    private void Start()
    {
        NotifyDayChanged();
    }

    public void StartNewRandomRun(int? forcedStartDatasetDay = null)
    {
        if (forcedStartDatasetDay.HasValue)
        {
            startDatasetDay = Mathf.Clamp(forcedStartDatasetDay.Value, 1, totalDatasetDays);
        }
        else
        {
            int maxValidStart = Mathf.Max(1, totalDatasetDays - maxTradingDays + 1);
            // UnityEngine.Random.Range(min, max) is max-exclusive for ints
            startDatasetDay = UnityEngine.Random.Range(1, maxValidStart + 1);
        }

        currentDay = 1;
        NotifyDayChanged();
    }

    public void LoadDayState(int loadedSpoofedDay, int loadedStartDatasetDay)
    {
        startDatasetDay = Mathf.Clamp(loadedStartDatasetDay, 1, totalDatasetDays);
        currentDay = Mathf.Clamp(loadedSpoofedDay, 1, maxTradingDays);
        NotifyDayChanged();
    }

    public void NextDay()
    {
        if (Time.unscaledTime - lastClickTime < clickCooldown)
            return;

        lastClickTime = Time.unscaledTime;

        if (currentDay >= maxTradingDays)
        {
            Debug.Log($"[DaySimulationManager] Reached {maxTradingDays}-day cap. Showing final result.");
            OnTradingSessionCompleted?.Invoke();
            return;
        }

        if (CurrentDatasetDay >= totalDatasetDays)
        {
            Debug.LogWarning("[DaySimulationManager] Reached end of CSV dataset.");
            OnTradingSessionCompleted?.Invoke();
            return;
        }

        currentDay++;
        Debug.Log($"[DaySimulationManager] Spoofed Day → {currentDay} (Dataset Day → {CurrentDatasetDay})");
        NotifyDayChanged();

        if (currentDay == maxTradingDays)
        {
            OnTradingSessionCompleted?.Invoke();
        }
    }

    public void PreviousDay()
    {
        if (Time.unscaledTime - lastClickTime < clickCooldown)
            return;

        lastClickTime = Time.unscaledTime;

        if (currentDay > 1)
        {
            currentDay--;
            Debug.Log($"[DaySimulationManager] Spoofed Day → {currentDay} (Dataset Day → {CurrentDatasetDay})");
            NotifyDayChanged();
        }
    }

    public void SetDay(int spoofedDay)
    {
        currentDay = Mathf.Clamp(spoofedDay, 1, maxTradingDays);
        NotifyDayChanged();
    }

    public void RefreshCurrentDay()
    {
        NotifyDayChanged();
    }

    private void NotifyDayChanged()
    {
        OnDayChanged?.Invoke(currentDay);
    }
}