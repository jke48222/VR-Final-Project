using UnityEngine;

public class ChaosTestTrigger : MonoBehaviour
{
    public ChaosManager chaosManager;

    // Optional: directly assign the RubberKnifeEvent
    public RubberKnifeEvent rubberKnifeEvent;


    private void Start()
    {
        ; Debug.Log("starting tester start");
        chaosManager.BeginChaosPhase();
    }
    void Update()
    {
    }

    private System.Collections.IEnumerator EndEventAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        rubberKnifeEvent.EndEvent(chaosManager);
    }
}
