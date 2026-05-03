using UnityEngine;

public class KickButton : MonoBehaviour
{
    [HideInInspector] public int ButtonNumber;
    [SerializeField] public LeaderBoard LB;
    [SerializeField] public string HandTag = "HandTag";

    public void ResetButton()
    {
        gameObject.SetActive(true);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(HandTag))
        {
            LB.KickPress(ButtonNumber);
        }
    }
}
