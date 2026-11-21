using UnityEngine;
using TMPro;

public class DayIndicator : MonoBehaviour
{
    private TextMeshProUGUI dayText;

    private void Awake()
    {
        dayText = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged += HandleDayChanged;
            HandleDayChanged(DaySimulationManager.Instance.currentDay);
        }
    }

    private void OnDestroy()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.OnDayChanged -= HandleDayChanged;
        }
    }

    private void HandleDayChanged(int spoofedDay)
    {
        if (dayText != null)
        {
            int cap = DaySimulationManager.Instance != null ? DaySimulationManager.Instance.maxTradingDays : 50;
            dayText.text = $"Day {spoofedDay} / {cap}";
        }
    }
}