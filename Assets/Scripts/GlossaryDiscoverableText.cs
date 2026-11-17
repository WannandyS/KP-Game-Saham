using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class GlossaryDiscoverableText : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text tmpText;
    private string lastFormattedText;

    private void Awake()
    {
        tmpText = GetComponent<TMP_Text>();
        tmpText.raycastTarget = true;
        tmpText.richText = true;
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTmpTextChanged);
        FormatCurrentText();
    }

    private void Start()
    {
        FormatCurrentText();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTmpTextChanged);
    }

    private void OnTmpTextChanged(UnityEngine.Object obj)
    {
        if (obj == tmpText)
        {
            FormatCurrentText();
        }
    }

    public void FormatCurrentText()
    {
        if (tmpText == null) return;

        if (GlossaryManager.Instance == null)
        {
            Debug.LogWarning($"[GlossaryText] '{name}': GlossaryManager.Instance is NULL.");
            return;
        }

        if (string.IsNullOrEmpty(tmpText.text) || tmpText.text == lastFormattedText) return;

        string processed = GlossaryManager.Instance.AutoLinkText(tmpText.text);
        lastFormattedText = processed;
        tmpText.text = processed;

        // Immediately rebuild mesh so textInfo.linkInfo is populated right away
        tmpText.ForceMeshUpdate();
        Debug.Log($"[GlossaryText] '{name}' formatted. Links found: {tmpText.textInfo.linkCount}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[GlossaryText] Pointer clicked on '{name}' at {eventData.position}");

        if (tmpText == null || GlossaryManager.Instance == null) return;

        Camera targetCamera = null;
        if (tmpText.canvas != null && tmpText.canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            targetCamera = tmpText.canvas.worldCamera != null ? tmpText.canvas.worldCamera : eventData.pressEventCamera;
        }

        int linkIndex = TMP_TextUtilities.FindIntersectingLink(tmpText, eventData.position, targetCamera);
        if (linkIndex != -1)
        {
            TMP_LinkInfo linkInfo = tmpText.textInfo.linkInfo[linkIndex];
            string termId = linkInfo.GetLinkID();
            Debug.Log($"[GlossaryText] Hit link '{termId}'! Forwarding to GlossaryManager...");
            GlossaryManager.Instance.HandleTermClicked(termId);
        }
        else
        {
            Debug.Log($"[GlossaryText] Clicked inside text bounds, but missed all links (linkCount = {tmpText.textInfo.linkCount}).");
        }
    }
}