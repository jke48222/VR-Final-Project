using UnityEngine;

public enum CookState
{
    Raw,
    Cooking,
    Done,
    Burnt
}

public class CookableItem : MonoBehaviour
{
    [Header("Cooking Progress")]
    [Range(0f, 1f)]
    [Tooltip("0 means completely raw, 1 means fully cooked.")]
    public float cookProgress;   // 0 to 1

    [Range(0f, 1f)]
    [Tooltip("0 means not burnt, 1 means fully burnt.")]
    public float burnProgress;   // 0 to 1

    [Tooltip("Current cooking state of this item.")]
    public CookState state = CookState.Raw;

    [Header("Rates")]
    [Tooltip("How fast this item cooks per second when heated. Heat value is expected to be scaled by deltaTime.")]
    public float cookRate = 0.4f;

    [Tooltip("How fast this item burns per second once done. Heat value is expected to be scaled by deltaTime.")]
    public float burnRate = 0.3f;

    [Tooltip("If true, the item can transition from Done to Burnt when overheated.")]
    public bool canBurn = true;

    [Header("Visual Feedback")]
    [Tooltip("Renderer that will have its color updated to show cooking state.")]
    public Renderer targetRenderer;

    [Tooltip("Color when the item is completely raw.")]
    public Color rawColor = Color.white;

    [Tooltip("Color used while the item is cooking. Blended based on cookProgress.")]
    public Color cookingColor = new Color(1f, 0.8f, 0.5f);

    [Tooltip("Color when the item is fully cooked and ready.")]
    public Color doneColor = new Color(1f, 0.6f, 0.3f);

    [Tooltip("Color when the item is completely burnt.")]
    public Color burntColor = new Color(0.2f, 0.1f, 0.05f);

    [Tooltip("If true, emission color will be applied based on the current surface color.")]
    public bool useEmission = true;

    [Tooltip("Multiplier applied to the emission color intensity.")]
    public float emissionIntensity = 1.5f;

    [Header("Audio")]
    [Tooltip("Audio source used for cooking and burning sounds.")]
    public AudioSource audioSource;

    [Tooltip("One shot sound played when cooking begins.")]
    public AudioClip sizzleStart;

    [Tooltip("Looping sound played while the item remains in the Cooking state.")]
    public AudioClip sizzleLoop;

    [Tooltip("Sound played when the item transitions to Burnt.")]
    public AudioClip burntSound;

