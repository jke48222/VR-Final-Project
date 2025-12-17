using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BowlRecipeCombiner : MonoBehaviour
{
    [Header("Recipes")]
    [Tooltip("All recipes this bowl can craft. Order does not matter.")]
    public Recipe[] recipes;

    [Header("Output")]
    [Tooltip("Where the crafted dish will appear.")]
    public Transform outputAnchor;

    [Header("Spit Back Settings")]
    [Tooltip("Impulse force applied to wrong ingredients when spat out.")]
    public float spitForce = 3f;

    [Tooltip("Upward bias for spit direction.")]
    public float spitUpwardForce = 2f;

    [Tooltip("If true, clears the bowl contents after a mix attempt.")]
    public bool clearAfterMix = true;

    // Current logical ingredients in the bowl.
    private readonly List<IngredientDescriptor> _currentIngredients = new List<IngredientDescriptor>();

    // Corresponding GameObjects for the ingredients.
    private readonly List<GameObject> _currentObjects = new List<GameObject>();

    private void Awake()
    {
        if (outputAnchor == null)
        {
            outputAnchor = transform;
            Debug.LogWarning("[BowlRecipeCombiner] Output anchor not assigned. Falling back to bowl transform.");
        }

        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogWarning("[BowlRecipeCombiner] No recipes assigned. Mixing will always fail.");
        }
    }

    /// <summary>
    /// Called by BowlTriggerPuzzle when an ingredient enters the bowl trigger.
    /// </summary>
    public void OnIngredientEntered(IngredientDescriptor descriptor, GameObject obj)
    {
        if (descriptor == null)
        {
            Debug.LogWarning("[BowlRecipeCombiner] OnIngredientEntered called with null descriptor.");
            return;
        }

        if (obj == null)
        {
            Debug.LogWarning("[BowlRecipeCombiner] OnIngredientEntered called with null GameObject.");
            return;
        }

        if (string.IsNullOrEmpty(descriptor.ingredientId))
        {
            Debug.LogWarning("[BowlRecipeCombiner] Ingredient has no valid ingredientId. Ignoring.");
            return;
        }

        _currentIngredients.Add(descriptor);
        _currentObjects.Add(obj);

        Debug.Log($"[BowlRecipeCombiner] ENTER: {descriptor.ingredientId} (bowl count: {_currentIngredients.Count})");
    }

    /// <summary>
    /// Called by BowlTriggerPuzzle when an ingredient leaves the bowl trigger.
    /// </summary>
    public void OnIngredientExited(IngredientDescriptor descriptor, GameObject obj)
    {
        if (descriptor == null)
        {
            Debug.LogWarning("[BowlRecipeCombiner] OnIngredientExited called with null descriptor.");
            return;
        }

        if (obj == null)
        {
            Debug.LogWarning("[BowlRecipeCombiner] OnIngredientExited called with null GameObject.");
            return;
        }

        int index = _currentObjects.IndexOf(obj);
        if (index >= 0 && index < _currentIngredients.Count)
        {
            Debug.Log($"[BowlRecipeCombiner] EXIT: {descriptor.ingredientId} removed at index {index}.");
            _currentObjects.RemoveAt(index);
            _currentIngredients.RemoveAt(index);
        }
        else
        {
            Debug.LogWarning("[BowlRecipeCombiner] OnIngredientExited: object not found in current list.");
        }
    }

    /// <summary>
    /// Called by the whisk when the player presses the mix button.
    /// Attempts to match the current ingredients to a recipe.
    /// </summary>
    public void Mix()
    {
        if (_currentIngredients.Count == 0)
        {
            Debug.Log("[BowlRecipeCombiner] Mix called but bowl is empty.");
            return;
        }

        var ids = _currentIngredients
            .Where(i => i != null && !string.IsNullOrEmpty(i.ingredientId))
            .Select(i => i.ingredientId)
            .ToList();

        Debug.Log($"[BowlRecipeCombiner] Mix triggered with ingredients: {string.Join(", ", ids)}");

        Recipe matched = FindMatchingRecipe(ids);

        if (matched != null)
        {
            Debug.Log($"[BowlRecipeCombiner] Matched recipe: {matched.displayName} (Recipe ID: {matched.recipeId})");
            CraftRecipe(matched);
        }
        else
        {
            Debug.Log("[BowlRecipeCombiner] No recipe matched. Spitting back ingredients.");
            SpitBackIngredients();
        }

        if (clearAfterMix)
        {
            Debug.Log("[BowlRecipeCombiner] Clearing bowl contents after mix.");
            _currentIngredients.Clear();
            _currentObjects.Clear();
        }
        else
        {
            Debug.Log("[BowlRecipeCombiner] clearAfterMix is false. Bowl contents remain for subsequent mixes.");
        }
    }

    /// <summary>
    /// Unordered matching:
    /// - All required ingredients must be present.
    /// - Any ingredients not in (required union extra) make it invalid.
    /// - Extra ingredients are allowed and can be used in external scoring.
    /// - If multiple recipes match, the one with the most required ingredients is chosen.
    /// </summary>
    private Recipe FindMatchingRecipe(List<string> ingredientIds)
    {
        if (recipes == null || recipes.Length == 0)
        {
            Debug.LogWarning("[BowlRecipeCombiner] FindMatchingRecipe called but no recipes are assigned.");
            return null;
        }

        if (ingredientIds == null || ingredientIds.Count == 0)
        {
            Debug.LogWarning("[BowlRecipeCombiner] FindMatchingRecipe called with no ingredient IDs.");
            return null;
        }

        var bowlSet = new HashSet<string>(ingredientIds);

        Recipe bestRecipe = null;
        int bestRequiredCount = -1;

        foreach (var recipe in recipes)
        {
            if (recipe == null)
            {
                Debug.LogWarning("[BowlRecipeCombiner] Encountered null recipe in recipes array.");
                continue;
            }

            var requiredIds = recipe.requiredIngredients != null
                ? recipe.requiredIngredients
                    .Where(r => r != null && !string.IsNullOrEmpty(r.ingredientId))
                    .Select(r => r.ingredientId)
                    .ToList()
                : new List<string>();

            var extraIds = recipe.extraIngredients != null
                ? recipe.extraIngredients
                    .Where(e => e != null && !string.IsNullOrEmpty(e.ingredientId))
                    .Select(e => e.ingredientId)
                    .ToList()
                : new List<string>();

            if (requiredIds.Count == 0)
            {
                // In this design, recipes with no required ingredients are ignored.
                Debug.LogWarning($"[BowlRecipeCombiner] Recipe '{recipe.displayName}' has no required ingredients and is skipped.");
                continue;
            }

            // Condition 1: all required must be present in the bowl.
            bool hasAllRequired = requiredIds.All(id => bowlSet.Contains(id));
            if (!hasAllRequired)
            {
                Debug.Log($"[BowlRecipeCombiner] Recipe '{recipe.displayName}' rejected: missing one or more required ingredients.");
                continue;
            }

            // Condition 2: no forbidden ingredients.
            var allowedIds = new HashSet<string>(requiredIds);
            foreach (var id in extraIds)
            {
                allowedIds.Add(id);
            }

            bool hasForbidden = bowlSet.Any(id => !allowedIds.Contains(id));
            if (hasForbidden)
            {
                Debug.Log($"[BowlRecipeCombiner] Recipe '{recipe.displayName}' rejected: bowl contains forbidden ingredients.");
                continue;
            }

            int requiredCount = requiredIds.Count;
            if (requiredCount > bestRequiredCount)
            {
                bestRequiredCount = requiredCount;
                bestRecipe = recipe;
            }
        }

        if (bestRecipe == null)
        {
            Debug.Log("[BowlRecipeCombiner] No recipes passed validation. Returning null.");
        }

        return bestRecipe;
    }

    /// <summary>
    /// Destroys ingredient objects in the bowl and spawns the final dish prefab.
    /// </summary>
    private void CraftRecipe(Recipe recipe)
    {
        if (recipe == null)
        {
            Debug.LogError("[BowlRecipeCombiner] CraftRecipe called with null recipe.");
            return;
        }

        // Destroy all current ingredient objects.
        foreach (var obj in _currentObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        if (recipe.finalDishPrefab == null)
        {
            Debug.LogWarning($"[BowlRecipeCombiner] Recipe '{recipe.displayName}' has no finalDishPrefab assigned.");
            return;
        }

        Vector3 pos = outputAnchor.position;
        Quaternion rot = outputAnchor.rotation;

        GameObject dish = Instantiate(recipe.finalDishPrefab, pos, rot);
        Debug.Log($"[BowlRecipeCombiner] Spawned dish: {dish.name} for recipe '{recipe.displayName}'.");

        // Ensure the final dish has an IngredientDescriptor and mark it as a final dish.
        var descriptor = dish.GetComponent<IngredientDescriptor>();
        if (descriptor == null)
        {
            descriptor = dish.AddComponent<IngredientDescriptor>();
            Debug.Log("[BowlRecipeCombiner] Added IngredientDescriptor to final dish.");
        }

        descriptor.isFinalDish = true;
        descriptor.ingredientId = !string.IsNullOrEmpty(recipe.finalDishId)
            ? recipe.finalDishId
            : $"dish_{recipe.recipeId}";
        descriptor.displayName = recipe.displayName;
    }

    /// <summary>
    /// Adds an impulse force to each ingredient to eject it from the bowl.
    /// </summary>
    private void SpitBackIngredients()
    {
        foreach (var obj in _currentObjects)
        {
            if (obj == null)
            {
                Debug.LogWarning("[BowlRecipeCombiner] SpitBackIngredients encountered a null object reference.");
                continue;
            }

            var rb = obj.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning($"[BowlRecipeCombiner] Object '{obj.name}' has no Rigidbody. Cannot apply spit force.");
                continue;
            }

            Vector3 randomDir = new Vector3(
                Random.Range(-0.5f, 0.5f),
                0f,
                Random.Range(-0.5f, 0.5f)
            ).normalized;

            Vector3 force = randomDir * spitForce + Vector3.up * spitUpwardForce;
            rb.AddForce(force, ForceMode.Impulse);

            Debug.Log($"[BowlRecipeCombiner] Spat back '{obj.name}' with force {force}.");
        }
    }
}
