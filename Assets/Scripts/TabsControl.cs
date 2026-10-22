using UnityEngine;
using UnityEngine.UI;

public class TabsControl : MonoBehaviour
{
    [Header("Buttons")]
    public Button newsButton;
    public Button graphButton;

    [Header("Panels")]
    public GameObject newsPanel;
    public GameObject graphPanel;

    [Header("Colors")]
    public Color activeColor = new Color(0.12f, 0.23f, 0.38f, 1f);
    public Color inactiveColor = new Color(0.05f, 0.08f, 0.12f, 1f);

    private Image newsImage;
    private Image graphImage;

    private void Awake()
    {
        if (newsButton != null) newsImage = newsButton.GetComponent<Image>();
        if (graphButton != null) graphImage = graphButton.GetComponent<Image>();
    }

    private void Start()
    {
        if (newsButton != null) newsButton.onClick.AddListener(SelectNewsTab);
        if (graphButton != null) graphButton.onClick.AddListener(SelectGraphTab);

        SelectNewsTab();
    }

    public void SelectNewsTab()
    {
        if (newsPanel != null) newsPanel.SetActive(true);
        if (graphPanel != null) graphPanel.SetActive(false);

        if (newsImage != null) newsImage.color = activeColor;
        if (graphImage != null) graphImage.color = inactiveColor;
    }

    public void SelectGraphTab()
    {
        if (newsPanel != null) newsPanel.SetActive(false);
        if (graphPanel != null) graphPanel.SetActive(true);

        if (newsImage != null) newsImage.color = inactiveColor;
        if (graphImage != null) graphImage.color = activeColor;
    }
}