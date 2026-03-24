using System;

[Serializable]
public class GameSaveData
{
    public int currentMonth;

    public float cashOnHand;
    public float generalSavingsBalance;
    public float generalSavingsMonthly;

    public float yearIncome;
    public float yearExpenses;
    public float yearPremiums;
    public float yearPayouts;
    public float yearEventLosses;
    public float financialMomentum;

    public int totalUnexpectedEvents;
    public int insuredEventsCount;
    public float totalRawEventDamage;
    public float totalInsurancePayoutAmount;
    public int forcedLoanCount;
    public int monthsUnderFinancialPressure;
    public float loanBalance;
    public float borrowingPower;
    public float totalContributed;
    public int monthsContributed;
    public float repaymentRate;
    public int missedPayments;
    public int onTimePayments;
    public bool loanUnlocked;

    public int savingsStreak;
    public int overBudgetStreak;
    public bool patternWarningIssued;
    public bool recoveryAcknowledged;
    public int lastMomentumZone;
    public float previousMomentum;
    public int monthsSinceMajorEvent;
    public float eventPressure;
}