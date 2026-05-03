using UnityEngine;
using Photon.Pun;

public class ReportButton : MonoBehaviour
{
    [HideInInspector] public int ButtonNumber;
    [SerializeField] private LeaderBoard LB;
    [SerializeField] private string HandTag = "HandTag";
    [SerializeField] private Material PressedMaterial;
    private Material UnPressedMaterial;
    private Renderer rend;

    private bool reported = false;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        UnPressedMaterial = rend.material;
    }

    public void ResetButton()
    {
        reported = false;
        if (rend != null)
            rend.material = UnPressedMaterial;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(HandTag) && !reported)
        {
            LB.Report(ButtonNumber);
            reported = true;
            rend.material = PressedMaterial;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(HandTag))
        {
            rend.material = UnPressedMaterial;
        }
    }
}
