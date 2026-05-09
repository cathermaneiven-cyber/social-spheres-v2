using Photon.Pun;
using System;
using UnityEngine;

public class TagCollider : MonoBehaviour
{
    [NonSerialized]
    public TagPlayerManager playerManager;

    private void Start()
    {
        gameObject.SetActive(!GetComponentInParent<PhotonView>().IsMine);
        playerManager = GetComponentInParent<TagPlayerManager>();
    }
}
