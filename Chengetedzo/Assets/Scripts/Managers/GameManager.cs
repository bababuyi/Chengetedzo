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
    private float previousMomentum = 0f;
    private bool recoveryAcknowledged = false;
    private int lastMomentumZone = int.MinValue;
    private bool patternWarningIssued = false;
    
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
            financeManager.RollMonthlyIncome();
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
            EvaluateMomentumSignals(); // math only
            EvaluateMentor();         // meaning & messaging
            // Mid-year mentor checkpoint
            if (currentMonth == 6)
            {
                uiManager.ShowMentorMessage(GetMidYearMentorReflection());
                lastMomentumZone = GetMomentumZone(
                    PlayerDataManager.Instance.financialMomentum);
                yield return new WaitUntil(() => !UIManager.Instance.IsPopupActive);
            }

            // Report
            string monthlyReport = financeManager.GetMonthlySummary(currentMonth);
            uiManager.ShowReportPanel($"<b>Month {currentMonth}</b>\n\n{monthlyReport}");

            yield return new WaitUntil(() => !UIManager.Instance.IsPopupActive);
            yield return new WaitForSecondsRealtime(monthDuration);

            currentMonth++;
        }

        string reflection = GetYearEndMentorReflection();
        uiManager.ShowEndOfYearSummary(reflection);
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
        bool skippedImportant = !savedThisMonth && !paidInsurance;

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
        
        float currentMomentum = player.financialMomentum;
    }

    private void EvaluateMentor()
    {
        if (currentMonth <= 1)
            return;

        float momentum = PlayerDataManager.Instance.financialMomentum;
        int currentZone = GetMomentumZone(momentum);

        // --- A. Zone Change ---
        if (currentZone != lastMomentumZone)
        {
            ShowZoneMentorLine(currentZone);
            lastMomentumZone = currentZone;
        }

        // --- B. Pattern Warning ---
        if (!patternWarningIssued && IsNegativePatternForming())
        {
            uiManager.ShowMentorMessage(
                MentorLines.PatternWarning[
                    Random.Range(0, MentorLines.PatternWarning.Length)]);
            patternWarningIssued = true;
        }

        // --- C. Recovery Acknowledgment ---
        CheckRecovery(momentum);

        previousMomentum = momentum;
    }


    private int GetMomentumZone(float momentum)
    {
        if (momentum >= 15f) return 2;
        if (momentum >= 0f) return 1;
        if (momentum > -15f) return -1;
        return -2;                        
    }

    private string GetYearEndMentorReflection()
    {
        float momentum = PlayerDataManager.Instance.financialMomentum;

        if (momentum >= 20f)
            return MentorLines.YearEndStrong[Random.Range(0, MentorLines.YearEndStrong.Length)];

        if (momentum >= 5f)
            return MentorLines.YearEndPositive[Random.Range(0, MentorLines.YearEndPositive.Length)];

        if (momentum >= -4f)
            return MentorLines.YearEndNeutral[Random.Range(0, MentorLines.YearEndNeutral.Length)];

        if (momentum >= -19f)
            return MentorLines.YearEndWarning[Random.Range(0, MentorLines.YearEndWarning.Length)];

        return MentorLines.YearEndNegative[Random.Range(0, MentorLines.YearEndNegative.Length)];
    }

    private string GetMidYearMentorReflection()
    {
        float momentum = PlayerDataManager.Instance.financialMomentum;

        if (momentum >= 15f)
            return MentorLines.MidYearStrong[Random.Range(0, MentorLines.MidYearStrong.Length)];

        if (momentum >= 5f)
            return MentorLines.MidYearPositive[Random.Range(0, MentorLines.MidYearPositive.Length)];

        if (momentum >= -4f)
            return MentorLines.MidYearNeutral[Random.Range(0, MentorLines.MidYearNeutral.Length)];

        if (momentum >= -14f)
            return MentorLines.MidYearWarning[Random.Range(0, MentorLines.MidYearWarning.Length)];

        return MentorLines.MidYearNegative[Random.Range(0, MentorLines.MidYearNegative.Length)];
    }

    private void ShowZoneMentorLine(int zone)
    {
        string line = "";

        switch (zone)
        {
            case 2:
                line = MentorLines.Positive[Random.Range(0, MentorLines.Positive.Length)];
                break;
            case 1:
                line = MentorLines.Neutral[Random.Range(0, MentorLines.Neutral.Length)];
                break;
            case -1:
                line = MentorLines.Warning[Random.Range(0, MentorLines.Warning.Length)];
                break;
            case -2:
                line = MentorLines.Negative[Random.Range(0, MentorLines.Negative.Length)];
                break;
        }

        uiManager.ShowMentorMessage(line);
    }

    private bool IsNegativePatternForming()
    {
        int skipCount = 0;
        foreach (bool skipped in skipHistory)
            if (skipped) skipCount++;

        return overBudgetStreak >= 2 || skipCount >= 3;
    }

    private void CheckRecovery(float currentMomentum)
    {
        if (recoveryAcknowledged)
            return;

        bool wasCritical = previousMomentum <= -15f;
        bool improving = currentMomentum > previousMomentum;
        bool escapedDanger = currentMomentum >= -5f;

        if (wasCritical && improving && escapedDanger)
        {
            uiManager.ShowMentorMessage(
                MentorLines.RecoveryLines[
                    Random.Range(0, MentorLines.RecoveryLines.Length)]);
            recoveryAcknowledged = true;
        }
    }
}
