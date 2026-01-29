using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class EventManager : MonoBehaviour
{
    [System.Serializable]
    public class MonthlyEvent
    {
        public string eventName;
        public string description;

        [Range(0, 100)]
        public int probability;

        public InsuranceManager.InsuranceType relatedInsurance;
        public int minLossPercent;
        public int maxLossPercent;
        public Season season;
    }

    [Header("Possible Events")]
    public List<MonthlyEvent> allEvents = new List<MonthlyEvent>();

    public void CheckForMonthlyEvent(int month)
    {
        Season currentSeason = GameManager.Instance.GetSeasonForMonth(month);

        var seasonalEvents = allEvents.FindAll(e =>
        e.season == currentSeason &&
        GameManager.Instance.insuranceManager.PlayerMeetsRequirement(
        GameManager.Instance.insuranceManager.GetPlan(e.relatedInsurance)
        )
        );

        if (seasonalEvents.Count == 0)
        {
            Debug.Log($"[Event] No events defined for {currentSeason}.");
            return;
        }

        var selectedEvent = seasonalEvents[Random.Range(0, seasonalEvents.Count)];

        int roll = Random.Range(0, 100);

        if (roll <= selectedEvent.probability)
        {
            Debug.Log($"[Event] {selectedEvent.eventName} OCCURRED!");

            int lossPercent = Random.Range(
                selectedEvent.minLossPercent,
                selectedEvent.maxLossPercent + 1
            );

            Debug.Log($"[Event] Loss severity: {lossPercent}%");

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowEventPopup(
                    selectedEvent.eventName,
                    selectedEvent.description
                );
            }

            // Apply loss + insurance
            GameManager.Instance.insuranceManager.HandleEvent(
                selectedEvent.relatedInsurance,
                lossPercent
            );
        }
        else
        {
            Debug.Log($"[Event] Month {month} ({currentSeason}): No major incidents.");
        }
        Debug.Log($"[Event] Checking for events in month {month}");
    }
}
