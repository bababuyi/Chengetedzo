using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Simulation Settings")]
    public int currentMonth = 1;
    public int totalMonths = 24;

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
    public PlayerSetupData setupData;

    [Header("Monthly Damage")]
    public float monthlyDamageTaken = 0f;
    public float maxMonthlyDamagePercent = 0.35f;
    private float monthlyDamageCapBase;

    // --- NEW: Disaster grace protection ---
    [Header("Event Protection")]
    public int monthsSinceMajorEvent = 3;
    public int majorEventGraceMonths = 2;

    [Header("Year Totals")]
    private float yearIncome = 0f;
    private float yearExpenses = 0f;
    private float yearPremiums = 0f;
    private float yearPayouts = 0f;
    private float yearEventLosses = 0f;
    public float YearIncome => yearIncome;
    public float YearExpenses => yearExpenses;
    public float YearPremiums => yearPremiums;
    public float YearPayouts => yearPayouts;
    public float YearEventLosses => yearEventLosses;

    private List<ResolvedEvent> monthlyEvents = new();
    public bool IsLoanDecisionActive { get; private set; }
    public bool IsSavingsDecisionActive { get; private set; }
    private Queue<bool> forcedLoanHistory = new(); // last 6 months
    private bool forcedLoanThisMonth = false;
    private List<IncomeEffect> activeIncomeEffects = new();
    public bool IsHeadlessSimulation = false;
    public System.Action OnSeasonChanged;
    private bool mentorSpokeThisMonth = false;
    private bool loanIntroShown = false;
    private bool monthResolutionStarted = false;
    private Queue<ResolvedEvent> pendingEvents = new();
    private ResolvedEvent currentEvent;
    public MonthlyFinancialLedger CurrentLedger { get; private set; }
    private bool isWaitingForEventConfirmation = false;
    private bool forecastBackLocked = false;
    public bool IsForecastBackLocked => forecastBackLocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        GameSaveData save = SaveSystem.LoadGame();

        if (save != null)
        {
            LoadFromSave(save);
            return;
        }

        if (setupData == null)
        {
            Debug.LogError("Game cannot start without SetupData.");
            enabled = false;
            return;
        }

        uiManager.UpdateMoneyText(financeManager.CashOnHand);
        uiManager.UpdateMonthText(currentMonth, totalMonths);

        visualManager?.UpdateVisuals();
        financeManager.InitializeFromSetup();
    }

    // ============================
    // BEHAVIOR TRACKING (YEAR)
    // ============================

    private int totalUnexpectedEvents = 0;
    private int insuredEventsCount = 0;
    private float totalRawEventDamage = 0f;
    private float totalInsurancePayoutAmount = 0f;
    private int forcedLoanCount = 0;
    private int monthsUnderFinancialPressure = 0;

    public int TotalUnexpectedEvents => totalUnexpectedEvents;
    public int InsuredEventsCount => insuredEventsCount;
    public float TotalRawEventDamage => totalRawEventDamage;
    public float TotalInsurancePayoutAmount => totalInsurancePayoutAmount;
    public int ForcedLoanCount => forcedLoanCount;
    public int MonthsUnderFinancialPressure => monthsUnderFinancialPressure;

    //Enums and Structs
    public enum GamePhase
    {
        Idle,
        Forecast,
        Insurance,
        Loan,
        Simulation,
        Report,
        Savings,
        End_of_Year
    }

    public enum Season
    {
        Any,
        Summer,
        Winter
    }

    public enum AssetRequirement
    {
        None,
        House,
        Motor,
        Crops,
        Livestock
    }

    [System.Serializable]
    public class IncomeEffect
    {
        public float reductionPercent;
        public int remainingMonths; // -1 = permanent
    }

    public void StartNewMonth()
    {
        monthResolutionStarted = false;
        mentorSpokeThisMonth = false;
        forcedLoanThisMonth = false;
        forecastManager.forecastGeneratedThisMonth = false;
        Debug.Log($"=== Month {currentMonth} START ===");
        EnterForecastPhase();
        loanManager?.ResetMonthlyFlags();
        UpdateIncomeEffects();
    }

    //Month Controls

    public void ConfirmMonthAndResolve()
    {
        CurrentLedger = new MonthlyFinancialLedger(currentMonth,financeManager.CashOnHand);
        if (monthResolutionStarted)
            return;

        monthResolutionStarted = true;

        Debug.Log($"[Month] Confirmed → Resolving Month {currentMonth}");

        SetPhase(GamePhase.Simulation);
        forecastBackLocked = true;
        uiManager.SwitchPanel(UIManager.UIPanelState.Simulation);

        monthlyDamageTaken = 0f;
        monthlyDamageCapBase = financeManager.CashOnHand;

        // 1. Apply Income & Budget
        financeManager.ProcessMonthlyBudget();

        // 2. Insurance Premiums
        insuranceManager.ProcessMonthlyPremiums();

        // 3. Loan Contribution
        loanManager?.ProcessContribution();
        loanManager?.UpdateLoans();

        // 4. Generate Events
        monthlyEvents = eventManager.GenerateMonthlyEvents(currentMonth);
        Debug.Log($"[Events] Generated: {monthlyEvents.Count} events for month {currentMonth}");

        pendingEvents.Clear();
        foreach (var ev in monthlyEvents)
            pendingEvents.Enqueue(ev);

        // 5. Start stepping through events
        ProcessNextEvent();
    }

    public ForecastManager.ForecastState GetCurrentForecast()
    {
        return forecastManager?.CurrentForecast;
    }

    private void SetPhase(GamePhase phase)
    {
        if (CurrentPhase == phase)
            return;
        CurrentPhase = phase;
        Debug.Log($"[GamePhase] → {phase}");
        Debug.Log($"Current State = {CurrentPhase}");
        UpdateTopButtons();
    }

    public GamePhase CurrentPhase { get; private set; } = GamePhase.Idle;

    public Season GetSeasonForMonth(int month)
    {
        int normalizedMonth = ((month - 1) % 12) + 1;

        if (normalizedMonth == 11 || normalizedMonth == 12 || (normalizedMonth >= 1 && normalizedMonth <= 4))
            return Season.Summer;

        return Season.Winter;
    }

    public Season GetCurrentSeason()
    {
        return GetSeasonForMonth(currentMonth);
    }

    public void OnEventPopupClosed()
    {
        UpdateTopButtons();

        isWaitingForEventConfirmation = false;

        if (CurrentPhase == GamePhase.Simulation)
            ProcessNextEvent();
    }

    private void EvaluateMomentumSignals()
    {
        var player = PlayerDataManager.Instance;

        // --- SIGNAL A: Consistency ---
        bool savedThisMonth = financeManager.LastMonthSavingsDelta > 0f;
        bool paidInsurance = insuranceManager.AnyPremiumPaidThisMonth;

        bool paidLoan = loanManager != null && loanManager.RepaidThisMonth;

        if (savedThisMonth || paidInsurance || paidLoan)
            savingsStreak++;
        else
            savingsStreak = 0;

        if (savingsStreak == 3)
        {
            PlayerDataManager.Instance.ModifyMomentum(3f);
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
            PlayerDataManager.Instance.ModifyMomentum(-5f);
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
            PlayerDataManager.Instance.ModifyMomentum(-4f);
            Debug.Log("[Momentum] Skipping became a habit (-4)");
            skipHistory.Clear();
        }

        if (financeManager.savingsWithdrawnThisMonth > 0 &&
        financeManager.generalSavingsBalance > 0)
        {
            PlayerDataManager.Instance.ModifyMomentum(1f);
        }

    }

    private void EvaluateMentor()
    {
        if (currentMonth <= 1 || mentorSpokeThisMonth)
            return;

        float momentum = PlayerDataManager.Instance.FinancialMomentum;

        string lineToShow = null;

        // PRIORITY 1 — Recovery
        if (IsRecovery(momentum))
        {
            lineToShow = MentorLines.RecoveryLines[
                Random.Range(0, MentorLines.RecoveryLines.Length)];
        }

        // PRIORITY 2 — Forced Loan Pattern
        else if (CountForcedLoans() >= 2)
        {
            lineToShow = MentorLines.ForcedLoanPattern[
                Random.Range(0, MentorLines.ForcedLoanPattern.Length)];
        }

        // PRIORITY 3 — Zone Change
        else if (HasZoneChanged(momentum))
        {
            lineToShow = GetZoneLine(momentum);
        }

        // PRIORITY 4 — Pattern Warning
        else if (!patternWarningIssued && IsNegativePatternForming())
        {
            lineToShow = MentorLines.PatternWarning[
                Random.Range(0, MentorLines.PatternWarning.Length)];

            patternWarningIssued = true;
        }

        if (IsHeadlessSimulation)
            return;

        if (lineToShow != null)
        {
            uiManager.ShowMentorMessage(lineToShow);
            mentorSpokeThisMonth = true;
        }

        previousMomentum = momentum;
    }

    private string GetZoneLine(float momentum)
    {
        int zone = GetMomentumZone(momentum);

        switch (zone)
        {
            case 2:
                return MentorLines.Positive[Random.Range(0, MentorLines.Positive.Length)];
            case 1:
                return MentorLines.Neutral[Random.Range(0, MentorLines.Neutral.Length)];
            case -1:
                return MentorLines.Warning[Random.Range(0, MentorLines.Warning.Length)];
            case -2:
                return MentorLines.Negative[Random.Range(0, MentorLines.Negative.Length)];
        }

        return null;
    }

    private string GetYearEndMentorReflection()
    {
        float momentum = PlayerDataManager.Instance.FinancialMomentum;

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
        float momentum = PlayerDataManager.Instance.FinancialMomentum;

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

    private bool IsNegativePatternForming()
    {
        int skipCount = 0;
        foreach (bool skipped in skipHistory)
            if (skipped) skipCount++;

        return overBudgetStreak >= 2 || skipCount >= 3;
    }

    public void EndMonthAndAdvance()
    {
        Debug.Log($"=== Month {currentMonth} END ===");

        SaveSystem.SaveGame(this);

        int finishedMonth = currentMonth;

        currentMonth++;
        UIManager.Instance.UpdateMonthText(currentMonth, totalMonths);

        OnSeasonChanged?.Invoke();

        monthsSinceMajorEvent++;

        // Mid-year checkpoints
        if (finishedMonth == 6 || finishedMonth == 12 || finishedMonth == 18)
        {
            uiManager.ShowMentorMessage(
                GetMidYearMentorReflection(),
                () => StartNewMonth()
            );
            return;
        }

        // Final year
        if (finishedMonth == 24)
        {
            uiManager.ShowEndOfYearSummary(GetYearEndMentorReflection());
            CurrentLedger = null;
            return;
        }

        // --- NEW: yearly savings interest ---
        if (finishedMonth % 12 == 0)
        {
            float savings = financeManager.generalSavingsBalance;

            if (savings > 0)
            {
                float interest = savings * 0.03f;

                ApplyMoneyChange(
                    FinancialEntry.EntryType.Income,
                    "Savings Interest",
                    interest,
                    true
                );

                Debug.Log($"[Savings] Interest gained: {interest}");
            }
        }

        StartNewMonth();
    }

    public float ApplyMonthlyDamage(float intendedLoss)
    {
        float maxAllowedLoss = monthlyDamageCapBase * maxMonthlyDamagePercent;
        float remainingCap = maxAllowedLoss - monthlyDamageTaken;

        float actualLoss = Mathf.Clamp(intendedLoss, 0f, remainingCap);

        monthlyDamageTaken += actualLoss;

        return actualLoss;
    }

    public void ProcessNextEvent()
    {
        if (pendingEvents.Count == 0)
        {
            EndMonthlyResolution();
            return;
        }

        if (isWaitingForEventConfirmation)
        {
            ShowEvent(currentEvent);
            return;
        }

        if (pendingEvents.Count == 0)
        {
            EndMonthlyResolution();
            return;
        }

        currentEvent = pendingEvents.Dequeue();

        // --- NEW: detect major disasters ---
        float lossPercent = Mathf.Abs(currentEvent.moneyChange) /
                            Mathf.Max(1f, financeManager.CashOnHand);

        if (lossPercent >= 0.25f)
        {
            monthsSinceMajorEvent = 0;
        }

        totalUnexpectedEvents++;
        totalRawEventDamage += Mathf.Abs(currentEvent.moneyChange);
        totalInsurancePayoutAmount += currentEvent.insurancePayout;

        if (currentEvent.insurancePayout > 0f)
        {
            insuredEventsCount++;
        }

        isWaitingForEventConfirmation = true;
        ShowEvent(currentEvent);
    }

    private void ShowEvent(ResolvedEvent ev)
    {
        if (IsHeadlessSimulation)
        {
            OnEventPopupClosed();
            return;
        }

        string fullText = BuildEventResultText(ev);
        uiManager.ShowEventPopup(ev.title, fullText, ev.icon);
    }

    private void EndMonthlyResolution()
    {
        Debug.Log("[Month] All events resolved");
        
        EvaluateMomentumSignals();
        if (financeManager.CashOnHand < 0f)
        {
            monthsUnderFinancialPressure++;
        }

        EvaluateMentor();
        HandleForcedLoan();

        if (CurrentLedger.IsFinalized())
        {
            Debug.LogWarning("Ledger already finalized. Preventing duplicate year accumulation.");
            return;
        }
        CurrentLedger.FinalizeLedger();

        yearIncome += CurrentLedger.TotalIncome;
        yearExpenses += CurrentLedger.TotalExpenses;
        yearPremiums += CurrentLedger.TotalInsurancePremiums;
        yearPayouts += CurrentLedger.TotalInsurancePayouts;
        yearEventLosses += CurrentLedger.TotalEventLosses;

        Debug.Log(CurrentLedger.GetMonthlyBreakdown());

        SetPhase(GamePhase.Report);

        uiManager.ShowReportPanel(
        CurrentLedger.GetMonthlyBreakdown()
        );
    }

    private string PickEventMentorLine()
    {
        if (currentEvent.insurancePayout > 0f)
            return "Protection reduced the impact. That wasn’t accidental.";

        if (currentEvent.moneyChange < 0f)
            return "Losses rarely arrive alone. Stay aware of the pattern.";

        return MentorLines.Neutral[
            Random.Range(0, MentorLines.Neutral.Length)
        ];
    }

    public void BeginLoanDecision()
    {
        if (IsLoanDecisionActive)
            return;
        SetPhase(GamePhase.Loan);
        IsLoanDecisionActive = true;
        uiManager.ShowLoanPanel();

    }

    public void OnInsuranceConfirmed()
    {
        // Only force loan panel once, on first unlock
        if (!loanIntroShown &&
            loanManager != null &&
            loanManager.IsLoanUnlocked)
        {
            loanIntroShown = true;

            SetPhase(GamePhase.Loan);
            uiManager.ShowLoanPanel();
            return;
        }

        uiManager.SwitchPanel(UIManager.UIPanelState.None);
        SetPhase(GamePhase.Simulation);
        ConfirmMonthAndResolve();
    }

    public void OnLoanDecisionFinished()
    {
        IsLoanDecisionActive = false;

        // If month hasn't started resolving yet, start it now
        if (!monthResolutionStarted)
        {
            SetPhase(GamePhase.Simulation);
            ConfirmMonthAndResolve();
            return;
        }

        // If month is already resolving
        if (isWaitingForEventConfirmation)
        {
            SetPhase(GamePhase.Simulation);
            ShowEvent(currentEvent);
            return;
        }

        if (pendingEvents.Count > 0)
        {
            SetPhase(GamePhase.Simulation);
            ProcessNextEvent();
            return;
        }

        // If no events and resolution already done → go to report
        if (CurrentPhase != GamePhase.Report)
        {
            EndMonthlyResolution();
        }
    }

    private void HandleForcedLoan()
    {
        float cash = financeManager.CashOnHand;

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
        
        forcedLoanCount++;
        PlayerDataManager.Instance.ModifyMomentum(-3f);


        forcedLoanHistory.Enqueue(true);
        TrimForcedLoanHistory();

        Debug.Log($"[Loan] Forced loan covered shortfall: ${shortfall:F0}");
    }

    private void TrimForcedLoanHistory()
    {
        if (forcedLoanHistory.Count > 6)
            forcedLoanHistory.Dequeue();
    }

    public void ApplyIncomeEffect(float percent, int months)
    {
        // FUTURE GUARD: prevent stacking permanent effects
        if (months <= 0 &&
        activeIncomeEffects.Exists(e =>
        e.remainingMonths == -1 &&
        Mathf.Approximately(e.reductionPercent, percent)))
        {
            return;
        }

        activeIncomeEffects.Add(new IncomeEffect
        {
            reductionPercent = percent,
            remainingMonths = months <= 0 ? -1 : months
        });

        Debug.Log(
        $"[Income] Applied income change: {percent:+0;-0}% for " +
        $"{(months <= 0 ? "permanent" : months + " months")}"
        );
    }

    public float GetIncomeMultiplier()
    {
        float netChange = 0f;

        foreach (var effect in activeIncomeEffects)
            netChange += effect.reductionPercent;

        netChange = Mathf.Clamp(netChange, -100f, 100f);

        return 1f + (netChange / 100f);
    }

    private void UpdateIncomeEffects()
    {
        for (int i = activeIncomeEffects.Count - 1; i >= 0; i--)
        {
            if (activeIncomeEffects[i].remainingMonths == -1)
                continue;

            activeIncomeEffects[i].remainingMonths--;

            if (activeIncomeEffects[i].remainingMonths <= 0)
                activeIncomeEffects.RemoveAt(i);
        }
    }
    private void UpdateTopButtons()
    {
        if (CurrentPhase != GamePhase.Simulation)
        {
            uiManager.HideLoanTopButton();
            uiManager.HideSavingsTopButton();
            return;
        }

        bool canShowLoan =
            CurrentPhase == GamePhase.Simulation &&
            loanManager != null &&
            loanManager.IsLoanUnlocked &&
            !uiManager.IsPopupActive;

        bool canShowSavings =
            CurrentPhase == GamePhase.Simulation &&
            financeManager != null &&
            (financeManager.CashOnHand > 0f ||
             financeManager.generalSavingsBalance > 0f) &&
            !uiManager.IsPopupActive;

        if (canShowLoan)
            uiManager.ShowLoanTopButton();
        else
            uiManager.HideLoanTopButton();

        if (canShowSavings)
            uiManager.ShowSavingsTopButton();
        else
            uiManager.HideSavingsTopButton();
    }   

    private bool IsRecovery(float currentMomentum)
    {
        if (recoveryAcknowledged)
            return false;

        bool wasCritical = previousMomentum <= -15f;
        bool improving = currentMomentum > previousMomentum;
        bool escapedDanger = currentMomentum >= -5f;

        if (wasCritical && improving && escapedDanger)
        {
            recoveryAcknowledged = true;
            return true;
        }

        return false;
    }

    private int CountForcedLoans()
    {
        int count = 0;

        foreach (bool forced in forcedLoanHistory)
            if (forced) count++;

        return count;
    }

    private bool HasZoneChanged(float momentum)
    {
        int currentZone = GetMomentumZone(momentum);

        if (currentZone != lastMomentumZone)
        {
            lastMomentumZone = currentZone;
            return true;
        }

        return false;
    }

    private int GetMomentumZone(float momentum)
    {
        if (momentum >= 15f) return 2;
        if (momentum >= 0f) return 1;
        if (momentum > -15f) return -1;
        return -2;
    }

    public void BeginSavingsDecision()
    {
        if (IsSavingsDecisionActive)
            return;

        IsSavingsDecisionActive = true;

        SetPhase(GamePhase.Savings);
        uiManager.ShowSavingsPanel();
    }

    public void OnSavingsDecisionFinished()
    {
        IsSavingsDecisionActive = false;

        if (!monthResolutionStarted)
        {
            SetPhase(GamePhase.Simulation);
            ConfirmMonthAndResolve();
            return;
        }

        if (isWaitingForEventConfirmation)
        {
            SetPhase(GamePhase.Simulation);
            ShowEvent(currentEvent);
            return;
        }

        if (pendingEvents.Count > 0)
        {
            SetPhase(GamePhase.Simulation);
            ProcessNextEvent();
            return;
        }

        if (CurrentPhase != GamePhase.Report)
        {
            EndMonthlyResolution();
        }
    }

    public void BeginInsuranceDecision()
    {
        SetPhase(GamePhase.Insurance);
        uiManager.ShowInsurancePanel();
    }

    public void OnSavingsSetupConfirmed(float savings)
    {
        financeManager.generalSavingsMonthly = savings;
        SetPhase(GamePhase.Forecast);
        uiManager.ShowForecastPanel();
        Debug.Log($"ForecastManager ref = {forecastManager}");
        forecastManager.forecastGeneratedThisMonth = false;
        forecastManager.GenerateForecast();
    }

    public void OnForecastConfirmed()
    {
        SetPhase(GamePhase.Insurance);
        uiManager.ShowInsurancePanel();
    }

    public void OnForecastBack()
    {
        SetPhase(GamePhase.Idle);
        uiManager.ShowBudgetPanel();
    }

    public void OnInsuranceBack()
    {
        SetPhase(GamePhase.Forecast);
        uiManager.ShowForecastPanel();
    }

    public void BeginBudgetSetup()
    {
        SetPhase(GamePhase.Idle);
        uiManager.ShowBudgetPanel();
    }

    private void EnterForecastPhase()
    {
        SetPhase(GamePhase.Forecast);
        uiManager.ShowForecastPanel();
        forecastManager.GenerateForecast();
    }

    public void OnBudgetBackRequested()
    {
        SetPhase(GamePhase.Idle);
        uiManager.ShowSetupPanel();
    }

    public void ApplyMoneyChange(
    FinancialEntry.EntryType type,
    string source,
    float amount,
    bool isCredit)
    {
        if (CurrentPhase != GamePhase.Simulation &&
        CurrentPhase != GamePhase.Insurance &&
        CurrentPhase != GamePhase.Loan &&
        CurrentPhase != GamePhase.Savings)
        {
            Debug.LogError("Money mutation outside allowed phases.");
            return;
        }

        if (CurrentLedger == null)
        {
            // Pre-resolution financial mutation (e.g., insurance purchase)
            financeManager.ApplyCashDelta(isCredit ? amount : -amount);
            Debug.Log("[Ledger] Pre-resolution transaction applied without ledger.");
            return;
        }

        var entry = new FinancialEntry(type, source, amount, isCredit);
        CurrentLedger.AddEntry(entry);

        float signed = entry.SignedAmount();

        financeManager.ApplyCashDelta(signed);
    }

    private string BuildEventResultText(ResolvedEvent ev)
    {
        string text = ev.description;

        // Direct money
        if (ev.moneyChange != 0f)
        {
            string sign = ev.moneyChange > 0 ? "+" : "-";
            text += $"\n\nMoney: {sign}${Mathf.Abs(ev.moneyChange):F0}";
        }

        // Insurance payout
        if (ev.insurancePayout > 0f)
        {
            text += $"\nInsurance Payout: +${ev.insurancePayout:F0}";
        }

        // Income percent change
        if (ev.incomePercentChange != 0f)
        {
            string sign = ev.incomePercentChange > 0 ? "+" : "";
            text += $"\nIncome Change: {sign}{ev.incomePercentChange:F0}%";

            if (ev.incomeDurationMonths > 0)
                text += $" for {ev.incomeDurationMonths} months";
            else if (ev.incomeDurationMonths < 0)
                text += " (Permanent)";
        }

        return text;
    }

    public void LoadFromSave(GameSaveData save)
    {
        currentMonth = save.currentMonth;

        financeManager.SetCash(save.cashOnHand);
        financeManager.generalSavingsBalance = save.generalSavingsBalance;
        financeManager.generalSavingsMonthly = save.generalSavingsMonthly;

        yearIncome = save.yearIncome;
        yearExpenses = save.yearExpenses;
        yearPremiums = save.yearPremiums;
        yearPayouts = save.yearPayouts;
        yearEventLosses = save.yearEventLosses;

        totalUnexpectedEvents = save.totalUnexpectedEvents;
        insuredEventsCount = save.insuredEventsCount;
        totalRawEventDamage = save.totalRawEventDamage;
        totalInsurancePayoutAmount = save.totalInsurancePayoutAmount;
        forcedLoanCount = save.forcedLoanCount;
        monthsUnderFinancialPressure = save.monthsUnderFinancialPressure;
        PlayerDataManager.Instance.SetMomentum(save.financialMomentum);

        uiManager.UpdateMonthText(currentMonth, totalMonths);
        uiManager.UpdateMoneyText(financeManager.CashOnHand);

        StartNewMonth();
    }

    public void FullRestart()
    {
        Debug.Log("=== FULL GAME RESET ===");

        // Reset systems
        financeManager?.ResetFinance();
        eventManager?.ResetAll();
        uiManager.UpdateMoneyText(financeManager.CashOnHand);
        loanManager?.ResetAll();
        insuranceManager.ResetAll();
        PlayerDataManager.Instance?.ResetPlayerData();

        // Reset setup data
        setupData.minIncome = 0f;
        setupData.maxIncome = 0f;
        setupData.isIncomeStable = true;
        setupData.hasSchoolFees = false;
        setupData.schoolFeesAmount = 0f;
        financeManager.assets = new PlayerAssets();
        if (forecastManager != null)
        {
            forecastManager.forecastGeneratedThisMonth = false;
        }
        totalUnexpectedEvents = 0;
        insuredEventsCount = 0;
        totalRawEventDamage = 0f;
        totalInsurancePayoutAmount = 0f;
        forcedLoanCount = 0;
        monthsUnderFinancialPressure = 0;

        // Reset GameManager state
        currentMonth = 1;

        yearIncome = 0f;
        yearExpenses = 0f;
        yearPremiums = 0f;
        yearPayouts = 0f;
        yearEventLosses = 0f;

        monthlyDamageTaken = 0f;

        savingsStreak = 0;
        overBudgetStreak = 0;
        skipHistory.Clear();
        forcedLoanHistory.Clear();
        activeIncomeEffects.Clear();

        mentorSpokeThisMonth = false;
        loanIntroShown = false;
        monthResolutionStarted = false;
        recoveryAcknowledged = false;
        patternWarningIssued = false;
        lastMomentumZone = int.MinValue;

        CurrentLedger = null;

        SetPhase(GamePhase.Idle);

        uiManager.ClearReportPanel();

        uiManager.UpdateMonthText(currentMonth, totalMonths);
        uiManager.UpdateMoneyText(0f);

        // HARD STOP SIMULATION STATE
        pendingEvents.Clear();
        monthlyEvents.Clear();
        IsHeadlessSimulation = false;

        uiManager.SwitchPanel(UIManager.UIPanelState.Setup);

        var setup = uiManager.setupPanel.GetComponent<SetupPanelController>();
        setup?.OnPanelOpened();

        SaveSystem.DeleteSave();

        Debug.Log("=== GAME RESET COMPLETE ===");
    }

    public bool HasMonthResolutionStarted()
    {
        return monthResolutionStarted;
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG_StressTest_24Months")]
    public void DEBUG_StressTest_24Months()
    {
        Debug.Log("===== STARTING 24 MONTH STRESS TEST =====");

        IsHeadlessSimulation = true;

        yearIncome = 0f;
        yearExpenses = 0f;
        yearPremiums = 0f;
        yearPayouts = 0f;
        yearEventLosses = 0f;

        if (setupData == null)
        {
            Debug.LogError("❌ SetupData not assigned. Assign a real asset in inspector.");
            return;
        }

        if (financeManager == null)
        {
            Debug.LogError("❌ FinanceManager not assigned in inspector.");
            return;
        }

        financeManager.InitializeFromSetup();

        if (CurrentPhase == GamePhase.Idle)
            StartNewMonth();

        int safetyCounter = 0;
        int maxSteps = 10000;

        financeManager.generalSavingsMonthly = 0f;
        //insuranceManager.DisableAllPlans(); // if you have it

        while (currentMonth <= totalMonths)
        {
            if (++safetyCounter > maxSteps)
            {
                Debug.LogError("❌ STRESS TEST ABORTED: Infinite loop detected.");
                return;
            }

            switch (CurrentPhase)
            {
                case GamePhase.Forecast:
                    OnForecastConfirmed();
                    break;

                case GamePhase.Insurance:
                    //insuranceManager.EnableBasicPlan();
                    OnInsuranceConfirmed();
                    break;

                case GamePhase.Loan:
                    OnLoanDecisionFinished();
                    break;

                case GamePhase.Savings:
                    OnSavingsDecisionFinished();
                    break;

                case GamePhase.Simulation:
                    if (pendingEvents.Count > 0)
                        ProcessNextEvent();
                    else
                        EndMonthlyResolution();
                    break;

                case GamePhase.Report:
                    EndMonthAndAdvance();
                    break;

                default:
                    Debug.LogError($"❌ Unknown phase: {CurrentPhase}");
                    return;
            }
        }

        Debug.Log("✅ STRESS TEST COMPLETE SUCCESSFULLY.");
    }
#endif
}