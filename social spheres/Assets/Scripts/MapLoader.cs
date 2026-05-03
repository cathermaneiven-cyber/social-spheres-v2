using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace SocialSpheres
{
    /// <summary>
    /// Loads a community map from a downloaded mod folder.
    /// Maps must be packaged as an AssetBundle named "map.bundle" inside the mod zip.
    /// The bundle must contain a scene or a root prefab tagged "MapRoot".
    /// </summary>
    public class MapLoader : MonoBehaviour
    {
        public static MapLoader Instance { get; private set; }

        [Header("Scene Management")]
        [Tooltip("The GameObject that holds the current map content. Gets cleared on map load.")]
        public Transform mapRoot;

        private AssetBundle _currentBundle;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Load a map from a mod folder. Call this when the player presses Play.
        /// </summary>
        public void LoadMap(string modFolderPath, string mapName)
        {
            StartCoroutine(LoadMapRoutine(modFolderPath, mapName));
        }

        private IEnumerator LoadMapRoutine(string modFolderPath, string mapName)
        {
            // Unload previous map
            if (_currentBundle != null)
            {
                _currentBundle.Unload(true);
                _currentBundle = null;
            }

            if (mapRoot != null)
            {
                foreach (Transform child in mapRoot)
                    Destroy(child.gameObject);
            }

            // Look for map.bundle inside the mod folder
            string bundlePath = Path.Combine(modFolderPath, "map.bundle");
            if (!File.Exists(bundlePath))
            {
                Debug.LogError($"[MapLoader] No map.bundle found in {modFolderPath}");
                yield break;
            }

            Debug.Log($"[MapLoader] Loading map: {mapName}");

            // Load AssetBundle
            var request = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return request;

            _currentBundle = request.assetBundle;
            if (_currentBundle == null)
            {
                Debug.LogError("[MapLoader] Failed to load AssetBundle.");
                yield break;
            }

            // Try loading a prefab tagged MapRoot, or load all assets
            string[] assetNames = _currentBundle.GetAllAssetNames();
            GameObject mapPrefab = null;

            foreach (var assetName in assetNames)
            {
                if (assetName.EndsWith(".prefab"))
                {
                    var prefabReq = _currentBundle.LoadAssetAsync<GameObject>(assetName);
                    yield return prefabReq;
                    mapPrefab = prefabReq.asset as GameObject;
                    break;
                }
            }

            if (mapPrefab == null)
            {
                Debug.LogError("[MapLoader] No prefab found in AssetBundle.");
                yield break;
            }

            // Spawn the map
            var spawnParent = mapRoot != null ? mapRoot : null;
            var instance    = Instantiate(mapPrefab, spawnParent);
            instance.name   = mapName;

            Debug.Log($"[MapLoader] Map '{mapName}' loaded successfully.");
        }
    }
}
