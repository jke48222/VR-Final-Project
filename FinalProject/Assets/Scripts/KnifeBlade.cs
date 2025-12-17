using UnityEngine;

[DisallowMultipleComponent]
public class KnifeBlade : MonoBehaviour
{
    [Header("Audio")]
    [Tooltip("AudioSource used to play the cutting sound.")]
    public AudioSource audioSource;

    [Tooltip("Sound played when a cut successfully occurs.")]
    public AudioClip cutSound;

    private void Awake()
    {
        if (audioSource == null)
        {
            Debug.LogWarning($"[KnifeBlade] No AudioSource assigned on '{name}'. Sound will not play.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Find a cuttable anywhere in the object hierarchy
        CuttableItem cuttable = other.GetComponentInParent<CuttableItem>();
        if (cuttable == null)
        {
            return;
        }

        Debug.Log($"[KnifeBlade] Blade contacted '{cuttable.name}'.");

        bool wasAlreadyCut = cuttable.hasBeenCut;

        // Let the item determine if cutting is allowed
        cuttable.TryCut();

        // If a cut actually happened on THIS collision (not previously)
        if (!wasAlreadyCut && cuttable.hasBeenCut)
        {
            PlayCutSound();
        }
    }

    /// <summary>
    /// Plays the cutting audio if everything is assigned correctly.
    /// Wrapped so we can add pitch randomization or pooling later.
    /// </summary>
    private void PlayCutSound()
    {
        if (audioSource == null || cutSound == null)
        {
            Debug.LogWarning($"[KnifeBlade] Missing audio assignment on '{name}'. Cannot play cut sound.");
            return;
        }

        audioSource.PlayOneShot(cutSound);
        Debug.Log("[KnifeBlade] Played cut sound.");
    }
}
