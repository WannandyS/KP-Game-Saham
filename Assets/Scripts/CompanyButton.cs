using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CompanyButton : MonoBehaviour
{
    public CompanyData companyData;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        int currentSimDay = 1; // Replace with your Global Day Manager's current day counter
        CompanySelectionManager.OpenCompanyDetails(companyData, currentSimDay);
    }
}