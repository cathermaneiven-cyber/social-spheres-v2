using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class MuteButton : MonoBehaviour
{
    [HideInInspector] public int ButtonNumber;
    [SerializeField] private LeaderBoard LB;
    [SerializeField] private string HandTag = "HandTag";
    [SerializeField] private Material MutedMaterial;
    private Material UnMutedMaterial;
    private Renderer rend;

    private bool Muted = false;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        UnMutedMaterial = rend.material;
    }

    public void ResetButton()
    {
        Muted = false;
        if (rend != null)
            rend.material = UnMutedMaterial;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(HandTag))
        {
            LB.MutePress(ButtonNumber);
            Muted = !Muted;
            rend.material = Muted ? MutedMaterial : UnMutedMaterial;
        }
    }
}
