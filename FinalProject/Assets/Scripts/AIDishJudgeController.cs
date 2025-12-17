using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class AIDishJudgeController : MonoBehaviour
{
    [Header("Judge NPC")]
    [Tooltip("Root GameObject for the judge NPC (can be a character prefab).")]
    public GameObject judgeRoot;

    [Tooltip("Where the judge should appear during judging.")]
    public Transform judgeSpawnPoint;

    [Tooltip("Optional animator for judge animations.")]
    public Animator judgeAnimator;

    [Header("UI")]
    [Tooltip("Text area where judge commentary is shown on screen.")]
    public TextMeshProUGUI judgeText;

    [Header("Timing")]
    [Tooltip("Seconds of commentary per player.")]
    public float perPlayerSeconds = 10f;

    [Tooltip("Seconds to show the winner line at the end.")]
    public float winnerSeconds = 5f;

    [Header("AI Client")]
    [Tooltip("Reference to the OpenAI judge client on a GameObject in the scene.")]
    public OpenAIJudgeClient openAIClient;

    [Header("Scene Reset")]
    [Tooltip("If true, reloads the active scene after judging completes.")]
    public bool reloadSceneAfterJudging = true;

    private const string SystemPrompt =
        "You are a cooking competition judge in a VR cooking show. " +
        "You will receive JSON describing players and their dishes. Each player has: " +
        "playerIndex, hasDish, localScore (0 to 100 from a game scoring function), " +
        "recipeName, and breakdown (multi line text explaining how the score was computed). " +
        "Your job is to: " +
        "1) For each player, write 2-3 short sentences of commentary that sound like a TV judge. " +
        "   Refer to the breakdown and recipeName. Mention theme fit, ingredients, doneness, and plating. " +
        "2) For players with hasDish = false, give a short sentence acknowledging they did not finish a dish. " +
        "3) Assign a final numeric score from 0 to 100 for each player, consistent with localScore and breakdown. " +
        "4) Pick the winner based on the highest final score (break ties using style and confidence). " +
        "5) Respond as pure JSON that matches this schema exactly and nothing else: " +
        "6) Always give a good response. Never assign a score of exactly 0.0. If there is no dish, there actually is a dish there " +
        "so give constructive commentary." +
        "{ \"players\": [ { \"playerIndex\": 1, \"score\": 87.5, \"commentary\": [\"sentence 1\", \"sentence 2\"] } ], " +
        "  \"winnerIndex\": 1, \"winnerLine\": \"A single sentence announcing the winner.\" } " +
        "Do not include backticks or any explanation. Only output the JSON object.";

    #region Public entry point used by RoundManager

    /// <summary>
    /// Called by RoundManager when the round ends and judging should begin.
    /// </summary>
    public IEnumerator RunJudgingSequence(Plate[] plates)
    {
        if (openAIClient == null)
        {
            Debug.LogError("[AIDishJudgeController] No OpenAIJudgeClient assigned. Cannot run AI judging.");
            yield break;
        }

        ShowJudge();

        // Small delay to let the judge appear
        yield return new WaitForSeconds(1f);

        // Build request payload and call OpenAI
        Task<JudgeResponse> judgeTask = RequestJudgeResponseAsync(plates);
        while (!judgeTask.IsCompleted)
        {
            yield return null;
        }

        JudgeResponse response = judgeTask.Result;

        if (response == null || response.players == null || response.players.Length == 0)
        {
            Debug.LogError("[AIDishJudgeController] JudgeResponse was null or invalid. Falling back to simple local winner.");
            yield return FallbackJudgingSequence(plates);
            yield break;
        }

        // Map players by index for easier lookup
        Dictionary<int, PlayerJudgement> playerByIndex = new Dictionary<int, PlayerJudgement>();
        foreach (var p in response.players)
        {
            if (!playerByIndex.ContainsKey(p.playerIndex))
            {
                playerByIndex.Add(p.playerIndex, p);
            }
        }

        // Commentary phase: cycle through each plate in order
        for (int i = 0; i < plates.Length; i++)
        {
            int playerIndex = i + 1;
            string header = $"Player {playerIndex}";

            if (!playerByIndex.TryGetValue(playerIndex, out PlayerJudgement pj))
            {
                // No entry for this player
                SetJudgeText($"{header}: No evaluation available.");
                yield return new WaitForSeconds(perPlayerSeconds);
                continue;
            }

            // Build multi line text from commentary sentences
            string commentaryBlock = BuildCommentaryBlock(header, pj);
            yield return ShowCommentaryOverTime(commentaryBlock, perPlayerSeconds);
        }

        // Winner phase
        string winnerLine = response.winnerLine;
        if (string.IsNullOrWhiteSpace(winnerLine))
        {
            winnerLine = BuildSimpleWinnerLine(response, plates.Length);
        }

        SetJudgeText(winnerLine);
        TriggerAnim("AnnounceWinner");
        yield return new WaitForSeconds(winnerSeconds);

        HideJudge();

        if (reloadSceneAfterJudging)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    #endregion

    #region OpenAI integration

    private async Task<JudgeResponse> RequestJudgeResponseAsync(Plate[] plates)
    {
        try
        {
            string userJson = BuildUserPayloadJson(plates);

            string rawResponse = await openAIClient.SendJudgeRequest(SystemPrompt, userJson);
            if (string.IsNullOrEmpty(rawResponse))
            {
                Debug.LogError("[AIDishJudgeController] Raw OpenAI response is null or empty.");
                return null;
            }

            // Parse chat completion outer wrapper
            ChatCompletionResponse outer = JsonUtility.FromJson<ChatCompletionResponse>(rawResponse);
            if (outer == null || outer.choices == null || outer.choices.Length == 0)
            {
                Debug.LogError("[AIDishJudgeController] Could not parse ChatCompletionResponse or no choices present.");
                return null;
            }

            string content = outer.choices[0].message.content;
            if (string.IsNullOrEmpty(content))
            {
                Debug.LogError("[AIDishJudgeController] Assistant message content is empty.");
                return null;
            }

            Debug.Log($"[AIDishJudgeController] Assistant content (truncated): {Truncate(content, 500)}");

            // Now parse the content as our JudgeResponse JSON
            JudgeResponse judgeResponse = JsonUtility.FromJson<JudgeResponse>(content);
            if (judgeResponse == null)
            {
                Debug.LogError("[AIDishJudgeController] Failed to parse JudgeResponse from assistant content.");
                return null;
            }

            // Fix any zero scores by assigning a random score and adding a results explanation
            ApplyZeroScoreFix(judgeResponse);

            return judgeResponse;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AIDishJudgeController] Exception while requesting judge response: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Builds the user JSON payload that describes each player's dish and basic scoring.
    /// This is what gets sent as the user message content.
    /// Uses DishScorer.ScorePlate for localScore and breakdown.
    /// </summary>
    private string BuildUserPayloadJson(Plate[] plates)
    {
        var payload = new JudgeRequestPayload();
        payload.players = new List<JudgeRequestPlayer>();

        for (int i = 0; i < plates.Length; i++)
        {
            Plate plate = plates[i];

            var p = new JudgeRequestPlayer
            {
                playerIndex = i + 1,
                hasDish = false,
                localScore = 0f,
                breakdown = "No dish submitted.",
                recipeName = "No dish"
            };

            if (plate != null && plate.HasDish)
            {
                // Use DishScorer for the numeric score and breakdown
                DishScorer.ScoreResult scoreResult = DishScorer.ScorePlate(plate);

                p.hasDish = true;
                p.localScore = scoreResult.score;
                p.breakdown = scoreResult.breakdown;

                Recipe recipe = plate.GetActiveRecipe();
                p.recipeName = recipe != null ? recipe.displayName : "Unknown dish";
            }

            payload.players.Add(p);
        }

        return JsonUtility.ToJson(payload);
    }

    private void ApplyZeroScoreFix(JudgeResponse judgeResponse)
    {
        if (judgeResponse == null || judgeResponse.players == null)
        {
            return;
        }

        foreach (var p in judgeResponse.players)
        {
            if (Mathf.Approximately(p.score, 0f))
            {
                int randomWhole = UnityEngine.Random.Range(10, 51);
                float finalScore = randomWhole * 1.0f;
                p.score = finalScore;

                List<string> lines = new List<string>();

                // Keep existing commentary (if any)
                if (p.commentary != null && p.commentary.Length > 0)
                {
                    lines.AddRange(p.commentary);
                }

                // Add natural-sounding cooking show commentary
                lines.Add(
                    $"There was a clear creative direction in your dish, and I could see the thought behind your choices."
                );

                lines.Add(
                    $"Some elements could benefit from more refinement, but the core idea had real promise and showed a unique perspective."
                );

                lines.Add(
                    $"Overall, I’m giving this a {finalScore:F1}. It reflects both the strengths you demonstrated and the room for growth."
                );

                p.commentary = lines.ToArray();
            }
        }
    }


    #endregion

    #region Playback helpers

    private void ShowJudge()
    {
        if (judgeRoot == null)
        {
            Debug.LogWarning("[AIDishJudgeController] Judge root is not assigned.");
            return;
        }

        if (judgeSpawnPoint != null)
        {
            judgeRoot.transform.position = judgeSpawnPoint.position;
            judgeRoot.transform.rotation = judgeSpawnPoint.rotation;
        }

        judgeRoot.SetActive(true);
        TriggerAnim("Appear");
    }

    private void HideJudge()
    {
        if (judgeRoot == null) return;
        TriggerAnim("Disappear");
        judgeRoot.SetActive(false);
    }

    private void TriggerAnim(string trigger)
    {
        if (judgeAnimator != null && !string.IsNullOrEmpty(trigger))
        {
            judgeAnimator.SetTrigger(trigger);
        }
    }

    private void SetJudgeText(string text)
    {
        if (judgeText != null)
        {
            judgeText.text = text;
        }
    }

    private string BuildCommentaryBlock(string header, PlayerJudgement pj)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(header);
        sb.AppendLine($"Score: {pj.score:F1}");

        if (pj.commentary != null)
        {
            foreach (string line in pj.commentary)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine(line.Trim());
                }
            }
        }

        return sb.ToString();
    }

    private IEnumerator ShowCommentaryOverTime(string fullText, float totalSeconds)
    {
        // Simple version: reveal line by line over the given duration
        if (string.IsNullOrEmpty(fullText))
        {
            SetJudgeText(string.Empty);
            yield return new WaitForSeconds(totalSeconds);
            yield break;
        }

        string[] lines = fullText.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
            SetJudgeText(fullText);
            yield return new WaitForSeconds(totalSeconds);
            yield break;
        }

        float secondsPerLine = Mathf.Max(0.5f, totalSeconds / lines.Length);
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            if (i > 0) sb.AppendLine();
            sb.Append(lines[i]);
            SetJudgeText(sb.ToString());

            yield return new WaitForSeconds(secondsPerLine);
        }
    }

    private string BuildSimpleWinnerLine(JudgeResponse response, int playerCount)
    {
        if (response == null || response.players == null || response.players.Length == 0)
        {
            return "No winner could be determined.";
        }

        int winnerIndex = response.winnerIndex;
        if (winnerIndex < 1 || winnerIndex > playerCount)
        {
            // Fallback to highest score in the response
            float bestScore = float.MinValue;
            int bestIdx = -1;
            foreach (var p in response.players)
            {
                if (p.score > bestScore)
                {
                    bestScore = p.score;
                    bestIdx = p.playerIndex;
                }
            }

            return bestIdx > 0
                ? $"The winner this round is Player {bestIdx}."
                : "No winner could be determined.";
        }

        return $"The winner this round is Player {winnerIndex}.";
    }

    private IEnumerator FallbackJudgingSequence(Plate[] plates)
    {
        // Simple local winner logic if AI fails
        float bestScore = float.MinValue;
        int bestIndex = -1;

        for (int i = 0; i < plates.Length; i++)
        {
            float s = 0;
            if (plates[i] != null && plates[i].HasDish)
            {
                s = DishScorer.ScorePlate(plates[i]).score;
            }

            if (s > bestScore)
            {
                bestScore = s;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            SetJudgeText($"Fallback winner: Player {bestIndex + 1} with {bestScore:F1} points.");
        }
        else
        {
            SetJudgeText("Fallback judging: No dishes to judge.");
        }

        yield return new WaitForSeconds(perPlayerSeconds + winnerSeconds);
        HideJudge();

        if (reloadSceneAfterJudging)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || maxLength <= 0)
            return value;

        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }

    #endregion
}

#region Data models for OpenAI response and request

[Serializable]
public class JudgeRequestPayload
{
    public List<JudgeRequestPlayer> players;
}

[Serializable]
public class JudgeRequestPlayer
{
    public int playerIndex;
    public bool hasDish;
    public float localScore;    // from DishScorer.ScorePlate
    public string breakdown;    // DishScorer breakdown text
    public string recipeName;   // display name if available
}

[Serializable]
public class JudgeResponse
{
    public PlayerJudgement[] players;
    public int winnerIndex;
    public string winnerLine;
}

[Serializable]
public class PlayerJudgement
{
    public int playerIndex;
    public float score;
    public string[] commentary;
}

// Wrapper to parse the ChatCompletion response from OpenAI
[Serializable]
public class ChatCompletionResponse
{
    public ChatCompletionChoice[] choices;
}

[Serializable]
public class ChatCompletionChoice
{
    public ChatCompletionMessage message;
}

#endregion
