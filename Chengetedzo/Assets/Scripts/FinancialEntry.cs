public class FinancialEntry
{
    public enum EntryType
    {
        Income,
        Expense,

        LoanContribution,
        LoanBorrow,
        LoanRepayment,

        InsurancePremium,
        InsuranceRefund,
        InsurancePayout,

        EventReward,
        EventLoss,

        SavingsContribution,
        SavingsWithdrawal,
        SavingsInterest,

        ManualAdjustment
    }

    public EntryType entryType { get; }
    public string description { get; }
    public float amount { get; }
    public bool isPositive { get; }

    public FinancialEntry(EntryType type, string desc, float amt, bool positive)
    {
        entryType = type;
        description = desc;
        amount = amt;
        isPositive = positive;
    }

    public float SignedAmount()
    {
        return isPositive ? amount : -amount;
    }
}