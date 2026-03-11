using System.Collections.Generic;
using UnityEngine;
using static GameManager;
using static InsuranceManager;

public class EventManager : MonoBehaviour
{
    /*[System.Serializable]

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

        public enum EventPool
        {
            Weather,
            Agriculture,
            Economic,
            Health,
            Crime,
            Opportunity
        }

        [Header("Event Pool")]
        public EventPool pool = EventPool.Weather;

        [Range(1, 100)]
        public int weight = 10;

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

    }*/

    [SerializeField] private int monthlyEventBudget = 100;
    private HashSet<EventData> eventsTriggeredThisYear = new();
    private List<PendingEvent> pendingEvents = new List<PendingEvent>();
    private int remainingEventBudget;

    [System.Serializable]
    public class PendingEvent
    {
        public EventData eventData;
        public int monthToTrigger;
    }

    [SerializeField] private int maxEventsPerMonth = 2;

    [Header("Possible Events")]
    [SerializeField] private EventDatabase eventDatabase;
    public EventDatabase EventDatabase => eventDatabase;

    private int GetEventCost(EventData ev)
    {
        switch (ev.severity)
        {
            case EventSeverity.Minor:
                return 20;

            case EventSeverity.Moderate:
                return 40;

            case EventSeverity.Major:
                return 80;

            default:
                return 20;
        }
    }

    public List<ResolvedEvent> GenerateMonthlyEvents(int month)
    {
        List<ResolvedEvent> results = new();

        if (month % 12 == 1)
        {
            eventsTriggeredThisYear.Clear();
        }

        remainingEventBudget = monthlyEventBudget;

        int disasterCount = 0;
        int triggeredEventCount = 0;

        var pending = GetPendingEventsForMonth(month);

        foreach (var ev in pending)
        {
            if (triggeredEventCount >= maxEventsPerMonth)
                break;

            ResolveEvent(ev, month, results, ref disasterCount);

            triggeredEventCount++;
        }

        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Simulation)
        {
            Debug.LogWarning("Attempted to generate events outside Simulation phase.");
            return new List<ResolvedEvent>();
        }

        Season currentSeason = GameManager.Instance.GetSeasonForMonth(month);

        if (eventDatabase == null || eventDatabase.events == null)
        {
            Debug.LogWarning("[EventManager] No EventDatabase assigned.");
            return results;
        }

        var eligibleEvents = eventDatabase.events.FindAll(e =>
        {
            if (e.season != Season.Any && e.season != currentSeason)
                return false;

            return true;
        });

        Dictionary<EventPool, List<EventData>> pools = new Dictionary<EventPool, List<EventData>>();

        foreach (var ev in eligibleEvents)
        {
            if (!pools.ContainsKey(ev.pool))
                pools[ev.pool] = new List<EventData>();

            pools[ev.pool].Add(ev);
        }

        var poolList = new List<KeyValuePair<EventPool, List<EventData>>>(pools);

        for (int i = 0; i < poolList.Count; i++)
        {
            int rand = Random.Range(i, poolList.Count);
            (poolList[i], poolList[rand]) = (poolList[rand], poolList[i]);
        }

