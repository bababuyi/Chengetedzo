using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SavePath =>
        Application.persistentDataPath + "/save.json";

    public static void SaveGame(GameManager gm)
    {
        GameSaveData data = new GameSaveData();
        var loan = gm.loanManager;
        if (loan != null)
        {
            data.loanBalance = loan.loanBalance;
            data.borrowingPower = loan.borrowingPower;
            data.totalContributed = loan.totalContributed;
            data.monthsContributed = loan.monthsContributed;
            data.repaymentRate = loan.repaymentRate;
            data.missedPayments = loan.missedPayments;
            data.onTimePayments = loan.onTimePayments;
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

        data.savingsStreak = gm.SavedSavingsStreak;
        data.overBudgetStreak = gm.SavedOverBudgetStreak;
        data.patternWarningIssued = gm.SavedPatternWarningIssued;
        data.recoveryAcknowledged = gm.SavedRecoveryAcknowledged;
        data.lastMomentumZone = gm.SavedLastMomentumZone;
        data.previousMomentum = gm.SavedPreviousMomentum;
        data.monthsSinceMajorEvent = gm.monthsSinceMajorEvent;
        data.eventPressure = gm.eventManager.GetEventPressure();
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

        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(SavePath, json);

        Debug.Log("Game Saved " + SavePath);
    }

    public static GameSaveData LoadGame()
    {
        if (!File.Exists(SavePath))
            return null;

        string json = File.ReadAllText(SavePath);

        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        Debug.Log("Game Loaded");

        return data;
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }
}