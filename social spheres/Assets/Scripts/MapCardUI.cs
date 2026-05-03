using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SocialSpheres.ModIO;

namespace SocialSpheres
{
    public class MapCardUI : MonoBehaviour
    {
        public RawImage thumbnail;
        public TextMeshProUGUI mapName;
        public TextMeshProUGUI authorLabel;
        public TextMeshProUGUI downloadCount;
        public TextMeshProUGUI ratingLabel;
        public GameObject downloadedBadge;
        public Button selectButton;
        public TextMeshProUGUI buttonLabel;

        public string subscribeText = "Subscribe";
        public string unsubscribeText = "Unsubscribe";
        public float colliderDepth = 0.05f;
        public Vector2 colliderPadding = new Vector2(8f, 8f);

        private ModProfile mod;
        private Action onClick;

        private void Awake()
        {
            AutoAssign();
            SetupButtonCollider();
        }

        public void Populate(ModProfile newMod, Action clickAction)
        {
            mod = newMod;
            onClick = clickAction;

            AutoAssign();

            if (mapName != null)
                mapName.text = mod.name;

            if (authorLabel != null)
                authorLabel.text = mod.submitted_by != null ? mod.submitted_by.username : "Unknown";

            if (downloadCount != null)
                downloadCount.text = mod.stats != null ? FormatCount(mod.stats.downloads_total) : "0";

            if (ratingLabel != null)
                ratingLabel.text = mod.stats != null ? (mod.stats.ratings_weighted_aggregate * 5f).ToString("0.0") : "0.0";

            if (selectButton != null)
            {
                selectButton.onClick.RemoveAllListeners();
                selectButton.onClick.AddListener(ToggleSubscribe);
            }

            UpdateSubscribeVisual();

            if (mod.logo != null && !string.IsNullOrEmpty(mod.logo.thumb_320x180))
                ModIOManager.Instance.FetchThumbnail(mod.logo.thumb_320x180, SetThumbnail);
        }

        private void AutoAssign()
        {
            if (selectButton == null)
                selectButton = GetComponentInChildren<Button>(true);

            if (buttonLabel == null && selectButton != null)
                buttonLabel = selectButton.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void ToggleSubscribe()
        {
            if (mod == null)
                return;

            bool downloaded = ModIOManager.Instance.IsDownloaded(mod.id);
            bool subscribed = PlayerPrefs.GetInt("subscribed_mod_" + mod.id, 0) == 1;

            if (downloaded || subscribed)
            {
                if (MapLoader.Instance != null && MapLoader.Instance.IsCurrentMap(mod.id))
                    MapLoader.Instance.UnloadCurrentMap();

                ModIOManager.Instance.RemoveDownloadedMod(mod.id);
                PlayerPrefs.SetInt("subscribed_mod_" + mod.id, 0);
                PlayerPrefs.Save();

                Debug.Log("[MapCardUI] Unsubscribed from " + mod.name);
            }
            else
            {
                PlayerPrefs.SetInt("subscribed_mod_" + mod.id, 1);
                PlayerPrefs.Save();

                Debug.Log("[MapCardUI] Subscribed to " + mod.name);
                onClick?.Invoke();
            }

            UpdateSubscribeVisual();
        }

        public void UpdateSubscribeVisual()
        {
            if (mod == null)
                return;

            bool downloaded = ModIOManager.Instance.IsDownloaded(mod.id);
            bool subscribed = PlayerPrefs.GetInt("subscribed_mod_" + mod.id, 0) == 1;

            if (buttonLabel != null)
                buttonLabel.text = downloaded || subscribed ? unsubscribeText : subscribeText;

            if (downloadedBadge != null)
                downloadedBadge.SetActive(downloaded || subscribed);
        }

        private void SetupButtonCollider()
        {
            if (selectButton == null)
                return;

            BoxCollider box = selectButton.GetComponent<BoxCollider>();

            if (box == null)
                box = selectButton.gameObject.AddComponent<BoxCollider>();

            RectTransform rt = selectButton.GetComponent<RectTransform>();

            if (rt != null)
            {
                Vector2 size = rt.rect.size;
                box.size = new Vector3(Mathf.Abs(size.x) + colliderPadding.x, Mathf.Abs(size.y) + colliderPadding.y, colliderDepth);
                box.center = Vector3.zero;
            }

            box.isTrigger = true;
        }

        private void SetThumbnail(Texture2D tex)
        {
            if (thumbnail != null && tex != null)
                thumbnail.texture = tex;
        }

        private static string FormatCount(int n)
        {
            if (n >= 1000000)
                return $"{n / 1000000f:0.0}M";

            if (n >= 1000)
                return $"{n / 1000f:0.0}k";

            return n.ToString();
        }
    }
}