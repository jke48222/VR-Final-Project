using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class KnifeBlockClickable : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [Tooltip("Controller that handles knife toggle logic.")]
    public KnifeBlockController knifeBlockController;

    [Header("Debug")]
    [Tooltip("If enabled, logs pointer click events for debugging.")]
    public bool logClicks = false;

    private void Awake()
    {
        // Ensure collider is set correctly for pointer interactions.
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"[KnifeBlockClickable] Collider missing on '{name}' despite RequireComponent.");
        }
        else if (!col.enabled)
        {
            Debug.LogWarning($"[KnifeBlockClickable] Collider on '{name}' is disabled. Pointer clicks will not register.");
        }

        if (knifeBlockController == null)
        {
            Debug.LogWarning($"[KnifeBlockClickable] No KnifeBlockController assigned on '{name}'. Clicks will do nothing.");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (logClicks)
        {
            Debug.Log($"[KnifeBlockClickable] Click detected on '{name}'. Event button: {eventData.button}");
        }

        if (knifeBlockController != null)
        {
            knifeBlockController.ToggleKnife();
            if (logClicks)
            {
                Debug.Log($"[KnifeBlockClickable] Toggled knife via '{knifeBlockController.name}'.");
            }
        }
        else
        {
            Debug.LogWarning($"[KnifeBlockClickable] Click received on '{name}' but no KnifeBlockController is assigned.");
        }
    }
}
