using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine;
using System.Linq;


public class TagPlayerManager : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Renderers To Change Material")]
    public Renderer[] PlayerParts;

    [Header("Materials")]
    [Tooltip("Your Tagged Materials, If You Have One Single Object For Your Player, Set All The Materials On Your Player Object Here, And Switch Out Your Fur Mat With A Lava Mat")]
    public Material[] TaggedMats;
    Material[] UnTaggedMats;

    [Header("The Tag Freeze CoolDown")]
    [Tooltip("When You Tag Someone, The Newly Tagged Player Will Be Frozen For This Amount Of Seconds")]
    public float FreezeTime = 3;

    [Header("If You Want To Have Particles Or Not, if Not Un-Check This Bool And Leave The Particle Stuff Blank")]
    public bool HasParticles = true;
    [Header("Tagged Particles")]
    [Tooltip("Your Tagged Particles, Preferibly Under Head")]
    public GameObject[] TaggedParticles;

    [Header("Tag Sound Stuff")]
    [Tooltip("The Sound That Plays When You Tag Somebody")]
    public AudioClip TagSound;
    [Tooltip("The Audio Source Of The Player, Make An Empty Under Hand, Add a AudioSource, and Put It In Here")]
    public AudioSource PlayerAudio;
    [Header("Infection Beta Stuff")]
    public bool HasInfection = true;
    public int PeopleNeededToActivate = 4;
    public Material[] InfectionMaterials;
    public AudioClip ResetGameSound;
    public float ResetGameDelay;
    [HideInInspector]
    public bool IsInfectionActive = false;

    // Private Values
    [NonSerialized]
    public bool InTagFreeze;
    PhotonView MyView;
    [NonSerialized]
    public bool IsPlaying = true;
    int TaggedPlayers;
    [NonSerialized]
    public bool Tagged;
    int PlayersinRoom;
    int PlayerCheck;
    GorillaLocomotion.Player GorillaPlayer;
    private bool IsResetting = false;
    public bool IsLastPlayer;

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        if (IsPlaying)
        {
            StartCoroutine(TagMaster());
        }
    }

    [PunRPC]
    public void TagThisPlayer()
    {
        foreach (TagPlayerManager Tag in FindObjectsOfType<TagPlayerManager>())
        {
            Tag.photonView.RPC("UnTaggedRPC", RpcTarget.All);

            photonView.RPC("TaggedRPC", RpcTarget.All, true);
        }
    }

    public IEnumerator TagMaster()
    {
        yield return new WaitForSeconds(1);

        if (TaggedPlayers == 0)
        {
            foreach (TagPlayerManager Managers in FindObjectsOfType<TagPlayerManager>())
            {
                if (Managers.GetComponent<PhotonView>().Owner.IsMasterClient)
                {
                    Managers.TaggedRPC(true);
                }
            }
        }
    }

    [PunRPC]
    public void TaggedRPC(bool HasTagFreeze)
    {
        if (IsPlaying)
        {
            Tagged = true;
            MyView.RPC("TagSoundRPC", RpcTarget.All);
            if (HasTagFreeze) StartCoroutine(TagFreeze());
        }
    }

    [PunRPC]
    public void TagSoundRPC()
    {
        PlayerAudio.clip = TagSound;
        PlayerAudio.Play();
    }

    [PunRPC]
    public void UnTaggedRPC()
    {
        if (IsPlaying)
        {
            Tagged = false;
        }
    }

    private IEnumerator TagFreeze()
    {
        InTagFreeze = true;
        yield return new WaitForSeconds(FreezeTime);
        InTagFreeze = false;
    }

    private void Start()
    {
        PlayerCheck = PhotonNetwork.CurrentRoom.PlayerCount;
        GorillaPlayer = FindObjectOfType<GorillaLocomotion.Player>();
        MyView = GetComponent<PhotonView>();

        foreach (Renderer Ren in PlayerParts)
        {
            UnTaggedMats = Ren.materials;
        }
    }


    private void Update()
    {
        PlayerHandler();
    }

    void PlayerHandler()
    {
        if (IsPlaying)
        {
            UpdateInfectionState();
            HandleParticles();
            UpdatePlayerCount();
            UpdatePlayerMaterials();

            if (PhotonNetwork.CurrentRoom.PlayerCount == 1 && !Tagged)
                TaggedRPC(false);
            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount - 1;
            if (TaggedPlayers == playerCount && !Tagged)
            {
                IsLastPlayer = true;
            }
            else
            {
                IsLastPlayer = false;
            }

            GorillaPlayer.disableMovement = InTagFreeze;
        }
        else
        {
            Tagged = false;
            IsLastPlayer = false;
            ResetPlayerState();
        }
    }

    void HandleParticles()
    {
        foreach (GameObject particleObj in TaggedParticles)
            particleObj.SetActive(HasParticles ? Tagged : false);
    }

    void UpdatePlayerCount()
    {
        PlayersinRoom = PhotonNetwork.CurrentRoom.PlayerCount;
        if (PlayerCheck != PlayersinRoom)
        {
            PlayerCheck = PlayersinRoom > PlayerCheck ? PlayerCheck : PlayersinRoom;
        }
    }

    void UpdatePlayerMaterials()
    {
        foreach (Renderer r in PlayerParts)
        {
            if (IsInfectionActive)
            {
                r.materials = Tagged ? InfectionMaterials : UnTaggedMats;
            }
            else
            {
                r.materials = Tagged ? TaggedMats : UnTaggedMats;
            }

            foreach (Material Mat in TaggedMats)
                Mat.color = Color.white;
        }
    }

    void UpdateInfectionState()
    {
        TaggedPlayers = FindObjectsOfType<TagPlayerManager>().Count(manager => manager.Tagged);
        if (PlayersinRoom >= PeopleNeededToActivate && HasInfection)
        {
            IsInfectionActive = true;
        }
        else
        {
            IsInfectionActive = false;
        }
        /*
        if (IsInfectionActive)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (TaggedPlayers == PlayersinRoom && !IsResetting) ResetGame();
            }
        }
        */
    }

    void ResetPlayerState()
    {
        foreach (Renderer renderer in PlayerParts)
            renderer.materials = UnTaggedMats;

        foreach (GameObject particleObj in TaggedParticles)
            particleObj.SetActive(false);
    }

    [PunRPC]
    void ResetGameRPC()
    {
        if (IsResetting == false)
        {
            IsResetting = true;
            StartCoroutine(WaitSeconds(ResetGameDelay));
        }
    }

    IEnumerator WaitSeconds(float Time)
    {
        yield return new WaitForSeconds(Time);
        PlayerAudio.clip = ResetGameSound;
        PlayerAudio.Play();
        UntagAll();
        while (TaggedPlayers != 0) yield return null;
        yield return new WaitForSeconds(1f);
        ChooseRandomToTag();
    }


    void UntagAll()
    {
        TagPlayerManager[] managers = FindObjectsOfType<TagPlayerManager>();
        for (int i = 0; i < managers.Length; i++)
        {
            PhotonView View = managers[i].photonView;
            View.RPC("UnTaggedRPC", View.Owner);
        }
    }

    void ChooseRandomToTag()
    {
        if (TaggedPlayers == 0)
        {
            Debug.Log("Choosing Person");
            int RandomPlayer = UnityEngine.Random.Range(0, PlayersinRoom);
            TagPlayerManager[] Managers = FindObjectsOfType<TagPlayerManager>();
            PhotonView TheChosenOne = Managers[RandomPlayer].photonView;
            Managers[RandomPlayer].Tagged = true;
            TheChosenOne.RPC("TaggedRPC", TheChosenOne.Owner, true);
            IsResetting = false;
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(Tagged);
            stream.SendNext(IsLastPlayer);
        }
        else
        {
            bool receivedTagged = (bool)stream.ReceiveNext();
            Tagged = receivedTagged;
            bool receivedPlayerStatus = (bool)stream.ReceiveNext();
            IsLastPlayer = receivedPlayerStatus;
        }
    }
}