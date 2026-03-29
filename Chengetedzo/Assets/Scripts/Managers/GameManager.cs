using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;
using static UIManager;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Simulation Settings")]
    public int currentMonth = 1;
    public int totalMonths = 24;

    [Header("Momentum")]
    private int savingsStreak = 0;
    private int overBudgetStreak = 0;
    private Queue<bool> skipHistory = new Queue<bool>();
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

    [Header("Event Protection")]
    public int monthsSinceMajorEvent = 3;
    public int majorEventGraceMonths = 2;

    [Header("Year Totals")]
    private float yearIncome = 0f;
    private float yearExpenses = 0f;
    private float yearPremiums = 0f;
    private float yearPayouts = 0f;
    private float yearEventLosses = 0f;

    [Header("Mode")]
    public bool IsGuidedMode = false;

    public float YearIncome => yearIncome;
    public float YearExpenses => yearExpenses;
    public float YearPremiums => yearPremiums;
    public float YearPayouts => yearPayouts;
    public float YearEventLosses => yearEventLosses;

    private List<ResolvedEvent> monthlyEvents = new();
    public bool IsLoanDecisionActive { get; private set; }
    public bool IsSavingsDecisionActive { get; private set; }
    private Queue<bool> forcedLoanHistory = new();
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
    public int SavedSavingsStreak => savingsStreak;
    public int SavedOverBudgetStreak => overBudgetStreak;
    public bool SavedPatternWarningIssued => patternWarningIssued;
    public bool SavedRecoveryAcknowledged => recoveryAcknowledged;
    public int SavedLastMomentumZone => lastMomentumZone;
    public float SavedPreviousMomentum => previousMomentum;
    public List<IncomeEffect> ActiveIncomeEffects => activeIncomeEffects;
    public List<ExpenseEffect> ActiveExpenseEffects => activeExpenseEffects;

    [ContextMenu("DEV — Full Reset (Save + Prefs)")]
    public void DEV_FullReset()
    {
        SaveSystem.DeleteSave();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[DEV] Save file deleted. All PlayerPrefs cleared. Ready for fresh start.");
    }
    [System.Serializable]
    public class ExpenseEffect
    {
        public ExpenseCategory category;
        public float flatIncrease;
        public int remainingMonths; //Remember -1 = permanent
    }

    private List<ExpenseEffect> activeExpenseEffects = new();

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

        uiManager.UpdateMoneyText(0f);
        uiManager.UpdateMonthText(currentMonth, totalMonths);
        visualManager?.UpdateVisuals();
    }

#if UNITY_EDITOR
    private void Update()
    {
        // Ctrl + Shift + R in the editor = instant full reset
        if (Input.GetKey(KeyCode.LeftControl) &&
            Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.R))
        {
            SaveSystem.DeleteSave();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            FullRestart();
            Debug.Log("[DEV] Hot reset triggered.");
        }
        // Ctrl + Shift + T = reset tutorials only (keep save)
        if (Input.GetKey(KeyCode.LeftControl) &&
            Input.GetKey(KeyCode.LeftShift) &&
            Input.GetKeyDown(KeyCode.T))
        {
            TutorialManager.Instance?.ResetAll();
            Debug.Log("[DEV] Tutorial flags cleared. Tutorials will replay.");
        }
    }
