using System.Text;
using UnityEngine;

/// <summary>
/// Handles all financial calculations and tracking for the player,
/// including income, expenses, cash flow, and monthly summaries.
/// </summary>
public class FinanceManager : MonoBehaviour
{
    [Header("Base Budget")]
    [Tooltip("The player's current monthly income (can change with events).")]
    public float currentIncome = 400f;
    public float rent = 100f;
    public float groceries = 80f;
    public float transport = 40f;
    public float utilities = 30f;

    [Header("Financial State")]
    [Tooltip("Current available cash on hand after all calculations.")]
    public float cashOnHand;
    [Tooltip("Sum of all monthly expenses.")]
    public float totalExpenses;
    [Tooltip("Net difference between income and expenses for the month.")]
    public float balance;

    [Header("Tracking")]
    [Tooltip("Total income accumulated over the simulation.")]
    public float totalEarned;
    [Tooltip("Total expenses accumulated over the simulation.")]
    public float totalSpent;
    public bool WasOverBudgetThisMonth { get; private set; }

    [Header("School Fees Savings")]
    public float schoolFeeSavingsMonthly;
    public float schoolFeeSavingsBalance;
    public float schoolFeeInterestRate = 0.02f; // 2%
    public float schoolFeesPerTerm = 100f; // can be dynamic later

    [Header("Income Variability")]
    public float minIncome;
    public float maxIncome;
    public bool isIncomeStable;

    [Header("General Savings")]
    public float generalSavingsMonthly;
    public float generalSavingsBalance;
    public float generalSavingsInterestRate = 0f; // optional later

    /// <summary>
    /// Sets the player’s income at game start or during simulation.
    /// </summary>
    /// 
    public void InitializeFromSetup()
    {
        var setup = GameManager.Instance.setupData;

        minIncome = setup.minIncome;
        maxIncome = setup.maxIncome;
        isIncomeStable = setup.isIncomeStable;

        if (setup.hasSchoolFees)
            schoolFeesPerTerm = setup.schoolFeesAmount;
        else
            schoolFeesPerTerm = 0f;

        RollMonthlyIncome();
        // Starting cash (choose ONE philosophy)
        cashOnHand = currentIncome * 0.5f;
    }


    public void SetIncomeRange(float min, float max, bool stable)
    {
        minIncome = min;
        maxIncome = max;
        isIncomeStable = stable;

        currentIncome = Random.Range(minIncome, maxIncome);
    }
    public void SetSchoolFeeSavings(float amount)
    {
        schoolFeeSavingsMonthly = amount;
        Debug.Log($"[Finance] School Fee Savings (monthly) set to: ${amount}");
    }

    /// <summary>
    /// Calculates and applies monthly expenses, updates balance and cash.
    /// </summary>
    public void ProcessMonthlyBudget()
    {
        // 1. Income arrives
        cashOnHand += currentIncome;

        // 2. Expenses
        totalExpenses = rent + groceries + transport + utilities;
        cashOnHand -= totalExpenses;

        balance = currentIncome - totalExpenses;
        WasOverBudgetThisMonth = balance < 0;

        // 3. General savings (flexible)
        if (generalSavingsMonthly > 0)
        {
            cashOnHand -= generalSavingsMonthly;
            generalSavingsBalance += generalSavingsMonthly;
            generalSavingsBalance *= 1 + generalSavingsInterestRate;
        }

        // 4. School fee savings (restricted)
        if (schoolFeeSavingsMonthly > 0)
        {
            cashOnHand -= schoolFeeSavingsMonthly;
            schoolFeeSavingsBalance += schoolFeeSavingsMonthly;
            schoolFeeSavingsBalance *= 1 + schoolFeeInterestRate;
        }

        // 5. Lifetime tracking
        totalEarned += currentIncome;
        totalSpent += totalExpenses;

        Debug.Log(
            $"[Finance] Income: {currentIncome}, " +
            $"Expenses: {totalExpenses}, " +
            $"Gen Savings: {generalSavingsMonthly}, " +
            $"School Savings: {schoolFeeSavingsMonthly}, " +
            $"End Cash: {cashOnHand}"
        );
    }

