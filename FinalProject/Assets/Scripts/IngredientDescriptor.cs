using UnityEngine;

[DisallowMultipleComponent]
public class IngredientDescriptor : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Stable ID used in recipe logic. Example: ing_tomato_half, ing_salmon_cooked.")]
    public string ingredientId;

    [Tooltip("Human-friendly name used in UI and judge output.")]
    public string displayName;

    [Header("Scoring")]
    [Tooltip("Base importance for scoring and evaluation.")]
    public float baseValue = 1f;

    [Tooltip("True if this represents a final crafted dish rather than a raw ingredient.")]
    public bool isFinalDish = false;

    private void Reset()
    {
        AutoFillIfEmpty();
    }

    private void OnValidate()
    {
        AutoFillIfEmpty();
    }

    /// <summary>
    /// Fills ingredientId and displayName with sane defaults if they are blank.
    /// Ensures no null or whitespace values leak into scoring or recipe systems.
    /// </summary>
    private void AutoFillIfEmpty()
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = MakeNiceNameFromGameObjectName(gameObject.name);
            Debug.Log($"[IngredientDescriptor] Auto-filled displayName for '{name}' → '{displayName}'.");
        }

        if (string.IsNullOrWhiteSpace(ingredientId))
        {
            ingredientId = MakeIdFromGameObjectName(gameObject.name, isFinalDish);
            Debug.Log($"[IngredientDescriptor] Auto-filled ingredientId for '{name}' → '{ingredientId}'.");
        }
    }

    /// <summary>
    /// Converts a GameObject name into a stable recipe-safe ID.
    /// Cleans parentheses, whitespace, prefab suffixes, and applies consistent formatting.
    /// </summary>
    private string MakeIdFromGameObjectName(string name, bool finalDish)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning("[IngredientDescriptor] MakeIdFromGameObjectName called with empty name.");
            return finalDish ? "dish_unknown" : "ing_unknown";
        }

        string trimmed = name.Trim().ToLowerInvariant();

        // Basic cleanup
        trimmed = trimmed.Replace(" ", "_")
                         .Replace("(", "")
                         .Replace(")", "")
                         .Replace(".prefab", "");

        // Prefix ensures no collisions with other gameplay IDs
        string prefix = finalDish ? "dish_" : "ing_";
        string id = prefix + trimmed;

        return id;
    }

    /// <summary>
    /// Creates a friendly UI name based on the GameObject name.
    /// Removes numeric suffixes and converts underscores to spaces.
    /// </summary>
    private string MakeNiceNameFromGameObjectName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Unknown";

        string trimmed = name.Trim()
                             .Replace(".prefab", "")
                             .Replace("_", " ");

        // Remove suffixes like "(1)"
        int parenIndex = trimmed.IndexOf("(");
        if (parenIndex > 0)
        {
            trimmed = trimmed.Substring(0, parenIndex).TrimEnd();
        }

        // Capitalize each word
        string[] parts = trimmed.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i])) continue;

            string word = parts[i].ToLowerInvariant();
            parts[i] = char.ToUpper(word[0]) + word.Substring(1);
        }

        return string.Join(" ", parts);
    }
}
