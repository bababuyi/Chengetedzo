using System.Collections.Generic;
using UnityEngine;
using static ForecastLines;
using static GameManager;
using static InsuranceManager;
using static UnityEngine.ParticleSystem;

public class EventManager : MonoBehaviour
{
    [Header("Event Pressure System")]

    [SerializeField] private float eventPressure = 0f;

    [SerializeField] private float pressureIncreasePerMonth = 6f;
    [SerializeField] private float maxPressure = 60f;

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
                return 120;

            default:
                return 20;
        }
    }

    public List<ResolvedEvent> GenerateMonthlyEvents(int month)
    {
        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Simulation)
        {
            Debug.LogWarning("Attempted to generate events outside Simulation phase.");
            return new List<ResolvedEvent>();
        }

        eventPressure = Mathf.Min(eventPressure + pressureIncreasePerMonth, maxPressure);

        Debug.Log(
            $"[PRESSURE] Month: {month} | Pressure: {eventPressure:F1}/{maxPressure}"
        );

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

            if (GetEventCost(ev) > remainingEventBudget)
                continue;

            if (!PlayerOwnsRequiredAsset(ev))
                continue;

            ResolveEvent(ev, month, results, ref disasterCount);

            remainingEventBudget -= GetEventCost(ev);
            triggeredEventCount++;
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

            Debug.Log($"[EVENT] {ev.eventName} | Severity: {ev.severity} | Pool: {ev.pool}");

            if (eventsTriggeredThisYear.Contains(ev))
                continue;

            int eventCost = GetEventCost(ev);

            if (eventCost > remainingEventBudget)
                continue;

            eventsTriggeredThisYear.Add(ev);
            float pressureMultiplier = Mathf.Lerp(1f, 1.6f, eventPressure / maxPressure);
            float adjustedProbability = ev.probability * pressureMultiplier;
            if (ev.severity == EventSeverity.Major)
                adjustedProbability *= 0.75f;

            var forecast = GameManager.Instance.GetCurrentForecast();

            if (forecast != null)
            {
                // Category influence
                if (forecast.categoryRiskMultiplier.TryGetValue(ev.category, out float multiplier))
                    adjustedProbability *= multiplier;

                // Signal influence
                var signal = ev.signal;

                if (forecast.signalRiskMultiplier.TryGetValue(signal, out float signalMultiplier))
                    adjustedProbability *= signalMultiplier;

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

                GameManager.AssetRequirement.House =>
                    GameManager.Instance.financeManager.assets.hasHouse,

                GameManager.AssetRequirement.Motor =>
                    GameManager.Instance.financeManager.assets.hasMotor,

                GameManager.AssetRequirement.Crops =>
                    GameManager.Instance.financeManager.assets.hasCrops,

                GameManager.AssetRequirement.Livestock =>
                    GameManager.Instance.financeManager.assets.hasLivestock,

                GameManager.AssetRequirement.CropsOrLivestock =>
                    GameManager.Instance.financeManager.assets.hasCrops ||
                    GameManager.Instance.financeManager.assets.hasLivestock,

                _ => false
            };

            if (!ownsRequiredAsset)
                continue;

            remainingEventBudget -= eventCost;

            triggeredEventCount++;
            if (ev.severity != EventSeverity.Minor)
                eventPressure = 0f;

            // Household changes
            if (ev.affectsHousehold)
            {
                for (int i = 0; i < ev.adultsLost; i++)
                    PlayerDataManager.Instance.RemoveAdult();
                for (int i = 0; i < ev.childrenLost; i++)
                    PlayerDataManager.Instance.RemoveChild();

                Debug.Log($"[HOUSEHOLD] Event: {ev.eventName} | " +
                          $"Adults lost: {ev.adultsLost} | Children lost: {ev.childrenLost} | " +
                          $"Remaining adults: {PlayerDataManager.Instance.Adults}");
            }

            // Expense effects
            if (ev.affectsExpenses)
            {
                GameManager.Instance.ApplyExpenseEffect(
                    ev.expenseCategory,
                    ev.expenseFlatChange,
                    ev.expenseEffectMonths
                );
            }
            if (ev.affectsLoan)
                GameManager.Instance.loanManager?.ModifyBorrowingPower(ev.borrowingPowerChange);

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
                    type = ev.insuranceType,
                    lossPercent = 0f,
                    moneyChange = gained,
                    insurancePayout = 0f
                });

                continue;
            }

            float lossPercent = Random.Range(ev.minLossPercent, ev.maxLossPercent + 1);

            float cash = GameManager.Instance.financeManager.CashOnHand;

            if (cash < 2000)
                lossPercent *= 0.6f;
            else if (cash < 4000)
                lossPercent *= 0.8f;

            float intendedLoss = GameManager.Instance.financeManager
                .CalculateEventLoss(ev, lossPercent);

            Debug.Log(
            $"[LOSS CALC] Event: {ev.eventName} | " +
            $"Severity: {ev.severity} | " +
            $"LossPercent: {lossPercent:F1}% | " +
            $"CalculatedLoss: {intendedLoss:F0} | " +
            $"CashBefore: {GameManager.Instance.financeManager.CashOnHand:F0}"
            );

            float payout = 0f;
            float finalLoss = 0f;

            if (ev.insuranceType != InsuranceType.None)
            {
                InsuranceManager.InsuranceResult result =
                GameManager.Instance.insuranceManager
                .HandleEvent(ev.insuranceType, intendedLoss);

                payout += result.payout;
                finalLoss = result.finalLoss;
                Debug.Log(
                $"[INSURANCE] Event: {ev.eventName} | " +
                $"Type: {ev.insuranceType} | " +
                $"Payout: {result.payout:F0} | " +
                $"FinalPlayerLoss: {result.finalLoss:F0}"
                );

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
                lossPercent = lossPercent,
                moneyChange = -finalLoss,
                insurancePayout = payout
            });

            Debug.Log(
            $"[FINANCIAL RESULT] Event: {ev.eventName} | " +
            $"PlayerLoss: {finalLoss:F0} | " +
            $"InsurancePayout: {payout:F0} | " +
            $"NetImpact: {-finalLoss + payout:F0}"
            );

            TryScheduleFollowUp(ev, month);

            if (ev.affectsIncome)
            {
                GameManager.Instance.ApplyIncomeEffect(
                    ev.incomePercentChange,
                    ev.incomeEffectMonths
                );
            }

            Debug.Log(
                $"[INCOME EFFECT] Event: {ev.eventName} | " +
                $"IncomeChange: {ev.incomePercentChange}% | " +
                $"Duration: {ev.incomeEffectMonths} months"
            );
        }

        if (triggeredEventCount == 0 && Random.value < 0.35f + (eventPressure / maxPressure) * 0.4f)
        {
            var fallback = GetWeightedEvent(eligibleEvents.FindAll(e => !eventsTriggeredThisYear.Contains(e)));

            if (fallback != null && Random.value < 0.5f)
            {
                ResolveEvent(fallback, month, results, ref disasterCount);
                eventPressure = 0f;
            }
        }

        return results;
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

        float adjustedChance = ev.followUpChance;

        switch (ev.severity)
        {
            case EventSeverity.Minor:
                adjustedChance *= 0.7f;
                break;

            case EventSeverity.Moderate:
                adjustedChance *= 1f;
                break;

            case EventSeverity.Major:
                adjustedChance *= 1.25f;
                break;
        }

        adjustedChance = Mathf.Clamp01(adjustedChance);

        if (Random.value > adjustedChance)
            return;

        EventData next = ev.followUpEvents[Random.Range(0, ev.followUpEvents.Count)];

        PendingEvent pending = new PendingEvent
        {
            eventData = next,
            monthToTrigger = currentMonth + ev.followUpDelay
        };

        pendingEvents.Add(pending);
        Debug.Log(
            $"[CHAIN EVENT] {ev.eventName} triggered follow-up: {next.eventName} " +
            $"in {ev.followUpDelay} months"
        );
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

            Debug.Log(
            $"[POSITIVE EVENT] {ev.eventName} | " +
            $"Reward: {gained:F0} | " +
            $"Momentum: {ev.momentumReward} | " +
            $"IncomeChange: {ev.incomePercentChange}% for {ev.incomeEffectMonths} months"
            );

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
                type = ev.insuranceType,
                lossPercent = 0f,
                moneyChange = gained,
                insurancePayout = 0f
            });

            return;
        }

        float lossPercent = Random.Range(ev.minLossPercent, ev.maxLossPercent + 1);

