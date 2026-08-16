using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ResolvedEvent
{
    public string title;
    public string description;
    public GameManager.AssetRequirement destroysAsset;

    public float moneyChange;
    public bool isReward;
    public float incomePercentChange;
    public int incomeDurationMonths;

    public Sprite icon;

    public InsuranceManager.InsuranceType type;

    public float lossPercent;
    public float insurancePayout;
    public float intendedLoss;
    public bool pendingClaimDecision;
    public float claimPayout;
    public float claimDeductible;

    public bool affectsExpenses;
    public string expenseCategoryName;
    public float expenseFlatChange;
    public int expenseEffectMonths;

    public bool hasChoices;
    public List<EventData.ChoiceOption> choices;
    public string senderName;
    public string senderRelation;
    public EventPool pool;
    public bool isFamilyPrompt;
    public ExpenseCategory familyPromptCategory;

    public bool isGoalPrompt;

    public bool schoolFeesPrompt;

    // Insurance claim denial, surfaced to the player instead of a silent $0 payout.
    // Gate any message on the player actually holding the policy (see InsuranceManager.
    // InsuranceResult.isSubscribed) - these two bools alone are not a safe gate for
    // waiting period, since an unsubscribed plan can still trip that flag.
    public bool claimDeniedWaitingPeriod;
    public bool claimDeniedLapsed;
    public int monthsPaid;
    public int waitingPeriodMonths;
    public string planName;
}