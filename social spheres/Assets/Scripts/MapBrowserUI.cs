using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SocialSpheres.ModIO;

namespace SocialSpheres
{
    public class MapBrowserUI : MonoBehaviour
    {
        public GameObject consentPanel;
        public GameObject browserPanel;
        public GameObject detailPanel;
        public GameObject loadingOverlay;

        public Button consentAcceptBtn;
        public Button consentDeclineBtn;

        public TMP_InputField searchInput;
        public TMP_Dropdown sortDropdown;
        public TMP_Dropdown themeDropdown;
        public Button prevPageBtn;
        public Button nextPageBtn;
        public TextMeshProUGUI pageLabel;

        public Transform cardContainer;
        public MapCardUI cardPrefab;

        public RawImage detailThumb;
        public TextMeshProUGUI detailName;
        public TextMeshProUGUI detailAuthor;
        public TextMeshProUGUI detailDescription;
        public TextMeshProUGUI detailDownloads;
        public TextMeshProUGUI detailRating;
        public TextMeshProUGUI detailSize;
        public Button playBtn;
        public Button downloadBtn;
        public Slider downloadProgress;
        public Button reportBtn;
        public Button backBtn;

        public Button tosBtn;
        public Button privacyBtn;

        private int currentOffset;
        private int totalResults;
        private ModProfile selectedMod;
        private List<MapCardUI> activeCards = new List<MapCardUI>();

        private const int PAGE_SIZE = 9;
        private const string TOS_URL = "https://mod.io/terms";
        private const string PRIV_URL = "https://mod.io/privacy";
        private const string REPORT_URL = "https://mod.io/report";

        private void Start()
        {
            if (consentAcceptBtn != null)
                consentAcceptBtn.onClick.AddListener(OnConsentAccepted);

            if (consentDeclineBtn != null)
                consentDeclineBtn.onClick.AddListener(OnConsentDeclined);

            if (searchInput != null)
                searchInput.onEndEdit.AddListener(_ => RefreshSearch());

            if (sortDropdown != null)
                sortDropdown.onValueChanged.AddListener(_ => RefreshSearch());

            if (themeDropdown != null)
                themeDropdown.onValueChanged.AddListener(_ => RefreshSearch());

            if (prevPageBtn != null)
                prevPageBtn.onClick.AddListener(PrevPage);

            if (nextPageBtn != null)
                nextPageBtn.onClick.AddListener(NextPage);

            if (backBtn != null)
                backBtn.onClick.AddListener(ShowBrowser);

            if (reportBtn != null)
                reportBtn.onClick.AddListener(() => Application.OpenURL(REPORT_URL));

            if (tosBtn != null)
                tosBtn.onClick.AddListener(() => Application.OpenURL(TOS_URL));

            if (privacyBtn != null)
                privacyBtn.onClick.AddListener(() => Application.OpenURL(PRIV_URL));

            if (playBtn != null)
                playBtn.onClick.AddListener(OnPlayPressed);

            if (downloadBtn != null)
                downloadBtn.onClick.AddListener(OnDownloadPressed);

            if (ModIOManager.Instance != null && ModIOManager.Instance.HasConsent)
                ShowBrowser();
            else
                ShowConsent();
        }

        private void ShowConsent()
        {
            if (consentPanel != null)
                consentPanel.SetActive(true);

            if (browserPanel != null)
                browserPanel.SetActive(false);

            if (detailPanel != null)
                detailPanel.SetActive(false);

            if (loadingOverlay != null)
                loadingOverlay.SetActive(false);
        }

        private void ShowBrowser()
        {
            if (consentPanel != null)
                consentPanel.SetActive(false);

            if (browserPanel != null)
                browserPanel.SetActive(true);

            if (detailPanel != null)
                detailPanel.SetActive(false);

            RefreshCardVisuals();

            if (activeCards.Count == 0)
                FetchPage();
        }

        private void ShowDetail(ModProfile mod)
        {
            selectedMod = mod;

            if (browserPanel != null)
                browserPanel.SetActive(false);

            if (detailPanel != null)
                detailPanel.SetActive(true);

            if (detailName != null)
                detailName.text = mod.name;

            if (detailAuthor != null)
                detailAuthor.text = "by " + (mod.submitted_by != null ? mod.submitted_by.username : "Unknown");

            if (detailDescription != null)
                detailDescription.text = !string.IsNullOrEmpty(mod.description_plaintext) ? mod.description_plaintext : mod.summary;

            if (detailDownloads != null)
                detailDownloads.text = mod.stats != null ? mod.stats.downloads_total.ToString("N0") + " downloads" : "0 downloads";

            if (detailRating != null)
                detailRating.text = mod.stats != null ? (mod.stats.ratings_weighted_aggregate * 5f).ToString("0.0") + " / 5" : "0.0 / 5";

            if (detailSize != null)
                detailSize.text = FormatBytes(mod.modfile != null ? mod.modfile.filesize : 0);

            bool downloaded = ModIOManager.Instance != null && ModIOManager.Instance.IsDownloaded(mod.id);

            if (playBtn != null)
                playBtn.gameObject.SetActive(downloaded);

            if (downloadBtn != null)
            {
                downloadBtn.gameObject.SetActive(!downloaded);
                downloadBtn.interactable = true;
            }

            if (downloadProgress != null)
                downloadProgress.gameObject.SetActive(false);

            if (mod.logo != null && !string.IsNullOrEmpty(mod.logo.thumb_320x180))
                ModIOManager.Instance.FetchThumbnail(mod.logo.thumb_320x180, tex =>
                {
                    if (detailThumb != null && tex != null)
                        detailThumb.texture = tex;
                });
        }

