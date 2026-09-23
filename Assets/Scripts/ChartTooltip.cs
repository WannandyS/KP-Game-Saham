using UnityEngine;
using TMPro;

public class ChartTooltip : MonoBehaviour
{
    public static ChartTooltip Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    [Tooltip("Offset relative to the top-center of the hovered element.")]
    public Vector2 offset = new Vector2(0f, 15f);

    private RectTransform panelRect;
    private Canvas parentCanvas;
    private RectTransform currentTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (tooltipPanel != null)
        {
            panelRect = tooltipPanel.GetComponent<RectTransform>();
            parentCanvas = GetComponentInParent<Canvas>();
            tooltipPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (tooltipPanel != null && tooltipPanel.activeSelf && currentTarget != null)
        {
            UpdatePosition();
        }
    }

    private void UpdatePosition()
    {
        // Get the world corners of the hovered chart element
        Vector3[] corners = new Vector3[4];
        currentTarget.GetWorldCorners(corners);

        // Calculate the top-center position of the element (corners[1] = top-left, corners[2] = top-right)
        Vector3 topCenterWorld = (corners[1] + corners[2]) * 0.5f;

        Camera cam = (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : parentCanvas.worldCamera;
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, topCenterWorld);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)panelRect.parent,
            screenPoint,
            cam,
            out localPoint
        );

        panelRect.anchoredPosition = localPoint + offset;
    }

    public void ShowTooltip(string text, RectTransform target)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        currentTarget = target;
        tooltipText.text = text;
        tooltipPanel.SetActive(true);
        UpdatePosition();
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            currentTarget = null;
            tooltipPanel.SetActive(false);
        }
    }
}