    /// <summary>
    /// Applies player’s chosen budget plan (from BudgetPanel UI),
    /// including income, expenses, and allocation totals.
    /// </summary>
    public void ApplyBudget(float income, float expenses, float allocations)
    {
        cashOnHand += income - (expenses + allocations);
        Debug.Log($"[Finance] Budget Applied — New Balance: ${cashOnHand}");
    }

    public void AdjustIncome(float percentageChange)
    {
        float change = currentIncome * (percentageChange / 100f);
        currentIncome += change;
        Debug.Log($"[Finance] Income adjusted by {percentageChange:+0;-0}% ? New income: {currentIncome}");
    }

    public void ProcessSchoolFees(int month)
    {
        if (month == 1 || month == 5 || month == 9)
        {
            if (schoolFeeSavingsBalance >= schoolFeesPerTerm)
            {
                schoolFeeSavingsBalance -= schoolFeesPerTerm;
                Debug.Log($"[School Fees] Paid ${schoolFeesPerTerm} from savings.");
            }
            else
            {
                Debug.LogWarning("[School Fees] Not enough savings! Consider insurance payout or borrowing.");
            }
        }
    }
    public void RollMonthlyIncome()
    {
        if (minIncome <= 0 || maxIncome <= 0 || maxIncome < minIncome)
        {
            Debug.LogWarning("[Finance] Income range not set. Using current income.");
            return;
        }

        float variance = isIncomeStable ? 0.1f : 0.3f;
        float range = maxIncome - minIncome;

        float fluctuation = Random.Range(-range * variance, range * variance);
        currentIncome = Mathf.Clamp(
            Random.Range(minIncome, maxIncome) + fluctuation,
            minIncome,
            maxIncome
        );
    }

    public void SetMonthlyIncome(float income)
    {
        currentIncome = income;
        Debug.Log($"[Finance] Monthly income set to: {currentIncome}");
    }
    public string GetMonthlySummary(int month)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"<b>Month {month} Financial Report</b>\n");

        // ===== Income =====
        sb.AppendLine("<b>Income</b>");
        sb.AppendLine($"+ Monthly Income: ${currentIncome:F2}\n");

        // ===== Expenses =====
        sb.AppendLine("<b>Expenses</b>");
        sb.AppendLine($"- Total Expenses: ${totalExpenses:F2}\n");

        // ===== Budget Result =====
        sb.AppendLine("<b>Monthly Result</b>");
        float net = currentIncome - totalExpenses;

        if (net >= 0)
            sb.AppendLine($"+ Surplus: ${net:F2}\n");
        else
            sb.AppendLine($"- Deficit: ${Mathf.Abs(net):F2}\n");

        // ===== Savings =====
        sb.AppendLine("<b>Savings</b>");

        if (generalSavingsMonthly > 0)
        {
            sb.AppendLine($"- General Savings: ${generalSavingsMonthly:F2}");
            sb.AppendLine($"  Balance: ${generalSavingsBalance:F2}");
        }

        if (schoolFeeSavingsMonthly > 0)
        {
            sb.AppendLine($"- School Fee Savings: ${schoolFeeSavingsMonthly:F2}");
            sb.AppendLine($"  Balance: ${schoolFeeSavingsBalance:F2}");
        }

        sb.AppendLine("");


        // ===== End Balance =====
        sb.AppendLine("<b>End of Month Balance</b>");
        sb.AppendLine($"${cashOnHand:F2}");

        // ===== Warnings (NO mentor) =====
        if (WasOverBudgetThisMonth)
        {
            sb.AppendLine(
                "\n<size=90%><color=#C94A4A>You spent more than your income this month.</color></size>");
        }

        if (cashOnHand <= 0)
        {
            sb.AppendLine(
                "<size=90%><color=#C94A4A>You have no remaining cash.</color></size>");
        }

        return sb.ToString();
    }
}
