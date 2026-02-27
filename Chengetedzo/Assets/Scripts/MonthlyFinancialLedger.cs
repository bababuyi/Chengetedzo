using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MonthlyFinancialLedger
{
    public int MonthNumber { get; }
    public float OpeningBalance { get; }

    private readonly List<FinancialEntry> entries = new List<FinancialEntry>();
    private bool isFinalized = false;

    public float TotalIncome { get; private set; }
    public float TotalExpenses { get; private set; }
    public float TotalInsurancePremiums { get; private set; }
    public float TotalInsurancePayouts { get; private set; }
    public float TotalEventLosses { get; private set; }

    public IReadOnlyList<FinancialEntry> Entries => entries.AsReadOnly();
    public float ClosingBalance { get; private set; }

    public MonthlyFinancialLedger(int monthNumber, float openingBalance)
    {
        MonthNumber = monthNumber;
        OpeningBalance = openingBalance;
        ClosingBalance = openingBalance;
    }

    public void AddEntry(FinancialEntry entry)
    {
        if (isFinalized)
        {
            Debug.LogError("[Ledger] Attempted to modify finalized ledger.");
            return;
        }

        entries.Add(entry);
        ClosingBalance += entry.SignedAmount();
    }

    public void FinalizeLedger()
    {
        if (isFinalized)
        {
            Debug.LogWarning("[Ledger] Ledger already finalized.");
            return;
        }

        TotalIncome = 0f;
        TotalExpenses = 0f;
        TotalInsurancePremiums = 0f;
        TotalInsurancePayouts = 0f;
        TotalEventLosses = 0f;

        foreach (var entry in entries)
        {
            switch (entry.entryType)
            {
                case FinancialEntry.EntryType.Income:
                case FinancialEntry.EntryType.EventReward:
                    TotalIncome += entry.SignedAmount();
                    break;

                case FinancialEntry.EntryType.Expense:
                    TotalExpenses += Mathf.Abs(entry.SignedAmount());
                    break;

                case FinancialEntry.EntryType.InsurancePremium:
                    TotalInsurancePremiums += Mathf.Abs(entry.SignedAmount());
                    break;

                case FinancialEntry.EntryType.InsurancePayout:
                    TotalInsurancePayouts += entry.SignedAmount();
                    break;

                case FinancialEntry.EntryType.EventLoss:
                    TotalEventLosses += Mathf.Abs(entry.SignedAmount());
                    break;
            }
        }

        isFinalized = true;
    }

    public bool IsFinalized()
    {
        return isFinalized;
    }

    public float GetTotalByType(FinancialEntry.EntryType type)
    {
        float total = 0f;

        foreach (var entry in entries)
        {
            if (entry.entryType == type)
                total += entry.SignedAmount();
        }

        return total;
    }

    public string GetMonthlyBreakdown()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"--- Month {MonthNumber} ---");
        sb.AppendLine($"Opening Balance: ${OpeningBalance:F2}");
        sb.AppendLine("");

        float income =
            GetTotalByType(FinancialEntry.EntryType.Income) +
            GetTotalByType(FinancialEntry.EntryType.EventReward);

        float fixedExpenses =
            -GetTotalByType(FinancialEntry.EntryType.Expense);

        float savingsContribution =
            -GetTotalByType(FinancialEntry.EntryType.SavingsContribution);

        float savingsWithdrawal =
            GetTotalByType(FinancialEntry.EntryType.SavingsWithdrawal);

        float savingsInterest =
            GetTotalByType(FinancialEntry.EntryType.SavingsInterest);

        float premiums =
            -GetTotalByType(FinancialEntry.EntryType.InsurancePremium);

        float payouts =
            GetTotalByType(FinancialEntry.EntryType.InsurancePayout);

        float eventLosses =
            -GetTotalByType(FinancialEntry.EntryType.EventLoss);

        float loanRepayments =
            -GetTotalByType(FinancialEntry.EntryType.LoanRepayment);

        float net = ClosingBalance - OpeningBalance;

        sb.AppendLine("Income:");
        sb.AppendLine($"  ${income:F2}");
        sb.AppendLine("");

        sb.AppendLine("Expenses:");
        sb.AppendLine($"  ${fixedExpenses:F2}");
        sb.AppendLine("");

        sb.AppendLine("Savings:");
        sb.AppendLine($"  Contributions: ${savingsContribution:F2}");
        sb.AppendLine($"  Withdrawals: ${savingsWithdrawal:F2}");
        sb.AppendLine($"  Interest: ${savingsInterest:F2}");
        sb.AppendLine("");

        sb.AppendLine("Insurance:");
        sb.AppendLine($"  Premiums: ${premiums:F2}");
        sb.AppendLine($"  Payouts: ${payouts:F2}");
        sb.AppendLine("");

        sb.AppendLine("Events:");
        sb.AppendLine($"  Losses: ${eventLosses:F2}");
        sb.AppendLine("");

        sb.AppendLine("Loans:");
        sb.AppendLine($"  Repayments: ${loanRepayments:F2}");
        sb.AppendLine("");

        sb.AppendLine($"Net Result: ${net:F2}");
        sb.AppendLine($"Closing Balance: ${ClosingBalance:F2}");

        return sb.ToString();
    }
}