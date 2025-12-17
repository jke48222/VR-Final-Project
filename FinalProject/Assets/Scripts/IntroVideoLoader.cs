using UnityEngine;
using UnityEngine.Video;

[DisallowMultipleComponent]
[RequireComponent(typeof(VideoPlayer))]
public class IntroVideoLoader : MonoBehaviour
{
    [Header("Status")]
    [Tooltip("True when the intro video has finished playing.")]
    public static bool HasFinished { get; private set; }

    private VideoPlayer videoPlayer;

    private void Awake()
    {
        HasFinished = false;

        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            Debug.LogError("[IntroVideoLoader] No VideoPlayer found. Component is required.");
        }
    }

    private void OnEnable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoEnd;
            Debug.Log("[IntroVideoLoader] Subscribed to video end event.");
        }
    }

    private void Start()
    {
        // Safety: Ensure video begins playing
        if (videoPlayer != null && !videoPlayer.isPlaying)
        {
            videoPlayer.Play();
            Debug.Log("[IntroVideoLoader] Auto-starting intro video.");
        }
    }

    private void OnDisable()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnd;
            Debug.Log("[IntroVideoLoader] Unsubscribed from video end event.");
        }
    }

    private void OnVideoEnd(VideoPlayer source)
    {
        HasFinished = true;
        Debug.Log("[IntroVideoLoader] Intro finished. Marking HasFinished = true.");
    }
}
