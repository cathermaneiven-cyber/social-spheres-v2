using UnityEngine;
using UnityEngine.Networking;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using Photon.VR;
using Photon.Voice.PUN;
using Photon.VR.Player;
using System.Text;
using System.Collections;

[RequireComponent(typeof(PhotonView))]
public class LeaderBoard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] public TMP_Text[] displaySpot;
    [SerializeField] public Renderer[] ColorSpot;

    [Header("Button References")]
    [SerializeField] public MuteButton[] MuteButtons;
    [SerializeField] public ReportButton[] ReportButtons;
    [SerializeField] public KickButton[] KickButtons;

    [Header("External")]
    [SerializeField] public string WebHookURL;
    [SerializeField] public Playfablogin playfablogin;

    private bool hashed = false;
    private bool Kicked = false;
    private PhotonView photonView;

    private void Start()
    {
        photonView = GetComponent<PhotonView>();
        if (photonView.OwnershipTransfer != OwnershipOption.Takeover)
        {
            photonView.OwnershipTransfer = OwnershipOption.Takeover;
        }
    }

    private void Update()
    {
        if (PhotonNetwork.IsConnected && !hashed)
        {
            ExitGames.Client.Photon.Hashtable hash = PhotonNetwork.LocalPlayer.CustomProperties;
            hash["PlayfabID"] = playfablogin.MyPlayFabID;
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
            hashed = true;
        }

        UpdateLeaderBoard();
    }

    private void UpdateLeaderBoard()
    {
        for (int i = 0; i < displaySpot.Length; i++)
        {
            if (i < PhotonNetwork.PlayerList.Length && !Kicked)
            {
                var player = PhotonNetwork.PlayerList[i];

                // Show slot
                displaySpot[i].gameObject.SetActive(true);
                ColorSpot[i].gameObject.SetActive(true);
                displaySpot[i].color = Color.white;
                displaySpot[i].text = player.NickName;

                // Assign color if available
                foreach (PhotonVRPlayer PVRP in FindObjectsOfType<PhotonVRPlayer>())
                {
                    if (PVRP.photonView.Owner == player)
                    {
                        if (player.CustomProperties.TryGetValue("Colour", out object colorJson))
                        {
                            ColorSpot[i].material.color = JsonUtility.FromJson<Color>((string)colorJson);
                        }
                    }
                }

                // Setup Mute button
                if (i < MuteButtons.Length && MuteButtons[i] != null)
                {
                    MuteButtons[i].ButtonNumber = i + 1;
                    if (player == PhotonNetwork.LocalPlayer)
                        MuteButtons[i].gameObject.SetActive(false);
                    else
                    {
                        MuteButtons[i].gameObject.SetActive(true);
                        MuteButtons[i].ResetButton();
                    }
                }

                // Setup Report button
                if (i < ReportButtons.Length && ReportButtons[i] != null)
                {
                    ReportButtons[i].ButtonNumber = i + 1;
                    if (player == PhotonNetwork.LocalPlayer)
                        ReportButtons[i].gameObject.SetActive(false);
                    else
                    {
                        ReportButtons[i].gameObject.SetActive(true);
                        ReportButtons[i].ResetButton();
                    }
                }

                // Setup Kick button
                if (i < KickButtons.Length && KickButtons[i] != null)
                {
                    KickButtons[i].ButtonNumber = i + 1;
                    if (player == PhotonNetwork.LocalPlayer)
                        KickButtons[i].gameObject.SetActive(false);
                    else
                    {
                        KickButtons[i].gameObject.SetActive(true);
                        KickButtons[i].ResetButton();
                    }
                }
            }
            else
            {
                // Hide empty slots
                displaySpot[i].gameObject.SetActive(false);
                ColorSpot[i].gameObject.SetActive(false);

                if (i < MuteButtons.Length && MuteButtons[i] != null)
                    MuteButtons[i].gameObject.SetActive(false);
                if (i < ReportButtons.Length && ReportButtons[i] != null)
                    ReportButtons[i].gameObject.SetActive(false);
                if (i < KickButtons.Length && KickButtons[i] != null)
                    KickButtons[i].gameObject.SetActive(false);
            }

            // Handle kicked state
            if (Kicked)
            {
                if (i == 0)
                {
                    displaySpot[i].gameObject.SetActive(true);
                    displaySpot[i].color = Color.red;
                    displaySpot[i].text = "You have been Kicked";
                    ColorSpot[i].gameObject.SetActive(false);
                }
                else
                {
                    displaySpot[i].gameObject.SetActive(false);
                    ColorSpot[i].gameObject.SetActive(false);
                }

                if (PhotonNetwork.IsConnected)
                    PhotonNetwork.Disconnect();
            }
        }
    }

    public void MutePress(int ButtonNumber)
    {
        int index = ButtonNumber - 1;
        if (index < PhotonNetwork.PlayerList.Length)
        {
            foreach (PhotonVRPlayer PVRP in FindObjectsOfType<PhotonVRPlayer>())
            {
                if (PVRP.photonView.Owner == PhotonNetwork.PlayerList[index])
                {
                    var speaker = PVRP.GetComponent<PhotonVoiceView>()?.SpeakerInUse;
                    if (speaker != null)
                    {
                        var audioSource = speaker.GetComponent<AudioSource>();
                        audioSource.mute = !audioSource.mute;
                    }
                    break;
                }
            }
        }
    }

    public void KickPress(int ButtonNumber)
    {
        int index = ButtonNumber - 1;
        if (index < PhotonNetwork.PlayerList.Length)
        {
            foreach (PhotonVRPlayer PVRP in FindObjectsOfType<PhotonVRPlayer>())
            {
                if (PVRP.photonView.Owner == PhotonNetwork.PlayerList[index])
                {
                    photonView.RequestOwnership();
                    photonView.RPC(nameof(KickPlayer), PVRP.photonView.Owner);
                }
            }
        }
    }

    [PunRPC]
    void KickPlayer()
    {
        Kicked = true;
    }

    public void Report(int ButtonNumber)
    {
        int index = ButtonNumber - 1;
        if (index < PhotonNetwork.PlayerList.Length)
        {
            foreach (PhotonVRPlayer PVRP in FindObjectsOfType<PhotonVRPlayer>())
            {
                if (PVRP.photonView.Owner == PhotonNetwork.PlayerList[index])
                {
                    if (PhotonNetwork.PlayerList[index].CustomProperties.TryGetValue("PlayfabID", out object targetPlayfabID))
                    {
                        string message = $"{PhotonNetwork.PlayerList[index].NickName} ({targetPlayfabID}) was reported by {PlayerPrefs.GetString("Username", "Unknown")} ({playfablogin.MyPlayFabID})";
                        SendtoWebhook(message);
                    }
                }
            }
        }
    }

    public void SendtoWebhook(string message)
    {
        StartCoroutine(PostToDiscord(message));
    }

    private IEnumerator PostToDiscord(string message)
    {
        string jsonPayload = "{\"content\": \"" + message + "\"}";

        using (UnityWebRequest www = new UnityWebRequest(WebHookURL, "POST"))
        {
            byte[] jsonToSend = new UTF8Encoding().GetBytes(jsonPayload);
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Reporting Webhook Error: " + www.error);
            }
        }
    }
}
