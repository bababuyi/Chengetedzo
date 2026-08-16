using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private const string LEGACY_SAVE_KEY = "GameSaveData";
    private const string LEGACY_CLEANED_FLAG = "SaveSystem_LegacyKeyCleaned";

    static SaveSystem()
    {
        CleanupLegacySingleSlotSave();
    }

    private static void CleanupLegacySingleSlotSave()
    {
        if (PlayerPrefs.GetInt(LEGACY_CLEANED_FLAG, 0) == 1) return;

        if (PlayerPrefs.HasKey(LEGACY_SAVE_KEY))
        {
            PlayerPrefs.DeleteKey(LEGACY_SAVE_KEY);
            Debug.Log("[SaveSystem] Cleared legacy single-slot save (pre-per-profile-slots).");
        }

        PlayerPrefs.SetInt(LEGACY_CLEANED_FLAG, 1);
        PlayerPrefs.Save();
    }

    private static string PathFor(GameManager.ProfileType p, bool guided)
    {
        string fileName = guided ? $"save_{p.ToString().ToLower()}.json" : "save_free.json";
        return Path.Combine(Application.persistentDataPath, fileName);
    }

    public static void SaveGame(GameManager gm)
    {
        GameSaveData data = new GameSaveData();
        var loan = gm.loanManager;
        if (loan != null)
        {
            data.moneylender = loan.moneylender;
            data.mukando = loan.mukando;
            data.loanUnlocked = loan.IsLoanUnlocked;
            data.mukandoJoined = loan.mukandoJoined;
            data.mukandoConsecutiveMisses = loan.MukandoConsecutiveMisses;
            data.mukandoRecoveryMonthsNeeded = loan.MukandoRecoveryMonthsNeeded;
        }
        data.currentMonth = gm.currentMonth;

        data.cashOnHand = gm.financeManager.CashOnHand;
        data.generalSavingsBalance = gm.financeManager.generalSavingsBalance;
        data.generalSavingsMonthly = gm.financeManager.generalSavingsMonthly;

        data.yearIncome = gm.YearIncome;
        data.yearExpenses = gm.YearExpenses;
        data.yearPremiums = gm.YearPremiums;
        data.yearPayouts = gm.YearPayouts;
        data.yearEventLosses = gm.YearEventLosses;

        data.totalUnexpectedEvents = gm.TotalUnexpectedEvents;
        data.insuredEventsCount = gm.InsuredEventsCount;
        data.totalRawEventDamage = gm.TotalRawEventDamage;
        data.totalInsurancePayoutAmount = gm.TotalInsurancePayoutAmount;
        data.forcedLoanCount = gm.ForcedLoanCount;
        data.monthsUnderFinancialPressure = gm.MonthsUnderFinancialPressure;
        data.financialMomentum = PlayerDataManager.Instance.FinancialMomentum;
        data.familyMorale = PlayerDataManager.Instance.FamilyMorale;
        data.socialMorale = PlayerDataManager.Instance.SocialMorale;

        data.savingsStreak = gm.SavedSavingsStreak;
        data.overBudgetStreak = gm.SavedOverBudgetStreak;
        data.patternWarningIssued = gm.SavedPatternWarningIssued;
        data.recoveryAcknowledged = gm.SavedRecoveryAcknowledged;
        data.lastMomentumZone = gm.SavedLastMomentumZone;
        data.previousMomentum = gm.SavedPreviousMomentum;
        data.monthsSinceMajorEvent = gm.monthsSinceMajorEvent;
        data.eventPressure = gm.eventManager.GetEventPressure();
        data.burialSocietyUnlocked = gm.BurialSocietyUnlocked;
        data.isGuidedMode = gm.IsGuidedMode;
        data.profileType = (int)gm.CurrentProfileType;

        data.goalBuilt = gm.GoalBuilt;
        data.goalReachedOnce = gm.HasGoalBeenReached;
        data.goalMonthsSinceOffer = gm.GoalMonthsSinceOffer;
        data.goalMilestoneReached = gm.GoalMilestoneReached;
        data.freeGoalIndex = gm.FreeGoalIndex;
        data.freeGoalTarget = gm.FreeGoalTarget;
        data.goalBuiltMonth = gm.GoalBuiltMonth;

        data.mentorMemory_familyStrainStreak = gm.MentorMemory_FamilyStrainStreak;
        data.mentorMemory_familyStrainMentioned = gm.MentorMemory_FamilyStrainMentioned;
        data.mentorMemory_communityHighMentioned = gm.MentorMemory_CommunityHighMentioned;
        data.mentorMemory_communityLowMentioned = gm.MentorMemory_CommunityLowMentioned;
        data.mentorMemory_goalBuiltMentioned = gm.MentorMemory_GoalBuiltMentioned;
        data.mentorMemory_scarAckPending = gm.MentorMemory_ScarAckPending;
        data.mentorMemory_bufferLineShown = gm.MentorMemory_BufferLineShown;

        data.originalAdults = PlayerDataManager.Instance.OriginalAdults;
        data.currentAdults = PlayerDataManager.Instance.RawAdults;
        data.currentChildren = PlayerDataManager.Instance.Children;

        var setup = gm.setupData;
        var fm = gm.financeManager;
        data.setupAdults = setup.adults;
        data.setupChildren = setup.children;
        data.setupIsIncomeStable = setup.isIncomeStable;
        data.setupHousing = (int)setup.housing;
        data.setupOwnsCar = setup.ownsCar;
        data.setupHasSchoolFees = setup.hasSchoolFees;
        data.setupSchoolFeesAmount = setup.schoolFeesAmount;
        data.setupMinIncome = setup.minIncome;
        data.setupMaxIncome = setup.maxIncome;
        data.setupHouseValue = setup.houseValue;

        data.financeRentCost = fm.rentCost;
        data.financeHouseMaintenanceCost = fm.houseMaintenanceCost;
        data.financeGroceries = fm.groceries;
        data.financeTransport = fm.transport;
        data.financeUtilities = fm.utilities;

        data.assetHasHouse = fm.assets.hasHouse;
        data.assetHasMotor = fm.assets.hasMotor;
        data.assetHasCrops = fm.assets.hasCrops;
        data.assetHasLivestock = fm.assets.hasLivestock;
        data.houseInsuredValue = fm.houseInsuredValue;
        data.motorInsuredValue = fm.motorInsuredValue;
        data.cropsInsuredValue = fm.cropsInsuredValue;
        data.livestockInsuredValue = fm.livestockInsuredValue;

        data.categoryStates = new List<GameManager.CategoryState>(gm.ActiveCategoryStates);
        data.insurancePlans = new List<GameSaveData.InsurancePlanSaveData>();
        foreach (var plan in gm.insuranceManager.allPlans)
        {
            data.insurancePlans.Add(new GameSaveData.InsurancePlanSaveData
            {
                type = plan.type,
                isSubscribed = plan.isSubscribed,
                isLapsed = plan.isLapsed,
                monthsPaid = plan.monthsPaid,
                missedPayments = plan.missedPayments
            });
        }

        data.incomeEffects = new List<GameSaveData.IncomeEffectSaveData>();
        foreach (var effect in gm.ActiveIncomeEffects)
        {
            data.incomeEffects.Add(new GameSaveData.IncomeEffectSaveData
            {
                reductionPercent = effect.reductionPercent,
                remainingMonths = effect.remainingMonths
            });
        }

        data.incomeBenefits = new List<GameSaveData.IncomeBenefitSaveData>();
        foreach (var benefit in gm.ActiveIncomeBenefits)
        {
            data.incomeBenefits.Add(new GameSaveData.IncomeBenefitSaveData
            {
                amount = benefit.amount,
                remainingMonths = benefit.remainingMonths
            });
        }

        data.expenseEffects = new List<GameSaveData.ExpenseEffectSaveData>();
        foreach (var effect in gm.ActiveExpenseEffects)
        {
            data.expenseEffects.Add(new GameSaveData.ExpenseEffectSaveData
            {
                category = (int)effect.category,
                flatIncrease = effect.flatIncrease,
                remainingMonths = effect.remainingMonths
            });
        }

        data.pendingEvents = new List<GameSaveData.PendingEventSaveData>();
        if (gm.eventManager != null)
        {
            foreach (var p in gm.eventManager.PendingEventsSnapshot)
            {
                if (p.eventData == null) continue;
                data.pendingEvents.Add(new GameSaveData.PendingEventSaveData
                {
                    eventName = p.eventData.eventName,
                    monthToTrigger = p.monthToTrigger
                });
            }
        }

        data.snapshots = new List<GameSaveData.MonthSnapshotSaveData>();
        foreach (var s in gm.monthHistory)
            data.snapshots.Add(new GameSaveData.MonthSnapshotSaveData
            {
                month = s.month,
                income = s.income,
                expenses = s.expenses,
                cashOnHand = s.cashOnHand,
                savingsBalance = s.savingsBalance,
                eventLoss = s.eventLoss,
                hadEvent = s.hadEvent,
                eventWasInsured = s.eventWasInsured
            });

        string json = JsonUtility.ToJson(data, false);
        string path = PathFor(gm.CurrentProfileType, gm.IsGuidedMode);
        File.WriteAllText(path, json);
        Debug.Log($"Game Saved → {path}");
    }

    public static GameSaveData LoadGame(GameManager.ProfileType p, bool guided)
    {
        string path = PathFor(p, guided);
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        Debug.Log($"Game Loaded ← {path}");
        return JsonUtility.FromJson<GameSaveData>(json);
    }

    public static bool SaveExists(GameManager.ProfileType p, bool guided)
    {
        return File.Exists(PathFor(p, guided));
    }

    public static void DeleteSave(GameManager.ProfileType p, bool guided)
    {
        string path = PathFor(p, guided);
        if (File.Exists(path)) File.Delete(path);
    }

    // Used by DEV_FullReset / FullRestart - a full reset clears every profile's slot,
    // not just the one currently active.
    public static void DeleteAllSaves()
    {
        DeleteSave(GameManager.ProfileType.Informal, true);
        DeleteSave(GameManager.ProfileType.Formal, true);
        DeleteSave(GameManager.ProfileType.Farmer, true);
        DeleteSave(GameManager.ProfileType.Informal, false);
    }
}
