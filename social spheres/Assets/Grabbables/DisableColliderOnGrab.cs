using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DisableColliderOnGrab : MonoBehaviour
{
    public Collider colliderToDisable;
    public XRGrabInteractable grabInteractable;

    private void Start()
    {
        // Ensure both the collider and grabInteractable are assigned
        if (colliderToDisable == null || grabInteractable == null)
        {
            Debug.LogError("Collider or XRGrabInteractable not assigned in DisableColliderOnGrab script.");
            return;
        }

        // Subscribe to the OnFirstHoverEntered event
        grabInteractable.onSelectEntered.AddListener(OnGrab);
        grabInteractable.onSelectExited.AddListener(OnRelease);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to avoid memory leaks
        grabInteractable.onSelectEntered.RemoveListener(OnGrab);
        grabInteractable.onSelectExited.RemoveListener(OnRelease);
    }

    private void OnGrab(XRBaseInteractor interactor)
    {
        // Disable the collider when the object is grabbed
        colliderToDisable.enabled = false;
    }

    private void OnRelease(XRBaseInteractor interactor)
    {
        // Enable the collider when the object is released
        colliderToDisable.enabled = true;
    }
}