    private bool sizzleLoopPlaying;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer == null)
            {
                Debug.LogWarning($"[CookableItem] No Renderer found on '{name}' or its children. Visual feedback will not work.");
            }
            else
            {
                Debug.Log($"[CookableItem] Auto assigned Renderer for '{name}'.");
            }
        }

        Debug.Log($"[CookableItem] Awake on '{name}'. Initial state={state}, cook={cookProgress:F2}, burn={burnProgress:F2}");
        UpdateVisuals();
    }

    private void Reset()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
            if (targetRenderer != null)
            {
                Debug.Log($"[CookableItem] Reset auto assigned Renderer for '{name}'.");
            }
        }
    }

    /// <summary>
    /// Called from HeatSource to apply heat in this frame.
    /// heatAmount is expected to already be multiplied by Time.deltaTime.
    /// </summary>
    public void ApplyHeat(float heatAmount)
    {
        if (heatAmount <= 0f)
        {
            // No heat this frame.
            return;
        }

        // First stage: cook up to Done.
        if (state == CookState.Raw || state == CookState.Cooking)
        {
            float before = cookProgress;
            cookProgress += heatAmount * Mathf.Max(0f, cookRate);
            cookProgress = Mathf.Clamp01(cookProgress);

            if (cookProgress >= 0.01f && state == CookState.Raw)
            {
                SetState(CookState.Cooking);
            }

            if (cookProgress >= 1f)
            {
                cookProgress = 1f;
                SetState(CookState.Done);
            }

            // Optional debug when value changes noticeably.
            if (Mathf.Abs(cookProgress - before) > 0.01f)
            {
                Debug.Log($"[CookableItem] '{name}' ApplyHeat - cookProgress updated from {before:F2} to {cookProgress:F2}");
            }
        }
        // Second stage: burning after done.
        else if (state == CookState.Done && canBurn)
        {
            float beforeBurn = burnProgress;
            burnProgress += heatAmount * Mathf.Max(0f, burnRate);
            burnProgress = Mathf.Clamp01(burnProgress);

            if (burnProgress >= 1f)
            {
                burnProgress = 1f;
                SetState(CookState.Burnt);
            }

            if (Mathf.Abs(burnProgress - beforeBurn) > 0.01f)
            {
                Debug.Log($"[CookableItem] '{name}' ApplyHeat - burnProgress updated from {beforeBurn:F2} to {burnProgress:F2}");
            }
        }
        else if (state == CookState.Burnt)
        {
            // Already burnt. No further state changes, but leaving this log commented to avoid spam.
            // Debug.Log($"[CookableItem] '{name}' is already burnt. Additional heat has no effect.");
        }
        else if (state == CookState.Done && !canBurn)
        {
            // Done but not allowed to burn.
            // Debug.Log($"[CookableItem] '{name}' is Done and cannot burn. Additional heat has no effect.");
        }
    }

    /// <summary>
    /// Updates the cooking state, visuals, and audio in a single call.
    /// </summary>
    private void SetState(CookState newState)
    {
        if (state == newState)
            return;

        CookState oldState = state;
        state = newState;

        UpdateVisuals();
        HandleAudioForState();

        Debug.Log($"[CookableItem] '{name}' state changed from {oldState} to {state}. cook={cookProgress:F2}, burn={burnProgress:F2}");
    }

    /// <summary>
    /// Updates the material color and emission based on the current state and progress.
    /// </summary>
    private void UpdateVisuals()
    {
        if (targetRenderer == null)
            return;

        // Use sharedMaterial in edit mode to avoid leaking materials.
        // Use material in play mode to allow per instance runtime changes.
        Material mat = Application.isPlaying
            ? targetRenderer.material
            : targetRenderer.sharedMaterial;

        if (mat == null)
        {
            Debug.LogWarning($"[CookableItem] '{name}' has a Renderer without a valid material.");
            return;
        }

        Color targetColor;

        switch (state)
        {
            case CookState.Raw:
                targetColor = rawColor;
                break;

            case CookState.Cooking:
                targetColor = Color.Lerp(rawColor, cookingColor, cookProgress);
                break;

            case CookState.Done:
                targetColor = doneColor;
                break;

            case CookState.Burnt:
                targetColor = burntColor;
                break;

            default:
                targetColor = rawColor;
                break;
        }

        mat.color = targetColor;

        if (useEmission)
        {
            Color emissionColor = targetColor * emissionIntensity;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
        }
    }

    /// <summary>
    /// Handles audio playback for state transitions.
    /// </summary>
    private void HandleAudioForState()
    {
        if (audioSource == null)
            return;

        if (state == CookState.Cooking)
        {
            if (sizzleStart != null)
            {
                audioSource.PlayOneShot(sizzleStart);
            }

            if (!sizzleLoopPlaying && sizzleLoop != null)
            {
                audioSource.clip = sizzleLoop;
                audioSource.loop = true;
                audioSource.Play();
                sizzleLoopPlaying = true;
            }
        }
        else
        {
            if (sizzleLoopPlaying)
            {
                audioSource.Stop();
                sizzleLoopPlaying = false;
            }

            if (state == CookState.Burnt && burntSound != null)
            {
                audioSource.PlayOneShot(burntSound);
            }
        }
    }

    private void OnValidate()
    {
        // Clamp rates to non negative to avoid confusing behavior.
        if (cookRate < 0f)
        {
            cookRate = 0f;
        }

        if (burnRate < 0f)
        {
            burnRate = 0f;
        }

        // In the editor, keep visuals in sync without leaking materials.
        if (!Application.isPlaying)
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            UpdateVisuals();
        }
    }
}
