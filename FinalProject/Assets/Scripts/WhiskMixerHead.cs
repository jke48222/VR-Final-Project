using UnityEngine;

/// <summary>
/// Simple mixer head that triggers a bowl mix when it enters a bowl collider,
/// with a cooldown to prevent rapid re-triggering.
/// Attach this to the whisk head and give it a trigger collider.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class WhiskMixerHead : MonoBehaviour
{
    [Header("Mix Settings")]
    [Tooltip("Minimum time (in seconds) between consecutive mix triggers.")]
    public float mixCooldown = 0.5f;

    [Header("Audio")]
    [Tooltip("AudioSource used to play the mix sound.")]
    public AudioSource audioSource;

    [Tooltip("Sound played when a mix is triggered.")]
    public AudioClip mixSound;

    private float _lastMixTime = -999f;

    private void Awake()
    {
        // Ensure our collider is configured as a trigger
        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            Debug.LogWarning("[WhiskMixerHead] Collider was not a trigger. Setting isTrigger = true.");
            col.isTrigger = true;
        }

        if (audioSource == null && mixSound != null)
        {
            Debug.LogWarning("[WhiskMixerHead] mixSound is assigned but audioSource is null. No sound will play.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[WhiskMixerHead] Trigger enter with: {other.name}");

        // Find a BowlRecipeCombiner on this collider or its parents
        BowlRecipeCombiner bowl = other.GetComponentInParent<BowlRecipeCombiner>();
        if (bowl == null)
        {
            Debug.Log("[WhiskMixerHead] No BowlRecipeCombiner found in parents of collider. Ignoring trigger.");
            return;
        }

        // Enforce cooldown to avoid spamming Mix() calls
        float timeSinceLast = Time.time - _lastMixTime;
        if (timeSinceLast < mixCooldown)
        {
            Debug.Log($"[WhiskMixerHead] Mix on cooldown ({timeSinceLast:F2}s < {mixCooldown:F2}s). Ignoring.");
            return;
        }

        _lastMixTime = Time.time;

        Debug.Log($"[WhiskMixerHead] Calling Mix() on bowl '{bowl.name}'.");
        bowl.Mix();

        // Play feedback sound
        if (audioSource != null && mixSound != null)
        {
            audioSource.PlayOneShot(mixSound);
        }
    }
}
