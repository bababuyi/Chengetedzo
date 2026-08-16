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
    public string senderName;
    public string senderRelation;
    public List<ChoiceOption> choices;

    [System.Serializable]
    public class ChoiceOption
    {
        public string label;
        public string resultDescription;
        public float moneyChange;
        public float momentumChange;
        public float moraleChange;
        public string moraleType;
        public float incomePercentChange;
        public int incomeEffectMonths;
        public bool affectsLoan;
        public float borrowingPowerChange;
        public string grantsAsset;

        // If set (not None) and the player currently has active, claimable cover of this
        // type, ApplyEventChoice routes the loss through CalculateClaim before applying
        // moneyChange, so insurance actually reduces the cost and shows as its own
        // InsurancePayout ledger line - previously choice events could never claim at all.
        public InsuranceManager.InsuranceType coveredBy = InsuranceManager.InsuranceType.None;
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
    public AssetRequirement destroysAsset = AssetRequirement.None;

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

    // Secondary policies this same event also touches, evaluated in order after the
    // primary. Each gets its own coverage limit, deductible and waiting-period check via
    // its own InsurancePlan - HandleEvent walks this list. A secondary only pays the
    // shortfall the primary (and any earlier secondary) left on this event's rawLoss, so
    // stacked cover never sums past the loss itself. Only the primary is ever a player
    // claim decision; secondaries resolve silently. Replaces the old hardcoded
    // Funeral-pays-BurialSociety-too special case in HandleEvent - that pairing is now
    // just data (see Death of Family Member / Elderly Family Member Death).
    public List<InsuranceType> additionalCoveredBy = new List<InsuranceType>();

    // Gates the Personal Accident/Health income benefit (see GameManager.ApplyIncomeEffect's
    // healthRelated parameter) for events whose income loss should count as health-related
    // even though neither the primary insuranceType nor any additionalCoveredBy entry is
    // Health/PersonalAccident - e.g. Breadwinner Death, primary cover Education (the $1000
    // education cap absorbs the lump sum, so PersonalAccident used to be listed in
    // additionalCoveredBy purely as a side-channel to switch this benefit on, with no
    // payout of its own). Most events don't need this: if the primary or a secondary cover
    // actually is Health/PersonalAccident, EventManager.IsHealthRelated already catches it.
    public bool grantsIncomeBenefit;

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
    [Tooltip("Who this event involves.")]
    public FamilyMemberType familyMemberType = FamilyMemberType.None;

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
    Opportunity,
    Choice
}

public enum FamilyMemberType
{
    None,
    AdultEarner,
    Grandparent,
    Child
}

