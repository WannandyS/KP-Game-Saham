using System;
using UnityEngine;

public class DaySimulationManager : MonoBehaviour
{
    public static DaySimulationManager Instance { get; private set; }

    [Header("Simulation Settings")]
    [Tooltip("Current active trading day in the simulator (1-indexed).")]
    public int currentDay = 1;

    [Tooltip("Maximum day available in the dataset.")]
    public int totalDays = 236;

    [Header("Debounce / Cooldown Settings")]
    [Tooltip("Minimum time (in seconds) required between day changes to prevent double-triggering.")]
    public float clickCooldown = 0.25f;

    public event Action<int> OnDayChanged;

    private float lastClickTime = 0f;

    private void Awake()
    {
        // Enforce a Single Persistent Singleton across scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        NotifyDayChanged();
    }

    public void NextDay()
    {
        if (Time.unscaledTime - lastClickTime < clickCooldown)
        {
            return;
        }

        lastClickTime = Time.unscaledTime;

        if (currentDay < totalDays)
        {
            currentDay++;
            Debug.Log($"[DaySimulationManager] Day Advanced → Day {currentDay}");
            NotifyDayChanged();
        }
        else
        {
            Debug.Log($"[DaySimulationManager] Already at final day: Day {totalDays}");
        }
    }

    public void PreviousDay()
    {
        if (Time.unscaledTime - lastClickTime < clickCooldown)
        {
            return;
        }

        lastClickTime = Time.unscaledTime;

        if (currentDay > 1)
        {
            currentDay--;
            Debug.Log($"[DaySimulationManager] Day Decremented → Day {currentDay}");
            NotifyDayChanged();
        }
    }

    public void SetDay(int day)
    {
        currentDay = Mathf.Clamp(day, 1, totalDays);
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