using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Simulation Settings")]
    public int currentMonth = 1;
    public int totalMonths = 12;
    public float monthDuration = 5f;

    [Header("Momentum")]
    private int savingsStreak = 0;
    private int overBudgetStreak = 0;
    private Queue<bool> skipHistory = new Queue<bool>(); // last 6 months

    [Header("Manager References")]
    public FinanceManager financeManager;
    public SavingsManager savingsManager;
    public LoanManager loanManager;
    public InsuranceManager insuranceManager;
    public EventManager eventManager;
    public UIManager uiManager;
    public VisualSimulationManager visualManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Debug.Log("Game Ready — Awaiting Start of Simulation.");
        uiManager.ShowBudgetPanel();
        uiManager.UpdateMoneyText(financeManager.cashOnHand);
        uiManager.UpdateMonthText(currentMonth, totalMonths);

        // Apply starting visuals
        visualManager?.UpdateVisuals();
    }

    public void BeginSimulation()
    {
        Debug.Log("Starting Life-Cycle Simulation...");
        uiManager.HideAllPanels();

        // Update visuals at the start of simulation
        visualManager?.UpdateVisuals();

        StartCoroutine(RunLifeCycle());
    }

    private IEnumerator RunLifeCycle()
    {
        Debug.Log("Simulation Running...");

        while (currentMonth <= totalMonths)
        {
            // UI
            uiManager.UpdateMonthText(currentMonth, totalMonths);
            uiManager.UpdateMoneyText(financeManager.cashOnHand);

            // Monthly logic
            financeManager.ProcessMonthlyBudget();
            insuranceManager.ProcessMonthlyPremiums();
            loanManager?.ProcessContribution();
            eventManager?.CheckForMonthlyEvent(currentMonth);
            insuranceManager.ProcessClaims();
            savingsManager?.AccrueInterest();
            loanManager?.UpdateLoans();
            financeManager.ProcessSchoolFees(currentMonth);

            // Visuals
            visualManager?.UpdateVisuals();

            // Evaluate long-term behavior (momentum)
            EvaluateMomentumSignals();

            // Report
            string monthlyReport = financeManager.GetMonthlySummary(currentMonth);
            uiManager.ShowReportPanel($"<b>Month {currentMonth}</b>\n\n{monthlyReport}");

            yield return new WaitUntil(() => !UIManager.Instance.IsPopupActive);
            yield return new WaitForSecondsRealtime(monthDuration);

            currentMonth++;
        }

        uiManager.ShowEndOfYearSummary();
        Debug.Log("Simulation Ended - Year Complete");
    }

    public enum Season
    {
        Summer,
        Winter
    }

    public Season GetSeasonForMonth(int month)
    {
        if (month == 11 || month == 12 || (month >= 1 && month <= 4))
            return Season.Summer;

        return Season.Winter;
    }

    public Season GetCurrentSeason()
    {
        return GetSeasonForMonth(currentMonth);
    }
    public void OnEventPopupClosed()
    {
        Debug.Log("[GameManager] Event popup closed — resuming simulation.");
    }

    private void EvaluateMomentumSignals()
    {
        var player = PlayerDataManager.Instance;

        // --- SIGNAL A: Consistency ---
        bool savedThisMonth = savingsManager.LastMonthSavings > 0f;
        bool paidInsurance = insuranceManager.PaidPremiumsThisMonth;
        bool paidLoan = loanManager != null && loanManager.PaidThisMonth;

        if (savedThisMonth || paidInsurance || paidLoan)
            savingsStreak++;
        else
            savingsStreak = 0;

        if (savingsStreak == 3)
        {
            player.financialMomentum += 3f;
            Debug.Log("[Momentum] Consistency streak complete (+3)");
            savingsStreak = 0;
        }

        // --- SIGNAL B: Overextension ---
        if (financeManager.WasOverBudgetThisMonth)
            overBudgetStreak++;
        else
            overBudgetStreak = 0;

        if (overBudgetStreak == 2)
        {
            player.financialMomentum -= 5f;
            Debug.Log("[Momentum] Repeated over-budget (-5)");
            overBudgetStreak = 0;
        }

        // --- SIGNAL C: Skipping Habit ---
        bool skippedImportant = !savedThisMonth || !paidInsurance;

        skipHistory.Enqueue(skippedImportant);
        if (skipHistory.Count > 6)
            skipHistory.Dequeue();

        int skipCount = 0;
        foreach (bool skipped in skipHistory)
            if (skipped) skipCount++;

        if (skipCount >= 3)
        {
            player.financialMomentum -= 4f;
            Debug.Log("[Momentum] Skipping became a habit (-4)");
            skipHistory.Clear();
        }

        player.financialMomentum = Mathf.Clamp(player.financialMomentum, -100f, 100f);
    }
}
