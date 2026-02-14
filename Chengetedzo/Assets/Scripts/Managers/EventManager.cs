using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class EventManager : MonoBehaviour
{
    [System.Serializable]

    public class MonthlyEvent
    {
        public ForecastManager.ForecastCategory category;

        public GameManager.AssetRequirement requiredAsset;

        public string eventName;
        public string description;

        [Range(0, 100f)]
        public float probability;

        [Header("Insurance Impact")]
        public List<InsuranceManager.InsuranceType> relatedInsurances
         = new List<InsuranceManager.InsuranceType>();

        [Header("Negative Event Impact")]
        public int minLossPercent;
        public int maxLossPercent;

        [Header("Seasons")]
        public Season season;

        [Header("Income Impact")]
        public bool affectsIncome = false;

        // Positive = increase, Negative = decrease
        [Range(-100f, 100f)]
        public float incomePercentChange = 0f;

        // Duration in months (0 = permanent)
        public int incomeEffectMonths = 0;

        public enum EventOutcomeType
        {
            Negative,
            Positive
        }

        [Header("Outcome")]
        public EventOutcomeType outcomeType = EventOutcomeType.Negative;

        [Header("Positive Rewards")]
        public float cashReward = 0f;
        public float momentumReward = 0f;
    }

    [SerializeField] private int maxEventsPerMonth = 3;


    [Header("Possible Events")]
    public List<MonthlyEvent> allEvents = new List<MonthlyEvent>();

    public List<ResolvedEvent> GenerateMonthlyEvents(int month)
    {
        int triggeredEventCount = 0;
        float monthlyDamageCap =
            GameManager.Instance.financeManager.cashOnHand *
            GameManager.Instance.maxMonthlyDamagePercent;

        List<ResolvedEvent> results = new();

        Season currentSeason = GameManager.Instance.GetSeasonForMonth(month);

        var eligibleEvents = allEvents.FindAll(e =>
        {
            if (e.season != Season.Any && e.season != currentSeason)
                return false;

            return true;
        });

        foreach (var ev in eligibleEvents)
        {
            if (triggeredEventCount >= maxEventsPerMonth)
                break;

            if (Random.value * 100f > ev.probability)
                continue;

            bool ownsRequiredAsset = ev.requiredAsset switch
            {
                GameManager.AssetRequirement.None => true,
                GameManager.AssetRequirement.House => GameManager.Instance.financeManager.assets.hasHouse,
                GameManager.AssetRequirement.Motor => GameManager.Instance.financeManager.assets.hasMotor,
                GameManager.AssetRequirement.Crops => GameManager.Instance.financeManager.assets.hasCrops,
                GameManager.AssetRequirement.Livestock => GameManager.Instance.financeManager.assets.hasLivestock,
                _ => false
            };

            if (!ownsRequiredAsset)
                continue;
            triggeredEventCount++;

            // ---------------- POSITIVE EVENT ----------------
            if (ev.outcomeType == MonthlyEvent.EventOutcomeType.Positive)
            {
                if (ev.cashReward > 0f)
                    GameManager.Instance.financeManager.cashOnHand += ev.cashReward;

                if (ev.momentumReward != 0f)
                    PlayerDataManager.Instance.ModifyMomentum(ev.momentumReward);

                if (ev.affectsIncome)
                {
                    GameManager.Instance.ApplyIncomeEffect(
                        ev.incomePercentChange,   // can be negative = boost
                        ev.incomeEffectMonths
                    );
                }

                results.Add(new ResolvedEvent
                {
                    title = ev.eventName,
                    description = ev.description,
                    type = InsuranceManager.InsuranceType.None,
                    lossPercent = 0f
                });

                continue;
            }

            float lossPercent =
                Random.Range(ev.minLossPercent, ev.maxLossPercent + 1);

            InsuranceManager.InsuranceType insuranceType =
            ev.relatedInsurances.Count > 0
            ? ev.relatedInsurances[0]
            : InsuranceManager.InsuranceType.None;

            results.Add(new ResolvedEvent
            {
                title = ev.eventName,
                description = ev.description,
                type = insuranceType,
                lossPercent = lossPercent
            });

            float estimatedLoss =
            GameManager.Instance.financeManager.cashOnHand * (lossPercent / 100f);

            GameManager.Instance.monthlyDamageTaken += estimatedLoss;

            if (GameManager.Instance.monthlyDamageTaken >= monthlyDamageCap)
                break;


            if (ev.affectsIncome)
            {
                GameManager.Instance.ApplyIncomeEffect(
                    ev.incomePercentChange,
                    ev.incomeEffectMonths
                );
            }

            //float maxAllowedLoss =
            //GameManager.Instance.financeManager.cashOnHand *
            //GameManager.Instance.maxMonthlyDamagePercent;

            //if (GameManager.Instance.monthlyDamageTaken >=
            //GameManager.Instance.financeManager.cashOnHand *
            //GameManager.Instance.maxMonthlyDamagePercent)
            //{
              //  break;
            //}
        }

        return results;
    }
}
