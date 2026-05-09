using Photon.Pun;
using System;
using UnityEngine;

public class TaggingHand : MonoBehaviour
{
    public float RayCastDistance = 0.05f;
    [NonSerialized] public GameObject LocalPlayer;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, RayCastDistance);
    }

    private void Update()
    {
        if (!PhotonNetwork.InRoom) return;

        foreach (TagPlayerManager manager in FindObjectsOfType<TagPlayerManager>())
        {
            if (manager.GetComponent<PhotonView>().IsMine)
            {
                LocalPlayer = manager.gameObject;
                break;
            }
        }

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, RayCastDistance);

        foreach (Collider collider in hitColliders)
        {
            TagCollider otherCollider = collider.GetComponent<TagCollider>();
            if (otherCollider != null)
            {
                if (!otherCollider.playerManager.Tagged)
                {
                    TagPlayerManager localPlayerManager = LocalPlayer.GetComponent<TagPlayerManager>();

                    if (localPlayerManager.Tagged && !localPlayerManager.InTagFreeze)
                    {
                        PhotonView localPlayerView = LocalPlayer.GetComponent<PhotonView>();
                        PhotonView otherPlayerView = otherCollider.playerManager.GetComponent<PhotonView>();

                        if (!localPlayerManager.IsInfectionActive) localPlayerView.RPC("UnTaggedRPC", localPlayerView.Owner);
                        if (localPlayerManager.IsInfectionActive)
                        {
                            if (otherCollider.playerManager.IsLastPlayer)
                            {
                                localPlayerView.RPC("ResetGameRPC", RpcTarget.MasterClient);
                            }
                        }
                        otherCollider.playerManager.Tagged = true;
                        otherPlayerView.RPC("TaggedRPC", otherPlayerView.Owner, true);
                    }
                }
            }
        }
    }
}
