using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace SocialSpheres
{
    public class HandMenuToggle : MonoBehaviour
    {
        public GameObject menuObject;
        public GameObject menuCanvas;
        public XRNode menuHand = XRNode.LeftHand;
        public InputFeatureUsage<bool> toggleButton = CommonUsages.primaryButton;

        private bool prevButtonState;
        private bool menuOpen;

        void Start()
        {
            SetMenu(false);
        }

        void Update()
        {
            bool pressed = GetButton(menuHand, toggleButton);

            if (pressed && !prevButtonState)
            {
                menuOpen = !menuOpen;
                SetMenu(menuOpen);
            }

            prevButtonState = pressed;
        }

        void SetMenu(bool state)
        {
            if (menuObject != null)
                menuObject.SetActive(state);

            if (menuCanvas != null)
                menuCanvas.SetActive(state);
        }

        bool GetButton(XRNode node, InputFeatureUsage<bool> feature)
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
}