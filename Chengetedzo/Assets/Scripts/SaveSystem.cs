using UnityEngine;
using System.IO;

public static class SaveSystem
{
    private static string SavePath =>
        Application.persistentDataPath + "/save.json";

    public static void SaveGame(GameManager gm)
    {
        GameSaveData data = new GameSaveData();

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