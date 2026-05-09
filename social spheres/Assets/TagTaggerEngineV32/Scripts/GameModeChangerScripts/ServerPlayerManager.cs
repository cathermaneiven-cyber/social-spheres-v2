using Photon.VR;
using System;
using UnityEngine;

public class ServerPlayerManager : MonoBehaviour
{
    public TagPlayerManager PlayertagManager;
    [NonSerialized]
    public ChangeGameMode Button;


    private void Start()
    {
        foreach (ChangeGameMode Buttons in FindObjectsOfType<ChangeGameMode>())
        {
            if (Buttons.IsTagButton)
            {
                Button = Buttons;
            }
        }
    }
    private void Update()
    {
        if (PhotonVRManager.Manager.AppId == Button.AppIdTag && PhotonVRManager.Manager.VoiceAppId == Button.VoiceIdTag)
        {
            if (PlayertagManager.IsPlaying != true)
            {
                PlayertagManager.IsPlaying = true;
            }
        }
        else if (PhotonVRManager.Manager.AppId == Button.AppId && PhotonVRManager.Manager.VoiceAppId == Button.VoiceId)
        {
            if (PlayertagManager.IsPlaying != false)
            {
                PlayertagManager.IsPlaying = false;
            }
        }
    }
}
