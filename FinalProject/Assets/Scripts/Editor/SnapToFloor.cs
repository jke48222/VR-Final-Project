using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor utility for precisely snapping selected objects so their lowest
/// renderer point rests on the floor beneath them. Useful for fixing
/// floating or sinking props in the scene.
/// </summary>
public class SnapToFloor : MonoBehaviour
{
    /// <summary>
    /// Snaps each selected object so the bottom of its renderer aligns with
    /// the nearest collider surface directly beneath it.
    /// </summary>
    [MenuItem("Tools/Snap Selected To Floor %#d")]
    private static void SnapSelectedToFloor()
    {
        foreach (var obj in Selection.transforms)
        {
            var rend = obj.GetComponentInChildren<Renderer>();
            if (!rend)
            {
                Debug.LogWarning($"{obj.name} has no Renderer — skipped.");
                continue;
            }

            // Determine the lowest point of the renderer bounds.
            float bottomY = rend.bounds.min.y;

            // Start a short ray just above the bottom to ensure clean hits.
            Vector3 rayStart = new Vector3(
                obj.position.x,
                bottomY + 0.05f,
                obj.position.z
            );

            // Cast downward to find the floor.
            if (Physics.Raycast(rayStart, Vector3.down, out var hit, 5f))
            {
                Undo.RecordObject(obj, "Snap To Floor Accurate");

                // Compute the local vertical offset between pivot and renderer bottom.
                float pivotToBottom = obj.position.y - bottomY;

                // Align the pivot so the bottom rests exactly on the hit point.
                var pos = obj.position;
                pos.y = hit.point.y + pivotToBottom;
                obj.position = pos;
            }
            else
            {
                Debug.LogWarning($"No floor detected under {obj.name}. Ensure the floor has a collider.");
            }
        }
    }
}
