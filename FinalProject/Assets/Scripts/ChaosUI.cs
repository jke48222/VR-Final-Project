using System.Collections;
using TMPro;
using UnityEngine;

public class ChaosUI : MonoBehaviour
{
    [Header("Chaos Text")]
    [Tooltip("Text element that shows the current chaos event name.")]
    public TMP_Text chaosText;

    [Tooltip("How long the text stays fully visible before fading out.")]
    public float visibleSeconds = 2f;

    [Tooltip("Fade out duration in seconds.")]
    public float fadeSeconds = 1f;

    [Header("Audio")]
    public AudioClip chaosClip;
    public float clipVolume = 1f;

    private Coroutine _routine;

    /// <summary>
    /// Called by ChaosManager when a chaos event starts.
    /// </summary>
    public void ShowChaosBanner(string text)
    {
        if (chaosText == null)
        {
            Debug.LogWarning("[ChaosUI] chaosText is not assigned.");
            return;
        }

        // Make sure it is enabled and fully visible
        chaosText.gameObject.SetActive(true);
        chaosText.text = text;

        Color c = chaosText.color;
        c.a = 1f;
        chaosText.color = c;

        // Optional one shot sound
        if (chaosClip != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(chaosClip, Camera.main.transform.position, clipVolume);
        }

        // Restart fade coroutine
        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _routine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        // Hold full opacity for a bit
        yield return new WaitForSeconds(visibleSeconds);

        float t = 0f;
        Color start = chaosText.color;

        while (t < fadeSeconds)
        {
            float a = Mathf.Lerp(1f, 0f, t / fadeSeconds);
            chaosText.color = new Color(start.r, start.g, start.b, a);

            t += Time.deltaTime;
            yield return null;
        }

        chaosText.gameObject.SetActive(false);
        _routine = null;
    }
}
