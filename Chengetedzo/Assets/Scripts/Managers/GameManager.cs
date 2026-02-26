using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public PlayerSetupData setupData = new PlayerSetupData();

    [Header("Monthly Damage")]
    public float monthlyDamageTaken = 0f;
    public float maxMonthlyDamagePercent = 0.35f;
    private float monthlyDamageCapBase;

    private List<ResolvedEvent> monthlyEvents = new();
    public bool IsLoanDecisionActive { get; private set; }
    public bool IsSavingsDecisionActive { get; private set; }
    private Queue<bool> forcedLoanHistory = new(); // last 6 months
    private bool forcedLoanThisMonth = false;
    private List<IncomeEffect> activeIncomeEffects = new();
    public bool IsSimulationPaused { get; private set; }
    public System.Action OnSeasonChanged;
    private bool mentorSpokeThisMonth = false;
    private bool loanIntroShown = false;
    private bool monthResolutionStarted = false;
    private Queue<ResolvedEvent> pendingEvents = new();
    private ResolvedEvent currentEvent;
    public MonthlyFinancialLedger CurrentLedger { get; private set; }

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

    private void Start()
    {        
        uiManager.UpdateMoneyText(financeManager.CashOnHand);
        uiManager.UpdateMonthText(currentMonth, totalMonths);

        visualManager?.UpdateVisuals();
        financeManager.InitializeFromSetup();
    }

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

    class MonthContext
    {
        public int monthNumber;
        public List<ResolvedEvent> events;
        public float totalDamage;
        public float totalIncome;
        public float totalInsurancePaid;
    }

    [System.Serializable]
    public class IncomeEffect
    {
        public float reductionPercent;
        public int remainingMonths; // -1 = permanent
    }

    public void StartNewMonth()
    {
        mentorSpokeThisMonth = false;
        forcedLoanThisMonth = false;
        Debug.Log($"=== Month {currentMonth} START ===");
        EnterForecastPhase();
        forecastManager.GenerateForecast();
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
        if (uiManager.IsPopupActive && phase != GamePhase.Simulation)
            return;
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
        UpdateTopButtons();

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

    //private void CheckRecovery(float currentMomentum)
    //{
        //if (recoveryAcknowledged)
          //  return;

        //bool wasCritical = previousMomentum <= -15f;
        //bool improving = currentMomentum > previousMomentum;
        //bool escapedDanger = currentMomentum >= -5f;

        //if (wasCritical && improving && escapedDanger)
        //{
            //if (mentorSpokeThisMonth) return;

            //uiManager.ShowMentorMessage(line);
            //mentorSpokeThisMonth = true;

            //uiManager.ShowMentorMessage(
            //    MentorLines.RecoveryLines[
          //          Random.Range(0, MentorLines.RecoveryLines.Length)]);
        //    recoveryAcknowledged = true;
      //  }
    //}

    public void EndMonthAndAdvance()
    {
        Debug.Log($"=== Month {currentMonth} END ===");

        currentMonth++;
        UIManager.Instance.UpdateMonthText(currentMonth, totalMonths);

        OnSeasonChanged?.Invoke();

        if (currentMonth <= totalMonths)
        {
            StartNewMonth();
        }
        else
        {
            uiManager.ShowEndOfYearSummary(GetYearEndMentorReflection());
        }
        monthResolutionStarted = false;
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

        currentEvent = pendingEvents.Dequeue();

        ShowEvent(currentEvent);
    }

    private void ShowEvent(ResolvedEvent ev)
    {
        string fullDescription = ev.description;

        // Money change
        if (ev.actualMoneyChange != 0f)
        {
            if (ev.actualMoneyChange > 0f)
                fullDescription += $"\n\nYou gained: +${ev.actualMoneyChange:F0}";
            else
                fullDescription += $"\n\nYou lost: -${Mathf.Abs(ev.actualMoneyChange):F0}";
        }

        // Insurance payout
        if (ev.insurancePayout > 0f)
        {
            fullDescription +=
                $"\nInsurance covered: +${ev.insurancePayout:F0}";
        }

        uiManager.ShowEventPopup(ev.title, fullDescription);

        // Update money display immediately
        uiManager.UpdateMoneyText(financeManager.CashOnHand);
    }

    private void EndMonthlyResolution()
    {
        Debug.Log("[Month] All events resolved");

        EvaluateMomentumSignals();
        EvaluateMentor();
        HandleForcedLoan();

        CurrentLedger.FinalizeLedger();
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

        if (currentEvent.actualMoneyChange < 0f)
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

        SetPhase(GamePhase.Simulation);
        ConfirmMonthAndResolve();
    }

    public void OnLoanDecisionFinished()
    {
        IsLoanDecisionActive = false;

        SetPhase(GamePhase.Simulation);

        if (pendingEvents.Count > 0)
            ProcessNextEvent();
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

        SetPhase(GamePhase.Simulation);

        if (pendingEvents.Count > 0)
            ProcessNextEvent();
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
        CurrentPhase != GamePhase.Loan)
        {
            Debug.LogError("Money mutation outside allowed phases.");
            return;
        }

        if (CurrentLedger == null)
        {
            Debug.LogError("Ledger not initialized.");
            return;
        }

        var entry = new FinancialEntry(type, source, amount, isCredit);
        CurrentLedger.AddEntry(entry);

        float signed = entry.SignedAmount();

        financeManager.ApplyCashDelta(signed);
    }
}