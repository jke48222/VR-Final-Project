using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class OpenAIJudgeClient : MonoBehaviour
{
    [Header("OpenAI Settings")]
    [Tooltip("OpenAI API key with access to the specified model.")]
    [SerializeField] private string apiKey;

    [Tooltip("Chat model name, for example gpt-4.1 or gpt-4.1-mini.")]
    public string model = "gpt-4.1";

    // Chat completions endpoint for OpenAI models
    private const string Endpoint = "https://api.openai.com/v1/chat/completions";

    /// <summary>
    /// Sends a judging request to the OpenAI API using the provided system prompt and user JSON payload.
    /// Returns the raw JSON string from the API, or null on failure.
    /// </summary>
    public async Task<string> SendJudgeRequest(string systemPrompt, string userJson)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Debug.LogError("[OpenAIJudgeClient] API key is not set. Please assign it in the inspector.");
            return null;
        }

        if (string.IsNullOrEmpty(systemPrompt))
        {
            Debug.LogError("[OpenAIJudgeClient] System prompt is null or empty.");
            return null;
        }

        if (string.IsNullOrEmpty(userJson))
        {
            Debug.LogError("[OpenAIJudgeClient] User JSON payload is null or empty.");
            return null;
        }

        // Build the request body using serializable types for JsonUtility.
        var body = new ChatCompletionRequest
        {
            model = model,
            messages = new[]
            {
                new ChatCompletionMessage { role = "system", content = systemPrompt },
                new ChatCompletionMessage { role = "user", content = userJson }
            },
            temperature = 0.2f,
            // This tells the model to output strict JSON
            response_format = new ChatCompletionResponseFormat
            {
                type = "json_object"
            }
        };

        string jsonBody;
        try
        {
            jsonBody = JsonUtility.ToJson(body);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OpenAIJudgeClient] Failed to serialize request body: {ex}");
            return null;
        }

        Debug.Log($"[OpenAIJudgeClient] Sending judge request. Model: {model}, Endpoint: {Endpoint}");
        Debug.Log($"[OpenAIJudgeClient] Request JSON (truncated): {Truncate(jsonBody, 500)}");

        string response = await Post(Endpoint, jsonBody);

        if (string.IsNullOrEmpty(response))
        {
            Debug.LogError("[OpenAIJudgeClient] Received null or empty response from OpenAI.");
            return null;
        }

        Debug.Log($"[OpenAIJudgeClient] Response received (truncated): {Truncate(response, 500)}");
        return response;
    }

    /// <summary>
    /// Executes a POST request with a JSON body and returns the response text.
    /// </summary>
    private async Task<string> Post(string url, string json)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("[OpenAIJudgeClient] URL is null or empty.");
            return null;
        }

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("[OpenAIJudgeClient] JSON body is null or empty.");
            return null;
        }

        using UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + apiKey);

        UnityWebRequestAsyncOperation operation;
        try
        {
            operation = req.SendWebRequest();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OpenAIJudgeClient] Exception while sending web request: {ex}");
            return null;
        }

        while (!operation.isDone)
        {
            await Task.Yield();
        }

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[OpenAIJudgeClient] OpenAI request failed. Result: {req.result}, Error: {req.error}");
            if (!string.IsNullOrEmpty(req.downloadHandler?.text))
            {
                Debug.LogError($"[OpenAIJudgeClient] Error body: {req.downloadHandler.text}");
            }
            return null;
        }

        string responseText = req.downloadHandler.text;
        return responseText;
    }

    /// <summary>
    /// Utility method to safely truncate large strings for logging.
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || maxLength <= 0)
            return value;

        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}

[Serializable]
public class ChatCompletionRequest
{
    public string model;
    public ChatCompletionMessage[] messages;
    public float temperature;

    // New: enforce JSON output so JudgeResponse parsing is easier
    public ChatCompletionResponseFormat response_format;
}

[Serializable]
public class ChatCompletionMessage
{
    public string role;
    public string content;
}

[Serializable]
public class ChatCompletionResponseFormat
{
    public string type; // should be "json_object"
}
