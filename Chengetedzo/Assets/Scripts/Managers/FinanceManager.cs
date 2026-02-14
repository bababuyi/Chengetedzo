using System.Text;
using UnityEngine;

[System.Serializable]
public struct PlayerAssets
{
    public bool hasHouse;
    public bool hasLivestock;
    public bool hasMotor;
    public bool hasCrops;
}

/// <summary>
/// Handles all financial calculations and tracking for the player,
/// including income, expenses, cash flow, and monthly summaries.
/// </summary>
public class FinanceManager : MonoBehaviour
{
    public PlayerAssets assets;

    [Header("Base Budget")]
    [Tooltip("The player's current monthly income (can change with events).")]
    public float currentIncome = 400f;
    // public float rent = 100f;
    public float groceries = 80f;
    public float transport = 40f;
    public float utilities = 30f;

    [Header("School Fees")]
    public float schoolFeesPerTerm;
    private bool schoolFeesOutstanding = false;


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

    [Header("Income Variability")]
    public float minIncome;
    public float maxIncome;
    public bool isIncomeStable;

    [Header("General Savings")]
    public float generalSavingsMonthly;
    public float generalSavingsBalance;
    public float generalSavingsInterestRate = 0f;

    [Header("Savings Tracking")]
    public float savingsWithdrawnThisMonth;

    public float LastMonthSavingsDelta { get; private set; }

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

    /// <summary>
    /// Calculates and applies monthly expenses, updates balance and cash.
    /// </summary>
    public void ProcessMonthlyBudget()
    {
        // income already rolled once per month
        RollMonthlyIncome();
        // 1. Income (apply income effects)
        float incomeMultiplier = GameManager.Instance.GetIncomeMultiplier();
        float effectiveIncome = currentIncome * incomeMultiplier;

        cashOnHand += effectiveIncome;
        Debug.Log($"[Income] Base: {currentIncome}, Multiplier: {incomeMultiplier:F2}, Effective: {effectiveIncome:F0}");

        // 2. Fixed expenses
        float housingCost = GetHousingCost();
        totalExpenses = housingCost + groceries + transport + utilities;
        cashOnHand -= totalExpenses;

        balance = effectiveIncome - totalExpenses;
        WasOverBudgetThisMonth = balance < 0;

        // 3. General savings (ONLY if affordable)
        LastMonthSavingsDelta = 0f;

        if (generalSavingsMonthly > 0 && cashOnHand >= generalSavingsMonthly)
        {
            cashOnHand -= generalSavingsMonthly;
            generalSavingsBalance += generalSavingsMonthly;
            LastMonthSavingsDelta = generalSavingsMonthly;
        }

        // 4. Interest (applied to current savings balance after contributions/withdrawals)
        float interestBase = generalSavingsBalance;

        if (savingsWithdrawnThisMonth > 0)
        {
            interestBase = Mathf.Max(
                0f,
                generalSavingsBalance
            );
        }

        if (interestBase > 0 && generalSavingsInterestRate > 0f)
        {
            float interest = interestBase * generalSavingsInterestRate;
            generalSavingsBalance += interest;

            Debug.Log($"[Savings] Interest gained: ${interest:F2}");
        }

        // 5. Lifetime tracking
        totalEarned += effectiveIncome;
        totalSpent += totalExpenses;

        ProcessSchoolFees(GameManager.Instance.currentMonth);

        Debug.Log(
            $"[Finance] Income: {currentIncome}, " +
            $"Expenses: {totalExpenses}, " +
            $"Savings: {LastMonthSavingsDelta}, " +
            $"End Cash: {cashOnHand}"
        );

        savingsWithdrawnThisMonth = 0f;
    }

    /// <summary>
    /// Applies player’s chosen budget plan (from BudgetPanel UI),
    /// including income, expenses, and allocation totals.
    /// </summary>

    // NOTE: UI-only adjustment. Does NOT represent full monthly simulation.
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

    public bool ProcessSchoolFees(int month)
    {
        if (schoolFeesPerTerm <= 0f)
            return false;

        bool isTermStart = (month == 1 || month == 5 || month == 9);

        if (isTermStart)
            schoolFeesOutstanding = true;

        if (!schoolFeesOutstanding)
            return false;

        if (cashOnHand >= schoolFeesPerTerm)
        {
            cashOnHand -= schoolFeesPerTerm;
            totalSpent += schoolFeesPerTerm;
            schoolFeesOutstanding = false;

            Debug.Log($"[School Fees] Paid ${schoolFeesPerTerm}");
            return false;
        }

        Debug.LogWarning("[School Fees] Unpaid — outstanding!");
        return true;
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
        sb.AppendLine($"+ Monthly Income: ${currentIncome:F2}");

        float incomeMultiplier = GameManager.Instance.GetIncomeMultiplier();

        if (!Mathf.Approximately(incomeMultiplier, 1f))

        {
            sb.AppendLine(
                $"<size=90%><color=#C94A4A>" +
                $"Income Modifiers: x{incomeMultiplier:F2}</color></size>"
            );
        }

        sb.AppendLine("");

        // ===== Expenses =====
        sb.AppendLine("<b>Expenses</b>");
        sb.AppendLine($"- Total Expenses: ${totalExpenses:F2}\n");

        // ===== Budget Result =====
        sb.AppendLine("<b>Monthly Result</b>");
        float net = (currentIncome * incomeMultiplier) - totalExpenses;

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

    public float rentCost;
    public float houseInsuredValue;
    public float houseMaintenanceCost;

    public float GetHousingCost()
    {
        bool ownsHouse = GameManager.Instance.setupData.housing == HousingType.OwnsHouse;

        return ownsHouse ? houseMaintenanceCost : rentCost;
    }

    public float GetAssetValue(InsuranceManager.InsuranceType type)
    {
        switch (type)
        {
            case InsuranceManager.InsuranceType.Home:
                return houseInsuredValue;

            case InsuranceManager.InsuranceType.Motor:
                return assets.hasMotor ? 3000f : 0f; // placeholder

            case InsuranceManager.InsuranceType.Crop:
                return assets.hasCrops ? 2000f : 0f; // placeholder

            case InsuranceManager.InsuranceType.Education:
                return schoolFeesPerTerm * 3f;

            case InsuranceManager.InsuranceType.Health:
            case InsuranceManager.InsuranceType.HospitalCash:
            case InsuranceManager.InsuranceType.PersonalAccident:
            case InsuranceManager.InsuranceType.Funeral:
                return 1000f; // abstract loss baseline

            default:
                return 0f;
        }
    }

    public bool WithdrawFromSavings(float amount)
    {
        if (amount <= 0f)
            return false;

        if (generalSavingsBalance < amount)
        {
            Debug.Log("[Savings] Not enough savings to withdraw.");
            return false;
        }

        generalSavingsBalance -= amount;
        cashOnHand += amount;
        savingsWithdrawnThisMonth += amount;

        // Momentum impact (lighter than loans)
        PlayerDataManager.Instance.ModifyMomentum(-1f);


        Debug.Log($"[Savings] Withdrew ${amount}. Savings left: ${generalSavingsBalance:F0}");
        return true;
    }
}
