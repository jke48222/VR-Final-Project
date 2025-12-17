using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[DisallowMultipleComponent]
public class RecipeBookManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The world space canvas that shows the recipe book UI.")]
    public Canvas recipeBookCanvas;

    [Tooltip("The HMD or main camera transform.")]
    public Transform headTransform;

    [Tooltip("Component that controls page content (title/body/page index).")]
    public RecipeBookPages recipeBookPages;

    [Header("Placement")]
    [Tooltip("Distance in front of the head when opened.")]
    public float distanceFromHead = 0.75f;

    [Tooltip("Vertical offset from head position (positive is up, negative is down).")]
    public float verticalOffset = -0.15f;

    [Header("Input")]
    [Tooltip("Which hand to read the menu button from.")]
    public XRNode menuButtonHand = XRNode.LeftHand;

    [Tooltip("Which hand to read A/B page buttons from (A=primary, B=secondary).")]
    public XRNode pageButtonHand = XRNode.LeftHand;

    private InputDevice _menuDevice;
    private InputDevice _pageDevice;

    private bool _menuButtonPrev;
    private bool _primaryPrev;   // X button
    private bool _secondaryPrev; // Y button

    private void Awake()
    {
        // Try to locate the canvas if not assigned
        if (recipeBookCanvas == null)
        {
            recipeBookCanvas = GetComponentInChildren<Canvas>(true);
            if (recipeBookCanvas == null)
            {
                Debug.LogWarning("[RecipeBookManager] No Canvas assigned and none found in children.");
            }
        }

        // Ensure the canvas is using world space (this is meant as an in world book)
        if (recipeBookCanvas != null && recipeBookCanvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogWarning($"[RecipeBookManager] Canvas on '{recipeBookCanvas.name}' is not World Space. " +
                             "Switching to World Space for proper VR placement.");
            recipeBookCanvas.renderMode = RenderMode.WorldSpace;
        }

        // Prefer LocalXRTargets if available, otherwise fall back to Camera.main
        if (headTransform == null && LocalXRTargets.IsReady)
        {
            headTransform = LocalXRTargets.Head;
        }

        if (headTransform == null && Camera.main != null)
        {
            headTransform = Camera.main.transform;
        }

        if (headTransform == null)
        {
            Debug.LogWarning("[RecipeBookManager] No headTransform assigned and no Camera.main found.");
        }

        // Auto-find RecipeBookPages if not wired
        if (recipeBookPages == null)
        {
            recipeBookPages = GetComponentInChildren<RecipeBookPages>(true);
            if (recipeBookPages == null)
            {
                Debug.LogWarning("[RecipeBookManager] No RecipeBookPages found under this object.");
            }
        }

        InitializeMenuDevice();
        InitializePageDevice();
    }

    /// <summary>
    /// Attempts to find an XR device for the configured menuButtonHand.
    /// </summary>
    private void InitializeMenuDevice()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(menuButtonHand, devices);

        if (devices.Count > 0)
        {
            _menuDevice = devices[0];
            Debug.Log($"[RecipeBookManager] Using XR device '{_menuDevice.name}' for menu button.");
        }
        else
        {
            Debug.LogWarning("[RecipeBookManager] No XR device found for menu button hand. Will retry.");
        }
    }

    /// <summary>
    /// Attempts to find an XR device for the configured pageButtonHand.
    /// </summary>
    private void InitializePageDevice()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(pageButtonHand, devices);

        if (devices.Count > 0)
        {
            _pageDevice = devices[0];
            Debug.Log($"[RecipeBookManager] Using XR device '{_pageDevice.name}' for page buttons (A/B).");
        }
        else
        {
            Debug.LogWarning("[RecipeBookManager] No XR device found for page button hand. Will retry.");
        }
    }

    private void Update()
    {
        // Keep devices alive
        if (!_menuDevice.isValid)
        {
            InitializeMenuDevice();
        }

        if (!_pageDevice.isValid)
        {
            InitializePageDevice();
        }

        HandleMenuInput();
        HandlePageInput();
    }

    private void HandleMenuInput()
    {
        bool menuPressed = false;
        if (_menuDevice.isValid &&
            _menuDevice.TryGetFeatureValue(CommonUsages.menuButton, out menuPressed))
        {
            // Edge detection. Only toggle when the button is pressed down.
            if (menuPressed && !_menuButtonPrev)
            {
                ToggleRecipeBook();
            }

            _menuButtonPrev = menuPressed;
        }
    }

    private void HandlePageInput()
    {
        // Only flip pages if the book is currently open
        if (recipeBookCanvas == null ||
            !recipeBookCanvas.gameObject.activeSelf ||
            recipeBookPages == null)
        {
            return;
        }

        if (!_pageDevice.isValid)
        {
            return;
        }

        // A button (primaryButton) → next page
        bool primaryPressed = false;
        if (_pageDevice.TryGetFeatureValue(CommonUsages.primaryButton, out primaryPressed))
        {
            if (primaryPressed && !_primaryPrev)
            {
                recipeBookPages.NextPage();
            }

            _primaryPrev = primaryPressed;
        }

        // B button (secondaryButton) → previous page
        bool secondaryPressed = false;
        if (_pageDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryPressed))
        {
            if (secondaryPressed && !_secondaryPrev)
            {
                recipeBookPages.PreviousPage();
            }

            _secondaryPrev = secondaryPressed;
        }
    }

    /// <summary>
    /// Opens or closes the recipe book canvas, and repositions it when opening.
    /// </summary>
    private void ToggleRecipeBook()
    {
        if (recipeBookCanvas == null || headTransform == null)
        {
            Debug.LogWarning("[RecipeBookManager] Cannot toggle recipe book. Missing canvas or headTransform.");
            return;
        }

        bool newState = !recipeBookCanvas.gameObject.activeSelf;
        recipeBookCanvas.gameObject.SetActive(newState);

        if (newState)
        {
            PositionInFrontOfHead();
        }

        Debug.Log($"[RecipeBookManager] Recipe book {(newState ? "opened" : "closed")}.");
    }

    /// <summary>
    /// Positions the recipe book canvas in front of the player's view, facing them.
    /// </summary>
    private void PositionInFrontOfHead()
    {
        if (headTransform == null || recipeBookCanvas == null)
        {
            return;
        }

        // Use a flattened forward direction so the book does not tilt up or down.
        Vector3 forwardProjected = Vector3.ProjectOnPlane(headTransform.forward, Vector3.up).normalized;
        if (forwardProjected.sqrMagnitude < 0.01f)
        {
            forwardProjected = headTransform.forward;
        }

        Vector3 targetPos =
            headTransform.position +
            forwardProjected * distanceFromHead +
            Vector3.up * verticalOffset;

        Transform canvasTransform = recipeBookCanvas.transform;
        canvasTransform.position = targetPos;

        // Make the book face the head while staying upright
        Vector3 lookDir = headTransform.position - targetPos;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.001f)
        {
            lookDir = -forwardProjected;
        }

        // Face the head, then flip 180° so the visible side of the canvas points toward the player
        Quaternion facing = Quaternion.LookRotation(lookDir, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
        canvasTransform.rotation = facing;
    }
}
