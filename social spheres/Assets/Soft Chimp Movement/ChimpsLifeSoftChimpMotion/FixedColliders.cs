using UnityEngine;

public class FixedColliders : MonoBehaviour
{
    [Header("SCRIPT BY VOUIE. DO NOT STEAL")]
    [Header("THIS IS A PUBLIC SCRIPT")]
    [Header("BUT IF YOU USE IT")]
    [Header("GIVE CREDIT")]
    [Header("COPYRIGHTED BY:")]
    [Header("GORILLA ANALYTICS LLC")]
    [Header("PUT THE COLLIDERS IN THIS AREA")]
    public Collider[] colliders;

    void Start()
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            for (int j = i + 1; j < colliders.Length; j++)
            {
                Physics.IgnoreCollision(colliders[i], colliders[j]);
            }
        }
    }

}
