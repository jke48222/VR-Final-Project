using System;
using UnityEngine;

[CreateAssetMenu(menuName = "KitchenChaos/Recipe", fileName = "NewRecipe")]
public class Recipe : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable ID used in code and saving. Example: recipe_garden_salad.")]
    public string recipeId = "recipe_id";

    [Tooltip("Name shown in UI, recipe book, and judge text.")]
    public string displayName = "New Recipe";

    [Header("Final Dish")]
    [Tooltip("Prefab that the bowl will spawn when this recipe is successfully crafted.")]
    public GameObject finalDishPrefab;

    [Tooltip("Optional. If set, should match IngredientDescriptor.ingredientId on the final dish prefab.")]
    public string finalDishId;

    [Header("Matching Rules")]
    [Tooltip("Required ingredients that must be present to craft this dish.")]
    public IngredientRequirement[] requiredIngredients;

    [Tooltip("Optional ingredients that grant bonus score or style points.")]
    public ExtraIngredient[] extraIngredients;

    [Tooltip("Minimum number of required ingredients that must be present. Zero means all required must match.")]
    public int minRequiredMatches = 0;

    [Header("Scoring Weights (used by DishScorer/Judge)")]
    [Tooltip("Base score before ingredient bonuses/penalties.")]
    public float baseScore = 50f;

    [Tooltip("Multiplier for matching required ingredients.")]
    public float requiredIngredientWeight = 20f;

    [Tooltip("Penalty per ingredient not in required or extras.")]
    public float extraIngredientPenalty = 3f;

    [Tooltip("Multiplier for extra ingredients that are present.")]
    public float extraIngredientBonusWeight = 5f;

    private void OnValidate()
    {
        // Ensure display name is not empty
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = name;
        }

        // Auto-fill finalDishId based on IngredientDescriptor if possible
        if (finalDishPrefab != null && string.IsNullOrWhiteSpace(finalDishId))
        {
            var descriptor = finalDishPrefab.GetComponent<IngredientDescriptor>();
            if (descriptor != null && !string.IsNullOrWhiteSpace(descriptor.ingredientId))
            {
                finalDishId = descriptor.ingredientId;
            }
        }

        // Prevent invalid minRequiredMatches
        if (minRequiredMatches < 0)
        {
            Debug.LogWarning($"[Recipe] minRequiredMatches on '{displayName}' was below zero. Clamping to 0.");
            minRequiredMatches = 0;
        }
    }
}

#region Ingredient Requirement Classes

[Serializable]
public class IngredientRequirement
{
    [Tooltip("IngredientDescriptor.ingredientId required. Example: ing_tomato_half.")]
    public string ingredientId;

    [Tooltip("Desired cook state. Ignored if ignoreCookState is true.")]
    public CookState desiredCookState = CookState.Raw;

    [Tooltip("If true, any cook state is accepted.")]
    public bool ignoreCookState = false;

    [Tooltip("How many of this ingredient are expected. Zero means count does not matter.")]
    public int expectedCount = 1;

    [Tooltip("Relative importance of this ingredient in scoring.")]
    public float weight = 1f;
}

[Serializable]
public class ExtraIngredient
{
    [Tooltip("IngredientDescriptor.ingredientId that counts as a bonus if present.")]
    public string ingredientId;

    [Tooltip("Relative bonus weight for scoring.")]
    public float weight = 1f;

    [Tooltip("If true, cook state does not matter.")]
    public bool ignoreCookState = true;

    [Tooltip("Desired cook state if ignoreCookState is false.")]
    public CookState desiredCookState = CookState.Raw;
}

#endregion
