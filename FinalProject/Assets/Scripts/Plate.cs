using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Plate : MonoBehaviour
{
    [Header("Snap Settings")]
    [Tooltip("Center point on the plate where items should rest.")]
    public Transform snapCenter;

    [Tooltip("Random radius around snap center on X/Z so items are not stacked perfectly.")]
    public float snapRadius = 0.06f;

    [Tooltip("If true, items become kinematic while on the plate.")]
    public bool freezeOnPlate = true;

    [Tooltip("Optional tag filter. Leave empty to accept any IngredientDescriptor.")]
    public string ingredientTag = "";

    [Header("Recipe Matching")]
    [Tooltip("Recipes that this plate can produce / be judged as.")]
    public Recipe[] possibleRecipes;

    [Header("Debug")]
    [Tooltip("All ingredient descriptors currently on the plate (raw + sides + final dish objects).")]
    public List<IngredientDescriptor> ingredientsOnPlate = new List<IngredientDescriptor>();

    [Header("State")]
    [Tooltip("If a final dish GameObject has been spawned on the plate, reference it here.")]
    public GameObject currentDish;

    /// <summary>
    /// True when there is either a spawned final dish or one or more ingredient descriptors on the plate.
    /// </summary>
    public bool HasDish => (currentDish != null) || (ingredientsOnPlate != null && ingredientsOnPlate.Count > 0);

    private Collider _collider;

    private void Reset()
    {
        _collider = GetComponent<Collider>();
        if (_collider != null)
        {
            _collider.isTrigger = true;
            Debug.Log("[Plate] Reset: collider set to trigger.");
        }

        if (snapCenter == null)
        {
            snapCenter = transform;
            Debug.Log("[Plate] Reset: snapCenter not assigned, using plate transform.");
        }
    }

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        if (_collider == null)
        {
            Debug.LogError($"[Plate] Collider missing on '{name}'.");
        }
        else if (!_collider.isTrigger)
        {
            Debug.LogWarning($"[Plate] Collider on '{name}' was not a trigger. Forcing isTrigger = true.");
            _collider.isTrigger = true;
        }

        if (snapCenter == null)
        {
            snapCenter = transform;
            Debug.LogWarning($"[Plate] snapCenter not assigned on '{name}'. Using plate transform.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Optional tag filter
        if (!string.IsNullOrEmpty(ingredientTag) && !other.CompareTag(ingredientTag))
        {
            return;
        }

        var descriptor = other.GetComponentInParent<IngredientDescriptor>();
        if (descriptor == null)
        {
            return;
        }

        if (ingredientsOnPlate.Contains(descriptor))
        {
            // Already tracked
            return;
        }

        ingredientsOnPlate.Add(descriptor);

        // Snap position
        Transform t = descriptor.transform;

        Vector3 basePos = snapCenter.position;
        Vector2 offset2D = Random.insideUnitCircle * snapRadius;

        Vector3 snapPos = new Vector3(
            basePos.x + offset2D.x,
            basePos.y,
            basePos.z + offset2D.y
        );

        t.position = snapPos;
        t.rotation = snapCenter.rotation;

        var rb = t.GetComponent<Rigidbody>();
        if (rb != null && freezeOnPlate)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Debug.Log($"[Plate] ENTER: '{descriptor.ingredientId}' added to plate '{name}'. Count={ingredientsOnPlate.Count}");
    }

    private void OnTriggerExit(Collider other)
    {
        var descriptor = other.GetComponentInParent<IngredientDescriptor>();
        if (descriptor == null)
        {
            return;
        }

        if (!ingredientsOnPlate.Remove(descriptor))
        {
            return;
        }

        // Restore physics if we froze it
        var rb = descriptor.GetComponent<Rigidbody>();
        if (rb != null && freezeOnPlate)
        {
            rb.isKinematic = false;
        }

        Debug.Log($"[Plate] EXIT: '{descriptor.ingredientId}' removed from plate '{name}'. Count={ingredientsOnPlate.Count}");
    }

    /// <summary>
    /// Returns a HashSet of ingredient IDs currently on the plate (for scoring).
    /// Null/empty IDs are skipped.
    /// </summary>
    public HashSet<string> GetIngredientIdSet()
    {
        var set = new HashSet<string>();
        foreach (var d in ingredientsOnPlate)
        {
            if (d == null) continue;
            if (string.IsNullOrEmpty(d.ingredientId)) continue;
            set.Add(d.ingredientId);
        }
        return set;
    }

    /// <summary>
    /// Attempts to determine which Recipe (if any) this plate currently represents.
    /// Preference order:
    /// 1. If `currentDish` has an IngredientDescriptor with an ingredientId that matches a recipe.finalDishId, return that recipe.
    /// 2. Otherwise, compare ingredient IDs on the plate against each recipe's requiredIngredients and return the first match.
    /// </summary>
    public Recipe GetActiveRecipe()
    {
        // 1) Check currentDish for finalDishId match
        if (currentDish != null)
        {
            var desc = currentDish.GetComponent<IngredientDescriptor>();
            if (desc != null && !string.IsNullOrEmpty(desc.ingredientId) && possibleRecipes != null)
            {
                foreach (var r in possibleRecipes)
                {
                    if (r == null) continue;
                    if (!string.IsNullOrEmpty(r.finalDishId) && r.finalDishId == desc.ingredientId)
                    {
                        return r;
                    }
                }
            }
        }

        // 2) Try to match by ingredients on the plate
        if (possibleRecipes == null || possibleRecipes.Length == 0)
            return null;

        var idSet = GetIngredientIdSet();
        foreach (var r in possibleRecipes)
        {
            if (r == null) continue;

            int requiredCount = r.requiredIngredients != null ? r.requiredIngredients.Length : 0;
            int matches = 0;

            if (r.requiredIngredients != null)
            {
                foreach (var req in r.requiredIngredients)
                {
                    if (req == null || string.IsNullOrEmpty(req.ingredientId)) continue;
                    if (idSet.Contains(req.ingredientId)) matches++;
                }
            }

            bool passes;
            if (r.minRequiredMatches <= 0)
            {
                passes = (requiredCount == 0) ? false : (matches >= requiredCount);
            }
            else
            {
                passes = matches >= r.minRequiredMatches;
            }

            if (passes)
                return r;
        }

        return null;
    }
}
