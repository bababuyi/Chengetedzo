using UnityEngine;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{
    [System.Serializable]
    public class EventData
    {
        public string eventName;
        public string description;
        public float minCost;           // monetary loss
        public float maxCost;
        public bool coveredByInsurance; // whether it triggers an insurance claim
    }

    [Header("Possible Monthly Events")]
    public List<EventData> allEvents = new List<EventData>();

    private UIManager uiManager;
    private FinanceManager financeManager;
    private InsuranceManager insuranceManager;

    private void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();
        financeManager = FindFirstObjectByType<FinanceManager>();
        insuranceManager = FindFirstObjectByType<InsuranceManager>();

        // Preload default events if none exist in Inspector
        if (allEvents.Count == 0)
            PopulateDefaultEvents();
    }

    private void PopulateDefaultEvents()
    {
        allEvents.Add(new EventData
        {
            eventName = "Medical Emergency",
            description = "A family member fell ill and required urgent care.",
            minCost = 30f,
            maxCost = 80f,
            coveredByInsurance = true
        });

        allEvents.Add(new EventData
        {
            eventName = "School Fees Due",
            description = "It’s time to pay this term’s school fees.",
            minCost = 100f,
            maxCost = 100f,
            coveredByInsurance = false
        });

        allEvents.Add(new EventData
        {
            eventName = "Food Price Hike",
            description = "Prices at the market have increased this month.",
            minCost = 20f,
            maxCost = 50f,
            coveredByInsurance = false
        });

        allEvents.Add(new EventData
        {
            eventName = "Death of Dependent",
            description = "A dependent has passed away — funeral and support needed.",
            minCost = 100f,
            maxCost = 200f,
            coveredByInsurance = true
        });

        allEvents.Add(new EventData
        {
            eventName = "Income Boost",
            description = "You earned some unexpected income this month!",
            minCost = -60f,
            maxCost = -120f,
            coveredByInsurance = false
        });
    }

    // Called by GameManager once per month
    public void CheckForMonthlyEvent(int month)
    {
        float roll = Random.value;
        if (roll > 0.65f) // 35% chance each month
        {
            EventData chosen = allEvents[Random.Range(0, allEvents.Count)];
            TriggerEvent(chosen);
        }
        else
        {
            Debug.Log($"[Month {month}] No event this month.");
        }
    }

    private void TriggerEvent(EventData e)
    {
        float impact = Random.Range(e.minCost, e.maxCost);
        string logText;

        if (impact > 0)
        {
            financeManager.cashOnHand -= impact;
            logText = $"Event: {e.eventName} — Lost ${impact:F0}";
        }
        else
        {
            financeManager.cashOnHand += Mathf.Abs(impact);
            logText = $"Event: {e.eventName} — Gained ${Mathf.Abs(impact):F0}";
        }

        Debug.Log(logText);

        if (e.coveredByInsurance)
        {
            insuranceManager.ProcessClaims(); // run claim handler if active insurance applies
        }

        uiManager.ShowEventPopup($"{e.eventName}\n{e.description}\nImpact: {impact:F0}");
    }
}
