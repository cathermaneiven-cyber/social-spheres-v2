using Photon.Pun;
using Photon.VR;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CurrentMode : MonoBehaviour
{
    TextMeshPro Text;


    private void Start()
    {
        Text = GetComponent<TextMeshPro>();
    }

    private void Update()
    {
        if (PhotonNetwork.InRoom)
        {
            TagPlayerManager LocalManager = PhotonVRManager.Manager.LocalPlayer.GetComponent<TagPlayerManager>();

            if (LocalManager != null)
            {
                if (LocalManager.IsPlaying)
                {
                    Text.text = "Current Mode: Tag";
                }
                else
                {
                    Text.text = "Current Mode: Casual";
                }
            }
        }
        else
        {
            Text.text = "Not In Room!";
        }
    }
}
