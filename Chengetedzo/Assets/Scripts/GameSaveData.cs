using System;
using System.Collections.Generic;

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
    public float familyMorale;
    public float socialMorale;

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
    public bool burialSocietyUnlocked;

    public bool isGuidedMode;
    public int profileType;   // (int)GameManager.ProfileType — which family's voices to use

    public bool goalBuilt;
    public bool goalReachedOnce;
    public int goalMonthsSinceOffer;
    public int goalMilestoneReached;
    public int freeGoalIndex;
    public float freeGoalTarget;
    public int goalBuiltMonth;

    // Mentor-memory flags — flavour only, but without persistence they re-fire after
    // every resume.
    public int mentorMemory_familyStrainStreak;
    public bool mentorMemory_familyStrainMentioned;
    public bool mentorMemory_communityHighMentioned;
    public bool mentorMemory_communityLowMentioned;
    public bool mentorMemory_goalBuiltMentioned;
    public bool mentorMemory_scarAckPending;

    public int savingsStreak;
    public int overBudgetStreak;
    public bool patternWarningIssued;
    public bool recoveryAcknowledged;
    public int lastMomentumZone;
    public float previousMomentum;
    public int monthsSinceMajorEvent;
    public float eventPressure;

    public int originalAdults;

    // Current household size (A3.6, pre-existing bug fix) — distinct from
    // originalAdults; without this a resumed game resurrects dead family members.
    public int currentAdults;
    public int currentChildren;

    // Setup block (A3.5) — REQUIRED for Free Mode resume (setupData/financeManager's
    // base fields are plain fields that don't survive an app restart on their own);
    // also makes guided resumes self-sufficient without re-running ApplyProfile.
    public int setupAdults;
    public int setupChildren;
    public bool setupIsIncomeStable;
    public int setupHousing;   // (int)HousingType
    public bool setupOwnsCar;
    public bool setupHasSchoolFees;
    public float setupSchoolFeesAmount;
    public float setupMinIncome;
    public float setupMaxIncome;
    public float setupHouseValue;

    public float financeRentCost;
    public float financeHouseMaintenanceCost;
    public float financeGroceries;
    public float financeTransport;
    public float financeUtilities;

    public bool assetHasHouse;
    public bool assetHasMotor;
    public bool assetHasCrops;
    public bool assetHasLivestock;
    public float houseInsuredValue;
    public float motorInsuredValue;
    public float cropsInsuredValue;
    public float livestockInsuredValue;

    public List<InsurancePlanSaveData> insurancePlans = new List<InsurancePlanSaveData>();
    public List<IncomeEffectSaveData> incomeEffects = new List<IncomeEffectSaveData>();
    public List<ExpenseEffectSaveData> expenseEffects = new List<ExpenseEffectSaveData>();
    public List<GameManager.CategoryState> categoryStates = new();

    [Serializable]
    public class IncomeEffectSaveData
    {
        public float reductionPercent;
        public int remainingMonths;
    }

    [Serializable]
    public class ExpenseEffectSaveData
    {
        public int category; // store as int to avoid enum serialization issues
        public float flatIncrease;
        public int remainingMonths;
    }

    [Serializable]
    public class InsurancePlanSaveData
    {
        public InsuranceManager.InsuranceType type;
        public bool isSubscribed;
        public bool isLapsed;
        public int monthsPaid;
        public int missedPayments;
    }

    public List<MonthSnapshotSaveData> snapshots = new List<MonthSnapshotSaveData>();

    [Serializable]
    public class MonthSnapshotSaveData
    {
        public int month;
        public float income;
        public float expenses;
        public float cashOnHand;
        public float savingsBalance;
        public float eventLoss;
        public bool hadEvent;
        public bool eventWasInsured;
    }
}