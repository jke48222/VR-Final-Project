#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class KitchenChaosIngredientTools
{
    [MenuItem("Tools/KitchenChaos/Add IngredientDescriptor To Selected")]
    public static void AddIngredientDescriptorToSelected()
    {
        var selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogWarning("[KitchenChaos] No GameObjects selected.");
            return;
        }

        int addedCount = 0;
        foreach (var go in selection)
        {
            if (go == null) continue;

            var existing = go.GetComponent<IngredientDescriptor>();
            if (existing == null)
            {
                var descriptor = Undo.AddComponent<IngredientDescriptor>(go);
                descriptor.AutoFillForEditor();
                addedCount++;
            }
        }

        Debug.Log($"[KitchenChaos] Added IngredientDescriptor to {addedCount} object(s).");
    }

    [MenuItem("Tools/KitchenChaos/Regenerate Ingredient IDs On Selected")]
    public static void RegenerateIdsOnSelected()
    {
        var selection = Selection.gameObjects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogWarning("[KitchenChaos] No GameObjects selected.");
            return;
        }

        int updatedCount = 0;
        foreach (var go in selection)
        {
            if (go == null) continue;

            var descriptor = go.GetComponent<IngredientDescriptor>();
            if (descriptor == null) continue;

            Undo.RecordObject(descriptor, "Regenerate Ingredient Id");
            // Use the auto fill logic again
            descriptor.RegenerateIdAndName();
            EditorUtility.SetDirty(descriptor);
            updatedCount++;
        }

        Debug.Log($"[KitchenChaos] Regenerated IDs for {updatedCount} IngredientDescriptor(s).");
    }

    // Helper extension methods that call the private logic via reflection style pattern
    private static void AutoFillForEditor(this IngredientDescriptor descriptor)
    {
        if (descriptor == null) return;

        // Call same logic as Reset / OnValidate
        var type = typeof(IngredientDescriptor);
        var method = type.GetMethod("AutoFillIfEmpty", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method?.Invoke(descriptor, null);
    }

    private static void RegenerateIdAndName(this IngredientDescriptor descriptor)
    {
        if (descriptor == null) return;

        // Clear first, then auto fill
        descriptor.ingredientId = string.Empty;
        descriptor.displayName = string.Empty;

        AutoFillForEditor(descriptor);
    }
}
#endif
