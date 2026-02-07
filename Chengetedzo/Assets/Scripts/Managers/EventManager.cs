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

    [Header("Possible Events")]
    public List<MonthlyEvent> allEvents = new List<MonthlyEvent>();

    public List<ResolvedEvent> GenerateMonthlyEvents(int month)
    {
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

            // ---------------- POSITIVE EVENT ----------------
            if (ev.outcomeType == MonthlyEvent.EventOutcomeType.Positive)
            {
                if (ev.cashReward > 0f)
                    GameManager.Instance.financeManager.cashOnHand += ev.cashReward;

                if (ev.momentumReward != 0f)
                    PlayerDataManager.Instance.financialMomentum += ev.momentumReward;

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

            // ---------------- NEGATIVE EVENT ----------------
            //if (ev.outcomeType == MonthlyEvent.EventOutcomeType.Negative)
            //{
              //  bool hasAnyRelevantInsurance = false;

                //foreach (var insurance in ev.relatedInsurances)
                //{
                  //  var plan = GameManager.Instance.insuranceManager.GetPlan(insurance);
                    //if (plan != null && plan.isSubscribed && !plan.isLapsed)
                    //{
                      //  hasAnyRelevantInsurance = true;
                        //break;
                    //}
                //}

                //if (!hasAnyRelevantInsurance)
                  //  continue;
            //}

            float lossPercent =
                Random.Range(ev.minLossPercent, ev.maxLossPercent + 1);

            foreach (var insurance in ev.relatedInsurances)
            {
                results.Add(new ResolvedEvent
                {
                    title = ev.eventName,
                    description = ev.description,
                    type = insurance,
                    lossPercent = lossPercent
                });
            }

            if (ev.affectsIncome)
            {
                GameManager.Instance.ApplyIncomeEffect(
                    ev.incomePercentChange,
                    ev.incomeEffectMonths
                );
            }

            float maxAllowedLoss =
            GameManager.Instance.financeManager.cashOnHand *
            GameManager.Instance.maxMonthlyDamagePercent;

            if (GameManager.Instance.monthlyDamageTaken >=
            GameManager.Instance.financeManager.cashOnHand *
            GameManager.Instance.maxMonthlyDamagePercent)
            {
                break;
            }
        }

        return results;
    }
}
