using System.Collections.Generic;
using UnityEngine;
using static GameManager;
using static InsuranceManager;

[CreateAssetMenu(fileName = "New Event", menuName = "Chengetedzo/Event")]
public class EventData : ScriptableObject
{
    [Header("Basic Info")]
    public string eventName;

    [TextArea(3, 6)]
    public string description;

    [TextArea(3, 6)]
    public string financialLesson;

    [Header("Classification")]
    public ForecastManager.ForecastCategory category;
    public EventPool pool;

    public AssetRequirement requiredAsset;

    [Header("UI Icon")]
    public Sprite icon;

    [Header("Probability")]
    [Range(0, 100)]
    public float probability;

    [Range(1, 100)]
    public int weight = 10;

    [Header("Financial Impact")]
    [Range(0f, 100f)]
    public float minLossPercent;

    [Range(0f, 100f)]
    public float maxLossPercent;

    [Header("Income Effects")]
    public bool affectsIncome;
    public float incomePercentChange;
    public int incomeEffectMonths;

    [Header("Insurance")]
    public InsuranceType insuranceType;

    [Range(0f, 1f)]
    public float insuranceCoverage;

    [Header("Outcome")]
    public EventOutcomeType outcomeType;

    [Header("Positive Rewards")]
    public float cashReward;
    public float momentumReward;

    [Header("Season")]
    public Season season = Season.Any;

    [Header("Severity")]
    public EventSeverity severity;

    public LossCalculationType lossType;
    public float fixedLossAmount;

    [Header("Event Chain")]
    public bool startsChain;

    [Tooltip("Events that can occur after this one")]
    public List<EventData> followUpEvents;

    [Range(0f, 1f)]
    public float followUpChance = 0.5f;

    [Tooltip("Months before follow-up event can occur")]
    public int followUpDelay = 1;
}

public enum EventSeverity
{
    Minor,
    Moderate,
    Major
}

public enum EventOutcomeType
{
    Negative,
    Positive
}

public enum LossCalculationType
{
    AssetValue,
    CashOnHand,
    FixedAmount
}

public enum EventPool
{
    Weather,
    Agriculture,
    Economic,
    Health,
    Crime,
    Opportunity
}

