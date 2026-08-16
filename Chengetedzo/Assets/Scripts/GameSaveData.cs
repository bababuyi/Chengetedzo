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

    // Two-channel loan system: Moneylender (Ndlovu, always available, expensive) and
    // Mukando (cheap, requires 3 months of contribution to build borrowing power).
    public LoanAccount moneylender;
    public LoanAccount mukando;
    public bool loanUnlocked;
    public bool mukandoJoined;
    public int mukandoConsecutiveMisses;
    public int mukandoRecoveryMonthsNeeded;

    public bool burialSocietyUnlocked;

    public bool isGuidedMode;
    public int profileType;

    public bool goalBuilt;
    public bool goalReachedOnce;
    public int goalMonthsSinceOffer;
    public int goalMilestoneReached;
    public int freeGoalIndex;
    public float freeGoalTarget;
    public int goalBuiltMonth;

    public int mentorMemory_familyStrainStreak;
    public bool mentorMemory_familyStrainMentioned;
    public bool mentorMemory_bufferLineShown;
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

    public int currentAdults;
    public int currentChildren;

    public int setupAdults;
    public int setupChildren;
    public bool setupIsIncomeStable;
    public int setupHousing;
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
    public List<IncomeBenefitSaveData> incomeBenefits = new List<IncomeBenefitSaveData>();

    [Serializable]
    public class IncomeEffectSaveData
    {
        public float reductionPercent;
        public int remainingMonths;
    }

    [Serializable]
    public class IncomeBenefitSaveData
    {
        public float amount;
        public int remainingMonths;
    }

    [Serializable]
    public class ExpenseEffectSaveData
    {
        public int category;
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

    // Scripted/chain follow-up events still queued but not yet triggered (e.g. a
    // death event's "New Income Source" follow-up two months out). Stored by name
    // rather than a direct EventData reference - see EventManager.PendingEventsSnapshot.
    public List<PendingEventSaveData> pendingEvents = new List<PendingEventSaveData>();

    [Serializable]
    public class PendingEventSaveData
    {
        public string eventName;
        public int monthToTrigger;
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