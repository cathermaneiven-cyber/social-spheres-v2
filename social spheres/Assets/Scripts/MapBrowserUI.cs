using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SocialSpheres.ModIO;

namespace SocialSpheres
{
    /// <summary>
    /// Attach this to the Menu GameObject (child of LeftHand Controller).
    /// It drives the World Space Canvas that sits on the physical hand menu board.
    /// </summary>
    public class MapBrowserUI : MonoBehaviour
    {
        // ── Inspector refs ────────────────────────────────────────────
        [Header("Panels")]
        public GameObject consentPanel;
        public GameObject browserPanel;
        public GameObject detailPanel;
        public GameObject loadingOverlay;

        [Header("Consent Panel")]
        public Button consentAcceptBtn;
        public Button consentDeclineBtn;

        [Header("Browser - Toolbar")]
        public TMP_InputField searchInput;
        public TMP_Dropdown   sortDropdown;   // Popular / Newest / Top Rated
        public TMP_Dropdown   themeDropdown;  // All / City / Nature / Sci-fi / etc
        public Button         prevPageBtn;
        public Button         nextPageBtn;
        public TextMeshProUGUI pageLabel;

        [Header("Browser - Map Grid")]
        public Transform      cardContainer;  // parent of the 9-card grid
        public MapCardUI      cardPrefab;

        [Header("Detail Panel")]
        public RawImage        detailThumb;
        public TextMeshProUGUI detailName;
        public TextMeshProUGUI detailAuthor;
        public TextMeshProUGUI detailDescription;
        public TextMeshProUGUI detailDownloads;
        public TextMeshProUGUI detailRating;
        public TextMeshProUGUI detailSize;
        public Button          playBtn;
        public Button          downloadBtn;
        public Slider          downloadProgress;
        public Button          reportBtn;
        public Button          backBtn;

        [Header("Footer")]
        public Button tosBtn;
        public Button privacyBtn;

        // ── State ─────────────────────────────────────────────────────
        private int          _currentOffset = 0;
        private int          _totalResults  = 0;
        private ModProfile   _selectedMod;
        private List<MapCardUI> _activeCards = new();

        private const int    PAGE_SIZE = 9;
        private const string TOS_URL   = "https://mod.io/terms";
        private const string PRIV_URL  = "https://mod.io/privacy";
        private const string REPORT_URL = "https://mod.io/report";

        // ── Unity lifecycle ───────────────────────────────────────────

        void Start()
        {
            // Wire up buttons
            consentAcceptBtn.onClick.AddListener(OnConsentAccepted);
            consentDeclineBtn.onClick.AddListener(OnConsentDeclined);
            searchInput.onEndEdit.AddListener(_ => RefreshSearch());
            sortDropdown.onValueChanged.AddListener(_ => RefreshSearch());
            themeDropdown.onValueChanged.AddListener(_ => RefreshSearch());
            prevPageBtn.onClick.AddListener(PrevPage);
            nextPageBtn.onClick.AddListener(NextPage);
            backBtn.onClick.AddListener(ShowBrowser);
            reportBtn.onClick.AddListener(() => Application.OpenURL(REPORT_URL));
            tosBtn.onClick.AddListener(() => Application.OpenURL(TOS_URL));
            privacyBtn.onClick.AddListener(() => Application.OpenURL(PRIV_URL));
            playBtn.onClick.AddListener(OnPlayPressed);
            downloadBtn.onClick.AddListener(OnDownloadPressed);

            // Show consent or go straight to browser
            if (ModIOManager.Instance.HasConsent)
                ShowBrowser();
            else
                ShowConsent();
        }

        // ── Panel switching ───────────────────────────────────────────

        void ShowConsent()
        {
            consentPanel.SetActive(true);
            browserPanel.SetActive(false);
            detailPanel.SetActive(false);
        }

        void ShowBrowser()
        {
            consentPanel.SetActive(false);
            browserPanel.SetActive(true);
            detailPanel.SetActive(false);
            if (_activeCards.Count == 0) FetchPage();
        }

