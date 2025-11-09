using UnityEngine;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{
    [System.Serializable]
    public class MonthlyEvent
    {
        public string eventName;
        public string description;
        public float impactAmount;               // how much it costs or affects income
        public bool affectsIncome;               // true = income reduction, false = extra expense
        public InsuranceManager.InsuranceType coveredBy;  // insurance protection type
        public float baseProbability;            // 0–100 chance each month
    }

    [Header("Possible Monthly Events")]
    public List<MonthlyEvent> allEvents = new List<MonthlyEvent>();

    [Header("Event Balancing")]
    [Range(0, 1)] public float eventChanceMultiplier = 1.0f;

    private UIManager uiManager;
    private FinanceManager financeManager;
    private InsuranceManager insuranceManager;

    private void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();
        financeManager = FindFirstObjectByType<FinanceManager>();
        insuranceManager = FindFirstObjectByType<InsuranceManager>();

        // Default events (for quick testing)
        if (allEvents.Count == 0)
        {
            allEvents.Add(new MonthlyEvent
            {
                eventName = "Drought",
                description = "Low rainfall affects your crop yields this month.",
                impactAmount = 100f,
                affectsIncome = true,
                coveredBy = InsuranceManager.InsuranceType.Funeral, // placeholder
                baseProbability = 40f
            });

            allEvents.Add(new MonthlyEvent
            {
                eventName = "Livestock Disease",
                description = "An outbreak reduces livestock health and output.",
                impactAmount = 80f,
                affectsIncome = false,
                coveredBy = InsuranceManager.InsuranceType.MicroMedical,
                baseProbability = 30f
            });

            allEvents.Add(new MonthlyEvent
            {
                eventName = "Medical Emergency",
                description = "A family member fell ill. Medical expenses increase.",
                impactAmount = 50f,
                affectsIncome = false,
                coveredBy = InsuranceManager.InsuranceType.Hospital,
                baseProbability = 50f
            });

            allEvents.Add(new MonthlyEvent
            {
                eventName = "Equipment Breakdown",
                description = "You had to repair key farming equipment.",
                impactAmount = 70f,
                affectsIncome = false,
                coveredBy = InsuranceManager.InsuranceType.MicroMedical,
                baseProbability = 25f
            });

            allEvents.Add(new MonthlyEvent
            {
                eventName = "Community Illness",
                description = "Productivity dropped due to illness in the community.",
                impactAmount = 60f,
                affectsIncome = true,
                coveredBy = InsuranceManager.InsuranceType.MicroMedical,
                baseProbability = 45f
            });
        }
    }

    public void CheckForMonthlyEvent(int month)
    {
        if (allEvents.Count == 0) return;

        foreach (var e in allEvents)
        {
            float chance = e.baseProbability * eventChanceMultiplier;
            if (Random.Range(0f, 100f) <= chance)
            {
                TriggerEvent(e, month);
                return; // trigger one event per month max
            }
        }

        // No event this month
        Debug.Log($"[Month {month}] No major event occurred.");
    }

    private void TriggerEvent(MonthlyEvent e, int month)
    {
        Debug.Log($"[Event] {e.eventName} — {e.description}");

        // Base impact
        float loss = e.impactAmount;

        // Apply to finances
        if (e.affectsIncome)
        {
            financeManager.currentIncome -= loss;
            Debug.Log($"Income reduced by ${loss} due to {e.eventName}");
        }
        else
        {
            financeManager.cashOnHand -= loss;
            Debug.Log($"Unexpected expense of ${loss} due to {e.eventName}");
        }

        // Attempt insurance coverage
        float payout = insuranceManager.HandleEvent(e.coveredBy, Random.Range(10f, 40f));

        // Feedback to player
        string msg = $"<b>{e.eventName}</b>\n{e.description}\n" +
                     $"Impact: -${loss:F0}\n" +
                     (payout > 0 ? $"Insurance covered ${payout:F0}!" : "No coverage available.");

        uiManager?.ShowEventPopup(msg);
        uiManager?.UpdateMoneyText(financeManager.cashOnHand);

        Debug.Log($"[EventManager] {e.eventName} processed (Month {month}).");
    }
}