        private void OnConsentAccepted()
        {
            if (ModIOManager.Instance != null)
                ModIOManager.Instance.SaveConsent();

            ShowBrowser();
        }

        private void OnConsentDeclined()
        {
            gameObject.SetActive(false);
        }

        private void RefreshSearch()
        {
            currentOffset = 0;
            FetchPage();
        }

        private void FetchPage()
        {
            if (ModIOManager.Instance == null)
            {
                Debug.LogWarning("[MapBrowserUI] ModIOManager missing.");
                return;
            }

            if (cardPrefab == null)
            {
                Debug.LogWarning("[MapBrowserUI] Card Prefab missing.");
                return;
            }

            if (loadingOverlay != null)
                loadingOverlay.SetActive(true);

            ClearCards();

            string sort = sortDropdown != null && sortDropdown.options.Count > 0 ? sortDropdown.options[sortDropdown.value].text : "Popular";
            string theme = themeDropdown != null && themeDropdown.options.Count > 0 ? themeDropdown.options[themeDropdown.value].text : "All";
            string search = searchInput != null ? searchInput.text : "";

            ModIOManager.Instance.FetchMaps(
                currentOffset,
                sort,
                search,
                theme,
                OnPageReceived,
                err =>
                {
                    if (loadingOverlay != null)
                        loadingOverlay.SetActive(false);

                    Debug.LogWarning("mod.io error: " + err);
                }
            );
        }

        private void OnPageReceived(ModPage page)
        {
            if (loadingOverlay != null)
                loadingOverlay.SetActive(false);

            if (page == null || page.data == null)
            {
                Debug.LogWarning("[MapBrowserUI] No page data received.");
                return;
            }

            totalResults = page.result_total;

            UpdatePageLabel();

            if (prevPageBtn != null)
                prevPageBtn.interactable = currentOffset > 0;

            if (nextPageBtn != null)
                nextPageBtn.interactable = currentOffset + PAGE_SIZE < totalResults;

            foreach (ModProfile mod in page.data)
            {
                MapCardUI card = Instantiate(cardPrefab, cardContainer);
                card.Populate(mod, () => ShowDetail(mod));
                activeCards.Add(card);
            }
        }

        private void ClearCards()
        {
            foreach (MapCardUI card in activeCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }

            activeCards.Clear();
        }

        private void RefreshCardVisuals()
        {
            foreach (MapCardUI card in activeCards)
            {
                if (card != null)
                    card.UpdateSubscribeVisual();
            }
        }

        private void PrevPage()
        {
            currentOffset = Mathf.Max(0, currentOffset - PAGE_SIZE);
            FetchPage();
        }

        private void NextPage()
        {
            currentOffset += PAGE_SIZE;
            FetchPage();
        }

        private void UpdatePageLabel()
        {
            int page = (currentOffset / PAGE_SIZE) + 1;
            int total = Mathf.Max(1, Mathf.CeilToInt(totalResults / (float)PAGE_SIZE));

            if (pageLabel != null)
                pageLabel.text = page + " / " + total;
        }

        private void OnDownloadPressed()
        {
            if (selectedMod == null || ModIOManager.Instance == null)
                return;

            if (downloadBtn != null)
                downloadBtn.interactable = false;

            if (downloadProgress != null)
            {
                downloadProgress.gameObject.SetActive(true);
                downloadProgress.value = 0f;
            }

            ModIOManager.Instance.DownloadMod(
                selectedMod,
                progress =>
                {
                    if (downloadProgress != null)
                        downloadProgress.value = progress;
                },
                path =>
                {
                    if (downloadProgress != null)
                        downloadProgress.gameObject.SetActive(false);

                    if (playBtn != null)
                        playBtn.gameObject.SetActive(true);

                    if (downloadBtn != null)
                        downloadBtn.gameObject.SetActive(false);

                    PlayerPrefs.SetInt("subscribed_mod_" + selectedMod.id, 1);
                    PlayerPrefs.Save();

                    RefreshCardVisuals();

                    Debug.Log("Map downloaded to: " + path);
                },
                err =>
                {
                    if (downloadBtn != null)
                        downloadBtn.interactable = true;

                    if (downloadProgress != null)
                        downloadProgress.gameObject.SetActive(false);

                    Debug.LogWarning("Download error: " + err);
                }
            );
        }

        private void OnPlayPressed()
        {
            if (selectedMod == null || ModIOManager.Instance == null)
                return;

            string mapPath = ModIOManager.Instance.GetModPath(selectedMod.id);

            if (MapLoader.Instance != null)
                MapLoader.Instance.LoadMap(selectedMod.id, mapPath, selectedMod.name);
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0)
                return "? MB";

            if (bytes < 1024)
                return bytes + " B";

            if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:0.0} KB";

            return $"{bytes / (1024f * 1024f):0.0} MB";
        }
    }
}