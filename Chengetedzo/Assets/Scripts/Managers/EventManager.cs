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

        // Filter events for this season
        var seasonalEvents = allEvents.FindAll(e => e.season == currentSeason);

        if (seasonalEvents.Count == 0)
        {
            Debug.Log($"[Event] No events defined for {currentSeason}.");
            return;
        }

        if (Random.value < 0.4f)
        {
            var e = seasonalEvents[Random.Range(0, seasonalEvents.Count)];
            float lossPercent = Random.Range(e.minLossPercent, e.maxLossPercent + 1);

            float payout = GameManager.Instance.insuranceManager.HandleEvent(e.relatedInsurance, lossPercent);

            string title = e.eventName;
            string description =
                $"{e.description}\n\n" +
                $"Season: {currentSeason}\n" +
                $"Loss: {lossPercent}%\n" +
                $"Payout: ${payout:F2}";

            UIManager.Instance.ShowEventPopup(title, description);

            Debug.Log($"[Event] {e.eventName} ({currentSeason}). Loss: {lossPercent}% | Payout: ${payout}");
        }
        else
        {
            Debug.Log($"[Event] Month {month} ({currentSeason}): No major incidents.");
        }
    }
}
