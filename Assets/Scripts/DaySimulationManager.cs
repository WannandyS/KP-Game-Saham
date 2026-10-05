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

    public event Action<int> OnDayChanged;

    private void Awake()
    {
        // Enforce a Single Persistent Singleton across scene loads
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Keeps this manager alive when loading new scenes
    }

    private void Start()
    {
        NotifyDayChanged();
    }

    /// <summary>
    /// Call this method from any "Next Day" UI Button OnClick() event in any scene.
    /// </summary>
    public void NextDay()
    {
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

    /// <summary>
    /// Call this method from any "Previous Day" UI Button OnClick() event in any scene.
    /// </summary>
    public void PreviousDay()
    {
        if (currentDay > 1)
        {
            currentDay--;
            Debug.Log($"[DaySimulationManager] Day Decremented → Day {currentDay}");
            NotifyDayChanged();
        }
    }

    /// <summary>
    /// Jump directly to a specific day.
    /// </summary>
    public void SetDay(int day)
    {
        currentDay = Mathf.Clamp(day, 1, totalDays);
        NotifyDayChanged();
    }

    /// <summary>
    /// Re-broadcasts the current day state (useful when opening a new scene).
    /// </summary>
    public void RefreshCurrentDay()
    {
        NotifyDayChanged();
    }

    private void NotifyDayChanged()
    {
        OnDayChanged?.Invoke(currentDay);
    }
}