using UnityEngine;
using TMPro;

public class NewsRowUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    public void SetNews(string title, string description, string dateStr)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;

        gameObject.SetActive(true);
    }

    public void Clear()
    {
        if (titleText != null) titleText.text = "";
        if (descriptionText != null) descriptionText.text = "";

        gameObject.SetActive(false);
    }
}
