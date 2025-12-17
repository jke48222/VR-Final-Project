using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

[DisallowMultipleComponent]
public class RoundManager : MonoBehaviour
{
    [Header("Round Settings")]
    [Tooltip("Length of the round in seconds.")]
    public float roundDurationSeconds = 120f;   // 2 minutes

    [Tooltip("Automatically start the round on Start().")]
    public bool autoStart = true;

    [Header("Themes")]
    [Tooltip("List of possible round themes. A random one is chosen at the start of each round. If empty, sensible defaults are used.")]
    public string[] themeNames;

    [Header("Judging Timeline")]
    [Tooltip("Seconds to show each player's breakdown during judging (local fallback).")]
    public float perPlayerRevealSeconds = 10f;

    [Tooltip("Seconds to show the winner at the end (local fallback).")]
    public float winnerRevealSeconds = 5f;

    [Header("Players and Plates")]
    [Tooltip("Plates that will be judged at the end of the round. Index is player number.")]
    public Plate[] playerPlates;

    [Header("RoundCanvas UI")]
    [Tooltip("TextMeshProUGUI used for the theme at the top of the RoundCanvas.")]
    public TextMeshProUGUI themeText;

    [Tooltip("TextMeshProUGUI used for the timer on the RoundCanvas.")]
    public TextMeshProUGUI timerText;

    [Tooltip("TextMeshProUGUI used for chaos event announcements on the RoundCanvas.")]
    public TextMeshProUGUI chaosText;

    [Tooltip("TextMeshProUGUI used for results and winner text on the RoundCanvas.")]
    public TextMeshProUGUI resultText;

    [Header("Chaos")]
    [Tooltip("ChaosManager that will run events during the active round.")]
    public ChaosManager chaosManager;

    [Header("AI Judge")]
    [Tooltip("Optional AI judge controller. If assigned, AI will handle commentary and winner display.")]
    public AIDishJudgeController aiJudgeController;

    [Header("TTS")]
    [Tooltip("Optional TTS manager that will speak themes and results during the local fallback judging.")]
    public TTSManager ttsManager;

    [Header("Events")]
    [Tooltip("Invoked when the round starts.")]
    public UnityEvent onRoundStarted;

    [Tooltip("Invoked when the round ends (before judging completes).")]
    public UnityEvent onRoundEnded;

    private float _timeRemaining;
    private bool _roundActive;
    private bool _sequenceRunning;
    private Coroutine _roundRoutine;

    /// <summary>
    /// True if the round is currently active and the timer is counting down.
    /// </summary>
    public bool IsRoundActive => _roundActive;

    private void Awake()
    {
        if (roundDurationSeconds <= 0f)
        {
            Debug.LogWarning($"[RoundManager] roundDurationSeconds on '{name}' was <= 0. Clamping to 10 seconds.");
            roundDurationSeconds = 10f;
        }

        if (perPlayerRevealSeconds <= 0f)
        {
            perPlayerRevealSeconds = 5f;
        }

        if (winnerRevealSeconds <= 0f)
        {
            winnerRevealSeconds = 3f;
        }

        if (playerPlates == null || playerPlates.Length == 0)
        {
            Debug.LogWarning($"[RoundManager] No player plates assigned on '{name}'. Judging will do nothing.");
        }

        if (timerText == null)
        {
            Debug.LogWarning($"[RoundManager] timerText is not assigned on '{name}'. Timer UI will not update.");
        }

        if (resultText == null)
        {
            Debug.LogWarning($"[RoundManager] resultText is not assigned on '{name}'. Results will not show in UI.");
        }

        if (themeText == null)
        {
            Debug.LogWarning($"[RoundManager] themeText is not assigned on '{name}'. Theme UI will not update.");
        }

        if (chaosText == null)
        {
            Debug.LogWarning($"[RoundManager] chaosText is not assigned on '{name}'. Chaos UI will not update.");
        }

        EnsureDefaultThemes();
    }

    private void Start()
    {
        ClearResultUI();
        ClearChaosUI();

        _timeRemaining = Mathf.Max(1f, roundDurationSeconds);
        UpdateTimerUI();

        if (autoStart)
        {
            StartRound();
        }
    }

