using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ChaosManager : MonoBehaviour
{
    [Header("Chaos Event Settings")]
    public List<ChaosEvent> chaosEvents;
    public float minTimeBetweenEvents = 8f;
    public float maxTimeBetweenEvents = 15f;

    private ChaosEvent currentEvent;
    private Dictionary<ChaosEvent, float> lastUsedTime = new Dictionary<ChaosEvent, float>();

    [Header("UI + SFX")]
    public ChaosUI ui;

    public TTSManager ttsManager;

    void Start()
    {
        foreach (var e in chaosEvents)
            lastUsedTime[e] = -999f;
    }

    public void BeginChaosPhase()
    {
        Debug.Log("Starting Chaos Phase");
        StartCoroutine(ChaosLoop());
    }

    public void EndChaosPhase()
    {
        Debug.Log("Ending Chaos Phase");
        StopAllCoroutines();
    }

    private IEnumerator ChaosLoop()
    {
        while (true)
        {
            float wait = Random.Range(minTimeBetweenEvents, maxTimeBetweenEvents);
            yield return new WaitForSeconds(wait);

            TriggerRandomEvent();
        }
    }

    private void TriggerRandomEvent()
    {
        Debug.Log("CHAOS TIME");
        var available = chaosEvents
            .Where(e => Time.time - lastUsedTime[e] >= e.cooldown)
            .ToList();

        if (available.Count == 0) return;

        currentEvent = available[Random.Range(0, available.Count)];
        lastUsedTime[currentEvent] = Time.time;


        //ui.ShowChaosBanner(currentEvent.eventName);
        ttsManager.SynthesizeAndPlay("Starting the " +  currentEvent.eventName + " event");

        currentEvent.StartEvent(this);
        StartCoroutine(EndEventAfter(currentEvent.duration));
    }

    private IEnumerator EndEventAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        Debug.Log("Event Over");

        currentEvent.EndEvent(this);
        currentEvent = null;
    }
}