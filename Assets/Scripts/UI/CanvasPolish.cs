using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this to any Canvas GameObject. It will automatically set the
/// Canvas Scaler to a mobile‑friendly configuration (Scale With Screen Size
/// + Match Width Or Height) and force a semi‑transparent dark background panel.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CanvasPolish : MonoBehaviour
{
    private void Awake()
    {
        // 1️⃣ Configure Canvas Scaler
        var scaler = GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f; // balanced scaling
        }

        // 2️⃣ Create a dark overlay panel if one does not exist
        var existing = transform.Find("ui_panel_bg");
        if (existing == null)
        {
            GameObject bg = new GameObject("ui_panel_bg");
            bg.transform.SetParent(transform, false);
            var rect = bg.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = bg.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.66f);
            img.raycastTarget = false;
            // Send to back so UI stays on top
            bg.transform.SetAsFirstSibling();
        }
    }
}
