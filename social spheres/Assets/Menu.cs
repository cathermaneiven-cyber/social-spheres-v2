using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using easyInputs;

public class Menu : MonoBehaviour
{
    [Header("This Script Was Made By Pear :) ")]
    public Transform rightHandController;
    public GameObject thing;

    // Update is called once per frame
    private void Update()
    {
        if (EasyInputs.GetPrimaryButtonDown(EasyHand.LeftHand))
        {
            thing.SetActive(true);
        }
        else
        {
            thing.SetActive(false);
        }
    }
}