using System.Collections.Generic;
using UnityEngine;

public class DishInstance : MonoBehaviour
{
    [Header("Recipe Metadata")]
    [Tooltip("The recipe that produced this dish.")]
    public Recipe recipe;

    [Tooltip("Ingredient IDs that were used to craft this dish. Optional, but useful for judge/debug.")]
    public List<string> ingredientIds = new List<string>();

    [Header("Debug")]
    [Tooltip("Human-readable summary of this dish instance for debugging and logs.")]
    [TextArea]
    public string debugSummary;

    /// <summary>
    /// Initializes this dish instance from a recipe and a collection of ingredient descriptors.
    /// Call this from BowlRecipeCombiner (or similar) when you spawn the dish.
    /// </summary>
    /// <param name="recipe">Recipe used to produce this dish.</param>
    /// <param name="ingredients">Collection of ingredient descriptors that went into the dish.</param>
    public void InitializeFromIngredients(Recipe recipe, IEnumerable<IngredientDescriptor> ingredients)
    {
        if (recipe == null)
        {
            Debug.LogWarning($"[DishInstance] InitializeFromIngredients called on '{name}' with null recipe.");
        }

        this.recipe = recipe;
        ingredientIds.Clear();

        if (ingredients != null)
        {
            foreach (var ing in ingredients)
            {
                if (ing == null)
                    continue;

                if (!string.IsNullOrEmpty(ing.ingredientId))
                {
                    ingredientIds.Add(ing.ingredientId);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[DishInstance] InitializeFromIngredients called on '{name}' with null ingredient collection.");
        }

        BuildDebugSummary();

        Debug.Log($"[DishInstance] '{name}' initialized. Recipe='{this.recipe?.displayName ?? "NULL"}', Ingredients={ingredientIds.Count}");
    }

    /// <summary>
    /// Rebuilds the debug summary string from current recipe and ingredient IDs.
    /// </summary>
    private void BuildDebugSummary()
    {
        if (recipe == null)
        {
            debugSummary = "DishInstance: No recipe assigned.\n";
            if (ingredientIds != null && ingredientIds.Count > 0)
            {
                debugSummary += "Ingredients (IDs only, recipe is null):\n";
                foreach (var id in ingredientIds)
                {
                    debugSummary += $"  - {id}\n";
                }
            }
            return;
        }

        debugSummary = $"Dish: {recipe.displayName}\n" +
                       $"Recipe ID: {recipe.recipeId}\n" +
                       "Ingredients:\n";

        if (ingredientIds == null || ingredientIds.Count == 0)
        {
            debugSummary += "  (none)\n";
        }
        else
        {
            foreach (var id in ingredientIds)
            {
                debugSummary += $"  - {id}\n";
            }
        }
    }

    private void OnValidate()
    {
        // Keep summary up to date in the editor whenever fields change.
        BuildDebugSummary();
    }
}
