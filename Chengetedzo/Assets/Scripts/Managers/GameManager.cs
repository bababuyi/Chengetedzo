using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
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
    public LoanManager loanManager;
    public InsuranceManager insuranceManager;
    public EventManager eventManager;
    public UIManager uiManager;
    public ForecastManager forecastManager;
    public VisualSimulationManager visualManager;
    public PlayerSetupData setupData = new PlayerSetupData();

    [Header("Monthly Damage")]
    public float monthlyDamageTaken = 0f;
    public float maxMonthlyDamagePercent = 0.35f;

    [Header("Event Timing")]
    public float delayBetweenEvents = 1.5f;

    private bool mentorCommentPending = false;
    private List<ResolvedEvent> monthlyEvents = new();
    public bool IsLoanDecisionActive { get; private set; }
    private Queue<bool> forcedLoanHistory = new(); // last 6 months
    private bool forcedLoanThisMonth = false;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (setupData == null)
            setupData = new PlayerSetupData();

        setupData.housing = HousingType.Renting;
        setupData.ownsCar = false;
        setupData.ownsFarm = false;
    }

    public class ResolvedEvent
    {
        public string title;
        public string description;
        public InsuranceManager.InsuranceType type;
        public float lossPercent;
    }

    private Queue<ResolvedEvent> pendingEvents = new();
    private ResolvedEvent currentEvent;

    private void Start()
    {
        Debug.Log("Game Ready — Awaiting Start of Simulation.");
        
        uiManager.UpdateMoneyText(financeManager.cashOnHand);
        uiManager.UpdateMonthText(currentMonth, totalMonths);

        visualManager?.UpdateVisuals();
        Debug.Log(
    $"[Setup] Housing: {setupData.housing}, " +
    $"Car: {setupData.ownsCar}, Farm: {setupData.ownsFarm}");

    }

    public void StartNewMonth()
    {
        forcedLoanThisMonth = false;
        Debug.Log($"=== Month {currentMonth} START ===");

        CurrentPhase = GamePhase.Forecast;

        uiManager.ShowForecastPanel();
        forecastManager.GenerateForecast();
        loanManager?.ResetMonthlyFlags();
    }

    public void BeginMonthlySimulation()
    {
        CurrentPhase = GamePhase.Simulation;
        
        uiManager.budgetPanel.GetComponent<BudgetPanelController>()?.ConfigureForPhase(GameManager.GamePhase.Simulation);

        monthlyDamageTaken = 0f;

        Debug.Log($"[Simulation] Running Month {currentMonth}");

        uiManager.HideAllPanels();

        financeManager.ProcessMonthlyBudget();
        insuranceManager.ProcessMonthlyPremiums();
        loanManager?.ProcessContribution();
        monthlyEvents = eventManager.GenerateMonthlyEvents(currentMonth);

        pendingEvents.Clear();
        foreach (var ev in monthlyEvents)pendingEvents.Enqueue(ev);

        ProcessNextEvent();
        insuranceManager.ProcessClaims();

        bool missedFees =
            financeManager.ProcessSchoolFees(currentMonth);

        HandleForcedLoan(); // reacts to negative cash AFTER fees

        loanManager?.UpdateLoans();

        // Momentum hit for missed fees
        if (missedFees)
        {
            PlayerDataManager.Instance.financialMomentum -= 4f;
            uiManager.ShowMentorMessage(
                MentorLines.SchoolFeesMissed[
                    Random.Range(0, MentorLines.SchoolFeesMissed.Length)
                ]);
        }

        visualManager?.UpdateVisuals();

        EvaluateMomentumSignals();
        EvaluateMentor();

        if (CurrentPhase == GamePhase.Simulation)
            return;

        insuranceManager.ProcessClaims();

        loanManager?.UpdateLoans();
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
        Loan,
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
        Debug.Log("[GameManager] Event popup closed");

        // Inject mentor commentary once
        if (mentorCommentPending)
        {
            mentorCommentPending = false;
            StartCoroutine(ShowMentorBetweenEvents());
            return;
        }

        ProcessNextEvent();
    }

    private void EvaluateMomentumSignals()
    {
        var player = PlayerDataManager.Instance;

        // --- SIGNAL A: Consistency ---
        bool savedThisMonth = financeManager.LastMonthSavingsDelta > 0f;
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

        if (financeManager.savingsWithdrawnThisMonth > 0 &&
        financeManager.generalSavingsBalance > 0)
        {
            PlayerDataManager.Instance.financialMomentum += 1f;
        }

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

        int forcedCount = 0;
        foreach (bool forced in forcedLoanHistory)
            if (forced) forcedCount++;

        if (forcedCount >= 2)
        {
            uiManager.ShowMentorMessage(
                MentorLines.ForcedLoanPattern[
                    Random.Range(0, MentorLines.ForcedLoanPattern.Length)
                ]);
        }
        else if (forcedCount == 1)
        {
            uiManager.ShowMentorMessage(
                MentorLines.ForcedLoan[
                    Random.Range(0, MentorLines.ForcedLoan.Length)
                ]);
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

    public float ApplyMonthlyDamageCap(float proposedLoss)
    {
        float maxAllowed =
            financeManager.cashOnHand * maxMonthlyDamagePercent;

        float remaining =
            maxAllowed - (monthlyDamageTaken * financeManager.cashOnHand);

        float finalLoss = Mathf.Min(proposedLoss, remaining);

        monthlyDamageTaken += finalLoss / financeManager.cashOnHand;

        return finalLoss;
    }

    public float ApplyMonthlyDamage(float intendedLoss)
    {
        float maxAllowedLoss = financeManager.cashOnHand * maxMonthlyDamagePercent;
        float remainingCap = maxAllowedLoss - monthlyDamageTaken;

        float actualLoss = Mathf.Max(0f, Mathf.Min(intendedLoss, remainingCap));

        monthlyDamageTaken += actualLoss;

        return actualLoss;
    }

    private void ProcessNextEvent()
    {
        if (pendingEvents.Count == 0)
        {
            if (IsLoanDecisionActive)
                return;

            StartCoroutine(SimulationRoutine());
            return;
        }

        mentorCommentPending = pendingEvents.Count >= 2;
        StartCoroutine(ShowNextEventWithDelay());
    }

    private IEnumerator ShowNextEventWithDelay()
    {
        // Small pause between events (real-time, unaffected by Time.timeScale)
        yield return new WaitForSecondsRealtime(delayBetweenEvents);

        currentEvent = pendingEvents.Dequeue();

        // Apply damage before showing
        insuranceManager.HandleEvent(
            currentEvent.type,
            currentEvent.lossPercent
        );

        uiManager.ShowEventPopup(
            currentEvent.title,
            currentEvent.description
        );
    }

    private IEnumerator ShowMentorBetweenEvents()
    {
        // Small breathing pause
        yield return new WaitForSecondsRealtime(1f);

        string line = PickEventMentorLine();
        uiManager.ShowMentorMessage(line);

        // Wait until mentor popup is closed
        while (uiManager.IsPopupActive)
            yield return null;

        // Small pause after mentor
        yield return new WaitForSecondsRealtime(0.5f);

        ProcessNextEvent();
    }

    private string PickEventMentorLine()
    {
        // You can expand this later with insurance awareness etc.
        return MentorLines.Warning[
            Random.Range(0, MentorLines.Warning.Length)
        ];
    }

    public void BeginLoanDecision()
    {
        IsLoanDecisionActive = true;
        CurrentPhase = GamePhase.Simulation; // still same month

        uiManager.ShowLoanPanel();
    }

    public void EndLoanDecision()
    {
        IsLoanDecisionActive = false;

        StartCoroutine(SimulationRoutine());
    }

    public void OnInsuranceConfirmed()
    {
        UIManager.Instance.HideAllPanels();

        if (loanManager != null && loanManager.IsLoanUnlocked)
        {
            SetPhase(GamePhase.Loan);
            UIManager.Instance.ShowLoanPanel();
            return;
        }

        BeginMonthlySimulation();
    }

    public void OnLoanDecisionFinished()
    {
        IsLoanDecisionActive = false;
        UIManager.Instance.HideAllPanels();
        StartCoroutine(SimulationRoutine());
    }

    private void HandleForcedLoan()
    {
        float cash = financeManager.cashOnHand;

        if (forcedLoanThisMonth)
            return;

        forcedLoanThisMonth = true;

        if (cash >= 0f)
        {
            forcedLoanHistory.Enqueue(false);
            TrimForcedLoanHistory();
            return;
        }

        if (loanManager == null || !loanManager.CanForceLoan)
            return;

        float shortfall = Mathf.Abs(cash);
        loanManager.ForceBorrow(shortfall);

        PlayerDataManager.Instance.financialMomentum -= 3f;

        forcedLoanHistory.Enqueue(true);
        TrimForcedLoanHistory();

        Debug.Log($"[Loan] Forced loan covered shortfall: ${shortfall:F0}");
    }

    private void TrimForcedLoanHistory()
    {
        if (forcedLoanHistory.Count > 6)
            forcedLoanHistory.Dequeue();
    }
}
