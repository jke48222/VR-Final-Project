using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

/// <summary>
/// Creates an AnimatorController asset for your avatar and assigns it
/// to the Animator on this GameObject.
/// 
/// You run this once in the Editor using the context menu.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AvatarAnimatorBuilder : MonoBehaviour
{
    [Header("Animator Target")]
    [Tooltip("Animator that should use the generated controller. If empty, will use the Animator on this GameObject.")]
    public Animator targetAnimator;

#if UNITY_EDITOR
    [Header("Asset Settings (Editor only)")]
    [Tooltip("Path where the AnimatorController asset will be created.")]
    public string controllerAssetPath = "Assets/Animations/Avatar/AvatarAnimator.controller";

    [Tooltip("If true, existing controller at this path will be reused instead of overwritten.")]
    public bool reuseIfExists = true;

    /// <summary>
    /// Right click the component header and choose this,
    /// or press the button in the Inspector.
    /// </summary>
    [ContextMenu("Build Avatar Animator Controller")]
    public void BuildAnimatorController()
    {
        if (targetAnimator == null)
        {
            targetAnimator = GetComponent<Animator>();
        }

        if (targetAnimator == null)
        {
            Debug.LogError("[AvatarAnimatorBuilder] No Animator found to assign controller to.");
            return;
        }

        // Load or create the controller asset
        AnimatorController controller = null;

        if (reuseIfExists)
        {
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerAssetPath);
        }

        if (controller == null)
        {
            EnsureFoldersForPath(controllerAssetPath);
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerAssetPath);
            Debug.Log($"[AvatarAnimatorBuilder] Created new AnimatorController at '{controllerAssetPath}'.");

            SetupBasicStatesAndParameters(controller);
        }
        else
        {
            Debug.Log($"[AvatarAnimatorBuilder] Reusing existing AnimatorController at '{controllerAssetPath}'.");
            EnsureBasicParametersExist(controller);
        }

        // Assign to Animator
        targetAnimator.runtimeAnimatorController = controller;
        EditorUtility.SetDirty(targetAnimator);
        AssetDatabase.SaveAssets();

        Debug.Log($"[AvatarAnimatorBuilder] Assigned controller '{controller.name}' to Animator on '{targetAnimator.gameObject.name}'.");
    }

    private void SetupBasicStatesAndParameters(AnimatorController controller)
    {
        // Create some useful parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("LeftGrab", AnimatorControllerParameterType.Bool);
        controller.AddParameter("RightGrab", AnimatorControllerParameterType.Bool);

        // Default layer
        var layer = controller.layers[0];
        var sm = layer.stateMachine;

        // Idle and Move states with no clips yet (you can drop clips in later)
        var idle = sm.AddState("Idle");
        var move = sm.AddState("Move");

        sm.defaultState = idle;

        // Simple Speed based transitions
        var idleToMove = idle.AddTransition(move);
        idleToMove.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToMove.hasExitTime = false;

        var moveToIdle = move.AddTransition(idle);
        moveToIdle.AddCondition(AnimatorConditionMode.Less, 0.09f, "Speed");
        moveToIdle.hasExitTime = false;

        Debug.Log("[AvatarAnimatorBuilder] Basic states and parameters created (Idle, Move, Speed, LeftGrab, RightGrab).");
    }

    private void EnsureBasicParametersExist(AnimatorController controller)
    {
        bool HasParam(string name) =>
            controller.parameters != null &&
            System.Array.Exists(controller.parameters, p => p.name == name);

        if (!HasParam("Speed"))
        {
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        }
        if (!HasParam("LeftGrab"))
        {
            controller.AddParameter("LeftGrab", AnimatorControllerParameterType.Bool);
        }
        if (!HasParam("RightGrab"))
        {
            controller.AddParameter("RightGrab", AnimatorControllerParameterType.Bool);
        }

        Debug.Log("[AvatarAnimatorBuilder] Ensured basic parameters exist on existing controller.");
    }

    private void EnsureFoldersForPath(string assetPath)
    {
        var normalized = assetPath.Replace("\\", "/");
        var dir = System.IO.Path.GetDirectoryName(normalized);
        if (string.IsNullOrEmpty(dir)) return;

        if (!dir.StartsWith("Assets"))
        {
            Debug.LogWarning($"[AvatarAnimatorBuilder] Path '{dir}' does not start with 'Assets'. No folders created.");
            return;
        }

        var parts = dir.Split('/');
        var current = parts[0]; // "Assets"

        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
#endif
}
