using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Photon.Pun;

public class XRGrabNetworkInteractable : XRGrabInteractable
{
    private PhotonView photonView;
    private XRBaseInteractor currentInteractor;

    void Start()
    {
        photonView = GetComponent<PhotonView>();
    }

    protected override void OnSelectEntered(XRBaseInteractor interactor)
    {
        if (currentInteractor != null && currentInteractor != interactor)
        {
            return;
        }

        photonView.RequestOwnership();

        currentInteractor = interactor;

        base.OnSelectEntered(interactor);
    }

    protected override void OnSelectExited(XRBaseInteractor interactor)
    {
        if (currentInteractor == interactor)
        {
            currentInteractor = null;
        }

        base.OnSelectExited(interactor);
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        if (currentInteractor != null && !photonView.IsMine)
        {
            interactionManager.SelectExit(currentInteractor, this);
            currentInteractor = null;
        }

        base.ProcessInteractable(updatePhase);
    }
}