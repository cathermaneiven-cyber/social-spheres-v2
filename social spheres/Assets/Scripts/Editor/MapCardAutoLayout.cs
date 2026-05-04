using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Select your MapCard prefab root in the Hierarchy, then run
/// Tools > Social Spheres > Fix MapCard Layout
/// </summary>
public class MapCardAutoLayout : Editor
{
    [MenuItem("Tools/Social Spheres/Fix MapCard Layout")]
    static void FixLayout()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null) { Debug.LogError("Select your MapCard root first!"); return; }

        Undo.RegisterFullObjectHierarchyUndo(selected, "Fix MapCard Layout");

        RectTransform root = selected.GetComponent<RectTransform>();
        if (root == null) { Debug.LogError("Selected object has no RectTransform."); return; }

        // Add a background image to the card root if missing
        Image bg = selected.GetComponent<Image>();
        if (bg == null)
        {
            bg = selected.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.14f, 0.22f, 1f);
        }

        // Fix each known child by name
        SetAnchored(selected, "Thumbnail",     0f,  1f, 0f,  1f,   0,  0,   0, -60);  // top stretch, 60px tall
        SetAnchored(selected, "MapName",       0f,  1f, 1f,  1f,   4, -4, -60, -80);  // below thumbnail
        SetAnchored(selected, "AuthorLabel",   0f,  1f, 1f,  1f,   4, -4, -80, -96);
        SetAnchored(selected, "DownloadCount", 0f, 0.5f,1f, 1f,   4, -2, -96,-112);
        SetAnchored(selected, "RatingLabel",  0.5f, 1f, 1f,  1f,   2, -4, -96,-112);
        SetAnchored(selected, "Owned",         1f,  1f, 1f,  1f, -22,  -4,  -4,  18, true); // top-right badge
        SetAnchored(selected, "Image",         1f,  1f, 1f,  1f, -22,  -4,  -4,  18, true); // same, alt name
        SetAnchored(selected, "Button",        0f,  1f, 0f,  0f,   4, -4,   4,  30, false, true); // bottom stretch

        // Fix font sizes on TMP labels
        SetFontSize(selected, "MapName",      10);
        SetFontSize(selected, "AuthorLabel",   8);
        SetFontSize(selected, "DownloadCount", 7);
        SetFontSize(selected, "RatingLabel",   7);

        EditorUtility.SetDirty(selected);
        Debug.Log("[MapCardAutoLayout] Done! Save the prefab.");
    }

    /// <summary>
    /// anchorMin/Max in 0-1, then offsets: left, right, top, bottom (all in pixels from anchor edge).
    /// topRight mode: treated as width/height pinned to top-right corner.
    /// bottomStretch: anchored to bottom, stretches horizontally.
    /// </summary>
    static void SetAnchored(GameObject parent, string childName,
        float anchorMinX, float anchorMaxX, float anchorMinY, float anchorMaxY,
        float left, float right, float top, float bottom,
        bool topRight = false, bool bottomStretch = false)
    {
        Transform t = parent.transform.Find(childName);
        if (t == null) return;

        RectTransform rt = t.GetComponent<RectTransform>();
        if (rt == null) return;

        rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rt.pivot     = new Vector2(0.5f, 0.5f);

        if (topRight)
        {
            // Pin to top-right: left/right = width, top/bottom = height
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot     = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(right, top);   // right=neg offset from right, top=neg offset from top
            rt.sizeDelta = new Vector2(Mathf.Abs(left), Mathf.Abs(bottom));
        }
        else if (bottomStretch)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.offsetMin = new Vector2(left, bottom);   // left offset, bottom offset
            rt.offsetMax = new Vector2(right, bottom + Mathf.Abs(top)); // right offset, height
            rt.sizeDelta = new Vector2(0, Mathf.Abs(top)); // height
            rt.anchoredPosition = new Vector2(0, bottom);
        }
        else
        {
            // Standard: stretch between anchors with pixel offsets
            rt.offsetMin = new Vector2(left,          -Mathf.Abs(bottom));
            rt.offsetMax = new Vector2(-Mathf.Abs(right), top);
        }
    }

    static void SetFontSize(GameObject parent, string childName, float size)
    {
        Transform t = parent.transform.Find(childName);
        if (t == null) return;
        TextMeshProUGUI tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp == null) return;
        tmp.fontSize = size;
        tmp.enableAutoSizing = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.alignment = TextAlignmentOptions.TopLeft;
    }
}
