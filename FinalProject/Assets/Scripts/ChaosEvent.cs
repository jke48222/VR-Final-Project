using UnityEngine;

public abstract class ChaosEvent : ScriptableObject
{
    [Header("Chaos Settings")]
    [Tooltip("Display name for this chaos event.")]
    public string eventName;

    [Tooltip("Duration in seconds the chaos effect should last before ending.")]
    public float duration = 5f;

    [Tooltip("Cooldown time in seconds before this event is allowed to trigger again.")]
    public float cooldown = 10f;

    /// <summary>Called once on each client when the chaos event begins.</summary>
    public abstract void StartEvent(ChaosManager manager);

    /// <summary>Called once on each client when the chaos event ends.</summary>
    public abstract void EndEvent(ChaosManager manager);
}
