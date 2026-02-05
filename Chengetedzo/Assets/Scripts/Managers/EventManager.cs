using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class EventManager : MonoBehaviour
{
    [System.Serializable]
    public class MonthlyEvent
    {
        public enum AssetRequirement
        {
            None,
            House,
            Motor,
            Crops,
            Livestock
        }

        public AssetRequirement requiredAsset;

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

    public List<ResolvedEvent> GenerateMonthlyEvents(int month)
    {
        List<ResolvedEvent> results = new();

        Season currentSeason = GameManager.Instance.GetSeasonForMonth(month);

        var eligibleEvents = allEvents.FindAll(e =>
            e.season == currentSeason &&
            GameManager.Instance.insuranceManager.PlayerMeetsRequirement(
                GameManager.Instance.insuranceManager.GetPlan(e.relatedInsurance)
            )
        );

        foreach (var ev in eligibleEvents)
        {
            int roll = Random.Range(0, 100);
            if (roll > ev.probability)
                continue;

            float lossPercent =
                Random.Range(ev.minLossPercent, ev.maxLossPercent + 1);

            bool ownsRequiredAsset = ev.requiredAsset switch
            {
                MonthlyEvent.AssetRequirement.None => true,
                MonthlyEvent.AssetRequirement.House => GameManager.Instance.financeManager.assets.hasHouse,
                MonthlyEvent.AssetRequirement.Motor => GameManager.Instance.financeManager.assets.hasMotor,
                MonthlyEvent.AssetRequirement.Crops => GameManager.Instance.financeManager.assets.hasCrops,
                MonthlyEvent.AssetRequirement.Livestock => GameManager.Instance.financeManager.assets.hasLivestock,
                _ => false
            };

            if (!ownsRequiredAsset)
                continue;

            results.Add(new ResolvedEvent
            {
                title = ev.eventName,
                description = ev.description,
                type = ev.relatedInsurance,
                lossPercent = lossPercent
            });

            if (GameManager.Instance.monthlyDamageTaken >=
                GameManager.Instance.maxMonthlyDamagePercent)
                break;
        }

        return results;
    }
}