        void ShowDetail(ModProfile mod)
        {
            _selectedMod = mod;
            browserPanel.SetActive(false);
            detailPanel.SetActive(true);

            detailName.text        = mod.name;
            detailAuthor.text      = "by " + mod.submitted_by.username;
            detailDescription.text = mod.description_plaintext ?? mod.summary;
            detailDownloads.text   = mod.stats.downloads_total.ToString("N0") + " downloads";
            detailRating.text      = (mod.stats.ratings_weighted_aggregate * 5f).ToString("0.0") + " / 5";
            detailSize.text        = FormatBytes(mod.modfile?.filesize ?? 0);

            bool downloaded = ModIOManager.Instance.IsDownloaded(mod.id);
            playBtn.gameObject.SetActive(downloaded);
            downloadBtn.gameObject.SetActive(!downloaded);
            downloadProgress.gameObject.SetActive(false);

            if (mod.logo != null)
                ModIOManager.Instance.FetchThumbnail(mod.logo.thumb_320x180,
                    tex => { if (detailThumb != null && tex != null) detailThumb.texture = tex; });
        }

        // ── Consent ───────────────────────────────────────────────────

        void OnConsentAccepted()
        {
            ModIOManager.Instance.SaveConsent();
            ShowBrowser();
        }

        void OnConsentDeclined()
        {
            // Just hide the whole menu — don't force them
            gameObject.SetActive(false);
        }

        // ── Fetching ──────────────────────────────────────────────────

        void RefreshSearch()
        {
            _currentOffset = 0;
            FetchPage();
        }

        void FetchPage()
        {
            loadingOverlay.SetActive(true);
            ClearCards();

            string sort   = sortDropdown.options[sortDropdown.value].text;
            string theme  = themeDropdown.options[themeDropdown.value].text;
            string search = searchInput.text;

            ModIOManager.Instance.FetchMaps(
                _currentOffset, sort, search, theme,
                OnPageReceived,
                err => { loadingOverlay.SetActive(false); Debug.LogWarning("mod.io error: " + err); }
            );
        }

        void OnPageReceived(ModPage page)
        {
            loadingOverlay.SetActive(false);
            _totalResults = page.result_total;

            UpdatePageLabel();
            prevPageBtn.interactable = _currentOffset > 0;
            nextPageBtn.interactable = _currentOffset + PAGE_SIZE < _totalResults;

            foreach (var mod in page.data)
            {
                var card = Instantiate(cardPrefab, cardContainer);
                card.Populate(mod, () => ShowDetail(mod));
                _activeCards.Add(card);
            }
        }

        void ClearCards()
        {
            foreach (var c in _activeCards) Destroy(c.gameObject);
            _activeCards.Clear();
        }

        void PrevPage() { _currentOffset = Mathf.Max(0, _currentOffset - PAGE_SIZE); FetchPage(); }
        void NextPage() { _currentOffset += PAGE_SIZE; FetchPage(); }

        void UpdatePageLabel()
        {
            int page  = (_currentOffset / PAGE_SIZE) + 1;
            int total = Mathf.CeilToInt(_totalResults / (float)PAGE_SIZE);
            pageLabel.text = $"{page} / {total}";
        }

        // ── Download & Play ───────────────────────────────────────────

        void OnDownloadPressed()
        {
            downloadBtn.interactable = false;
            downloadProgress.gameObject.SetActive(true);
            downloadProgress.value = 0f;

            ModIOManager.Instance.DownloadMod(
                _selectedMod,
                progress => downloadProgress.value = progress,
                path =>
                {
                    downloadProgress.gameObject.SetActive(false);
                    playBtn.gameObject.SetActive(true);
                    downloadBtn.gameObject.SetActive(false);
                    Debug.Log($"Map downloaded to: {path}");
                },
                err =>
                {
                    downloadBtn.interactable = true;
                    downloadProgress.gameObject.SetActive(false);
                    Debug.LogWarning("Download error: " + err);
                }
            );
        }

        void OnPlayPressed()
        {
            if (_selectedMod == null) return;
            string mapPath = ModIOManager.Instance.GetModPath(_selectedMod.id);
            MapLoader.Instance?.LoadMap(mapPath, _selectedMod.name);
        }

        // ── Helpers ───────────────────────────────────────────────────

        static string FormatBytes(long bytes)
        {
            if (bytes <= 0)   return "? MB";
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:0.0} KB";
            return $"{bytes / (1024f * 1024f):0.0} MB";
        }
    }
}
