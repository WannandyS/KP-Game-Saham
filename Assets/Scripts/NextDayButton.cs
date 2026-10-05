using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NextDayButtonBridge : MonoBehaviour
{
    private void Start()
    {
        Button btn = GetComponent<Button>();
        btn.onClick.AddListener(OnNextDayClicked);
    }

    private void OnNextDayClicked()
    {
        if (DaySimulationManager.Instance != null)
        {
            DaySimulationManager.Instance.NextDay();
        }
        else
        {
            Debug.LogError("DaySimulationManager Instance not found in scene!");
        }
    }
}
