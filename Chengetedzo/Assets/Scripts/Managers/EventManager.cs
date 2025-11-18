using UnityEngine;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{
    [System.Serializable]
    public class MonthlyEvent
    {
        public string eventName;
        public string description;
        public InsuranceManager.InsuranceType relatedInsurance;
        public int minLossPercent;
        public int maxLossPercent;
    }

    [Header("Possible Events")]
    public List<MonthlyEvent> allEvents = new List<MonthlyEvent>();

    public void CheckForMonthlyEvent(int month)
    {
        if (Random.value < 0.4f) // 40% chance per month
        {
            var e = allEvents[Random.Range(0, allEvents.Count)];
            float lossPercent = Random.Range(e.minLossPercent, e.maxLossPercent + 1);

            float payout = GameManager.Instance.insuranceManager.HandleEvent(e.relatedInsurance, lossPercent);

            // Separate title and description for the popup
            string title = e.eventName;
            string description = $"{e.description}\n\nLoss: {lossPercent}%\nPayout: ${payout:F2}";

            // Correct method call — 3 parameters (last one optional)
            UIManager.Instance.ShowEventPopup(title, description, null);

            Debug.Log($"[Event] {e.eventName} occurred. Loss: {lossPercent}% | Payout: ${payout}");
        }
        else
        {
            Debug.Log($"[Event] Month {month}: No major incidents this month.");
        }
    }
}
