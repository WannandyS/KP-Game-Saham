using UnityEngine;
using TMPro;

public class ChartTooltip : MonoBehaviour
{
    public static ChartTooltip Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;

    [Header("Cursor Offset")]
    [Tooltip("Offset relative to the cursor position (X = horizontal, Y = vertical beneath pointer).")]
    public Vector2 cursorOffset = new Vector2(0f, -25f);

    private RectTransform panelRect;
    private Canvas parentCanvas;
    private bool isShowing = false;

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
        if (isShowing && tooltipPanel != null && tooltipPanel.activeSelf)
        {
            UpdatePositionToCursor();
        }
    }

    public void ShowTooltip(string text)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipText.text = text;
        tooltipPanel.SetActive(true);
        isShowing = true;

        Canvas.ForceUpdateCanvases();
        UpdatePositionToCursor();
    }

    private void UpdatePositionToCursor()
    {
        Vector2 mousePos = Input.mousePosition;

        if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)parentCanvas.transform,
                mousePos,
                parentCanvas.worldCamera,
                out Vector2 localPoint
            );

            panelRect.anchoredPosition = localPoint + cursorOffset;
        }
        else
        {
            panelRect.position = (Vector3)mousePos + (Vector3)cursorOffset;
        }

        ClampToScreen();
    }

    private void ClampToScreen()
    {
        Vector3[] corners = new Vector3[4];
        panelRect.GetWorldCorners(corners);

        float panelWidth = corners[2].x - corners[0].x;
        float panelHeight = corners[2].y - corners[0].y;

        Vector3 pos = panelRect.position;

        // Keep tooltip inside screen boundaries
        if (pos.x - (panelWidth * panelRect.pivot.x) < 0)
            pos.x = panelWidth * panelRect.pivot.x;

        if (pos.x + (panelWidth * (1f - panelRect.pivot.x)) > Screen.width)
            pos.x = Screen.width - (panelWidth * (1f - panelRect.pivot.x));

        if (pos.y - (panelHeight * panelRect.pivot.y) < 0)
            pos.y = panelHeight * panelRect.pivot.y;

        if (pos.y + (panelHeight * (1f - panelRect.pivot.y)) > Screen.height)
            pos.y = Screen.height - (panelHeight * (1f - panelRect.pivot.y));

        panelRect.position = pos;
    }

    public void HideTooltip()
    {
        isShowing = false;
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
        }
    }
}
