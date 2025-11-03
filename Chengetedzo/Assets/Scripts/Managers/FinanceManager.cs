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

    /// <summary>
    /// Sets the player’s income at game start or during simulation.
    /// </summary>
    public void SetPlayerIncome(float value)
    {
        currentIncome = value;
    }

    /// <summary>
    /// Calculates and applies monthly expenses, updates balance and cash.
    /// </summary>
    public void ProcessMonthlyBudget()
    {
        totalExpenses = rent + groceries + transport + utilities;
        balance = currentIncome - totalExpenses;
        cashOnHand += balance;

        totalEarned += currentIncome;
        totalSpent += totalExpenses;

        Debug.Log($"[Finance] Income: {currentIncome}, Expenses: {totalExpenses}, Cash: {cashOnHand}");
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

    /// <summary>
    /// Returns a text summary of the player’s monthly financial state.
    /// </summary>
    public string GetMonthlySummary(int month)
    {
        return $"Month {month}\n" +
               $"Income: ${currentIncome}\n" +
               $"Expenses: ${totalExpenses}\n" +
               $"Cash Remaining: ${cashOnHand}";
    }

    /// <summary>
    /// Optional helper for income fluctuation events.
    /// </summary>
    public void AdjustIncome(float percentageChange)
    {
        float change = currentIncome * (percentageChange / 100f);
        currentIncome += change;
        Debug.Log($"[Finance] Income adjusted by {percentageChange:+0;-0}% ? New income: {currentIncome}");
    }
}
