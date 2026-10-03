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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        NotifyDayChanged();
    }

    /// <summary>
    /// Call this method from your "Next Day" UI Button OnClick() event.
    /// </summary>
    public void NextDay()
    {
        if (currentDay < totalDays)
        {
            currentDay++;

            Debug.Log("Next Day → Day " + currentDay);

            NotifyDayChanged();
        }
        else
        {
            Debug.Log("Sudah mencapai hari terakhir: Day " + totalDays);
        }
    }

    /// <summary>
    /// Call this method from your "Previous Day" UI Button OnClick() event if needed.
    /// </summary>
    public void PreviousDay()
    {
        if (currentDay > 1)
        {
            currentDay--;
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

    private void NotifyDayChanged()
    {
        OnDayChanged?.Invoke(currentDay);
    }
}
