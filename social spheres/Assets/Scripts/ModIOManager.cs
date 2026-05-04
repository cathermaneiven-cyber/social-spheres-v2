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

        private const string API_PATH = "https://g-12943.modapi.io/v1";
        private const int GAME_ID = 12943;
        private const string API_KEY = "f0c7d3f58d8422d8c36d46ee16544ad1"; // regenerate this in mod.io dashboard!

        private const int PAGE_SIZE = 9;

        private Dictionary<string, Texture2D> _thumbCache = new Dictionary<string, Texture2D>();
        private HashSet<int> _downloadedMods = new HashSet<int>();

        private const string CONSENT_KEY = "modio_consent_v1";
        public bool HasConsent => PlayerPrefs.GetInt(CONSENT_KEY, 0) == 1;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadDownloadedMods();
        }

        public void SaveConsent()
        {
            PlayerPrefs.SetInt(CONSENT_KEY, 1);
            PlayerPrefs.Save();
        }

        public void FetchMaps(int offset, string sortBy, string searchQuery, string themeFilter, Action<ModPage> onSuccess, Action<string> onError)
        {
            StartCoroutine(FetchMapsRoutine(offset, sortBy, searchQuery, themeFilter, onSuccess, onError));
        }

        public void DownloadMod(ModProfile mod, Action<float> onProgress, Action<string> onComplete, Action<string> onError)
        {
            StartCoroutine(DownloadModRoutine(mod, onProgress, onComplete, onError));
        }

        public void FetchThumbnail(string url, Action<Texture2D> onDone)
        {
            if (string.IsNullOrEmpty(url))
            {
                onDone?.Invoke(null);
                return;
            }

            if (_thumbCache.TryGetValue(url, out Texture2D cached))
            {
                onDone?.Invoke(cached);
                return;
            }

            StartCoroutine(FetchThumbnailRoutine(url, onDone));
        }

        public bool IsDownloaded(int modId)
        {
            return _downloadedMods.Contains(modId);
        }

        public string GetModPath(int modId)
        {
            return System.IO.Path.Combine(Application.persistentDataPath, "Mods", modId.ToString());
        }

        public void RemoveDownloadedMod(int modId)
        {
            string path = GetModPath(modId);

            if (System.IO.Directory.Exists(path))
                System.IO.Directory.Delete(path, true);

            if (_downloadedMods.Contains(modId))
                _downloadedMods.Remove(modId);

            SaveDownloadedMods();
        }

        private IEnumerator FetchMapsRoutine(int offset, string sortBy, string searchQuery, string themeFilter, Action<ModPage> onSuccess, Action<string> onError)
        {
            string sort = sortBy switch
            {
                "Newest" => "-date_added",
                "Top Rated" => "-ratings_weighted_aggregate",
                "Top rated" => "-ratings_weighted_aggregate",
                _ => "-downloads_total"
            };

            string url = $"{API_PATH}/games/{GAME_ID}/mods" +
                         $"?api_key={API_KEY}" +
                         $"&tags=Map" +
                         $"&_limit={PAGE_SIZE}" +
                         $"&_offset={offset}" +
                         $"&_sort={sort}";

            if (!string.IsNullOrWhiteSpace(searchQuery))
                url += $"&_q={UnityWebRequest.EscapeURL(searchQuery)}";

            if (!string.IsNullOrWhiteSpace(themeFilter) && themeFilter != "All")
                url += $"&tags[]={UnityWebRequest.EscapeURL(themeFilter)}";

            using UnityWebRequest req = UnityWebRequest.Get(url);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            // DEBUG
            Debug.Log("[ModIO] URL: " + url);

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[ModIO] FAILED: " + req.error + " | " + req.downloadHandler.text);
                onError?.Invoke(req.error + " | " + req.downloadHandler.text);
                yield break;
            }

            // DEBUG - paste this output here so we can see what mod.io returned
            Debug.Log("[ModIO] Raw JSON: " + req.downloadHandler.text);

            ModPage page = JsonUtility.FromJson<ModPage>(req.downloadHandler.text);

            // DEBUG
            if (page == null)
                Debug.LogError("[ModIO] ModPage parsed as null!");
            else
                Debug.Log("[ModIO] result_count=" + page.result_count + " result_total=" + page.result_total + " data.Length=" + (page.data != null ? page.data.Length.ToString() : "NULL"));

            onSuccess?.Invoke(page);
        }

        private IEnumerator DownloadModRoutine(ModProfile mod, Action<float> onProgress, Action<string> onComplete, Action<string> onError)
        {
            if (mod == null)
            {
                onError?.Invoke("Mod is null.");
                yield break;
            }

            if (mod.modfile == null)
            {
                onError?.Invoke("Mod has no modfile.");
                yield break;
            }

            string apiUrl = $"{API_PATH}/games/{GAME_ID}/mods/{mod.id}/files/{mod.modfile.id}?api_key={API_KEY}";

            using UnityWebRequest infoReq = UnityWebRequest.Get(apiUrl);
            yield return infoReq.SendWebRequest();

            if (infoReq.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(infoReq.error + " | " + infoReq.downloadHandler.text);
                yield break;
            }

            ModFile fileInfo = JsonUtility.FromJson<ModFile>(infoReq.downloadHandler.text);

            if (fileInfo == null || fileInfo.download == null || string.IsNullOrEmpty(fileInfo.download.binary_url))
            {
                onError?.Invoke("No download URL returned from mod.io.");
                yield break;
            }

            string downloadUrl = fileInfo.download.binary_url;
            string destDir = GetModPath(mod.id);
            string destFile = System.IO.Path.Combine(destDir, mod.id + ".zip");

            System.IO.Directory.CreateDirectory(destDir);

            using UnityWebRequest dlReq = new UnityWebRequest(downloadUrl, UnityWebRequest.kHttpVerbGET);
            dlReq.downloadHandler = new DownloadHandlerFile(destFile);

            UnityWebRequestAsyncOperation op = dlReq.SendWebRequest();

            while (!op.isDone)
            {
                onProgress?.Invoke(op.progress);
                yield return null;
            }

            if (dlReq.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(dlReq.error);
                yield break;
            }

            try
            {
                if (System.IO.File.Exists(System.IO.Path.Combine(destDir, "map.bundle")))
                    System.IO.File.Delete(System.IO.Path.Combine(destDir, "map.bundle"));

                System.IO.Compression.ZipFile.ExtractToDirectory(destFile, destDir);
                System.IO.File.Delete(destFile);

                _downloadedMods.Add(mod.id);
                SaveDownloadedMods();

                onProgress?.Invoke(1f);
                onComplete?.Invoke(destDir);
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
        }

        private IEnumerator FetchThumbnailRoutine(string url, Action<Texture2D> onDone)
        {
            using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                _thumbCache[url] = tex;
                onDone?.Invoke(tex);
            }
            else
            {
                onDone?.Invoke(null);
            }
        }

        private void SaveDownloadedMods()
        {
            PlayerPrefs.SetString("downloaded_mods", string.Join(",", _downloadedMods));
            PlayerPrefs.Save();
        }

        private void LoadDownloadedMods()
        {
            string saved = PlayerPrefs.GetString("downloaded_mods", "");

            if (string.IsNullOrEmpty(saved))
                return;

            foreach (string s in saved.Split(','))
            {
                if (int.TryParse(s, out int id))
                    _downloadedMods.Add(id);
            }
        }
    }
}