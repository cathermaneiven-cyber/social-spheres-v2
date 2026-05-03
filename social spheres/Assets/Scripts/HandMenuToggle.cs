using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace SocialSpheres
{
    /// <summary>
    /// Attach to the GorillaPlayer or LeftHand Controller.
    /// Toggles the physical hand menu when the player presses the menu button.
    /// Works with standard XR input (Quest, Index, etc.)
    /// </summary>
    public class HandMenuToggle : MonoBehaviour
    {
        [Header("Menu Reference")]
        [Tooltip("The Menu GameObject (child of LeftHand Controller)")]
        public GameObject menuObject;

        [Header("Input Settings")]
        [Tooltip("Which hand holds the menu")]
        public XRNode menuHand = XRNode.LeftHand;

        [Tooltip("Button that toggles the menu (Primary = X on Quest left controller)")]
        public InputFeatureUsage<bool> toggleButton = CommonUsages.menuButton;

        private bool _prevButtonState = false;
        private bool _menuOpen        = false;

        void Update()
        {
            bool pressed = GetButton(menuHand, toggleButton);

            // Toggle on button down (rising edge only)
            if (pressed && !_prevButtonState)
                ToggleMenu();

            _prevButtonState = pressed;
        }

        void ToggleMenu()
        {
            _menuOpen = !_menuOpen;
            menuObject.SetActive(_menuOpen);
        }

        private bool GetButton(XRNode node, InputFeatureUsage<bool> feature)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevicesAtXRNode(node, devices);
            foreach (var device in devices)
                if (device.TryGetFeatureValue(feature, out bool val) && val) return true;
            return false;
        }
    }
}
