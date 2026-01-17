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
    public ForecastManager forecastManager;
    public VisualSimulationManager visualManager;
    public PlayerSetupData setupData = new PlayerSetupData();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Debug.Log("Game Ready — Awaiting Start of Simulation.");
        
        uiManager.UpdateMoneyText(financeManager.cashOnHand);
        uiManager.UpdateMonthText(currentMonth, totalMonths);

        visualManager?.UpdateVisuals();
    }

    public void StartNewMonth()
    {
        Debug.Log($"=== Month {currentMonth} START ===");

        CurrentPhase = GamePhase.Forecast;

        uiManager.ShowForecastPanel();
        forecastManager.GenerateForecast();
    }

    public void BeginMonthlySimulation()
    {
        CurrentPhase = GamePhase.Simulation;

        Debug.Log($"[Simulation] Running Month {currentMonth}");

        uiManager.HideAllPanels();

        financeManager.ProcessMonthlyBudget();
        insuranceManager.ProcessMonthlyPremiums();
        loanManager?.ProcessContribution();
        eventManager?.CheckForMonthlyEvent(currentMonth);
        insuranceManager.ProcessClaims();
        savingsManager?.AccrueInterest();
        loanManager?.UpdateLoans();
        financeManager.ProcessSchoolFees(currentMonth);

        visualManager?.UpdateVisuals();

        EvaluateMomentumSignals();
        EvaluateMentor();

        StartCoroutine(SimulationRoutine());
    }

    public void EndMonthlySimulation()
    {
        uiManager.UpdateMoneyText(financeManager.cashOnHand);
        Debug.Log($"[Simulation] Month {currentMonth} complete");

        CurrentPhase = GamePhase.Report;

        uiManager.ShowReportPanel(financeManager.GetMonthlySummary(currentMonth));
    }

    private IEnumerator SimulationRoutine()
    {
        yield return new WaitForSeconds(monthDuration);

        EndMonthlySimulation();
    }

    public enum GamePhase
    {
        Idle,
        Forecast,
        Insurance,
        Simulation,
        Report
    }

    public void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        Debug.Log($"[GamePhase] → {phase}");
    }

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Idle;

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

    public void EndMonthAndAdvance()
    {
        Debug.Log($"=== Month {currentMonth} END ===");

        currentMonth++;

        if (currentMonth <= totalMonths)
        {
            StartNewMonth();
        }
        else
        {
            uiManager.ShowEndOfYearSummary(GetYearEndMentorReflection());
        }
    }
}
