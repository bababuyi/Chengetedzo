using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;

public class MonthlyFinancialLedger
{
    public int MonthNumber { get; }
    public float OpeningBalance { get; }

    private readonly List<FinancialEntry> entries = new List<FinancialEntry>();
    private bool isFinalized = false;

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
            UnityEngine.Debug.LogError("[Ledger] Attempted to modify finalized ledger.");
            return;
        }

        entries.Add(entry);
        ClosingBalance += entry.SignedAmount();
    }

    public void FinalizeLedger()
    {
        if (isFinalized)
        {
            UnityEngine.Debug.LogWarning("[Ledger] Ledger already finalized.");
            return;
        }

        isFinalized = true;
    }

    public bool IsFinalized()
    {
        return isFinalized;
    }

    public string GetMonthlyBreakdown()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"--- Month {MonthNumber} ---");
        sb.AppendLine($"Opening Balance: ${OpeningBalance:F2}");
        sb.AppendLine("");

        foreach (var entry in entries)
        {
            string sign = entry.isPositive ? "+" : "-";
            sb.AppendLine($"{sign} {entry.description}: ${entry.amount:F2}");
        }

        sb.AppendLine("");
        sb.AppendLine($"Closing Balance: ${ClosingBalance:F2}");

        return sb.ToString();
    }
}