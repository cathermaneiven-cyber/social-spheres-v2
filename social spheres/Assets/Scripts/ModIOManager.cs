using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SocialSpheres.ModIO
{
    public class ModIOManager : MonoBehaviour
    {
        public static ModIOManager Instance { get; private set; }

        // ── Your Social Spheres credentials ──────────────────────────
        private const string API_PATH   = "https://g-12943.modapi.io/v1";
        private const int    GAME_ID    = 12943;
        private const string API_KEY    = "YOUR_API_KEY_HERE"; // paste your key
        // ─────────────────────────────────────────────────────────────

        private const int PAGE_SIZE = 12;

        // Cache downloaded thumbnails so we don't re-fetch
        private Dictionary<string, Texture2D> _thumbCache = new();

        // Track which mod IDs the local player has already downloaded
        private HashSet<int> _downloadedMods = new();

        // Persisted consent flag
        private const string CONSENT_KEY = "modio_consent_v1";
        public bool HasConsent => PlayerPrefs.GetInt(CONSENT_KEY, 0) == 1;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDownloadedMods();
        }

        // ── Public API ────────────────────────────────────────────────

        /// <summary>Call this when the player accepts the mod.io consent dialog.</summary>
        public void SaveConsent()
        {
            PlayerPrefs.SetInt(CONSENT_KEY, 1);
            PlayerPrefs.Save();
        }

        /// <summary>Fetch a page of maps from mod.io, filtered by the "Map" tag.</summary>
        public void FetchMaps(int offset, string sortBy, string searchQuery,
                              string themeFilter, Action<ModPage> onSuccess, Action<string> onError)
        {
            StartCoroutine(FetchMapsRoutine(offset, sortBy, searchQuery, themeFilter, onSuccess, onError));
        }

        /// <summary>Download a mod's zip file to persistentDataPath/Mods/{modId}/</summary>
        public void DownloadMod(ModProfile mod, Action<float> onProgress,
                                Action<string> onComplete, Action<string> onError)
        {
            StartCoroutine(DownloadModRoutine(mod, onProgress, onComplete, onError));
        }

        /// <summary>Download a thumbnail texture, with cache.</summary>
        public void FetchThumbnail(string url, Action<Texture2D> onDone)
        {
            if (_thumbCache.TryGetValue(url, out var cached)) { onDone?.Invoke(cached); return; }
            StartCoroutine(FetchThumbnailRoutine(url, onDone));
        }

        public bool IsDownloaded(int modId) => _downloadedMods.Contains(modId);

        public string GetModPath(int modId) =>
            System.IO.Path.Combine(Application.persistentDataPath, "Mods", modId.ToString());

        // ── Private coroutines ────────────────────────────────────────

        private IEnumerator FetchMapsRoutine(int offset, string sortBy, string searchQuery,
                                             string themeFilter, Action<ModPage> onSuccess, Action<string> onError)
        {
            string sort  = sortBy switch { "Newest" => "-date_added", "Top rated" => "-ratings_weighted_aggregate", _ => "-downloads_total" };
            string url   = $"{API_PATH}/games/{GAME_ID}/mods" +
                           $"?api_key={API_KEY}" +
                           $"&tags=Map" +
                           $"&_limit={PAGE_SIZE}" +
                           $"&_offset={offset}" +
                           $"&_sort={sort}";

            if (!string.IsNullOrWhiteSpace(searchQuery))
                url += $"&_q={UnityWebRequest.EscapeURL(searchQuery)}";
            if (!string.IsNullOrWhiteSpace(themeFilter) && themeFilter != "All")
                url += $"&tags[]={UnityWebRequest.EscapeURL(themeFilter)}";

            using var req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Content-Type", "application/json");
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            { onError?.Invoke(req.error); yield break; }

            var page = JsonUtility.FromJson<ModPage>(req.downloadHandler.text);
            onSuccess?.Invoke(page);
        }

        private IEnumerator DownloadModRoutine(ModProfile mod, Action<float> onProgress,
                                               Action<string> onComplete, Action<string> onError)
        {
            // First get a fresh download URL (binary_url expires)
            string apiUrl = $"{API_PATH}/games/{GAME_ID}/mods/{mod.id}/files/{mod.modfile.id}" +
                            $"?api_key={API_KEY}";

            using var infoReq = UnityWebRequest.Get(apiUrl);
            yield return infoReq.SendWebRequest();

            if (infoReq.result != UnityWebRequest.Result.Success)
            { onError?.Invoke(infoReq.error); yield break; }

            var fileInfo = JsonUtility.FromJson<ModFile>(infoReq.downloadHandler.text);
            string downloadUrl = fileInfo.download.binary_url;

            // Download the zip
            string destDir  = GetModPath(mod.id);
            string destFile = System.IO.Path.Combine(destDir, $"{mod.id}.zip");
            System.IO.Directory.CreateDirectory(destDir);

            using var dlReq = new UnityWebRequest(downloadUrl, UnityWebRequest.kHttpVerbGET);
            dlReq.downloadHandler = new DownloadHandlerFile(destFile);
            var op = dlReq.SendWebRequest();

            while (!op.isDone)
            {
                onProgress?.Invoke(op.progress);
                yield return null;
            }

            if (dlReq.result != UnityWebRequest.Result.Success)
            { onError?.Invoke(dlReq.error); yield break; }

            // Extract zip using System.IO.Compression
            try
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(destFile, destDir);
                System.IO.File.Delete(destFile);
                _downloadedMods.Add(mod.id);
                SaveDownloadedMods();
                onComplete?.Invoke(destDir);
            }
            catch (Exception e)
            { onError?.Invoke(e.Message); }
        }

        private IEnumerator FetchThumbnailRoutine(string url, Action<Texture2D> onDone)
        {
            using var req = UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                var tex = DownloadHandlerTexture.GetContent(req);
                _thumbCache[url] = tex;
                onDone?.Invoke(tex);
            }
            else onDone?.Invoke(null);
        }

        // ── Persistence helpers ───────────────────────────────────────

        private void SaveDownloadedMods()
        {
            PlayerPrefs.SetString("downloaded_mods", string.Join(",", _downloadedMods));
            PlayerPrefs.Save();
        }

        private void LoadDownloadedMods()
        {
            var saved = PlayerPrefs.GetString("downloaded_mods", "");
            if (string.IsNullOrEmpty(saved)) return;
            foreach (var s in saved.Split(','))
                if (int.TryParse(s, out int id)) _downloadedMods.Add(id);
        }
    }
}
