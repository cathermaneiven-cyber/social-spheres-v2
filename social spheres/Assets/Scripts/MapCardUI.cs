using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SocialSpheres.ModIO;

namespace SocialSpheres
{
    /// <summary>
    /// One card in the 3x3 map grid. Assign this prefab in MapBrowserUI.
    /// The prefab needs: a RawImage (thumbnail), two TMP labels (name, author),
    /// a downloads label, a rating label, and a Button on the root.
    /// </summary>
    public class MapCardUI : MonoBehaviour
    {
        [Header("Card Elements")]
        public RawImage        thumbnail;
        public TextMeshProUGUI mapName;
        public TextMeshProUGUI authorLabel;
        public TextMeshProUGUI downloadCount;
        public TextMeshProUGUI ratingLabel;
        public GameObject      downloadedBadge; // a small "✓" badge overlay
        public Button          selectButton;

        private ModProfile _mod;
        private Action     _onClick;

        public void Populate(ModProfile mod, Action onClick)
        {
            _mod     = mod;
            _onClick = onClick;

            mapName.text      = mod.name;
            authorLabel.text  = mod.submitted_by?.username ?? "Unknown";
            downloadCount.text = mod.stats != null
                ? FormatCount(mod.stats.downloads_total)
                : "—";
            ratingLabel.text  = mod.stats != null
                ? (mod.stats.ratings_weighted_aggregate * 5f).ToString("0.0") + "★"
                : "—";

            if (downloadedBadge != null)
                downloadedBadge.SetActive(ModIOManager.Instance.IsDownloaded(mod.id));

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onClick?.Invoke());

            // Load thumbnail async
            if (mod.logo != null && !string.IsNullOrEmpty(mod.logo.thumb_320x180))
                ModIOManager.Instance.FetchThumbnail(mod.logo.thumb_320x180, SetThumbnail);
        }

        private void SetThumbnail(Texture2D tex)
        {
            if (thumbnail != null && tex != null)
                thumbnail.texture = tex;
        }

        private static string FormatCount(int n)
        {
            if (n >= 1000000) return $"{n / 1000000f:0.0}M";
            if (n >= 1000)    return $"{n / 1000f:0.0}k";
            return n.ToString();
        }
    }
}
