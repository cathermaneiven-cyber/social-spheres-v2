using System.Collections;
using System.IO;
using UnityEngine;

namespace SocialSpheres
{
    public class MapLoader : MonoBehaviour
    {
        public static MapLoader Instance { get; private set; }

        public Transform mapRoot;
        public GameObject mainLobby;
        public Transform playerRig;
        public Transform playerHead;
        public Transform lobbySpawn;
        public float fallRespawnY = -5f;

        private AssetBundle currentBundle;
        private GameObject currentMap;
        private int currentModId = -1;
        private Transform currentSpawn;
        private bool mapLoaded;
        private bool teleporting;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!mapLoaded || teleporting || currentSpawn == null || playerRig == null)
                return;

            Transform checkTarget = playerHead != null ? playerHead : playerRig;

            if (checkTarget.position.y < fallRespawnY)
            {
                Debug.Log("[MapLoader] Player fell out of map. Respawning.");
                TeleportPlayerToSpawn(currentSpawn);
            }
        }

        public void LoadMap(int modId, string modFolderPath, string mapName)
        {
            StartCoroutine(LoadMapRoutine(modId, modFolderPath, mapName));
        }

        public void UnloadCurrentMap()
        {
            UnloadMapOnly();

            if (mainLobby != null)
                mainLobby.SetActive(true);
            else
                Debug.LogWarning("[MapLoader] Main Lobby is not assigned.");

            if (lobbySpawn != null)
                TeleportPlayerToSpawn(lobbySpawn);
            else
                Debug.LogWarning("[MapLoader] Lobby Spawn is not assigned.");

            Debug.Log("[MapLoader] Returned to lobby.");
        }

        public bool IsCurrentMap(int modId)
        {
            return currentModId == modId;
        }

        private IEnumerator LoadMapRoutine(int modId, string modFolderPath, string mapName)
        {
            if (currentMap != null || currentBundle != null)
                UnloadMapOnly();

            string bundlePath = Path.Combine(modFolderPath, "map.bundle");

            if (!File.Exists(bundlePath))
            {
                Debug.LogError("[MapLoader] No map.bundle found at " + bundlePath);
                yield break;
            }

            AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
            yield return request;

            currentBundle = request.assetBundle;

            if (currentBundle == null)
            {
                Debug.LogError("[MapLoader] Failed to load AssetBundle.");
                yield break;
            }

            GameObject mapPrefab = null;

            foreach (string assetName in currentBundle.GetAllAssetNames())
            {
                if (assetName.EndsWith(".prefab"))
                {
                    AssetBundleRequest prefabReq = currentBundle.LoadAssetAsync<GameObject>(assetName);
                    yield return prefabReq;
                    mapPrefab = prefabReq.asset as GameObject;
                    break;
                }
            }

            if (mapPrefab == null)
            {
                Debug.LogError("[MapLoader] No prefab found in map.bundle.");
                UnloadCurrentMap();
                yield break;
            }

            currentMap = Instantiate(mapPrefab, mapRoot);
            currentMap.name = mapName;
            currentModId = modId;

            currentSpawn = FindDeepChild(currentMap.transform, "PlayerSpawn");

            if (currentSpawn == null)
            {
                Debug.LogError("[MapLoader] Map missing required PlayerSpawn object.");
                UnloadCurrentMap();
                yield break;
            }

            if (mainLobby != null)
                mainLobby.SetActive(false);
            else
                Debug.LogWarning("[MapLoader] Main Lobby is not assigned.");

            mapLoaded = true;
            TeleportPlayerToSpawn(currentSpawn);

            Debug.Log("[MapLoader] Loaded map " + mapName);
        }

        private void UnloadMapOnly()
        {
            mapLoaded = false;
            currentSpawn = null;

            if (currentMap != null)
            {
                Destroy(currentMap);
                currentMap = null;
            }

            if (currentBundle != null)
            {
                currentBundle.Unload(true);
                currentBundle = null;
            }

            currentModId = -1;
        }

        private void TeleportPlayerToSpawn(Transform spawn)
        {
            if (playerRig == null)
            {
                Debug.LogWarning("[MapLoader] Player Rig is not assigned.");
                return;
            }

            if (spawn == null)
            {
                Debug.LogWarning("[MapLoader] Spawn target is missing.");
                return;
            }

            if (!teleporting)
                StartCoroutine(HardTeleportRoutine(spawn));
        }

        private IEnumerator HardTeleportRoutine(Transform spawn)
        {
            teleporting = true;

            if (playerHead == null && Camera.main != null)
                playerHead = Camera.main.transform;

            Rigidbody[] bodies = playerRig.GetComponentsInChildren<Rigidbody>(true);
            CharacterController[] controllers = playerRig.GetComponentsInChildren<CharacterController>(true);
            Collider[] colliders = playerRig.GetComponentsInChildren<Collider>(true);

            foreach (CharacterController controller in controllers)
                controller.enabled = false;

            foreach (Rigidbody body in bodies)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }

            foreach (Collider collider in colliders)
                collider.enabled = false;

            Vector3 headOffset = Vector3.zero;

            if (playerHead != null)
                headOffset = playerHead.position - playerRig.position;

            headOffset.y = 0f;

            Vector3 finalPosition = spawn.position - headOffset;
            Quaternion finalRotation = Quaternion.Euler(0f, spawn.eulerAngles.y, 0f);

            playerRig.SetPositionAndRotation(finalPosition, finalRotation);

            foreach (Transform child in playerRig.GetComponentsInChildren<Transform>(true))
            {
                Rigidbody rb = child.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.position = child.position;
                    rb.rotation = child.rotation;
                }
            }

            Physics.SyncTransforms();

            yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            playerRig.SetPositionAndRotation(finalPosition, finalRotation);

            foreach (Rigidbody body in bodies)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = false;
            }

            foreach (Collider collider in colliders)
                collider.enabled = true;

            foreach (CharacterController controller in controllers)
                controller.enabled = true;

            Physics.SyncTransforms();

            teleporting = false;

            Debug.Log("[MapLoader] Hard teleported player to " + spawn.name);
        }

        private Transform FindDeepChild(Transform parent, string targetName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == targetName)
                    return child;

                Transform found = FindDeepChild(child, targetName);

                if (found != null)
                    return found;
            }

            return null;
        }
    }
}