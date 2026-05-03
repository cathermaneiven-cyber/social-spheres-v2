using UnityEngine;
using easyInputs;

public class Menu : MonoBehaviour
{
    public GameObject thing;
    public GameObject menuCanvas;

    private bool isOpen;

    private void Update()
    {
        if (EasyInputs.GetPrimaryButtonDown(EasyHand.LeftHand))
        {
            isOpen = !isOpen;

            thing.SetActive(isOpen);

            if (menuCanvas != null)
                menuCanvas.SetActive(isOpen);
        }
    }
}