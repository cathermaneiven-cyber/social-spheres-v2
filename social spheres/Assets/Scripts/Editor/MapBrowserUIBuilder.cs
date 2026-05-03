using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using SocialSpheres;

public class MapBrowserUIBuilder : EditorWindow
{
    private GameObject targetMenu;
    private Color panelColor      = new Color(0.1f, 0.12f, 0.18f, 0.97f);
    private Color accentColor     = new Color(0.35f, 0.31f, 0.81f, 1f);
    private Color buttonColor     = new Color(0.35f, 0.31f, 0.81f, 1f);
    private Color textColor       = new Color(0.9f, 0.9f, 0.95f, 1f);
    private Color darkPanelColor  = new Color(0.08f, 0.09f, 0.14f, 0.97f);

    private enum MenuFace { Front, Back, Left, Right, Top }
    private MenuFace selectedFace = MenuFace.Front;

    [MenuItem("Tools/Social Spheres/Build Map Browser UI")]
    public static void ShowWindow()
    {
        GetWindow<MapBrowserUIBuilder>("Map Browser Builder");
    }

    void OnGUI()
    {
        GUILayout.Label("Map Browser UI Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUILayout.Label("1. Select your Menu GameObject in the Hierarchy");
        GUILayout.Label("2. Drag it into the slot below");
        GUILayout.Label("3. Pick which face the UI appears on");
        GUILayout.Label("4. Click Build!");
        EditorGUILayout.Space();

        targetMenu = (GameObject)EditorGUILayout.ObjectField(
            "Menu GameObject", targetMenu, typeof(GameObject), true);

        EditorGUILayout.Space();
        GUILayout.Label("Which face should the UI appear on?", EditorStyles.boldLabel);
        selectedFace = (MenuFace)GUILayout.SelectionGrid((int)selectedFace,
            new[] { "Front (Z-)", "Back (Z+)", "Left (X-)", "Right (X+)", "Top (Y+)" }, 3);

        EditorGUILayout.Space();
        GUILayout.Label("Colors (optional)", EditorStyles.boldLabel);
        panelColor   = EditorGUILayout.ColorField("Panel Background", panelColor);
        accentColor  = EditorGUILayout.ColorField("Accent / Header",  accentColor);
        buttonColor  = EditorGUILayout.ColorField("Button Color",     buttonColor);
        textColor    = EditorGUILayout.ColorField("Text Color",       textColor);

        EditorGUILayout.Space();

        GUI.enabled = targetMenu != null;
        if (GUILayout.Button("Build Map Browser UI", GUILayout.Height(40)))
            BuildUI();
        GUI.enabled = true;

        if (targetMenu == null)
            EditorGUILayout.HelpBox("Drag your Menu GameObject into the slot above.", MessageType.Info);
    }

    // Returns local position offset and rotation based on chosen face
    (Vector3 pos, Quaternion rot) GetFaceTransform()
    {
        // Use the mesh bounds to push canvas just in front of the face
        float offset = 0.15f;
        return selectedFace switch
        {
            MenuFace.Front  => (new Vector3(0,  0, -offset), Quaternion.Euler(90, 180, 0)),
            MenuFace.Back   => (new Vector3(0,  0,  offset), Quaternion.Euler(90,   0, 0)),
            MenuFace.Left   => (new Vector3(-offset, 0, 0),  Quaternion.Euler(90,  90, 0)),
            MenuFace.Right  => (new Vector3( offset, 0, 0),  Quaternion.Euler(90, 270, 0)),
            MenuFace.Top    => (new Vector3(0,  offset, 0),  Quaternion.Euler(  0,   0, 0)),
            _               => (new Vector3(0,  0, -offset), Quaternion.Euler(90, 180, 0)),
        };
    }

    void BuildUI()
    {
        Undo.RegisterFullObjectHierarchyUndo(targetMenu, "Build Map Browser UI");

        // ── 1. Canvas ─────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("MenuCanvas");
        canvasGO.transform.SetParent(targetMenu.transform, false);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        // Try to assign main camera
        if (Camera.main != null) canvas.worldCamera = Camera.main;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        var (facePos, faceRot) = GetFaceTransform();
        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta      = new Vector2(400, 600);
        canvasRT.localScale     = new Vector3(0.001f, 0.001f, 0.001f);
        canvasRT.localPosition  = facePos;
        canvasRT.localRotation  = faceRot;

        // ── 2. Four root panels ───────────────────────────────────────
        GameObject consentPanel  = MakePanel(canvasGO, "ConsentPanel",  panelColor);
        GameObject browserPanel  = MakePanel(canvasGO, "BrowserPanel",  panelColor);
        GameObject detailPanel   = MakePanel(canvasGO, "DetailPanel",   panelColor);
        GameObject loadingPanel  = MakePanel(canvasGO, "LoadingOverlay",new Color(0,0,0,0.7f));

        // Only consent visible by default; rest hidden
        browserPanel.SetActive(false);
        detailPanel.SetActive(false);
        loadingPanel.SetActive(false);

        // ── 3. Consent Panel contents ─────────────────────────────────
        BuildConsentPanel(consentPanel);

        // ── 4. Browser Panel contents ─────────────────────────────────
        BuildBrowserPanel(browserPanel);

        // ── 5. Detail Panel contents ──────────────────────────────────
        BuildDetailPanel(detailPanel);

        // ── 6. Loading Overlay ────────────────────────────────────────
        MakeTMPLabel(loadingPanel, "LoadingLabel", "Loading...", 28,
                     new Vector2(0, 0), new Vector2(200, 50), textColor);

        // ── 7. Wire up MapBrowserUI script ────────────────────────────
        MapBrowserUI ui = canvasGO.AddComponent<MapBrowserUI>();
        AssignUIFields(ui, consentPanel, browserPanel, detailPanel, loadingPanel);

        EditorUtility.SetDirty(canvasGO);
        Selection.activeGameObject = canvasGO;
        Debug.Log("[MapBrowserBuilder] Done! Check the MenuCanvas Inspector to verify all fields.");
        EditorUtility.DisplayDialog("Done!",
            "Map Browser UI built successfully!\n\n" +
            "Next steps:\n" +
            "1. Create a MapCard prefab and assign it to MapBrowserUI > Card Prefab\n" +
            "2. Add MapBrowserUI script fields are all wired (check Inspector)\n" +
            "3. Paste your API key in ModIOManager.cs", "OK");
    }

    // ── Panel builders ────────────────────────────────────────────────

    void BuildConsentPanel(GameObject parent)
    {
        // Header bar
        GameObject header = MakeImage(parent, "Header", accentColor,
                                      new Vector2(0, 260), new Vector2(400, 60));
        MakeTMPLabel(header, "Title", "Community Maps  •  mod.io", 18,
                     Vector2.zero, new Vector2(380, 50), Color.white);

        // Body text
        MakeTMPLabel(parent, "BodyText",
            "This feature uses mod.io to let you browse and download\ncommunity-made maps.\n\n" +
            "By tapping Accept you agree to the\nmod.io Terms of Use, Privacy Policy,\nand Acceptable Use Policy.",
            14, new Vector2(0, 30), new Vector2(350, 180), textColor);

        // Buttons
        MakeButton(parent, "AcceptButton",  "Accept",  buttonColor, new Vector2(-80, -220), new Vector2(140, 44));
        MakeButton(parent, "DeclineButton", "Decline", darkPanelColor, new Vector2(80, -220), new Vector2(140, 44));

        // Footer links
        MakeTMPLabel(parent, "FooterLinks",
            "Terms of Use  |  Privacy Policy  |  AUP",
            10, new Vector2(0, -280), new Vector2(380, 24),
            new Color(0.5f, 0.5f, 0.8f, 1f));
    }

    void BuildBrowserPanel(GameObject parent)
    {
        // Header
        GameObject header = MakeImage(parent, "Header", accentColor,
                                      new Vector2(0, 270), new Vector2(400, 50));
        MakeTMPLabel(header, "Title", "Community Maps", 16, Vector2.zero, new Vector2(200, 40), Color.white);

        // Toolbar row
        GameObject toolbar = MakeImage(parent, "Toolbar", darkPanelColor,
                                       new Vector2(0, 225), new Vector2(400, 40));
        MakeInputField(toolbar, "SearchInput",   new Vector2(-90, 0), new Vector2(180, 32));
        MakeDropdown(toolbar,   "SortDropdown",  new Vector2(60,  0), new Vector2(110, 32), new[]{"Popular","Newest","Top Rated"});
        MakeDropdown(toolbar,   "ThemeDropdown", new Vector2(175, 0), new Vector2(100, 32), new[]{"All","City","Nature","Sci-fi","Fantasy"});

        // Card grid area
        GameObject gridArea = MakeImage(parent, "CardGrid", new Color(0,0,0,0), new Vector2(0, 30), new Vector2(390, 340));
        GridLayoutGroup grid = gridArea.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(120, 110);
        grid.spacing         = new Vector2(8, 8);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        grid.padding         = new RectOffset(6, 6, 6, 6);

        // Pagination row
        GameObject pager = MakeImage(parent, "Pager", darkPanelColor,
                                     new Vector2(0, -155), new Vector2(400, 36));
        MakeButton(pager, "PrevPageBtn", "◀", buttonColor, new Vector2(-150, 0), new Vector2(50, 28));
        MakeTMPLabel(pager, "PageLabel", "1 / 1", 13, Vector2.zero, new Vector2(80, 28), textColor);
        MakeButton(pager, "NextPageBtn", "▶", buttonColor, new Vector2(150, 0), new Vector2(50, 28));

        // Footer
        GameObject footer = MakeImage(parent, "Footer", darkPanelColor,
                                      new Vector2(0, -280), new Vector2(400, 30));
        MakeTMPLabel(footer, "PoweredBy", "Powered by mod.io", 9,
                     new Vector2(-60, 0), new Vector2(160, 26), new Color(0.5f,0.5f,0.8f,1f));
        MakeButton(footer, "ToSBtn",     "Terms",   darkPanelColor, new Vector2(80,  0), new Vector2(70, 24));
        MakeButton(footer, "PrivacyBtn", "Privacy", darkPanelColor, new Vector2(155, 0), new Vector2(70, 24));
    }

    void BuildDetailPanel(GameObject parent)
    {
        // Back button
        MakeButton(parent, "BackButton", "◀ Back", darkPanelColor, new Vector2(-160, 270), new Vector2(90, 32));

        // Thumbnail - RawImage directly (no Image component conflict)
        GameObject thumb = new GameObject("DetailThumb");
        thumb.transform.SetParent(parent.transform, false);
        RawImage rawImg = thumb.AddComponent<RawImage>();
        rawImg.color = new Color(0.2f, 0.2f, 0.3f, 1f);
        RectTransform thumbRT = thumb.GetComponent<RectTransform>();
        thumbRT.anchoredPosition = new Vector2(0, 180);
        thumbRT.sizeDelta = new Vector2(380, 120);

        // Text fields
        MakeTMPLabel(parent, "DetailName",    "Map Name",    18, new Vector2(0,  90),  new Vector2(370, 30), textColor);
        MakeTMPLabel(parent, "DetailAuthor",  "by author",   12, new Vector2(0,  60),  new Vector2(370, 22), accentColor);
        MakeTMPLabel(parent, "DetailDesc",    "Description", 11, new Vector2(0,  10),  new Vector2(370, 80), new Color(0.7f,0.7f,0.8f,1f));
        MakeTMPLabel(parent, "DetailDownloads","0 downloads",11, new Vector2(-90,-80), new Vector2(160, 22), textColor);
        MakeTMPLabel(parent, "DetailRating",  "★ 0.0 / 5",  11, new Vector2(60, -80), new Vector2(120, 22), textColor);
        MakeTMPLabel(parent, "DetailSize",    "0 MB",        11, new Vector2(160,-80), new Vector2(80,  22), textColor);

        // Buttons
        MakeButton(parent, "PlayButton",     "▶  Play Now", buttonColor,     new Vector2(0, -150), new Vector2(350, 44));
        MakeButton(parent, "DownloadButton", "↓  Download", accentColor,     new Vector2(0, -150), new Vector2(350, 44));
        MakeButton(parent, "ReportButton",   "⚑ Report",    darkPanelColor,  new Vector2(0, -205), new Vector2(350, 32));

        // Progress bar
        GameObject sliderGO = new GameObject("DownloadProgress");
        sliderGO.transform.SetParent(parent.transform, false);
        Slider slider = sliderGO.AddComponent<Slider>();
        RectTransform srt = sliderGO.GetComponent<RectTransform>();
        srt.anchoredPosition = new Vector2(0, -150);
        srt.sizeDelta        = new Vector2(350, 20);
        sliderGO.SetActive(false);

        // Footer
        GameObject footer = MakeImage(parent, "Footer", darkPanelColor,
                                      new Vector2(0, -280), new Vector2(400, 30));
        MakeTMPLabel(footer, "PoweredBy", "Powered by mod.io", 9,
                     new Vector2(-60, 0), new Vector2(160, 26), new Color(0.5f,0.5f,0.8f,1f));
        MakeButton(footer, "ToSBtn2",      "Terms",   darkPanelColor, new Vector2(80,  0), new Vector2(70, 24));
        MakeButton(footer, "PrivacyBtn2",  "Privacy", darkPanelColor, new Vector2(155, 0), new Vector2(70, 24));
    }

    void AssignUIFields(MapBrowserUI ui,
                        GameObject consent, GameObject browser,
                        GameObject detail,  GameObject loading)
    {
        ui.consentPanel   = consent;
        ui.browserPanel   = browser;
        ui.detailPanel    = detail;
        ui.loadingOverlay = loading;

        // Consent
        ui.consentAcceptBtn  = consent.transform.Find("AcceptButton")?.GetComponent<Button>();
        ui.consentDeclineBtn = consent.transform.Find("DeclineButton")?.GetComponent<Button>();

        // Browser toolbar
        Transform toolbar = browser.transform.Find("Toolbar");
        if (toolbar != null)
        {
            ui.searchInput   = toolbar.Find("SearchInput")?.GetComponent<TMP_InputField>();
            ui.sortDropdown  = toolbar.Find("SortDropdown")?.GetComponent<TMP_Dropdown>();
            ui.themeDropdown = toolbar.Find("ThemeDropdown")?.GetComponent<TMP_Dropdown>();
        }
        Transform pager = browser.transform.Find("Pager");
        if (pager != null)
        {
            ui.prevPageBtn = pager.Find("PrevPageBtn")?.GetComponent<Button>();
            ui.nextPageBtn = pager.Find("NextPageBtn")?.GetComponent<Button>();
            ui.pageLabel   = pager.Find("PageLabel")?.GetComponent<TextMeshProUGUI>();
        }
        ui.cardContainer = browser.transform.Find("CardGrid");

        // Detail
        ui.detailThumb       = detail.transform.Find("DetailThumb")?.GetComponent<RawImage>();
        ui.detailName        = detail.transform.Find("DetailName")?.GetComponent<TextMeshProUGUI>();
        ui.detailAuthor      = detail.transform.Find("DetailAuthor")?.GetComponent<TextMeshProUGUI>();
        ui.detailDescription = detail.transform.Find("DetailDesc")?.GetComponent<TextMeshProUGUI>();
        ui.detailDownloads   = detail.transform.Find("DetailDownloads")?.GetComponent<TextMeshProUGUI>();
        ui.detailRating      = detail.transform.Find("DetailRating")?.GetComponent<TextMeshProUGUI>();
        ui.detailSize        = detail.transform.Find("DetailSize")?.GetComponent<TextMeshProUGUI>();
        ui.playBtn           = detail.transform.Find("PlayButton")?.GetComponent<Button>();
        ui.downloadBtn       = detail.transform.Find("DownloadButton")?.GetComponent<Button>();
        ui.downloadProgress  = detail.transform.Find("DownloadProgress")?.GetComponent<Slider>();
        ui.reportBtn         = detail.transform.Find("ReportButton")?.GetComponent<Button>();
        ui.backBtn           = detail.transform.Find("BackButton")?.GetComponent<Button>();

        // Footer buttons
        Transform footer = browser.transform.Find("Footer");
        if (footer != null)
        {
            ui.tosBtn     = footer.Find("ToSBtn")?.GetComponent<Button>();
            ui.privacyBtn = footer.Find("PrivacyBtn")?.GetComponent<Button>();
        }
    }

    // ── UI Helper methods ─────────────────────────────────────────────

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

    TextMeshProUGUI MakeTMPLabel(GameObject parent, string name, string text,
                                  float size, Vector2 pos, Vector2 sizeDelta, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        return tmp;
    }

    Button MakeButton(GameObject parent, string name, string label,
                      Color color, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image img   = go.AddComponent<Image>();
        img.color   = color;
        Button btn  = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(color.r + 0.1f, color.g + 0.1f, color.b + 0.1f);
        btn.colors = cb;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;

        // Label child
        MakeTMPLabel(go, "Label", label, 13, Vector2.zero, size, Color.white);
        return btn;
    }

    void MakeInputField(GameObject parent, string name, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image bg   = go.AddComponent<Image>();
        bg.color   = new Color(0.15f, 0.17f, 0.25f, 1f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;

        // Placeholder
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(go.transform, false);
        TextMeshProUGUI ph = placeholder.AddComponent<TextMeshProUGUI>();
        ph.text     = "Search...";
        ph.fontSize = 11;
        ph.color    = new Color(0.5f, 0.5f, 0.6f, 1f);
        RectTransform phrt = placeholder.GetComponent<RectTransform>();
        phrt.anchorMin = Vector2.zero; phrt.anchorMax = Vector2.one;
        phrt.offsetMin = new Vector2(4, 0); phrt.offsetMax = new Vector2(-4, 0);

        // Text area
        GameObject textArea = new GameObject("Text");
        textArea.transform.SetParent(go.transform, false);
        TextMeshProUGUI txt = textArea.AddComponent<TextMeshProUGUI>();
        txt.fontSize = 11;
        txt.color    = Color.white;
        RectTransform trt = textArea.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new Vector2(4, 0); trt.offsetMax = new Vector2(-4, 0);

        TMP_InputField field    = go.AddComponent<TMP_InputField>();
        field.textViewport      = trt;
        field.textComponent     = txt;
        field.placeholder       = ph;
    }

    void MakeDropdown(GameObject parent, string name, Vector2 pos, Vector2 size, string[] options)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image bg  = go.AddComponent<Image>();
        bg.color  = new Color(0.15f, 0.17f, 0.25f, 1f);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;

        GameObject label = new GameObject("Label");
        label.transform.SetParent(go.transform, false);
        TextMeshProUGUI lbl = label.AddComponent<TextMeshProUGUI>();
        lbl.fontSize = 10;
        lbl.color    = Color.white;
        RectTransform lrt = label.GetComponent<RectTransform>();
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = new Vector2(4, 0); lrt.offsetMax = new Vector2(-4, 0);

        TMP_Dropdown dd  = go.AddComponent<TMP_Dropdown>();
        dd.captionText   = lbl;
        dd.options.Clear();
        foreach (var o in options)
            dd.options.Add(new TMP_Dropdown.OptionData(o));
        dd.RefreshShownValue();
    }
}