#endif

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
        Livestock,
        CropsOrLivestock
    }

    public enum ProfileType
    {
        Informal,
        Formal,
        Farmer
    }

    [System.Serializable]
    public class IncomeEffect
    {
        public float reductionPercent;
        public int remainingMonths; // Remember -1 = permanent
    }

    public void StartNewMonth()
    {
        monthResolutionStarted = false;
        mentorSpokeThisMonth = false;
        forcedLoanThisMonth = false;
        forecastBackLocked = false;
        forecastManager.forecastGeneratedThisMonth = false;
        CurrentLedger = null;

        Debug.Log($"=== Month {currentMonth} START ===");
        EnterForecastPhase();
        loanManager?.ResetMonthlyFlags();
        UpdateIncomeEffects();
        UpdateExpenseEffects();
    }

    public void ConfirmMonthAndResolve()
    {
        if (monthResolutionStarted)
            return;

        monthResolutionStarted = true;

        CurrentLedger = new MonthlyFinancialLedger(currentMonth,financeManager.CashOnHand);

        Debug.Log($"[Month] Confirmed → Resolving Month {currentMonth}");

        SetPhase(GamePhase.Simulation);
        forecastBackLocked = true;
        uiManager.SwitchPanel(UIManager.UIPanelState.Simulation);

        monthlyDamageTaken = 0f;
        monthlyDamageCapBase = financeManager.CashOnHand;

        financeManager.ProcessMonthlyBudget();
        insuranceManager.ProcessMonthlyPremiums();
        loanManager?.ProcessContribution();
        loanManager?.UpdateLoans();

        monthlyEvents = eventManager.GenerateMonthlyEvents(currentMonth);
        Debug.Log($"[Events] Generated: {monthlyEvents.Count} events for month {currentMonth}");

        pendingEvents.Clear();
        foreach (var ev in monthlyEvents)
            pendingEvents.Enqueue(ev);

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

        if (IsRecovery(momentum))
        {
            lineToShow = MentorLines.RecoveryLines[
                Random.Range(0, MentorLines.RecoveryLines.Length)];
        }

        else if (CountForcedLoans() >= 2)
        {
            lineToShow = MentorLines.ForcedLoanPattern[
                Random.Range(0, MentorLines.ForcedLoanPattern.Length)];
        }

        else if (HasZoneChanged(momentum))
        {
            lineToShow = GetZoneLine(momentum);
        }

        else if (!patternWarningIssued && IsNegativePatternForming())
        {
            lineToShow = MentorLines.PatternWarning[
                Random.Range(0, MentorLines.PatternWarning.Length)];

            patternWarningIssued = true;
        }

        if (IsHeadlessSimulation)
            return;

        float monthlyChance = 0.35f;

        if (momentum >= 15f || momentum <= -15f)
            monthlyChance = 0.60f;
        bool forceShow = momentum <= -15f || momentum >= 20f;

        if (lineToShow != null && (forceShow || Random.value < monthlyChance))
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
        if (currentMonth > totalMonths)
        {
            Debug.LogWarning("[GameManager] EndMonthAndAdvance called beyond totalMonths. Ignoring.");
            uiManager.SwitchPanel(UIManager.UIPanelState.None);
            return;
        }

        Debug.Log($"=== Month {currentMonth} END ===");

        SaveSystem.SaveGame(this);

        int finishedMonth = currentMonth;
        currentMonth++;
        OnSeasonChanged?.Invoke();
        monthsSinceMajorEvent++;

        int half = totalMonths / 2;
        int third = totalMonths / 3;
        int twoThirds = (totalMonths * 2) / 3;

        bool isMidYearCheckpoint = totalMonths == 24
            ? (finishedMonth == 6 || finishedMonth == 12 || finishedMonth == 18)
            : (finishedMonth == third || finishedMonth == half || finishedMonth == twoThirds);

        if (isMidYearCheckpoint)
        {
            if (IsHeadlessSimulation)
                StartNewMonth();
            else
                uiManager.ShowMentorMessage(GetMidYearMentorReflection(), () => StartNewMonth());
            return;
        }

        if (finishedMonth >= totalMonths)
        {
            if (!IsHeadlessSimulation)
                uiManager.ShowEndOfYearSummary(GetYearEndMentorReflection());
            CurrentLedger = null;
            return;
        }

        UIManager.Instance.UpdateMonthText(currentMonth, totalMonths);

        if (finishedMonth % 12 == 0)
        {
            float savings = financeManager.generalSavingsBalance;
            if (savings > 0)
            {
                float interest = savings * 0.03f;
                ApplyMoneyChange(FinancialEntry.EntryType.Income, "Savings Interest", interest, true);
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
            if (!mentorSpokeThisMonth && Random.value < 0.25f)
            {
                uiManager.ShowMentorMessage(PickEventMentorLine());
                mentorSpokeThisMonth = true;
            }
            return;
        }

        currentEvent = pendingEvents.Dequeue();

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
        Debug.Log($"[Event] Processing: {currentEvent.title} | MoneyChange: {currentEvent.moneyChange}");

        if (TutorialManager.Instance != null && totalUnexpectedEvents == 1)
        {
            TutorialManager.Instance.OnFirstEvent(
                currentEvent.insurancePayout > 0f,
                currentEvent.insurancePayout,
                () => ShowOrChooseEvent(currentEvent)
            );
        }
        else
        {
            ShowOrChooseEvent(currentEvent);
        }
    }

    private void ShowEvent(ResolvedEvent ev)
    {
        if (IsHeadlessSimulation)
        {
            OnEventPopupClosed();
            return;
        }

        if (uiManager.IsPopupActive)
        {
            StartCoroutine(WaitAndShowEvent(ev));
            return;
        }

        string fullText = BuildEventResultText(ev);
        uiManager.ShowEventPopup(ev.title, fullText, ev.icon);
    }

    private IEnumerator WaitAndShowEvent(ResolvedEvent ev)
    {
        yield return new WaitUntil(() => !uiManager.IsPopupActive);
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
            if (CurrentPhase != GamePhase.Report)
            {
                SetPhase(GamePhase.Report);
                uiManager.ShowReportPanel(CurrentLedger.GetMonthlyBreakdown());
            }
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

        if (financeManager.LastMonthSavingsDelta > 0)
            patternWarningIssued = false;

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

        // First-ever simulation start: show tutorial before processing begins
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnSimulationFirstStart(ConfirmMonthAndResolve);
        }
        else
        {
            ConfirmMonthAndResolve();
        }
    }

    public void OnLoanDecisionFinished()
    {
        IsLoanDecisionActive = false;

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

        if (loanManager == null)
            return;

        float shortfall = Mathf.Abs(cash);

        if (!loanManager.CanForceLoan)
        {
            loanManager.ForceBorrow(shortfall * 0.75f);
        }
        else
        {
            loanManager.ForceBorrow(shortfall);
        }
        
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

    public void ApplyExpenseEffect(ExpenseCategory category, float increase, int months)
    {
        activeExpenseEffects.Add(new ExpenseEffect
        {
            category = category,
            flatIncrease = increase,
            remainingMonths = months <= 0 ? -1 : months
        });

        Debug.Log($"[Expense] {category} increased by ${increase:F0} for " +
                  $"{(months <= 0 ? "permanent" : months + " months")}");
    }

    public float GetExpenseModifier(ExpenseCategory category)
    {
        float total = 0f;
        foreach (var effect in activeExpenseEffects)
            if (effect.category == category)
                total += effect.flatIncrease;
        return total;
    }

    private void UpdateExpenseEffects()
    {
        for (int i = activeExpenseEffects.Count - 1; i >= 0; i--)
        {
            if (activeExpenseEffects[i].remainingMonths == -1)
                continue;

            activeExpenseEffects[i].remainingMonths--;

            if (activeExpenseEffects[i].remainingMonths <= 0)
                activeExpenseEffects.RemoveAt(i);
        }
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
            loanManager != null &&
            loanManager.IsLoanUnlocked &&
            !uiManager.IsPopupActive;

        bool canShowSavings =
            financeManager != null &&
            (financeManager.CashOnHand > 0f ||
             financeManager.generalSavingsBalance > 0f) &&
            !uiManager.IsPopupActive;

        if (canShowLoan) uiManager.ShowLoanTopButton();
        else uiManager.HideLoanTopButton();

        if (canShowSavings) uiManager.ShowSavingsTopButton();
        else uiManager.HideSavingsTopButton();
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

        if (ev.moneyChange != 0f)
        {
            string sign = ev.moneyChange > 0 ? "+" : "-";
            text += $"\n\nMoney: {sign}${Mathf.Abs(ev.moneyChange):F0}";
        }

        if (ev.insurancePayout > 0f)
        {
            text += $"\nInsurance Payout: +${ev.insurancePayout:F0}";
        }

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

    private void ShowOrChooseEvent(ResolvedEvent ev)
    {
        if (IsHeadlessSimulation)
        {
            // Headless: auto-pick first choice (or just close)
            if (ev.hasChoices && ev.choices != null && ev.choices.Count > 0)
                ApplyEventChoice(ev, 0);
            OnEventPopupClosed();
            return;
        }

        if (ev.hasChoices && ev.choices != null && ev.choices.Count > 0)
        {
            UIManager.Instance.ShowChoicePopup(
                ev.title,
                ev.description,
                ev.senderName,
                ev.senderRelation,
                ev.choices,
                choiceIndex =>
                {
                    ApplyEventChoice(ev, choiceIndex);
                    OnEventPopupClosed();
                }
            );
        }
        else
        {
            ShowEvent(ev);
        }
    }

    private void ApplyEventChoice(ResolvedEvent ev, int choiceIndex)
    {
        if (ev.choices == null || choiceIndex < 0 || choiceIndex >= ev.choices.Count)
            return;

        var choice = ev.choices[choiceIndex];

        Debug.Log($"[CHOICE] {ev.title} → '{choice.label}' | Money: {choice.moneyChange:+0;-0} | Momentum: {choice.momentumChange:+0;-0}");

        if (choice.moneyChange != 0f)
        {
            bool isCredit = choice.moneyChange > 0f;
            ApplyMoneyChange(
                isCredit ? FinancialEntry.EntryType.EventReward
                         : FinancialEntry.EntryType.EventLoss,
                $"{ev.title} — {choice.label}",
                Mathf.Abs(choice.moneyChange),
                isCredit
            );
            totalRawEventDamage += Mathf.Max(0f, -choice.moneyChange);
        }

        if (choice.momentumChange != 0f)
            PlayerDataManager.Instance.ModifyMomentum(choice.momentumChange);

        if (choice.incomePercentChange != 0f)
            ApplyIncomeEffect(choice.incomePercentChange, choice.incomeEffectMonths);

        if (choice.affectsLoan && loanManager != null)
            loanManager.ModifyBorrowingPower(choice.borrowingPowerChange);
    }

    public void LoadFromSave(GameSaveData save)
    {
        if (loanManager != null)
        {
            loanManager.loanBalance = save.loanBalance;
            loanManager.borrowingPower = save.borrowingPower;
            loanManager.totalContributed = save.totalContributed;
            loanManager.monthsContributed = save.monthsContributed;
            loanManager.SetRepaymentRate(save.repaymentRate);
            loanManager.missedPayments = save.missedPayments;
            loanManager.onTimePayments = save.onTimePayments;
        }

        if (save.insurancePlans != null)
        {
            foreach (var saved in save.insurancePlans)
            {
                var plan = insuranceManager.allPlans.Find(p => p.type == saved.type);
                if (plan == null) continue;
                plan.isSubscribed = saved.isSubscribed;
                plan.isLapsed = saved.isLapsed;
                plan.monthsPaid = saved.monthsPaid;
                plan.missedPayments = saved.missedPayments;
            }
        }

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

        savingsStreak = save.savingsStreak;
        overBudgetStreak = save.overBudgetStreak;
        patternWarningIssued = save.patternWarningIssued;
        recoveryAcknowledged = save.recoveryAcknowledged;
        lastMomentumZone = save.lastMomentumZone;
        previousMomentum = save.previousMomentum;
        monthsSinceMajorEvent = save.monthsSinceMajorEvent;
        eventManager.SetEventPressure(save.eventPressure);

        uiManager.UpdateMonthText(currentMonth, totalMonths);
        uiManager.UpdateMoneyText(financeManager.CashOnHand);

        StartNewMonth();
    }

    public void FullRestart()
    {
        Debug.Log("=== FULL GAME RESET ===");
        uiManager.SwitchPanel(UIManager.UIPanelState.None);
        financeManager?.ResetFinance();
        eventManager?.ResetAll();
        uiManager.UpdateMoneyText(financeManager.CashOnHand);
        loanManager?.ResetAll();
        insuranceManager.ResetAll();
        PlayerDataManager.Instance?.ResetPlayerData();
        activeExpenseEffects.Clear();

        setupData.minIncome = 0f;
        setupData.maxIncome = 0f;
        setupData.isIncomeStable = true;
        setupData.hasSchoolFees = false;
        setupData.schoolFeesAmount = 0f;
        financeManager.assets = new PlayerAssets();
        setupData.adults = 1;
        setupData.children = 0;
        setupData.housing = HousingType.Renting;
        setupData.ownsCar = false;
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
        monthsSinceMajorEvent = 3;

        currentMonth = 1;

        yearIncome = 0f;
        yearExpenses = 0f;
        yearPremiums = 0f;
        yearPayouts = 0f;
        yearEventLosses = 0f;
        previousMomentum = 0f;

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
        IsLoanDecisionActive = false;
        IsSavingsDecisionActive = false;
        forcedLoanThisMonth = false;
        isWaitingForEventConfirmation = false;
        lastMomentumZone = int.MinValue;

        CurrentLedger = null;

        SetPhase(GamePhase.Idle);

        uiManager.ClearReportPanel();

        uiManager.UpdateMonthText(currentMonth, totalMonths);
        uiManager.UpdateMoneyText(0f);

        pendingEvents.Clear();
        monthlyEvents.Clear();
        IsHeadlessSimulation = false;

        uiManager.SwitchPanel(UIManager.UIPanelState.MainMenu);

        var setup = uiManager.setupPanel.GetComponent<SetupPanelController>();
        setup?.OnPanelOpened();

        SaveSystem.DeleteSave();

        Debug.Log("=== GAME RESET COMPLETE ===");
    }

    public bool HasMonthResolutionStarted()
    {
        return monthResolutionStarted;
    }

    public void ClearProfile()
    {
        IsGuidedMode = false;
        if (setupData == null || financeManager == null)
            return;

        Debug.Log("[Profile] Free Mode selected");

        // Reset to neutral defaults
        setupData.adults = 1;
        setupData.children = 0;
        setupData.isIncomeStable = true;
        setupData.housing = HousingType.Renting;
        setupData.ownsCar = false;

        setupData.minIncome = 0f;
        setupData.maxIncome = 0f;

        financeManager.rentCost = 0f;
        financeManager.groceries = 0f;
        financeManager.transport = 0f;
        financeManager.utilities = 0f;

        setupData.hasSchoolFees = false;
        setupData.schoolFeesAmount = 0f;

        financeManager.assets = new PlayerAssets();

        financeManager.InitializeFromSetup();
    }

    public void ApplyProfile(ProfileType profile)
    {
        IsGuidedMode = true;
        if (setupData == null || financeManager == null)
        {
            Debug.LogError("SetupData or FinanceManager missing.");
            return;
        }

        Debug.Log($"[Profile] Applying: {profile}");

        switch (profile)
        {
            case ProfileType.Informal:

                setupData.adults = 2;
                setupData.children = 2;
                setupData.isIncomeStable = false;
                setupData.housing = HousingType.Renting;
                setupData.ownsCar = false;
                setupData.hasSchoolFees = true;
                setupData.schoolFeesAmount = 60f;

                setupData.minIncome = 180f;
                setupData.maxIncome = 400f;

                financeManager.rentCost = 60f;
                financeManager.groceries = 90f;
                financeManager.transport = 25f;
                financeManager.utilities = 15f;

                financeManager.assets = new PlayerAssets();
            break;

            case ProfileType.Formal:

                setupData.adults = 2;
                setupData.children = 2;
                setupData.isIncomeStable = true;
                setupData.housing = HousingType.Renting;
                setupData.ownsCar = true;
                setupData.hasSchoolFees = true;
                setupData.schoolFeesAmount = 450f;

                setupData.minIncome = 900f;
                setupData.maxIncome = 1600f;

                financeManager.rentCost = 500f;
                financeManager.groceries = 200f;
                financeManager.transport = 80f;
                financeManager.utilities = 45f;

                financeManager.assets = new PlayerAssets
                {
                    hasMotor = true
                };

                financeManager.motorInsuredValue = 12000f;
            break;

            case ProfileType.Farmer:

                setupData.adults = 2;
                setupData.children = 2;
                setupData.isIncomeStable = false;
                setupData.housing = HousingType.OwnsHouse;
                setupData.ownsCar = false;
                setupData.hasSchoolFees = true;
                setupData.schoolFeesAmount = 150f;

                setupData.minIncome = 100f;   // very low months
                setupData.maxIncome = 1200f;  // harvest months

                financeManager.rentCost = 0f;
                financeManager.groceries = 150f;
                financeManager.transport = 40f;
                financeManager.utilities = 25f;

                financeManager.assets = new PlayerAssets
                {
                    hasCrops = true,
                    hasLivestock = true
                };

                financeManager.cropsInsuredValue = 4000f;
                financeManager.livestockInsuredValue = 6000f;
            break;

            /*case ProfileType.HighClass:

                setupData.adults = 2;
                setupData.children = 2;
                setupData.isIncomeStable = false;
                setupData.housing = HousingType.OwnsHouse;
                setupData.ownsCar = true;
                setupData.hasSchoolFees = true;
                setupData.schoolFeesAmount = 3500f;

                setupData.minIncome = 2500f;
                setupData.maxIncome = 5000f;

                financeManager.rentCost = 0f;
                financeManager.groceries = 750f;
                financeManager.transport = 250f;
                financeManager.utilities = 300f;

                financeManager.assets = new PlayerAssets
                {
                    hasHouse = true,
                    hasMotor = true,
                    hasCrops = true,
                    hasLivestock = true
                };

                setupData.houseValue = 150000f;
                financeManager.motorInsuredValue = 50000f;
                financeManager.cropsInsuredValue = 4000f;
                financeManager.livestockInsuredValue = 6000f;
            break;*/
        }

        financeManager.InitializeFromSetup();
        uiManager.UpdateMoneyText(financeManager.CashOnHand);
    }

#if UNITY_EDITOR
    private void RunHeadlessLoop(string testName)
    {
        financeManager.generalSavingsMonthly = 0f;
        financeManager.InitializeFromSetup();


        //insuranceManager?.EnableBasicPlan();


        if (CurrentPhase == GamePhase.Idle)
            StartNewMonth();

        int safetyCounter = 0;
        const int maxSteps = 10000;

        while (currentMonth <= totalMonths)
        {
            if (++safetyCounter > maxSteps)
            {
                Debug.LogError($"❌ {testName} ABORTED: Infinite loop at Month {currentMonth}, Phase {CurrentPhase}.");
                IsHeadlessSimulation = false;
                return;
            }

            switch (CurrentPhase)
            {
                case GamePhase.Forecast: OnForecastConfirmed(); break;
                case GamePhase.Insurance: OnInsuranceConfirmed(); break;
                case GamePhase.Loan: OnLoanDecisionFinished(); break;
                case GamePhase.Savings: OnSavingsDecisionFinished(); break;

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
                Debug.LogError($"❌ {testName} hit unknown phase: {CurrentPhase}");
                IsHeadlessSimulation = false;
                return;
            }
        }

        IsHeadlessSimulation = false;
        uiManager.SwitchPanel(UIManager.UIPanelState.None);
        uiManager.UpdateMoneyText(financeManager.CashOnHand);

        Debug.Log($"✅ {testName} COMPLETE — " +
                  $"Final Cash: ${financeManager.CashOnHand:F0} | " +
                  $"Income: ${yearIncome:F0} | " +
                  $"Expenses: ${yearExpenses:F0} | " +
                  $"Event Losses: ${yearEventLosses:F0} | " +
                  $"Forced Loans: {forcedLoanCount} | " +
                  $"Months Under Pressure: {monthsUnderFinancialPressure}");
    }

    private bool StressTestPreCheck(string testName)
    {
        if (setupData == null)
        {
            Debug.LogError($"❌ {testName}: SetupData not assigned in inspector.");
            return false;
        }
        if (financeManager == null)
        {
            Debug.LogError($"❌ {testName}: FinanceManager not assigned in inspector.");
            return false;
        }
        loanManager?.ForceUnlock();
        currentMonth = 1;
        monthsSinceMajorEvent = 3;
        monthlyDamageTaken = 0f;
        monthResolutionStarted = false;
        isWaitingForEventConfirmation = false;
        forecastBackLocked = false;
        forcedLoanThisMonth = false;
        IsLoanDecisionActive = false;
        IsSavingsDecisionActive = false;
        loanIntroShown = false;
        mentorSpokeThisMonth = false;
        CurrentLedger = null;
        pendingEvents.Clear();
        monthlyEvents.Clear();
        activeIncomeEffects.Clear();
        activeExpenseEffects.Clear();
        savingsStreak = 0;
        overBudgetStreak = 0;
        skipHistory.Clear();
        forcedLoanHistory.Clear();
        previousMomentum = 0f;
        recoveryAcknowledged = false;
        patternWarningIssued = false;
        lastMomentumZone = int.MinValue;
        totalUnexpectedEvents = 0;
        insuredEventsCount = 0;
        totalRawEventDamage = 0f;
        totalInsurancePayoutAmount = 0f;
        forcedLoanCount = 0;
        monthsUnderFinancialPressure = 0;

        yearIncome = 0f;
        yearExpenses = 0f;
        yearPremiums = 0f;
        yearPayouts = 0f;
        yearEventLosses = 0f;

        eventManager?.ResetAll();
        loanManager?.ResetAll();
        insuranceManager?.ResetAll();
        PlayerDataManager.Instance?.ResetPlayerData();
        financeManager?.ResetFinance();

        SetPhase(GamePhase.Idle);
        IsHeadlessSimulation = true;
        return true;
    }

    // ============================================================
    // TEST 1 — DEFAULT (original middle-class baseline, now fixed)
    // ============================================================
    [ContextMenu("DEBUG_StressTest_24Months")]
    public void DEBUG_StressTest_24Months()
    {
        const string NAME = "StressTest [DEFAULT]";
        if (!StressTestPreCheck(NAME)) return;

        Debug.Log($"===== STARTING {NAME} =====");

        setupData.adults = 1;
        setupData.children = 0;
        setupData.isIncomeStable = false;
        setupData.housing = HousingType.Renting;
        setupData.ownsCar = false;
        setupData.hasSchoolFees = false;
        setupData.schoolFeesAmount = 0f;
        setupData.minIncome = 400f;
        setupData.maxIncome = 700f;

        financeManager.rentCost = 100f;
        financeManager.groceries = 80f;
        financeManager.transport = 40f;
        financeManager.utilities = 30f;
        financeManager.assets = new PlayerAssets();

        RunHeadlessLoop(NAME);
    }

    // ============================================================
    // TEST 2 — ZIMBABWE LOW CLASS
    // ============================================================
    [ContextMenu("DEBUG_StressTest_ZW_LowClass")]
    public void DEBUG_StressTest_ZW_LowClass()
    {
        const string NAME = "StressTest [ZW LOW CLASS]";
        if (!StressTestPreCheck(NAME)) return;

        Debug.Log($"===== STARTING {NAME} =====");
        Debug.Log("Profile: Informal sector, 2 adults, 2 kids, renting, no car, no farm.");

        setupData.adults = 2;
        setupData.children = 2;
        setupData.isIncomeStable = false;
        setupData.housing = HousingType.Renting;
        setupData.ownsCar = false;
        setupData.hasSchoolFees = true;
        setupData.schoolFeesAmount = 60f;
        setupData.minIncome = 180f;
        setupData.maxIncome = 400f;

        financeManager.rentCost = 60f;
        financeManager.groceries = 90f;
        financeManager.transport = 25f;
        financeManager.utilities = 15f;
        financeManager.assets = new PlayerAssets();

        RunHeadlessLoop(NAME);
    }

    // ============================================================
    // TEST 3 — ZIMBABWE MIDDLE CLASS
    // ============================================================
    [ContextMenu("DEBUG_StressTest_ZW_MiddleClass")]
    public void DEBUG_StressTest_ZW_MiddleClass()
    {
        const string NAME = "StressTest [ZW MIDDLE CLASS]";
        if (!StressTestPreCheck(NAME)) return;

        Debug.Log($"===== STARTING {NAME} =====");
        Debug.Log("Profile: Civil servant / NGO, 2 adults, 2 kids, renting, owns car.");

        setupData.adults = 2;
        setupData.children = 2;
        setupData.isIncomeStable = false;
        setupData.housing = HousingType.Renting;
        setupData.ownsCar = true;
        setupData.hasSchoolFees = true;
        setupData.schoolFeesAmount = 450f; //Waterfalls Highschool
        setupData.minIncome = 900f;
        setupData.maxIncome = 1600f;

        // Waterfalls 3-room house rental in medium-density suburb
        financeManager.rentCost = 500f;
        financeManager.groceries = 200f;
        financeManager.transport = 80f;
        financeManager.utilities = 45f;
        financeManager.assets = new PlayerAssets
        {
            hasMotor = true
        };
        financeManager.motorInsuredValue = 12000f;

        RunHeadlessLoop(NAME);
    }

    // ============================================================
    // TEST 4 — ZIMBABWE HIGH CLASS
    // ============================================================
    [ContextMenu("DEBUG_StressTest_ZW_HighClass")]
    public void DEBUG_StressTest_ZW_HighClass()
    {
        const string NAME = "StressTest [ZW HIGH CLASS]";
        if (!StressTestPreCheck(NAME)) return;

        Debug.Log($"===== STARTING {NAME} =====");
        Debug.Log("Profile: Business owner, 2 adults, 2 kids, owns house + car + farm.");

        setupData.adults = 2;
        setupData.children = 2;
        setupData.isIncomeStable = false;
        setupData.housing = HousingType.OwnsHouse;
        setupData.ownsCar = true;
        setupData.hasSchoolFees = true;
        setupData.schoolFeesAmount = 3500f; //Lomagundi College
        setupData.minIncome = 2500f;
        setupData.maxIncome = 5000f;
        financeManager.rentCost = 0f;
        financeManager.groceries = 750f;
        financeManager.transport = 250f;
        financeManager.utilities = 300f;
        financeManager.assets = new PlayerAssets
        {
            hasHouse = true,
            hasMotor = true,
            hasCrops = true,
            hasLivestock = true
        };

        setupData.houseValue = 150000f;
        financeManager.motorInsuredValue = 50000f;
        financeManager.cropsInsuredValue = 4000f;
        financeManager.livestockInsuredValue = 6000f;

        RunHeadlessLoop(NAME);
    }
#endif
}