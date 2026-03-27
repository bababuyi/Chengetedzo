using System.Collections.Generic;
using UnityEngine;
using static ForecastManager;
using static GameManager;
using static InsuranceManager;

[CreateAssetMenu(fileName = "New Event", menuName = "Chengetedzo/Event")]
public class EventData : ScriptableObject
{
    [Header("Choice System")]
    public bool hasChoices;
    public List<ChoiceOption> choices;

    [System.Serializable]
    public class ChoiceOption
    {
        public string label;               // e.g. "Help them"
        public string resultDescription;   // shown after pick
        public float moneyChange;          // negative = cost, positive = gain
        public float momentumChange;
        public float incomePercentChange;
        public int incomeEffectMonths;   // -1 = permanent
        public bool affectsLoan;
        public float borrowingPowerChange;
    }

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

    [Header("Outcome")]
    public EventOutcomeType outcomeType;

    [Header("Positive Rewards")]
    public float cashReward;
    public float momentumReward;

    [Header("Season")]
    public Season season = Season.Any;
    public ForecastSignal signal;

    [Header("Severity")]
    public EventSeverity severity;

    [Header("Household Effects")]
    public bool affectsHousehold;
    public int adultsLost;
    public int childrenLost;

    [Header("Expense Effects")]
    public bool affectsExpenses;
    public ExpenseCategory expenseCategory;
    public float expenseFlatChange;
    public int expenseEffectMonths; // -1 = permanent

    public LossCalculationType lossType;
    public float fixedLossAmount;

    [Header("Loan Effects")]
    public bool affectsLoan;
    public float borrowingPowerChange;

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

