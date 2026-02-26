using System.Collections.Generic;
using UnityEngine;
using static GameManager;
using static InsuranceManager;

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

        public enum LossCalculationType
        {
            AssetValue,
            CashOnHand,
            FixedAmount
        }

        public LossCalculationType lossType = LossCalculationType.AssetValue;
        public float fixedLossAmount = 0f;

    }

    [SerializeField] private int maxEventsPerMonth = 2;


    [Header("Possible Events")]
    public List<MonthlyEvent> allEvents = new List<MonthlyEvent>();

    public List<ResolvedEvent> GenerateMonthlyEvents(int month)
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Simulation)
        {
            Debug.LogWarning("Attempted to generate events outside Simulation phase.");
            return new List<ResolvedEvent>();
        }

        int triggeredEventCount = 0;
        float monthlyDamageCap =
            GameManager.Instance.financeManager.CashOnHand *
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

            float adjustedProbability = ev.probability;

            var forecast = GameManager.Instance.GetCurrentForecast();

            if (forecast != null &&
                forecast.categoryRiskMultiplier.TryGetValue(ev.category, out float multiplier))
            {
                adjustedProbability *= multiplier;
            }

            if (Random.value * 100f > adjustedProbability)
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
                float gained = ev.cashReward;

                if (gained > 0f)
                    GameManager.Instance.ApplyMoneyChange(
                    FinancialEntry.EntryType.EventReward,
                    ev.eventName,
                    gained,
                    true
                    );

                if (ev.momentumReward != 0f)
                    PlayerDataManager.Instance.ModifyMomentum(ev.momentumReward);

                if (ev.affectsIncome)
                {
                    GameManager.Instance.ApplyIncomeEffect(
                        ev.incomePercentChange,
                        ev.incomeEffectMonths
                    );
                }

                results.Add(new ResolvedEvent
                {
                    title = ev.eventName,
                    description = ev.description,
                    type = InsuranceManager.InsuranceType.None,
                    lossPercent = 0f,
                    actualMoneyChange = gained,
                    insurancePayout = 0f
                });

                continue;
            }

            float intendedLoss = Random.Range(ev.minLossPercent, ev.maxLossPercent + 1);
            float lossAmount = GameManager.Instance.financeManager.CashOnHand * (intendedLoss / 100f);

            // Let insurance handle payout AFTER loss
            float payout = 0f;
            float finalLoss = 0f;

            if (ev.relatedInsurances != null && ev.relatedInsurances.Count > 0)
            {
                foreach (var insuranceType in ev.relatedInsurances)
                {
                    InsuranceManager.InsuranceResult result =
                        GameManager.Instance.insuranceManager
                            .HandleEvent(insuranceType, intendedLoss, ev.lossType, ev.fixedLossAmount);

                    payout += result.payout;
                    finalLoss = result.finalLoss; // loss already applied inside InsuranceManager

                    if (result.waitingPeriodBlocked)
                        Debug.Log("Claim blocked: waiting period.");

                    if (result.lapsedBlocked)
                        Debug.Log("Claim blocked: policy lapsed.");
                }
            }

            if (payout > 0f)
            {
                GameManager.Instance.ApplyMoneyChange(
                    FinancialEntry.EntryType.InsurancePayout,
                    "Insurance Payout",
                    payout,
                    true
                );
            }

            results.Add(new ResolvedEvent
            {
                title = ev.eventName,
                description = ev.description,
                type = InsuranceManager.InsuranceType.None,
                lossPercent = intendedLoss,
                actualMoneyChange = -finalLoss,
                insurancePayout = payout
            });

            if (ev.affectsIncome)
            {
                GameManager.Instance.ApplyIncomeEffect(
                    ev.incomePercentChange,
                    ev.incomeEffectMonths
                );
            }
        }

        return results;
    }

    private float CalculateLossAmount(MonthlyEvent ev, float lossPercent)
    {
        switch (ev.lossType)
        {
            case MonthlyEvent.LossCalculationType.CashOnHand:
                return GameManager.Instance.financeManager.CashOnHand * (lossPercent / 100f);

            case MonthlyEvent.LossCalculationType.AssetValue:
                float assetValue = GameManager.Instance.financeManager
                    .GetAssetValue(ev.relatedInsurances.Count > 0
                    ? ev.relatedInsurances[0]
                    : InsuranceManager.InsuranceType.None);
                return assetValue * (lossPercent / 100f);

            case MonthlyEvent.LossCalculationType.FixedAmount:
                return ev.fixedLossAmount;

            default:
                return 0f;
        }
    }
}