    /// <summary>
    /// Starts the full round sequence:
    /// 1) Pick a random theme and display it.
    /// 2) Countdown with chaos.
    /// 3) End round.
    /// 4) Judge dishes (AI or local).
    /// </summary>
    public void StartRound()
    {
        if (_sequenceRunning)
        {
            Debug.LogWarning("[RoundManager] StartRound called while a sequence is already running.");
            return;
        }

        if (_roundRoutine != null)
        {
            StopCoroutine(_roundRoutine);
        }
    
        _roundRoutine = StartCoroutine(RoundSequence());
    }

    /// <summary>
    /// Early end of the round. Will make the timer hit zero and jump into judging.
    /// </summary>
    public void EndRoundEarly()
    {
        if (!_roundActive || !_sequenceRunning)
        {
            return;
        }

        _timeRemaining = 0f;
    }

    private IEnumerator RoundSequence()
    {
        _sequenceRunning = true;

        // Pick theme for this round
        string themeForThisRound = PickRandomTheme();
        UpdateThemeUI(themeForThisRound);
        SpeakLine($"Tonight's theme is {themeForThisRound}.");

        // Phase 1: active round
        _timeRemaining = Mathf.Max(1f, roundDurationSeconds);
        _roundActive = true;

        ClearResultUI();
        ClearChaosUI();
        UpdateTimerUI();

        if (chaosManager != null)
        {
            chaosManager.BeginChaosPhase();
        }

        onRoundStarted?.Invoke();

        while (_timeRemaining > 0f)
        {
            _timeRemaining -= Time.deltaTime;
            if (_timeRemaining < 0f)
            {
                _timeRemaining = 0f;
            }

            UpdateTimerUI();
            yield return null;
        }

        _roundActive = false;

        if (chaosManager != null)
        {
            chaosManager.EndChaosPhase();
        }

        onRoundEnded?.Invoke();

        // Phase 2: judging and results (AI or local)
        yield return RunJudgingAndResults();

        _sequenceRunning = false;
    }

    /// <summary>
    /// Uses AI judge if available, otherwise falls back to local scoring and scene reload.
    /// </summary>
    private IEnumerator RunJudgingAndResults()
    {
        if (aiJudgeController != null)
        {
            Debug.Log("[RoundManager] Delegating judging to AI judge controller.");
            yield return aiJudgeController.RunJudgingSequence(playerPlates);
            // AIDishJudgeController handles scene reload if reloadSceneAfterJudging is true.
        }
        else
        {
            Debug.Log("[RoundManager] No AI judge configured. Using local results sequence.");
            yield return ShowResultsSequence();
            ReloadCurrentScene();
        }
    }

    /// <summary>
    /// Local judging fallback:
    /// Scores plates, then shows each player result for N seconds, then the winner.
    /// Also drives TTS if a TTSManager is assigned.
    /// </summary>
    private IEnumerator ShowResultsSequence()
    {
        if (playerPlates == null || playerPlates.Length == 0)
        {
            Debug.LogWarning("[RoundManager] ShowResultsSequence called but no plates assigned.");
            SpeakLine("No valid dishes were plated this round.");
            yield break;
        }

        float bestScore = float.MinValue;
        int bestIndex = -1;

        // Precompute all results so they are stable
        var breakdowns = new string[playerPlates.Length];
        var scores = new float[playerPlates.Length];

        for (int i = 0; i < playerPlates.Length; i++)
        {
            Plate plate = playerPlates[i];
            if (plate == null)
            {
                breakdowns[i] = $"Player {i + 1}\nNo plate assigned.\nScore: 0";
                scores[i] = 0f;
                continue;
            }

            var scoreResult = DishScorer.ScorePlate(plate);

            scores[i] = scoreResult.score;
            breakdowns[i] =
                $"Player {i + 1}\n" +
                $"{scoreResult.breakdown}";

            if (scores[i] > bestScore)
            {
                bestScore = scores[i];
                bestIndex = i;
            }
        }

        // Log full summary once
        var summary = new System.Text.StringBuilder();
        summary.AppendLine("Round Results:");
        for (int i = 0; i < breakdowns.Length; i++)
        {
            summary.AppendLine();
            summary.AppendLine(breakdowns[i]);
        }

        if (bestIndex >= 0)
        {
            summary.AppendLine();
            summary.AppendLine($"Winner: Player {bestIndex + 1} with {bestScore:F1} points!");
        }
        else
        {
            summary.AppendLine();
            summary.AppendLine("No winner. No valid dishes were plated.");
        }

        Debug.Log(summary.ToString());

        // Sequential UI reveal on resultText + TTS per player
        for (int i = 0; i < breakdowns.Length; i++)
        {
            if (resultText != null)
            {
                resultText.text = breakdowns[i];
            }

            SpeakLine(breakdowns[i]);
            yield return new WaitForSeconds(perPlayerRevealSeconds);
        }

        // Winner screen
        if (resultText != null)
        {
            if (bestIndex >= 0)
            {
                string winnerLine =
                    $"Winner\n\nPlayer {bestIndex + 1} wins with {bestScore:F1} points!";
                resultText.text = winnerLine;
                SpeakLine($"Player {bestIndex + 1} wins with a score of {bestScore:F1}.");
                ttsManager.SynthesizeAndPlay($"Player {bestIndex + 1} wins with a score of {bestScore:F1}.");
            }
            else
            {
                string noWinnerLine = "No winner this round.\nNo valid dishes were plated.";
                resultText.text = noWinnerLine;
                SpeakLine("No winner this round. No valid dishes were plated.");
            }
        }

        yield return new WaitForSeconds(winnerRevealSeconds);
    }

