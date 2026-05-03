using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;
using System.Collections.Generic;

public class HandMenuRayInteractor : MonoBehaviour
{
    public XRNode hand = XRNode.RightHand;
    public InputFeatureUsage<bool> clickButton = CommonUsages.triggerButton;
    public float rayDistance = 5f;
    public LayerMask buttonLayers = ~0;
    public LineRenderer lineRenderer;
    public GameObject menuObject;
    public bool debugLogs = true;

    private bool lastPressed;
    private Button hoveredButton;

    private void Update()
    {
        if (menuObject == null || !menuObject.activeInHierarchy)
        {
            if (lineRenderer != null)
                lineRenderer.enabled = false;

            return;
        }

        if (lineRenderer != null)
            lineRenderer.enabled = true;

        Ray ray = new Ray(transform.position, transform.forward);

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, ray.origin);
            lineRenderer.SetPosition(1, ray.origin + ray.direction * rayDistance);
        }

        hoveredButton = null;

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, buttonLayers, QueryTriggerInteraction.Collide))
        {
            hoveredButton = hit.collider.GetComponent<Button>();

            if (hoveredButton == null)
                hoveredButton = hit.collider.GetComponentInParent<Button>();

            if (lineRenderer != null)
                lineRenderer.SetPosition(1, hit.point);

            if (debugLogs && hoveredButton != null)
                Debug.DrawRay(ray.origin, ray.direction * hit.distance, Color.green);
        }

        bool pressed = GetButton(hand, clickButton);

        if (pressed && !lastPressed)
        {
            if (hoveredButton != null && hoveredButton.interactable)
            {
                if (debugLogs)
                    Debug.Log("[RayInteractor] Clicked: " + hoveredButton.name);

                hoveredButton.onClick.Invoke();
            }
        }

        lastPressed = pressed;
    }

    private bool GetButton(XRNode node, InputFeatureUsage<bool> feature)
    {
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(node, devices);

        foreach (InputDevice device in devices)
        {
            if (device.TryGetFeatureValue(feature, out bool value) && value)
                return true;
        }

        return false;
    }
}