using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public static class DishScorer
{
    public struct ScoreResult
    {
        public float score;
        public string breakdown;
    }

    /// <summary>
    /// Scores a plate based on:
    /// - What ingredients are ON the plate (IngredientDescriptor list).
    /// - Which recipe from plate.possibleRecipes best matches those ingredients.
    ///
    /// Behavior:
    /// - If a recipe's required ingredients are all present, it can match.
    /// - Extra ingredients defined on the recipe give bonus points.
    /// - Random extra junk gives small penalties.
    /// - If NO recipe matches, we give "pity points" for at least plating something.
    /// </summary>
    public static ScoreResult ScorePlate(Plate plate)
    {
        var result = new ScoreResult
        {
            score = 0f,
            breakdown = "No dish."
        };

        if (plate == null)
        {
            Debug.LogWarning("[DishScorer] ScorePlate called with null Plate reference.");
            return result;
        }

        // Gather ingredient IDs from the plate
        HashSet<string> ingIds = plate.GetIngredientIdSet();

        if (ingIds.Count == 0)
        {
            Debug.Log("[DishScorer] Plate has no ingredients. Score is 0.");
            result.breakdown = "Plate is empty. Score 0.";
            return result;
        }

        StringBuilder sb = new StringBuilder();

        // Choose best recipe from plate.possibleRecipes
        Recipe matchedRecipe = FindBestMatchingRecipe(plate.possibleRecipes, ingIds);

        if (matchedRecipe == null)
        {
            // No proper recipe matched, but we still give some points
            float pityScore = Mathf.Clamp(ingIds.Count * 5f, 0f, 40f);

            sb.AppendLine("Improvised plate (no full recipe matched).");
            sb.AppendLine("Ingredients on plate: " + string.Join(", ", ingIds));
            sb.AppendLine($"Pity points for effort: {pityScore:F1}");

            result.score = pityScore;
            result.breakdown = sb.ToString();

            Debug.Log($"[DishScorer] Improvised plate. Final score={pityScore:F1}\n{result.breakdown}");
            return result;
        }

        // We have a matched recipe; compute a detailed score
        float score = matchedRecipe.baseScore;
        sb.AppendLine($"Dish: {matchedRecipe.displayName}");
        sb.AppendLine($"Matched recipe ID: {matchedRecipe.recipeId}");
        sb.AppendLine($"Base score: {matchedRecipe.baseScore:F1}");
        sb.AppendLine("Ingredients on plate: " + string.Join(", ", ingIds));

        // Build lists of required and extra ids
        List<string> requiredIds = matchedRecipe.requiredIngredients != null
            ? matchedRecipe.requiredIngredients
                .Where(r => r != null && !string.IsNullOrEmpty(r.ingredientId))
                .Select(r => r.ingredientId)
                .ToList()
            : new List<string>();

        List<string> extraIds = matchedRecipe.extraIngredients != null
            ? matchedRecipe.extraIngredients
                .Where(e => e != null && !string.IsNullOrEmpty(e.ingredientId))
                .Select(e => e.ingredientId)
                .ToList()
            : new List<string>();

        float requiredScore = 0f;

        // Required ingredients
        if (matchedRecipe.requiredIngredients != null)
        {
            foreach (var req in matchedRecipe.requiredIngredients)
            {
                if (req == null || string.IsNullOrEmpty(req.ingredientId)) continue;

                bool present = ingIds.Contains(req.ingredientId);
                if (present)
                {
                    float add = req.weight * matchedRecipe.requiredIngredientWeight;
                    requiredScore += add;
                    sb.AppendLine($" + Required ingredient present: {req.ingredientId} (+{add:F1})");
                }
                else
                {
                    float penalty = req.weight * matchedRecipe.extraIngredientPenalty;
                    requiredScore -= penalty;
                    sb.AppendLine($" - Missing required ingredient: {req.ingredientId} (-{penalty:F1})");
                }
            }
        }

        score += requiredScore;

        // Extra ingredients
        float extraScore = 0f;
        if (matchedRecipe.extraIngredients != null)
        {
            foreach (var extra in matchedRecipe.extraIngredients)
            {
                if (extra == null || string.IsNullOrEmpty(extra.ingredientId)) continue;

                if (ingIds.Contains(extra.ingredientId))
                {
                    float add = extra.weight * matchedRecipe.extraIngredientBonusWeight;
                    extraScore += add;
                    sb.AppendLine($" + Extra ingredient: {extra.ingredientId} (+{add:F1})");
                }
            }
        }

        if (extraScore > 0f)
        {
            score += extraScore;
        }

        // Stray or random ingredients not part of this recipe's required+extra sets
        HashSet<string> allowed = new HashSet<string>(requiredIds);
        foreach (var id in extraIds) allowed.Add(id);

        float strayPenaltyPer = matchedRecipe.extraIngredientPenalty > 0f
            ? matchedRecipe.extraIngredientPenalty
            : 1f;

        foreach (string id in ingIds)
        {
            if (!allowed.Contains(id))
            {
                score -= strayPenaltyPer;
                sb.AppendLine($" - Unneeded ingredient on plate: {id} (-{strayPenaltyPer:F1})");
            }
        }

        // Clamp final score
        score = Mathf.Clamp(score, 0f, 100f);
        sb.AppendLine($"Final score: {score:F1}");

        result.score = score;
        result.breakdown = sb.ToString();

        Debug.Log($"[DishScorer] Scored plate. Final score={score:F1}\n{result.breakdown}");
        return result;
    }

    /// <summary>
    /// Chooses the best recipe based on:
    /// - All required ingredients must be present in ingIds.
    /// - Among those, prefers the recipe with the most required ingredients.
    /// </summary>
    private static Recipe FindBestMatchingRecipe(Recipe[] recipes, HashSet<string> ingIds)
    {
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogWarning("[DishScorer] FindBestMatchingRecipe called but no recipes were provided.");
            return null;
        }

        if (ingIds == null || ingIds.Count == 0)
        {
            return null;
        }

        Recipe best = null;
        int bestRequiredCount = -1;

        foreach (var recipe in recipes)
        {
            if (recipe == null) continue;

            var requiredIds = recipe.requiredIngredients != null
                ? recipe.requiredIngredients
                    .Where(r => r != null && !string.IsNullOrEmpty(r.ingredientId))
                    .Select(r => r.ingredientId)
                    .ToList()
                : new List<string>();

            if (requiredIds.Count == 0)
            {
                // Skip recipes with no required ingredients
                continue;
            }

            bool hasAllRequired = requiredIds.All(id => ingIds.Contains(id));
            if (!hasAllRequired)
            {
                continue;
            }

            int requiredCount = requiredIds.Count;
            if (requiredCount > bestRequiredCount)
            {
                bestRequiredCount = requiredCount;
                best = recipe;
            }
        }

        if (best == null)
        {
            Debug.Log("[DishScorer] No recipe matched all required ingredients.");
        }

        return best;
    }
}
