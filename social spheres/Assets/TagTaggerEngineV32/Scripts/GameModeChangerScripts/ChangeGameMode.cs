using Photon.VR;
using UnityEngine;

public class ChangeGameMode : MonoBehaviour
{
    public string HandTag = "Finger";

    public string AppIdTag;
    public string VoiceIdTag;
    public string AppId;
    public string VoiceId;
    public bool IsTagButton;
    public bool EditorTrigger;



    private void Start()
    {
        if (PlayerPrefs.GetInt("IsTag") == 1)
        {
            EditorTrigger = true;
        }
        else
        {
            EditorTrigger = false;
        }
    }

    private void Update()
    {
        if (EditorTrigger)
        {
            EditorTrigger = false;

            if (IsTagButton)
            {
                PhotonVRManager.ChangeServers(AppIdTag, VoiceIdTag);
            }
            else
            {
                PhotonVRManager.ChangeServers(AppId, VoiceId);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == HandTag)
        {
            if (IsTagButton)
            {
                PhotonVRManager.ChangeServers(AppIdTag, VoiceIdTag);
                PlayerPrefs.SetInt("IsTag", 1);
            }
            else
            {
                PhotonVRManager.ChangeServers(AppId, VoiceId);
                PlayerPrefs.SetInt("IsTag", 0);
            }
        }
    }
}