        foreach (var pool in poolList)
        {
            if (triggeredEventCount >= maxEventsPerMonth)
                break;

            EventData ev = GetWeightedEvent(pool.Value);

            if (ev == null)
                continue;

            if (eventsTriggeredThisYear.Contains(ev))
                continue;

            int eventCost = GetEventCost(ev);

            if (eventCost > remainingEventBudget)
                continue;

            eventsTriggeredThisYear.Add(ev);

            float adjustedProbability = ev.probability;

            var forecast = GameManager.Instance.GetCurrentForecast();

            if (forecast != null &&
                forecast.categoryRiskMultiplier.TryGetValue(ev.category, out float multiplier))
            {
                adjustedProbability *= multiplier;
            }

            if (Random.value * 100f > adjustedProbability)
                continue;

            // Prevent disaster streaks
            if (ev.severity == EventSeverity.Major)
            {
                if (GameManager.Instance.monthsSinceMajorEvent <
                    GameManager.Instance.majorEventGraceMonths)
                    continue;

                if (disasterCount >= 1)
                    continue;

                disasterCount++;
            }

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

            remainingEventBudget -= eventCost;

            triggeredEventCount++;

            // ---------------- POSITIVE EVENT ----------------
            if (ev.outcomeType == EventOutcomeType.Positive)
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

            float income = GameManager.Instance.financeManager.CashOnHand;

            if (income < 2000)
                intendedLoss *= 0.6f;
            else if (income < 4000)
                intendedLoss *= 0.8f;

            float payout = 0f;
            float finalLoss = 0f;

            if (ev.insuranceType != InsuranceType.None)
            {
                InsuranceManager.InsuranceResult result =
                    GameManager.Instance.insuranceManager
                        .HandleEvent(ev.insuranceType, intendedLoss, ev.lossType, ev.fixedLossAmount);

                payout += result.payout;
                finalLoss = result.finalLoss;

                if (result.waitingPeriodBlocked)
                    Debug.Log("Claim blocked: waiting period.");

                if (result.lapsedBlocked)
                    Debug.Log("Claim blocked: policy lapsed.");
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

            TryScheduleFollowUp(ev, month);

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

    private float CalculateLossAmount(EventData ev, float lossPercent)
    {
        switch (ev.lossType)
        {
            case LossCalculationType.CashOnHand:
                return GameManager.Instance.financeManager.CashOnHand * (lossPercent / 100f);

            case LossCalculationType.AssetValue:
                float assetValue = GameManager.Instance.financeManager
                    .GetAssetValue(ev.insuranceType);

                return assetValue * (lossPercent / 100f);

            case LossCalculationType.FixedAmount:
                return ev.fixedLossAmount;

            default:
                return 0f;
        }
    }

    private List<EventData> GetPendingEventsForMonth(int currentMonth)
    {
        List<EventData> result = new List<EventData>();

        for (int i = pendingEvents.Count - 1; i >= 0; i--)
        {
            if (pendingEvents[i].monthToTrigger <= currentMonth)
            {
                result.Add(pendingEvents[i].eventData);
                pendingEvents.RemoveAt(i);
            }
        }

        return result;
    }

    private void TryScheduleFollowUp(EventData ev, int currentMonth)
    {
        if (!ev.startsChain)
            return;

        if (ev.followUpEvents == null || ev.followUpEvents.Count == 0)
            return;

        if (Random.value > ev.followUpChance)
            return;

        EventData next = ev.followUpEvents[Random.Range(0, ev.followUpEvents.Count)];

        PendingEvent pending = new PendingEvent
        {
            eventData = next,
            monthToTrigger = currentMonth + ev.followUpDelay
        };

        pendingEvents.Add(pending);
    }

    private EventData GetWeightedEvent(List<EventData> events)
    {
        if (events == null || events.Count == 0)
            return null;

        int totalWeight = 0;

        foreach (var e in events)
            totalWeight += e.weight;

        int roll = Random.Range(0, totalWeight);

        int cumulative = 0;

        foreach (var e in events)
        {
            cumulative += e.weight;

            if (roll < cumulative)
                return e;
        }

        return events[0];
    }

    private void ResolveEvent(EventData ev, int month, List<ResolvedEvent> results, ref int disasterCount)
    {
        // ---------------- POSITIVE EVENT ----------------
        if (ev.outcomeType == EventOutcomeType.Positive)
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
                type = InsuranceType.None,
                lossPercent = 0f,
                actualMoneyChange = gained,
                insurancePayout = 0f
            });

            return;
        }

        float intendedLoss = Random.Range(ev.minLossPercent, ev.maxLossPercent + 1);

        float income = GameManager.Instance.financeManager.CashOnHand;

        if (income < 2000)
            intendedLoss *= 0.6f;
        else if (income < 4000)
            intendedLoss *= 0.8f;

        float rawLoss = CalculateLossAmount(ev, intendedLoss);
        float cappedLoss = GameManager.Instance.ApplyMonthlyDamage(rawLoss);

        float payout = 0f;
        float finalLoss = cappedLoss;

        if (ev.insuranceType != InsuranceType.None)
        {
            var result = GameManager.Instance.insuranceManager
                .HandleEvent(ev.insuranceType, intendedLoss, ev.lossType, ev.fixedLossAmount);

            payout += result.payout;
            finalLoss = result.finalLoss;
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
            type = InsuranceType.None,
            lossPercent = intendedLoss,
            actualMoneyChange = -finalLoss,
            insurancePayout = payout
        });

        TryScheduleFollowUp(ev, month);
    }

    public void ResetAll()
    {
        pendingEvents.Clear();
        eventsTriggeredThisYear.Clear();
        remainingEventBudget = monthlyEventBudget;
    }
}