float intendedLoss = GameManager.Instance.financeManager
    .CalculateEventLoss(ev, lossPercent);


        float income = GameManager.Instance.financeManager.CashOnHand;

        if (income < 2000)
            intendedLoss *= 0.6f;
        else if (income < 4000)
            intendedLoss *= 0.8f;

        var result = GameManager.Instance.insuranceManager
        .HandleEvent(ev.insuranceType, intendedLoss);

        float payout = result.payout;
        float finalLoss = result.finalLoss;

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
            type = ev.insuranceType,
            lossPercent = lossPercent,
            moneyChange = -finalLoss,
            insurancePayout = payout
        });

        TryScheduleFollowUp(ev, month);

        if (ev.affectsHousehold)
        {
            for (int i = 0; i < ev.adultsLost; i++)
                PlayerDataManager.Instance.RemoveAdult();
            for (int i = 0; i < ev.childrenLost; i++)
                PlayerDataManager.Instance.RemoveChild();
        }

        if (ev.affectsExpenses)
        {
            GameManager.Instance.ApplyExpenseEffect(
                ev.expenseCategory,
                ev.expenseFlatChange,
                ev.expenseEffectMonths
            );
        }
        if (ev.affectsLoan)
            GameManager.Instance.loanManager?.ModifyBorrowingPower(ev.borrowingPowerChange);
    }

    private bool PlayerOwnsRequiredAsset(EventData ev)
    {
        return ev.requiredAsset switch
        {
            GameManager.AssetRequirement.None => true,

            GameManager.AssetRequirement.House =>
                GameManager.Instance.financeManager.assets.hasHouse,

            GameManager.AssetRequirement.Motor =>
                GameManager.Instance.financeManager.assets.hasMotor,

            GameManager.AssetRequirement.Crops =>
                GameManager.Instance.financeManager.assets.hasCrops,

            GameManager.AssetRequirement.Livestock =>
                GameManager.Instance.financeManager.assets.hasLivestock,

            GameManager.AssetRequirement.CropsOrLivestock =>
                GameManager.Instance.financeManager.assets.hasCrops ||
                GameManager.Instance.financeManager.assets.hasLivestock,

            _ => false
        };
    }

    public void ResetAll()
    {
        pendingEvents.Clear();
        eventsTriggeredThisYear.Clear();
        remainingEventBudget = monthlyEventBudget;
    }
}
