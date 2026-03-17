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

public enum ExpenseCategory
{
    Transport,
    Groceries,
    Utilities,
    Housing,
    SchoolFees
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
    [SerializeField] private float cashOnHand;
    public float CashOnHand => cashOnHand;
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

        currentIncome = isIncomeStable
            ? minIncome
            : Random.Range(minIncome, maxIncome);

        if (setup.hasSchoolFees)
            schoolFeesPerTerm = setup.schoolFeesAmount;
        else
            schoolFeesPerTerm = 0f;

        cashOnHand = Mathf.Max(0f, currentIncome * 0.5f);

        Debug.Log("Income at init: " + currentIncome);
        Debug.Log("Starting cash: " + cashOnHand);

        UpdateHUD();
    }

    public void ApplyCashDelta(float amount)
    {
        cashOnHand += amount;
        UpdateHUD();
    }

    public void SetCash(float amount)
    {
        cashOnHand = amount;
        UpdateHUD();
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

        GameManager.Instance.ApplyMoneyChange(
        FinancialEntry.EntryType.Income,
        "Monthly Income",
        effectiveIncome,
        true
        );
         
        Debug.Log($"[Income] Base: {currentIncome}, Multiplier: {incomeMultiplier:F2}, Effective: {effectiveIncome:F0}");

        // 2. Fixed expenses
        float housingCost = GetHousingCost();
        float effectiveTransport = transport + GameManager.Instance.GetExpenseModifier(ExpenseCategory.Transport);
        float effectiveGroceries = groceries + GameManager.Instance.GetExpenseModifier(ExpenseCategory.Groceries);
        float effectiveUtilities = utilities + GameManager.Instance.GetExpenseModifier(ExpenseCategory.Utilities);

        totalExpenses = housingCost + effectiveGroceries + effectiveTransport + effectiveUtilities;

        GameManager.Instance.ApplyMoneyChange(
            FinancialEntry.EntryType.Expense,
            "Fixed Expenses",
            totalExpenses,
            false
        );

        balance = effectiveIncome - totalExpenses;
        WasOverBudgetThisMonth = balance < 0;

        ProcessSchoolFees(GameManager.Instance.currentMonth);

        // 3. General savings (ONLY if affordable)
        LastMonthSavingsDelta = 0f;

        if (generalSavingsMonthly > 0 && CashOnHand >= generalSavingsMonthly)
        {
            GameManager.Instance.ApplyMoneyChange(
            FinancialEntry.EntryType.SavingsContribution,
            "Savings Contribution",
            generalSavingsMonthly,
            false
            );

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
            GameManager.Instance.ApplyMoneyChange(
            FinancialEntry.EntryType.SavingsInterest,
            "Savings Interest",
            interest,
            true
            );

            Debug.Log($"[Savings] Interest gained: ${interest:F2}");
        }

        // 5. Lifetime tracking
        totalEarned += effectiveIncome;
        totalSpent += totalExpenses;

        Debug.Log(
            $"[Finance] Income: {currentIncome}, " +
            $"Expenses: {totalExpenses}, " +
            $"Savings: {LastMonthSavingsDelta}, " +
            $"End Cash: {cashOnHand}"
        );

        savingsWithdrawnThisMonth = 0f;
        UpdateHUD();

        generalSavingsBalance = Mathf.Max(0f, generalSavingsBalance);

        Debug.Log("Monthly Income Applied: " + currentIncome);
        Debug.Log("Cash After Income: " + cashOnHand);
        if (GameManager.Instance.CurrentLedger != null)
        {
            Debug.Log("Ledger Entry Count: " + GameManager.Instance.CurrentLedger.EntryCount);
        }
    }

    /// <summary>
    /// Applies player’s chosen budget plan (from BudgetPanel UI),
    /// including income, expenses, and allocation totals.
    /// </summary>

    // NOTE: UI-only adjustment. Does NOT represent full monthly simulation.
    public void ApplyBudget(float income, float expenses, float allocations)
    {
        float delta = income - (expenses + allocations);

        GameManager.Instance.ApplyMoneyChange(
            FinancialEntry.EntryType.ManualAdjustment,
            "Budget Adjustment",
            Mathf.Abs(delta),
            delta >= 0
            );
        UpdateHUD();
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

        int childCount = Mathf.Max(0, PlayerDataManager.Instance?.Children ?? 0);
        if (childCount == 0)
        {
            schoolFeesOutstanding = false;
            return false;
        }

        float effectiveFees = (schoolFeesPerTerm * childCount)
            + GameManager.Instance.GetExpenseModifier(ExpenseCategory.SchoolFees);

        if (cashOnHand >= effectiveFees)
        {
            GameManager.Instance.ApplyMoneyChange(
                FinancialEntry.EntryType.Expense,
                "School Fees",
                effectiveFees,
                false
            );
            totalSpent += effectiveFees;
            schoolFeesOutstanding = false;
            Debug.Log($"[School Fees] Paid ${effectiveFees} for {childCount} child(ren)");
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

    //public string GetMonthlySummary(int month)
    //{
      //  StringBuilder sb = new StringBuilder();

        //sb.AppendLine($"<b>Month {month} Financial Report</b>\n");

        // ===== Income =====
        //sb.AppendLine("<b>Income</b>");
        //sb.AppendLine($"+ Monthly Income: ${currentIncome:F2}");

        //float incomeMultiplier = GameManager.Instance.GetIncomeMultiplier();

        //if (!Mathf.Approximately(incomeMultiplier, 1f))

        //{
          //  sb.AppendLine(
            //    $"<size=90%><color=#C94A4A>" +
              //  $"Income Modifiers: x{incomeMultiplier:F2}</color></size>"
            //);
        //}

        //sb.AppendLine("");

        // ===== Expenses =====
        //sb.AppendLine("<b>Expenses</b>");
        //sb.AppendLine($"- Total Expenses: ${totalExpenses:F2}\n");

        // ===== Budget Result =====
        //sb.AppendLine("<b>Monthly Result</b>");
        //float net = (currentIncome * incomeMultiplier) - totalExpenses;

        //if (net >= 0)
          //  sb.AppendLine($"+ Surplus: ${net:F2}\n");
        //else
          //  sb.AppendLine($"- Deficit: ${Mathf.Abs(net):F2}\n");

        // ===== Savings =====
//        sb.AppendLine("<b>Savings</b>");

  //      if (generalSavingsMonthly > 0)
    //    {
      //      sb.AppendLine($"- General Savings: ${generalSavingsMonthly:F2}");
        //    sb.AppendLine($"  Balance: ${generalSavingsBalance:F2}");
        //}

        //sb.AppendLine("");


        // ===== End Balance =====
        //sb.AppendLine("<b>End of Month Balance</b>");
        //sb.AppendLine($"${cashOnHand:F2}");

        // ===== Warnings (NO mentor) =====
//        if (WasOverBudgetThisMonth)
//        {
 //           sb.AppendLine(
   //             "\n<size=90%><color=#C94A4A>You spent more than your income this month.</color></size>");
     //   }

       // if (cashOnHand <= 0)
       // {
         //   sb.AppendLine(
         //       "<size=90%><color=#C94A4A>You have no remaining cash.</color></size>");
        //}
        //
        //return sb.ToString();
    //}


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
        GameManager.Instance.ApplyMoneyChange(
        FinancialEntry.EntryType.SavingsWithdrawal,
        "Savings Withdrawal",
        amount,
        true
        );
        UpdateHUD();
        savingsWithdrawnThisMonth += amount;

        // Momentum impact (lighter than loans)
        PlayerDataManager.Instance.ModifyMomentum(-1f);

        Debug.Log("Cash after withdrawal: " + cashOnHand);
        Debug.Log($"[Savings] Withdrew ${amount}. Savings left: ${generalSavingsBalance:F0}");
        return true;
    }

    private void UpdateHUD()
    {
        if (UIManager.Instance == null)
            Debug.LogError("UIManager.Instance is NULL");

        if (UIManager.Instance.moneyText == null)
            Debug.LogError("moneyText reference is NULL");

        UIManager.Instance?.UpdateMoneyText(cashOnHand);
    }

    public float GetProjectedMonthlyExpenses()
    {
        float housingCost = GetHousingCost();

        float projected =
            housingCost +
            groceries + GameManager.Instance.GetExpenseModifier(ExpenseCategory.Groceries) +
            transport + GameManager.Instance.GetExpenseModifier(ExpenseCategory.Transport) +
            utilities + GameManager.Instance.GetExpenseModifier(ExpenseCategory.Utilities);

        if (schoolFeesPerTerm > 0f)
        {
            int childCount = Mathf.Max(0, PlayerDataManager.Instance?.Children ?? 0);
            projected += ((schoolFeesPerTerm * childCount) * 3f) / 12f;
        }

        return projected;
    }

    public void ResetFinance()
    {
        cashOnHand = 0f;
        totalExpenses = 0f;
        balance = 0f;

        totalEarned = 0f;
        totalSpent = 0f;

        generalSavingsMonthly = 0f;
        generalSavingsBalance = 0f;
        savingsWithdrawnThisMonth = 0f;
        LastMonthSavingsDelta = 0f;

        WasOverBudgetThisMonth = false;

        schoolFeesOutstanding = false;

        minIncome = 0f;
        maxIncome = 0f;
        isIncomeStable = true;

        currentIncome = 0f;

        UpdateHUD();
    }

    public float CalculateEventLoss(EventData ev, float percentLoss)
    {
        float baseValue = 0f;

        switch (ev.lossType)
        {
            case LossCalculationType.AssetValue:
                baseValue = GetAssetValue(ev.insuranceType);
                break;

            case LossCalculationType.CashOnHand:
                baseValue = CashOnHand;
                break;

            case LossCalculationType.FixedAmount:
                return ev.fixedLossAmount;
        }

        return baseValue * (percentLoss / 100f);
    }
}