using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.VR;
using PlayFab;
using PlayFab.ClientModels;
using Photon.Pun;

public class DefaultNameManager : MonoBehaviour
{
    public TextMeshPro NameText;
    public string DefaultNamePrefix = "Farmer"; // The prefix for the default name
    private string generatedName;

    private void Start()
    {
        // Check if a name is already saved in PlayerPrefs
        if (PlayerPrefs.HasKey("username"))
        {
            generatedName = PlayerPrefs.GetString("username");
        }
        else
        {
            // Generate a default name
            GenerateDefaultName();
        }

        // Set the name in the UI and Photon VR Manager
        NameText.text = generatedName;
        PhotonVRManager.SetUsername(generatedName);
        PhotonNetwork.LocalPlayer.NickName = generatedName;

        // Update PlayFab Display Name if logged in
        if (PlayFabClientAPI.IsClientLoggedIn())
        {
            UpdatePlayFabDisplayName(generatedName);
        }
    }

    private void GenerateDefaultName()
    {
        // Generate a random 4-digit number and append it to the prefix
        int randomNumber = Random.Range(1000, 10000); // Random number between 1000 and 9999
        generatedName = DefaultNamePrefix + randomNumber;

        // Save the generated name to PlayerPrefs
        PlayerPrefs.SetString("username", generatedName);
    }

    public void UpdateName(string newName)
    {
        // Update the name only if it has been changed by the user
        if (!string.IsNullOrEmpty(newName))
        {
            generatedName = newName;
            PlayerPrefs.SetString("username", newName);
            NameText.text = newName;
            PhotonVRManager.SetUsername(newName);
            PhotonNetwork.LocalPlayer.NickName = newName;

            // Update PlayFab Display Name if logged in
            if (PlayFabClientAPI.IsClientLoggedIn())
            {
                UpdatePlayFabDisplayName(newName);
            }
        }
    }

    private void UpdatePlayFabDisplayName(string displayName)
    {
        PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest
        {
            DisplayName = displayName
        },
        result => Debug.Log("PlayFab display name updated successfully!"),
        error => Debug.LogError("Failed to update PlayFab display name: " + error.GenerateErrorReport()));
    }
}