    /// <summary>
    /// Updates the timer text UI to match the remaining time.
    /// </summary>
    private void UpdateTimerUI()
    {
        if (timerText == null)
        {
            return;
        }

        int seconds = Mathf.Max(0, Mathf.RoundToInt(_timeRemaining));
        int minutes = seconds / 60;
        int secs = seconds % 60;
        timerText.text = $"{minutes:00}:{secs:00}";
    }

    /// <summary>
    /// Updates the theme text UI with the given theme name.
    /// </summary>
    private void UpdateThemeUI(string themeName)
    {
        if (themeText == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(themeName))
        {
            themeText.text = "Theme: Freestyle Round";
        }
        else
        {
            themeText.text = $"Theme: {themeName}";
        }
    }

    /// <summary>
    /// Allows other systems to change the theme at runtime.
    /// </summary>
    public void SetTheme(string themeName)
    {
        UpdateThemeUI(themeName);
    }

    /// <summary>
    /// Clears the resultText UI.
    /// </summary>
    private void ClearResultUI()
    {
        if (resultText != null)
        {
            resultText.text = string.Empty;
        }
    }

    /// <summary>
    /// Clears the chaosText UI.
    /// Called on round start. Chaos events can overwrite it.
    /// </summary>
    private void ClearChaosUI()
    {
        if (chaosText != null)
        {
            chaosText.text = string.Empty;
        }
    }

    /// <summary>
    /// Reloads the active scene, which resets the entire kitchen.
    /// Used only by the local fallback path.
    /// </summary>
    private void ReloadCurrentScene()
    {
        Scene current = SceneManager.GetActiveScene();
        Debug.Log($"[RoundManager] Reloading scene '{current.name}' to reset round.");
        SceneManager.LoadScene(current.name);
    }

    /// <summary>
    /// If no themes are configured in the inspector, fill with defaults that match your recipes.
    /// </summary>
    private void EnsureDefaultThemes()
    {
        if (themeNames != null && themeNames.Length > 0)
        {
            return;
        }

        themeNames = new[]
        {
            "Breakfast Rush",         // breakfast plate
            "Burger Night Showdown",  // burger
            "Fresh Fruit Fiesta",     // fruit salad, strawberry sundae
            "Garden Greens Throwdown",// garden salad
            "Ramen Bowl Rumble",      // ramen bowl
            "Sushi and Salmon Rolls", // salmon roll
            "Veggie Skewer Showdown", // skewer vegetables
            "Sweet Tooth Finale"      // strawberry sundae, desserts
        };
    }

    /// <summary>
    /// Picks a random theme string from themeNames.
    /// </summary>
    private string PickRandomTheme()
    {
        EnsureDefaultThemes();

        if (themeNames == null || themeNames.Length == 0)
        {
            return "Freestyle Round";
        }

        int index = Random.Range(0, themeNames.Length);
        return themeNames[index];
    }

    /// <summary>
    /// Helper to speak a line through TTSManager if assigned.
    /// </summary>
    private void SpeakLine(string line)
    {
        if (ttsManager == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        ttsManager.SynthesizeAndPlay(line);
    }
}
