using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class BowlWhisk : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Which hand holds the whisk.")]
    public XRNode hand = XRNode.RightHand;

    [Tooltip("Button used to trigger mixing while the whisk is in the bowl.")]
    public InputFeatureUsage<bool> mixButton = CommonUsages.primaryButton;

    [Header("Debug")]
    [Tooltip("The bowl the whisk is currently inside of (for mixing).")]
    public BowlRecipeCombiner currentBowl;

    private InputDevice _device;
    private bool _prevButtonState;
    private bool _triedInitializeOnce;

    private void Awake()
    {
        InitializeDevice();
    }

    /// <summary>
    /// Attempts to bind the whisk to the XR input device for the configured hand.
    /// </summary>
    private void InitializeDevice()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(hand, devices);

        if (devices.Count > 0)
        {
            _device = devices[0];
            _triedInitializeOnce = true;
            Debug.Log($"[BowlWhisk] Using XR device '{_device.name}' for hand '{hand}'.");
        }
        else
        {
            _device = default;
            if (!_triedInitializeOnce)
            {
                Debug.LogWarning($"[BowlWhisk] No XR device found for hand '{hand}'. Will retry in Update.");
                _triedInitializeOnce = true;
            }
        }
    }

    private void Update()
    {
        // Ensure we have a valid device
        if (!_device.isValid)
        {
            InitializeDevice();
        }

        if (currentBowl == null)
        {
            // Whisk is not inside any bowl so do not process input
            return;
        }

        bool pressed = false;
        if (_device.isValid && _device.TryGetFeatureValue(mixButton, out pressed))
        {
            // Button just pressed while whisk is currently inside a bowl
            if (pressed && !_prevButtonState)
            {
                Debug.Log("[BowlWhisk] Mix button pressed. Triggering bowl mix.");
                currentBowl.Mix();
            }

            _prevButtonState = pressed;
        }
        else if (!_device.isValid && currentBowl != null)
        {
            // Device invalid while we are in a bowl
            Debug.LogWarning("[BowlWhisk] XR device is invalid while in bowl. Mix input will not be read.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var bowl = other.GetComponentInParent<BowlRecipeCombiner>();
        if (bowl != null)
        {
            currentBowl = bowl;
            Debug.Log($"[BowlWhisk] Whisk entered bowl '{bowl.name}'.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var bowl = other.GetComponentInParent<BowlRecipeCombiner>();
        if (bowl != null && currentBowl == bowl)
        {
            Debug.Log($"[BowlWhisk] Whisk left bowl '{bowl.name}'. Clearing currentBowl reference.");
            currentBowl = null;
        }
    }
}
