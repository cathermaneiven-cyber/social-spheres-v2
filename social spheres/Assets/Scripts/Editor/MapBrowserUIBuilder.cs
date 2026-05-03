using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using SocialSpheres;

public class MapBrowserUIBuilder : EditorWindow
{
    private GameObject targetMenu;
    private Color panelColor = new Color(0.1f, 0.12f, 0.18f, 0.97f);
    private Color accentColor = new Color(0.35f, 0.31f, 0.81f, 1f);
    private Color buttonColor = new Color(0.35f, 0.31f, 0.81f, 1f);
    private Color textColor = new Color(0.9f, 0.9f, 0.95f, 1f);
    private Color darkPanelColor = new Color(0.08f, 0.09f, 0.14f, 0.97f);
    private bool pickingFace;
    private float fixedCanvasScale = 0.0067f;

    private enum MenuFace { Front, Back, Left, Right, Top }
    private MenuFace selectedFace = MenuFace.Right;

    [MenuItem("Tools/Social Spheres/Build Map Browser UI")]
    public static void ShowWindow()
    {
        GetWindow<MapBrowserUIBuilder>("Map Browser Builder");
    }

    void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    void OnGUI()
    {
        GUILayout.Label("Map Browser UI Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetMenu = (GameObject)EditorGUILayout.ObjectField("Menu GameObject", targetMenu, typeof(GameObject), true);

        EditorGUILayout.Space();
        GUILayout.Label("Which face should the UI appear on?", EditorStyles.boldLabel);
        selectedFace = (MenuFace)GUILayout.SelectionGrid((int)selectedFace, new[] { "Front (Z-)", "Back (Z+)", "Left (X-)", "Right (X+)", "Top (Y+)" }, 3);

        EditorGUILayout.Space();

        GUI.enabled = targetMenu != null;
        if (GUILayout.Button(pickingFace ? "Click a side in Scene View..." : "Pick Side By Clicking In Scene", GUILayout.Height(30)))
        {
            pickingFace = true;
            SceneView.lastActiveSceneView?.Focus();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        fixedCanvasScale = EditorGUILayout.FloatField("Canvas Scale", fixedCanvasScale);

        EditorGUILayout.Space();
        GUILayout.Label("Colors", EditorStyles.boldLabel);
        panelColor = EditorGUILayout.ColorField("Panel Background", panelColor);
        accentColor = EditorGUILayout.ColorField("Accent / Header", accentColor);
        buttonColor = EditorGUILayout.ColorField("Button Color", buttonColor);
        textColor = EditorGUILayout.ColorField("Text Color", textColor);

        EditorGUILayout.Space();

        GUI.enabled = targetMenu != null;
        if (GUILayout.Button("Build Map Browser UI", GUILayout.Height(40)))
            BuildUI();
        GUI.enabled = true;

        if (targetMenu == null)
            EditorGUILayout.HelpBox("Drag your Menu GameObject into the slot above.", MessageType.Info);
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        if (!pickingFace || targetMenu == null)
            return;

        Event e = Event.current;
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 280, 44), EditorStyles.helpBox);
        GUILayout.Label("Click a side of the menu object");
        GUILayout.EndArea();
        Handles.EndGUI();

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (TryPickFace(ray))
            {
                pickingFace = false;
                Repaint();
                e.Use();
            }
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            pickingFace = false;
            Repaint();
            e.Use();
        }
    }

    bool TryPickFace(Ray ray)
    {
        if (!TryGetWorldBounds(out Bounds worldBounds))
            return false;

        Vector3 hitPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            if (hit.transform == targetMenu.transform || hit.transform.IsChildOf(targetMenu.transform))
                hitPoint = hit.point;
            else if (worldBounds.IntersectRay(ray, out float enter))
                hitPoint = ray.GetPoint(enter);
            else
                return false;
        }
        else
        {
            if (!worldBounds.IntersectRay(ray, out float enter))
                return false;

            hitPoint = ray.GetPoint(enter);
        }

        Bounds localBounds = GetLocalBounds();
        Vector3 localPoint = targetMenu.transform.InverseTransformPoint(hitPoint);

        float dxMin = Mathf.Abs(localPoint.x - localBounds.min.x);
        float dxMax = Mathf.Abs(localPoint.x - localBounds.max.x);
        float dyMax = Mathf.Abs(localPoint.y - localBounds.max.y);
        float dzMin = Mathf.Abs(localPoint.z - localBounds.min.z);
        float dzMax = Mathf.Abs(localPoint.z - localBounds.max.z);

        float best = Mathf.Min(dxMin, dxMax, dyMax, dzMin, dzMax);

        if (best == dxMin) selectedFace = MenuFace.Left;
        else if (best == dxMax) selectedFace = MenuFace.Right;
        else if (best == dyMax) selectedFace = MenuFace.Top;
        else if (best == dzMin) selectedFace = MenuFace.Front;
        else selectedFace = MenuFace.Back;

        return true;
    }

    bool TryGetWorldBounds(out Bounds bounds)
    {
        Renderer[] renderers = targetMenu.GetComponentsInChildren<Renderer>();
        bounds = new Bounds(targetMenu.transform.position, Vector3.zero);

        if (renderers.Length == 0)
            return false;

        bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        return true;
    }

    Bounds GetLocalBounds()
    {
        Renderer[] renderers = targetMenu.GetComponentsInChildren<Renderer>();
        Bounds localBounds = new Bounds(Vector3.zero, Vector3.zero);
        bool initialized = false;

        foreach (Renderer r in renderers)
        {
            Bounds b = r.bounds;
            Vector3 min = b.min;
            Vector3 max = b.max;

            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 local = targetMenu.transform.InverseTransformPoint(corner);

                if (!initialized)
                {
                    localBounds = new Bounds(local, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    localBounds.Encapsulate(local);
                }
            }
        }

        if (!initialized)
            localBounds = new Bounds(Vector3.zero, Vector3.one);

        return localBounds;
    }

    (Vector3 pos, Quaternion rot) GetFaceTransform(Bounds bounds)
    {
        float offset = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.01f;
        Vector3 c = bounds.center;

        switch (selectedFace)
        {
            case MenuFace.Front:
                return (new Vector3(c.x, c.y, bounds.min.z - offset), Quaternion.Euler(0, 180, 90));
            case MenuFace.Back:
                return (new Vector3(c.x, c.y, bounds.max.z + offset), Quaternion.Euler(0, 0, -90));
            case MenuFace.Left:
                return (new Vector3(bounds.min.x - offset, c.y, c.z), Quaternion.Euler(0, -90, -90));
            case MenuFace.Right:
                return (new Vector3(bounds.max.x + offset, c.y, c.z), Quaternion.Euler(0, 90, 90));
            case MenuFace.Top:
                return (new Vector3(c.x, bounds.max.y + offset, c.z), Quaternion.Euler(-90, 0, 0));
            default:
                return (new Vector3(bounds.max.x + offset, c.y, c.z), Quaternion.Euler(0, 90, 90));
        }
    }

    void BuildUI()
    {
        Undo.RegisterFullObjectHierarchyUndo(targetMenu, "Build Map Browser UI");

        Transform oldCanvas = targetMenu.transform.Find("MenuCanvas");
        if (oldCanvas != null)
            Undo.DestroyObjectImmediate(oldCanvas.gameObject);

        Bounds localBounds = GetLocalBounds();
        var face = GetFaceTransform(localBounds);

        GameObject canvasGO = new GameObject("MenuCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Map Browser UI");
        canvasGO.transform.SetParent(targetMenu.transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        if (Camera.main != null)
            canvas.worldCamera = Camera.main;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(400, 600);
        canvasRT.localPosition = face.pos;
        canvasRT.localRotation = face.rot;
        canvasRT.localScale = new Vector3(fixedCanvasScale, fixedCanvasScale, fixedCanvasScale);

        GameObject consentPanel = MakePanel(canvasGO, "ConsentPanel", panelColor);
        GameObject browserPanel = MakePanel(canvasGO, "BrowserPanel", panelColor);
        GameObject detailPanel = MakePanel(canvasGO, "DetailPanel", panelColor);
        GameObject loadingPanel = MakePanel(canvasGO, "LoadingOverlay", new Color(0, 0, 0, 0.7f));

        browserPanel.SetActive(false);
        detailPanel.SetActive(false);
        loadingPanel.SetActive(false);

        BuildConsentPanel(consentPanel);
        BuildBrowserPanel(browserPanel);
        BuildDetailPanel(detailPanel);

        MakeTMPLabel(loadingPanel, "LoadingLabel", "Loading...", 28, new Vector2(0, 0), new Vector2(200, 50), textColor);

        MapBrowserUI ui = canvasGO.AddComponent<MapBrowserUI>();
        AssignUIFields(ui, consentPanel, browserPanel, detailPanel, loadingPanel);

        EditorUtility.SetDirty(canvasGO);
        Selection.activeGameObject = canvasGO;

        Debug.Log("[MapBrowserBuilder] Done! Check the MenuCanvas Inspector to verify all fields.");
        EditorUtility.DisplayDialog("Done!", "Map Browser UI built successfully!", "OK");
    }

    void BuildConsentPanel(GameObject parent)
    {
        GameObject header = MakeImage(parent, "Header", accentColor, new Vector2(0, 260), new Vector2(400, 60));
        MakeTMPLabel(header, "Title", "Community Maps  •  mod.io", 18, Vector2.zero, new Vector2(380, 50), Color.white);

        MakeTMPLabel(parent, "BodyText",
            "This feature uses mod.io to let you browse and download\ncommunity-made maps.\n\n" +
            "By tapping Accept you agree to the\nmod.io Terms of Use, Privacy Policy,\nand Acceptable Use Policy.",
            14, new Vector2(0, 30), new Vector2(350, 180), textColor);

        MakeButton(parent, "AcceptButton", "Accept", buttonColor, new Vector2(-80, -220), new Vector2(140, 44));
        MakeButton(parent, "DeclineButton", "Decline", darkPanelColor, new Vector2(80, -220), new Vector2(140, 44));

        MakeTMPLabel(parent, "FooterLinks", "Terms of Use  |  Privacy Policy  |  AUP", 10, new Vector2(0, -280), new Vector2(380, 24), new Color(0.5f, 0.5f, 0.8f, 1f));
    }

    void BuildBrowserPanel(GameObject parent)
    {
        GameObject header = MakeImage(parent, "Header", accentColor, new Vector2(0, 270), new Vector2(400, 50));
        MakeTMPLabel(header, "Title", "Community Maps", 16, Vector2.zero, new Vector2(200, 40), Color.white);

        GameObject toolbar = MakeImage(parent, "Toolbar", darkPanelColor, new Vector2(0, 225), new Vector2(400, 40));
        MakeInputField(toolbar, "SearchInput", new Vector2(-90, 0), new Vector2(180, 32));
        MakeDropdown(toolbar, "SortDropdown", new Vector2(60, 0), new Vector2(110, 32), new[] { "Popular", "Newest", "Top Rated" });
        MakeDropdown(toolbar, "ThemeDropdown", new Vector2(175, 0), new Vector2(100, 32), new[] { "All", "City", "Nature", "Sci-fi", "Fantasy" });

        GameObject gridArea = MakeImage(parent, "CardGrid", new Color(0, 0, 0, 0), new Vector2(0, 30), new Vector2(390, 340));
        GridLayoutGroup grid = gridArea.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(120, 110);
        grid.spacing = new Vector2(8, 8);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.padding = new RectOffset(6, 6, 6, 6);

        GameObject pager = MakeImage(parent, "Pager", darkPanelColor, new Vector2(0, -155), new Vector2(400, 36));
        MakeButton(pager, "PrevPageBtn", "◀", buttonColor, new Vector2(-150, 0), new Vector2(50, 28));
        MakeTMPLabel(pager, "PageLabel", "1 / 1", 13, Vector2.zero, new Vector2(80, 28), textColor);
        MakeButton(pager, "NextPageBtn", "▶", buttonColor, new Vector2(150, 0), new Vector2(50, 28));

        GameObject footer = MakeImage(parent, "Footer", darkPanelColor, new Vector2(0, -280), new Vector2(400, 30));
        MakeTMPLabel(footer, "PoweredBy", "Powered by mod.io", 9, new Vector2(-60, 0), new Vector2(160, 26), new Color(0.5f, 0.5f, 0.8f, 1f));
        MakeButton(footer, "ToSBtn", "Terms", darkPanelColor, new Vector2(80, 0), new Vector2(70, 24));
        MakeButton(footer, "PrivacyBtn", "Privacy", darkPanelColor, new Vector2(155, 0), new Vector2(70, 24));
    }

    void BuildDetailPanel(GameObject parent)
    {
        MakeButton(parent, "BackButton", "◀ Back", darkPanelColor, new Vector2(-160, 270), new Vector2(90, 32));

        GameObject thumb = new GameObject("DetailThumb");
        thumb.transform.SetParent(parent.transform, false);
        RawImage rawImg = thumb.AddComponent<RawImage>();
        rawImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);
        RectTransform thumbRT = thumb.GetComponent<RectTransform>();
        thumbRT.anchoredPosition = new Vector2(0, 180);
        thumbRT.sizeDelta = new Vector2(380, 120);

        MakeTMPLabel(parent, "DetailName", "Map Name", 18, new Vector2(0, 90), new Vector2(370, 30), textColor);
        MakeTMPLabel(parent, "DetailAuthor", "by author", 12, new Vector2(0, 60), new Vector2(370, 22), accentColor);
        MakeTMPLabel(parent, "DetailDesc", "Description", 11, new Vector2(0, 10), new Vector2(370, 80), new Color(0.7f, 0.7f, 0.8f, 1f));
        MakeTMPLabel(parent, "DetailDownloads", "0 downloads", 11, new Vector2(-90, -80), new Vector2(160, 22), textColor);
        MakeTMPLabel(parent, "DetailRating", "★ 0.0 / 5", 11, new Vector2(60, -80), new Vector2(120, 22), textColor);
        MakeTMPLabel(parent, "DetailSize", "0 MB", 11, new Vector2(160, -80), new Vector2(80, 22), textColor);

        MakeButton(parent, "PlayButton", "▶  Play Now", buttonColor, new Vector2(0, -150), new Vector2(350, 44));
        MakeButton(parent, "DownloadButton", "↓  Download", accentColor, new Vector2(0, -150), new Vector2(350, 44));
        MakeButton(parent, "ReportButton", "⚑ Report", darkPanelColor, new Vector2(0, -205), new Vector2(350, 32));

        GameObject sliderGO = new GameObject("DownloadProgress");
        sliderGO.transform.SetParent(parent.transform, false);
        Slider slider = sliderGO.AddComponent<Slider>();
        RectTransform srt = sliderGO.GetComponent<RectTransform>();
        srt.anchoredPosition = new Vector2(0, -150);
        srt.sizeDelta = new Vector2(350, 20);
        sliderGO.SetActive(false);

        GameObject footer = MakeImage(parent, "Footer", darkPanelColor, new Vector2(0, -280), new Vector2(400, 30));
        MakeTMPLabel(footer, "PoweredBy", "Powered by mod.io", 9, new Vector2(-60, 0), new Vector2(160, 26), new Color(0.5f, 0.5f, 0.8f, 1f));
        MakeButton(footer, "ToSBtn2", "Terms", darkPanelColor, new Vector2(80, 0), new Vector2(70, 24));
        MakeButton(footer, "PrivacyBtn2", "Privacy", darkPanelColor, new Vector2(155, 0), new Vector2(70, 24));
    }

    void AssignUIFields(MapBrowserUI ui, GameObject consent, GameObject browser, GameObject detail, GameObject loading)
    {
        ui.consentPanel = consent;
        ui.browserPanel = browser;
        ui.detailPanel = detail;
        ui.loadingOverlay = loading;

        ui.consentAcceptBtn = consent.transform.Find("AcceptButton")?.GetComponent<Button>();
        ui.consentDeclineBtn = consent.transform.Find("DeclineButton")?.GetComponent<Button>();

        Transform toolbar = browser.transform.Find("Toolbar");
        if (toolbar != null)
        {
            ui.searchInput = toolbar.Find("SearchInput")?.GetComponent<TMP_InputField>();
            ui.sortDropdown = toolbar.Find("SortDropdown")?.GetComponent<TMP_Dropdown>();
            ui.themeDropdown = toolbar.Find("ThemeDropdown")?.GetComponent<TMP_Dropdown>();
        }

        Transform pager = browser.transform.Find("Pager");
        if (pager != null)
        {
            ui.prevPageBtn = pager.Find("PrevPageBtn")?.GetComponent<Button>();
            ui.nextPageBtn = pager.Find("NextPageBtn")?.GetComponent<Button>();
            ui.pageLabel = pager.Find("PageLabel")?.GetComponent<TextMeshProUGUI>();
        }

        ui.cardContainer = browser.transform.Find("CardGrid");

        ui.detailThumb = detail.transform.Find("DetailThumb")?.GetComponent<RawImage>();
        ui.detailName = detail.transform.Find("DetailName")?.GetComponent<TextMeshProUGUI>();
        ui.detailAuthor = detail.transform.Find("DetailAuthor")?.GetComponent<TextMeshProUGUI>();
        ui.detailDescription = detail.transform.Find("DetailDesc")?.GetComponent<TextMeshProUGUI>();
        ui.detailDownloads = detail.transform.Find("DetailDownloads")?.GetComponent<TextMeshProUGUI>();
        ui.detailRating = detail.transform.Find("DetailRating")?.GetComponent<TextMeshProUGUI>();
        ui.detailSize = detail.transform.Find("DetailSize")?.GetComponent<TextMeshProUGUI>();
        ui.playBtn = detail.transform.Find("PlayButton")?.GetComponent<Button>();
        ui.downloadBtn = detail.transform.Find("DownloadButton")?.GetComponent<Button>();
        ui.downloadProgress = detail.transform.Find("DownloadProgress")?.GetComponent<Slider>();
        ui.reportBtn = detail.transform.Find("ReportButton")?.GetComponent<Button>();
        ui.backBtn = detail.transform.Find("BackButton")?.GetComponent<Button>();

        Transform footer = browser.transform.Find("Footer");
        if (footer != null)
        {
            ui.tosBtn = footer.Find("ToSBtn")?.GetComponent<Button>();
            ui.privacyBtn = footer.Find("PrivacyBtn")?.GetComponent<Button>();
        }
    }

    GameObject MakePanel(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return go;
    }

    GameObject MakeImage(GameObject parent, string name, Color color, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    TextMeshProUGUI MakeTMPLabel(GameObject parent, string name, string text, float size, Vector2 pos, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return tmp;
    }

    Button MakeButton(GameObject parent, string name, string label, Color color, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(color.r + 0.1f, color.g + 0.1f, color.b + 0.1f);
        btn.colors = cb;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        MakeTMPLabel(go, "Label", label, 13, Vector2.zero, size, Color.white);
        return btn;
    }

    void MakeInputField(GameObject parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.17f, 0.25f, 1f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(go.transform, false);
        TextMeshProUGUI ph = placeholder.AddComponent<TextMeshProUGUI>();
        ph.text = "Search...";
        ph.fontSize = 11;
        ph.color = new Color(0.5f, 0.5f, 0.6f, 1f);
        RectTransform phrt = placeholder.GetComponent<RectTransform>();
        phrt.anchorMin = Vector2.zero;
        phrt.anchorMax = Vector2.one;
        phrt.offsetMin = new Vector2(4, 0);
        phrt.offsetMax = new Vector2(-4, 0);

        GameObject textArea = new GameObject("Text");
        textArea.transform.SetParent(go.transform, false);
        TextMeshProUGUI txt = textArea.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 11;
        txt.color = Color.white;
        RectTransform trt = textArea.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(4, 0);
        trt.offsetMax = new Vector2(-4, 0);

        TMP_InputField field = go.AddComponent<TMP_InputField>();
        field.textViewport = trt;
        field.textComponent = txt;
        field.placeholder = ph;
    }

    void MakeDropdown(GameObject parent, string name, Vector2 pos, Vector2 size, string[] options)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.17f, 0.25f, 1f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        GameObject label = new GameObject("Label");
        label.transform.SetParent(go.transform, false);
        TextMeshProUGUI lbl = label.AddComponent<TextMeshProUGUI>();
        lbl.fontSize = 10;
        lbl.color = Color.white;
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(4, 0);
        lrt.offsetMax = new Vector2(-4, 0);

        TMP_Dropdown dd = go.AddComponent<TMP_Dropdown>();
        dd.captionText = lbl;
        dd.options.Clear();

        foreach (var o in options)
            dd.options.Add(new TMP_Dropdown.OptionData(o));

        dd.RefreshShownValue();
    }
}