using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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
    private bool mentorMemory_hasEverClaimed = false;
    private int mentorMemory_consecutiveLowSavingsMonths = 0;
    private bool burialSocietyUnlocked = false;
    public bool BurialSocietyUnlocked => burialSocietyUnlocked;

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
    public bool HasMentorSpokenThisMonth() => mentorSpokeThisMonth;
    public int SavedSavingsStreak => savingsStreak;
    public int SavedOverBudgetStreak => overBudgetStreak;
    public bool SavedPatternWarningIssued => patternWarningIssued;
    public bool SavedRecoveryAcknowledged => recoveryAcknowledged;
    public int SavedLastMomentumZone => lastMomentumZone;
    public float SavedPreviousMomentum => previousMomentum;
    public List<IncomeEffect> ActiveIncomeEffects => activeIncomeEffects;
    public List<ExpenseEffect> ActiveExpenseEffects => activeExpenseEffects;

    private const float BudgetBoostCeilingFraction = 0.80f;
    private const int BudgetBoostCycleMonths = 3;
    private const float BudgetBoostEarlyExitFraction = 1f / 3f;

    private int _sessionId = 0;

    [ContextMenu("DEV — Full Reset (Save + Prefs)")]
    public void DEV_FullReset()
    {
        SaveSystem.DeleteSave();
        TutorialManager.Instance?.ResetAll();
        PlayerPrefs.DeleteKey("SaveExists");
        Debug.Log("[DEV] Save file deleted. Tutorial flags cleared. Settings preserved.");
    }

#if UNITY_EDITOR
    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        bool ctrlShift = kb.leftCtrlKey.isPressed && kb.leftShiftKey.isPressed;

        if (ctrlShift && kb.rKey.wasPressedThisFrame)
        {
            DEV_FullReset();
            FullRestart();
            Debug.Log("[DEV] Hot reset triggered.");
        }

        if (ctrlShift && kb.tKey.wasPressedThisFrame)
        {
            TutorialManager.Instance?.ResetAll();
            Debug.Log("[DEV] Tutorial flags cleared. Tutorials will replay.");
        }
    }
#endif

    [System.Serializable]
    public class ExpenseEffect
    {
        public ExpenseCategory category;
        public float flatIncrease;
        public int remainingMonths; //Remember -1 = permanent
    }

    [System.Serializable]
    public class CategoryState
    {
        public ExpenseCategory category;
        public float baselineAmount;

        public float cutAmount;
        public float accruedMoraleDebt;
        public float originalCutHit;
        public float scar;

        public float boostAmount;
        public float boostCycleValue;
        public int boostMonthsInCycle;

        public int monthsSinceCut;
        public int timesRaised;
        public int monthsSinceLastRaise;
    }

    private float GetCategoryEventInflation(ExpenseCategory cat)
    {
        float total = 0f;
        foreach (var effect in activeExpenseEffects)
            if (effect.category == cat)
                total += effect.flatIncrease;
        return Mathf.Max(0f, total);
    }

    private float GetSurvivingOverProvision(CategoryState s)
    {
        if (s.boostAmount <= 0f) return 0f;
        return Mathf.Max(0f, s.boostAmount - GetCategoryEventInflation(s.category));
    }

    private float BoostValue(float overProvision, float baseline)
    {
        if (baseline <= 0f) return 0f;
        return (overProvision / baseline) * BudgetCutMoraleK;
    }

    private float GetAverageIncome()
    {
        float lo = setupData.minIncome;
        float hi = setupData.maxIncome > 0 ? setupData.maxIncome : lo;
        return (lo + hi) * 0.5f;
    }

    private float ProjectedExpensesWithBoost(ExpenseCategory cat, float newBoostAmount)
    {
        float total = financeManager.GetHousingCost();
        foreach (var c in CuttableCategories)
        {
            float baseline = GetCategoryBaseline(c);
            float boost = (c == cat) ? newBoostAmount : (categoryStates.TryGetValue(c, out var st) ? st.boostAmount : 0f);
            float cut = categoryStates.TryGetValue(c, out var st2) ? st2.cutAmount : 0f;
            total += baseline + GetCategoryEventInflation(c) - cut + boost;
        }
        if (setupData.hasSchoolFees)
        {
            int childCount = Mathf.Max(0, PlayerDataManager.Instance?.Children ?? 0);
            total += ((setupData.schoolFeesAmount * childCount) * 3f) / 12f;
        }
        return total;
    }

    private Dictionary<ExpenseCategory, CategoryState> categoryStates = new();

    private static readonly ExpenseCategory[] CuttableCategories =
    {
        ExpenseCategory.Groceries,
        ExpenseCategory.Transport,
        ExpenseCategory.Utilities
    };

    private CategoryState GetCategoryState(ExpenseCategory cat)
    {
        if (!categoryStates.TryGetValue(cat, out var state))
        {
            state = new CategoryState
            {
                category = cat,
                baselineAmount = GetCategoryBaseline(cat)
            };
            categoryStates[cat] = state;
        }
        if (state.baselineAmount <= 0f)
            state.baselineAmount = GetCategoryBaseline(cat);
        return state;
    }

    private void ApplyBoostPayout(CategoryState s, float payout)
    {
        if (payout <= 0f) return;

        if (s.scar > 0f)
        {
            float absorbed = Mathf.Min(s.scar, payout);
            s.scar -= absorbed;
            payout -= absorbed;
        }
        if (payout > 0f)
            PlayerDataManager.Instance.ModifyFamilyMorale(payout);
    }

    public IEnumerable<CategoryState> ActiveCategoryStates => categoryStates.Values;

    public void SetCategoryBoost(ExpenseCategory cat, float newBoostAmount)
    {
        float baseline = GetCategoryBaseline(cat);
        if (baseline <= 0f) return;

        var s = GetCategoryState(cat);
        newBoostAmount = Mathf.Max(0f, newBoostAmount);

        float ceiling = GetAverageIncome() * BudgetBoostCeilingFraction;
        float projected = ProjectedExpensesWithBoost(cat, newBoostAmount);
        if (projected > ceiling)
        {
            float overshoot = projected - ceiling;
            newBoostAmount = Mathf.Max(0f, newBoostAmount - overshoot);
            Debug.Log($"[BudgetBoost] {cat}: boost clamped to ${newBoostAmount:F0} (ceiling ${ceiling:F0}).");
        }

        float oldBoost = s.boostAmount;

        if (newBoostAmount > oldBoost + 0.01f)
        {
 
            float added = newBoostAmount - oldBoost;
            s.boostAmount = newBoostAmount;
            if (oldBoost <= 0.01f) s.boostMonthsInCycle = 0;
            float addedValue = BoostValue(Mathf.Max(0f, added - 0f), baseline);
            ApplyBoostPayout(s, addedValue);
            Debug.Log($"[BudgetBoost] {cat}: {(oldBoost <= 0.01f ? "set" : "raised")} to ${newBoostAmount:F0} (+${added:F0}). " +
                      $"Immediate +{addedValue:F1} morale (scar now {s.scar:F1}).");
        }
        else if (newBoostAmount < oldBoost - 0.01f)
        {

            float cycleValue = BoostValue(GetSurvivingOverProvision(s), baseline);
            float consolation = cycleValue * BudgetBoostEarlyExitFraction;
            s.boostAmount = newBoostAmount;
            s.boostMonthsInCycle = 0;
            ApplyBoostPayout(s, consolation);
            Debug.Log($"[BudgetBoost] {cat}: reduced to ${newBoostAmount:F0}. " +
                      $"Early-exit consolation +{consolation:F1} morale.");
        }
    }

    private void ProcessBudgetBoosts()
    {
        foreach (var s in categoryStates.Values)
        {
            if (s.boostAmount <= 0f) continue;

            s.boostMonthsInCycle++;
            if (s.boostMonthsInCycle >= BudgetBoostCycleMonths)
            {
                float baseline = GetCategoryBaseline(s.category);
                float value = BoostValue(GetSurvivingOverProvision(s), baseline);
                ApplyBoostPayout(s, value);
                s.boostMonthsInCycle = 0;
                Debug.Log($"[BudgetBoost] {s.category}: 3-month dividend +{value:F1} morale " +
                          $"(surviving over-provision, scar now {s.scar:F1}).");
            }
        }
    }

    [System.Serializable]
    public class MonthSnapshot
    {
        public int month;
        public float income;
        public float expenses;
        public float cashOnHand;
        public float savingsBalance;
        public float eventLoss;
        public bool hadEvent;
        public bool eventWasInsured;
    }

    public List<MonthSnapshot> monthHistory = new List<MonthSnapshot>();

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
        bool hasWeatherEvent = false;
        FindFirstObjectByType<SeasonalBackgroundManager>()?.UpdateForMonth(currentMonth, hasWeatherEvent);
    }

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
        monthResolutionFinished = false;
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
        AdvanceCategoryMonths();
        RollFamilyPrompts();
    }

    // Double check if happened or not
    public void SetMentorSpokeThisMonth(bool value)
    {
        mentorSpokeThisMonth = value;
    }

    public void ConfirmMonthAndResolve()
    {
        if (monthResolutionStarted)
            return;

        monthResolutionStarted = true;
        monthResolutionFinished = false;

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
        bool hasWeatherEvent = monthlyEvents.Exists(e => e.pool == EventPool.Weather);
        FindFirstObjectByType<SeasonalBackgroundManager>()?.UpdateForMonth(currentMonth, hasWeatherEvent);
        Debug.Log($"[Events] Generated: {monthlyEvents.Count} events for month {currentMonth}");
        Debug.Log($"[Background] Month {currentMonth} | Weather Event: {hasWeatherEvent}");
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
        if (!monthResolutionStarted)
        {
            Debug.LogWarning("[GameManager] OnEventPopupClosed ignored — stale callback.");
            return;
        }

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

        if (financeManager.WasOverBudgetThisMonth && financeManager.IncomeCoveredExpensesThisMonth)
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
        {
            Debug.Log($"[MENTOR] EvaluateMentor SKIPPED — month={currentMonth}, alreadySpoke={mentorSpokeThisMonth}");
            return;
        }

        float momentum = PlayerDataManager.Instance.FinancialMomentum;
        Debug.Log($"[MENTOR] EvaluateMentor running — momentum={momentum:F1}");

        string lineToShow = null;
        string reason = "none";

        if (IsRecovery(momentum))
        {
            lineToShow = MentorLines.RecoveryLines[Random.Range(0, MentorLines.RecoveryLines.Length)];
            reason = "Recovery";
        }
        else if (CountForcedLoans() >= 2)
        {
            lineToShow = MentorLines.ForcedLoanPattern[Random.Range(0, MentorLines.ForcedLoanPattern.Length)];
            reason = "ForcedLoanPattern";
        }
        else if (HasZoneChanged(momentum))
        {
            lineToShow = GetZoneLine(momentum);
            reason = $"ZoneChanged (newZone={lastMomentumZone})";
        }
        else if (!patternWarningIssued && IsNegativePatternForming())
        {
            lineToShow = MentorLines.PatternWarning[Random.Range(0, MentorLines.PatternWarning.Length)];
            reason = "PatternWarning";
            patternWarningIssued = true;
        }
        else if (!patternWarningIssued && IsNegativePatternForming())
        {
            lineToShow = MentorLines.PatternWarning[Random.Range(0, MentorLines.PatternWarning.Length)];
            reason = "PatternWarning";
            patternWarningIssued = true;
        }

        Debug.Log($"[MENTOR] Selected reason={reason} | line={(lineToShow ?? "null")}");

        if (IsHeadlessSimulation)
        {
            Debug.Log("[MENTOR] Headless — skipping display");
            return;
        }

        float monthlyChance = 0.35f;
        if (momentum >= 15f || momentum <= -15f) monthlyChance = 0.60f;
        bool forceShow = momentum <= -15f || momentum >= 20f;

        float roll = Random.value;
        Debug.Log($"[MENTOR] forceShow={forceShow} | chance={monthlyChance:F2} | roll={roll:F2} | willShow={lineToShow != null && (forceShow || roll < monthlyChance)}");

        if (lineToShow != null && (forceShow || roll < monthlyChance))
        {
            Debug.Log($"[MENTOR-SHOW] Showing mentor message (reason={reason}): \"{lineToShow}\"");
            uiManager.ShowMentorMessageTransparent(lineToShow);
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

        ProcessBudgetBoosts();

        int finishedMonth = currentMonth;
        currentMonth++;
        OnSeasonChanged?.Invoke();
        monthsSinceMajorEvent++;

        int half = totalMonths / 2;
        int third = totalMonths / 3;
        int twoThirds = (totalMonths * 2) / 3;

        bool isYear1End = totalMonths == 24 && finishedMonth == 12;
        bool isMidYearCheckpoint = isYear1End
            ? false
            : (totalMonths == 24
                ? (finishedMonth == 6 || finishedMonth == 18)
                : (finishedMonth == third || finishedMonth == twoThirds));

        if (isYear1End)
        {
            if (IsHeadlessSimulation)
            {
                ResetYearTotals();
                StartNewMonth();
            }
            else
            {
                uiManager.ShowYearlyReview(GetYearEndMentorReflection(), () =>
                {
                    ResetYearTotals();
                    StartNewMonth();
                });
            }
            return;
        }

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
            if (finishedMonth >= totalMonths)
            {
                SettleBoostsAtGameEnd();
                if (!IsHeadlessSimulation)
                    uiManager.ShowEndOfYearSummary(GetYearEndMentorReflection());
                CurrentLedger = null;
                return;
            }
        }

        StartNewMonth();
    }

    private void SettleBoostsAtGameEnd()
    {
        foreach (var s in categoryStates.Values)
        {
            if (s.boostAmount <= 0f || s.boostMonthsInCycle == 0) continue;
            float baseline = GetCategoryBaseline(s.category);
            float value = BoostValue(GetSurvivingOverProvision(s), baseline);
            ApplyBoostPayout(s, value);
            s.boostMonthsInCycle = 0;
            Debug.Log($"[BudgetBoost] {s.category}: end-of-game settle +{value:F1} morale.");
        }
    }

    public float ApplyMonthlyDamage(float intendedLoss)
    {
        float maxAllowedLoss = Mathf.Max(0f, monthlyDamageCapBase) * maxMonthlyDamagePercent;
        float remainingCap = maxAllowedLoss - monthlyDamageTaken;
        float actualLoss = Mathf.Clamp(intendedLoss, 0f, Mathf.Max(0f, remainingCap));
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
            /*if (!mentorSpokeThisMonth && Random.value < 0.25f)
            {
                uiManager.ShowMentorMessage(PickEventMentorLine());
                mentorSpokeThisMonth = true;
            }*/
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

        if (!currentEvent.pendingClaimDecision)
        {
            totalRawEventDamage += Mathf.Abs(currentEvent.moneyChange);
            totalInsurancePayoutAmount += currentEvent.insurancePayout;
            if (currentEvent.insurancePayout > 0f)
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
        Debug.Log($"[ShowEvent] Calling ShowEventPopup for: {ev.title} | IsPopupActive: {uiManager.IsPopupActive}");
        UIManager.Instance.ShowEventPopup(ev.title, fullText, ev.pool);
    }

    private IEnumerator WaitAndShowEvent(ResolvedEvent ev)
    {
        yield return new WaitUntil(() => !uiManager.IsPopupActive);
        string fullText = BuildEventResultText(ev);
        UIManager.Instance.ShowEventPopup(ev.title, fullText, ev.pool);
    }

    private bool monthResolutionFinished = false;

    private void EndMonthlyResolution()
    {
        if (monthResolutionFinished)
        {
            Debug.LogWarning("[Month] EndMonthlyResolution called twice — ignoring.");
            return;
        }
        monthResolutionFinished = true;
        Debug.Log("[Month] All events resolved");

        EvaluateMomentumSignals();
        if (financeManager.CashOnHand < 0f)
            monthsUnderFinancialPressure++;

        EvaluateMentor();
        CheckMentorMemory();
        HandleForcedLoanDecision();
    }

    /*
    private void EndMonthlyResolution()
    {
        if (monthResolutionFinished)
        {
            Debug.LogWarning("[Month] EndMonthlyResolution called twice — ignoring.");
            return;
        }
        monthResolutionFinished = true;
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

        monthHistory.Add(new MonthSnapshot
        {
            month = currentMonth,
            income = CurrentLedger.TotalIncome,
            expenses = CurrentLedger.TotalExpenses + CurrentLedger.TotalInsurancePremiums,
            cashOnHand = financeManager.CashOnHand,
            savingsBalance = financeManager.generalSavingsBalance,
            eventLoss = CurrentLedger.TotalEventLosses,
            hadEvent = CurrentLedger.TotalEventLosses > 0f,
            eventWasInsured = CurrentLedger.TotalInsurancePayouts > 0f
        });

        SetPhase(GamePhase.Report);

        if (financeManager.LastMonthSavingsDelta > 0)
            patternWarningIssued = false;

        uiManager.ShowReportPanel(
        CurrentLedger.GetMonthlyBreakdown()
        );
    }
    */

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

        // First-ever simulation start: show tutorial before continuing begins
        if (!IsHeadlessSimulation && TutorialManager.Instance != null)
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

        if (CurrentPhase != GamePhase.Report && !monthResolutionFinished)
        {
            EndMonthlyResolution();
        }
    }

    private void HandleForcedLoanDecision()
    {
        if (forcedLoanThisMonth)
        {
            FinalizeLedgerAndShowReport();
            return;
        }
        forcedLoanThisMonth = true;

        float cash = financeManager.CashOnHand;
        if (cash >= 0f)
        {
            forcedLoanHistory.Enqueue(false);
            TrimForcedLoanHistory();
            FinalizeLedgerAndShowReport();
            return;
        }

        float shortfall = Mathf.Abs(cash);
        forcedLoanHistory.Enqueue(true);
        TrimForcedLoanHistory();

        if (CheckConsecutiveForcedLoans())
            return; // TriggerDebtSpiralEnding handles continuation

        if (IsHeadlessSimulation)
        {
            ApplyEmergencyLoan(shortfall);
            FinalizeLedgerAndShowReport();
            return;
        }

        ShowEmergencyLoanChoicePanel(shortfall);
    }

    private bool CheckConsecutiveForcedLoans()
    {
        if (forcedLoanHistory.Count < 3) return false;
        var arr = forcedLoanHistory.ToArray();
        int last = arr.Length;
        if (arr[last - 1] && arr[last - 2] && arr[last - 3])
        {
            TriggerDebtSpiralEnding();
            return true;
        }
        return false;
    }

    private void TriggerDebtSpiralEnding()
    {
        string message = "Three months running, your expenses have outrun your income. " +
                         "This is the debt spiral — borrowing to survive creates the debt that makes survival harder. " +
                         "The simulation ends here, but the lesson is the same in real life: the time to act is before the spiral starts.";
        mentorSpokeThisMonth = true;

        if (IsHeadlessSimulation)
        {
            ApplyEmergencyLoan(Mathf.Abs(financeManager.CashOnHand));
            FinalizeLedgerAndShowReport();
            return;
        }

        uiManager.ShowMentorMessage(message, () =>
        {
            ApplyEmergencyLoan(Mathf.Abs(financeManager.CashOnHand));
            FinalizeLedgerAndShowReport();
        });
    }

    private void ApplyEmergencyLoan(float amount)
    {
        if (loanManager == null) return;
        loanManager.ForceBorrow(loanManager.CanForceLoan ? amount : amount * 0.75f);
        forcedLoanCount++;
        PlayerDataManager.Instance.ModifyMomentum(-3f);
        Debug.Log($"[Loan] Emergency loan applied: ${amount:F0}");
    }

    private float RoundLoanAmount(float amount)
    {
        if (amount <= 0f) return 0f;
        float step = amount > 500f ? 100f : 50f;
        return Mathf.Ceil(amount / step) * step;
    }

    private void ShowEmergencyLoanChoicePanel(float shortfall)
    {
        uiManager.ForceCloseAllPopups();
        float coverAmount = RoundLoanAmount(shortfall);
        float bufferAmount = RoundLoanAmount(shortfall * 2f);

        var choices = new List<EventData.ChoiceOption>
        {
            new EventData.ChoiceOption
            {
                label = $"Borrow ${coverAmount:F0} — just enough",
                resultDescription = $"Borrowed ${coverAmount:F0}. Covers the gap. Pay it back as fast as you can.",
                moneyChange = 0f, momentumChange = 0f
            },
            new EventData.ChoiceOption
            {
                label = $"Borrow ${bufferAmount:F0} — with a buffer",
                resultDescription = $"Borrowed ${bufferAmount:F0}. More cash now, but more to repay later.",
                moneyChange = 0f, momentumChange = 0f
            },
            new EventData.ChoiceOption
            {
                label = "Borrow the minimum and cut spending",
                resultDescription = $"Borrowed ${coverAmount:F0} to cover this month, and you'll trim your budget going forward.",
                moneyChange = 0f, momentumChange = 0f
            }
        };

        uiManager.ShowChoicePopup(
            "You're short this month.",
            "Your expenses came to more than you had. A money lender can cover you — but every dollar borrowed comes back with interest. You can also trim your household spending to lean on debt less.",
            "Farai",
            "Money Lender",
            choices,
            index =>
            {
                if (index == 2)
                {
                    ApplyEmergencyLoan(coverAmount);
                    BeginBudgetCutFlow();
                    return;
                }

                float chosen = index == 0 ? coverAmount : bufferAmount;
                ApplyEmergencyLoan(chosen);
                if (!mentorSpokeThisMonth)
                {
                    string line = MentorLines.ForcedLoan[Random.Range(0, MentorLines.ForcedLoan.Length)];
                    uiManager.ShowMentorMessage(line, FinalizeLedgerAndShowReport);
                    mentorSpokeThisMonth = true;
                }
                else
                {
                    FinalizeLedgerAndShowReport();
                }
            }
        );
    }

    private void BeginBudgetCutFlow()
    {
        if (IsHeadlessSimulation)
        {
            ExpenseCategory best = ExpenseCategory.Groceries;
            float bestRoom = 0f;
            foreach (var cat in new[] { ExpenseCategory.Groceries, ExpenseCategory.Transport, ExpenseCategory.Utilities })
            {
                float room = GetCategoryEffective(cat) - GetCategoryFloor(cat);
                if (room > bestRoom) { bestRoom = room; best = cat; }
            }
            if (bestRoom > 0f)
                SetCategoryProvision(best, GetCategoryFloor(best));
            FinalizeLedgerAndShowReport();
            return;
        }

        uiManager.ShowExpenseAdjustment(FinalizeLedgerAndShowReport);
    }

    private void CheckMentorMemory()
    {
        if (financeManager.generalSavingsBalance < 100f)
            mentorMemory_consecutiveLowSavingsMonths++;
        else
            mentorMemory_consecutiveLowSavingsMonths = 0;

        if (mentorSpokeThisMonth) return;

        if (mentorMemory_consecutiveLowSavingsMonths >= 3)
        {
            uiManager.ShowMentorMessage(
                "Three months without a meaningful buffer. That's not bad luck — that's a gap in the plan. Even $20 set aside consistently changes what you can survive.");
            mentorSpokeThisMonth = true;
            mentorMemory_consecutiveLowSavingsMonths = 0;
            return;
        }

        if (currentMonth == 18 && !mentorMemory_hasEverClaimed)
        {
            bool hasInsurance = insuranceManager.allPlans.Exists(p => p.isSubscribed && !p.isLapsed);
            string line = hasInsurance
                ? "Eighteen months of premiums, no claims. That's not money wasted — that's the cost of protection you were fortunate not to need. Stay consistent."
                : "Eighteen months in without insurance. Think about what a single serious event would cost you right now with nothing in place.";
            uiManager.ShowMentorMessage(line);
            mentorSpokeThisMonth = true;
        }
    }

    private void FinalizeLedgerAndShowReport()
    {
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

        monthHistory.Add(new MonthSnapshot
        {
            month = currentMonth,
            income = CurrentLedger.TotalIncome,
            expenses = CurrentLedger.TotalExpenses + CurrentLedger.TotalInsurancePremiums,
            cashOnHand = financeManager.CashOnHand,
            savingsBalance = financeManager.generalSavingsBalance,
            eventLoss = CurrentLedger.TotalEventLosses,
            hadEvent = CurrentLedger.TotalEventLosses > 0f,
            eventWasInsured = CurrentLedger.TotalInsurancePayouts > 0f
        });

        SetPhase(GamePhase.Report);

        if (financeManager.LastMonthSavingsDelta > 0)
            patternWarningIssued = false;

        uiManager.ShowReportPanel(CurrentLedger.GetMonthlyBreakdown());
    }

    private void TrimForcedLoanHistory()
    {
        if (forcedLoanHistory.Count > 6)
            forcedLoanHistory.Dequeue();
    }

    private const float BudgetCutMoraleK = 20f;
    private const float BudgetCutFloorFraction = 0.5f;

    public float GetCategoryBaseline(ExpenseCategory cat)
    {
        switch (cat)
        {
            case ExpenseCategory.Groceries: return financeManager.groceries;
            case ExpenseCategory.Transport: return financeManager.transport;
            case ExpenseCategory.Utilities: return financeManager.utilities;
            default: return 0f;
        }
    }

    public float GetCategoryEffective(ExpenseCategory cat)
    {
        return GetCategoryBaseline(cat) + GetExpenseModifier(cat);
    }

    public float GetCategoryFloor(ExpenseCategory cat)
    {
        return GetCategoryBaseline(cat) * BudgetCutFloorFraction;
    }

    public void ApplyProvisionChange(ExpenseCategory cat, float newEffective)
    {
        float currentEffective = GetCategoryEffective(cat);
        if (newEffective < currentEffective - 0.01f)
            SetCategoryProvision(cat, newEffective);
        else if (newEffective > currentEffective + 0.01f)
            RestoreCategoryProvision(cat, newEffective);
    }

    private const float BudgetRestoreRecoveryFraction = 0.75f;
    private static readonly float[] NagFractions = { 0.3f, 0.4f, 0.5f };

    public bool RaiseCategory(ExpenseCategory cat, int response)
    {
        var s = GetCategoryState(cat);

        if (s.cutAmount <= 0.01f) return false;
        if (s.timesRaised >= NagFractions.Length) return false;

        if (response == 2)
        {
            float fraction = NagFractions[s.timesRaised];
            float strain = s.originalCutHit * fraction;
            s.timesRaised++;
            s.monthsSinceLastRaise = 0;
            PlayerDataManager.Instance.ModifyFamilyMorale(-strain);
            Debug.Log($"[FamilyPrompt] {cat}: declined (nag #{s.timesRaised}, {fraction:P0} of " +
                      $"original {s.originalCutHit:F1}). FamilyMorale -{strain:F1}.");
            return true;
        }

        float baseline = GetCategoryBaseline(cat);
        float currentEffective = GetCategoryEffective(cat);
        float target = (response == 1)
            ? currentEffective + (baseline - currentEffective) * 0.5f : baseline;

        s.monthsSinceLastRaise = 0;
        Debug.Log($"[FamilyPrompt] {cat}: {(response == 0 ? "restored fully" : "restored halfway")} on request.");
        RestoreCategoryProvision(cat, target);
        return true;
    }

    private void RollFamilyPrompts()
    {
        var candidates = new List<CategoryState>();

        foreach (var s in categoryStates.Values)
        {
            if (s.cutAmount <= 0.01f) continue;
            if (s.timesRaised >= NagFractions.Length) continue;

            float chance = Mathf.Lerp(0.25f, 0.65f, Mathf.Clamp01((s.monthsSinceCut - 1) / 4f));
            if (s.monthsSinceLastRaise >= 3) chance = 0.90f;

            if (Random.value < chance)
                candidates.Add(s);
        }

        if (candidates.Count == 0) return;

        candidates.Sort((a, b) => b.monthsSinceCut.CompareTo(a.monthsSinceCut));
        int raiseCount = Mathf.Min(2, candidates.Count);

        for (int i = 0; i < raiseCount; i++)
        {
            var s = candidates[i];
            if (IsHeadlessSimulation)
            {
                RaiseCategory(s.category, 2);
            }
            else
            {
                ShowFamilyPrompt(s.category);
            }
        }
    }

    private void ShowFamilyPrompt(ExpenseCategory cat)
    {
        var s = GetCategoryState(cat);
        int tier = Mathf.Clamp(s.timesRaised, 0, NagFractions.Length - 1);

        string body = GetFamilyPromptLine(cat, tier);
        string baseName = cat.ToString();

        var choices = new List<EventData.ChoiceOption>
        {
            new EventData.ChoiceOption { label = $"Restore {baseName} fully",   resultDescription = "You restore the full amount. The family is relieved.", moneyChange = 0f, momentumChange = 0f },
            new EventData.ChoiceOption { label = $"Restore {baseName} halfway",  resultDescription = "You put some back. It helps, a little.", moneyChange = 0f, momentumChange = 0f },
            new EventData.ChoiceOption { label = "Not now",                      resultDescription = "You keep things as they are for now.", moneyChange = 0f, momentumChange = 0f }
        };

        uiManager.ShowChoicePopup(
            "A word at home",
            body,
            "Your family",
            "Home",
            choices,
            index => RaiseCategory(cat, index)
        );
    }

    private string GetFamilyPromptLine(ExpenseCategory cat, int tier)
    {
        string thing = cat switch
        {
            ExpenseCategory.Groceries => "food on the table",
            ExpenseCategory.Transport => "getting around",
            ExpenseCategory.Utilities => "the lights and water",
            _ => "things at home"
        };

        switch (tier)
        {
            case 0: return $"The family has noticed there's less for {thing} lately. They're asking, gently, if things can go back to how they were.";
            case 1: return $"It's come up again — {thing} has been tight for a while now, and they're really hoping you can restore it.";
            default: return $"There's tension at home. The cut to {thing} has worn on everyone, and they don't understand why it hasn't been fixed.";
        }
    }

    public void RestoreCategoryProvision(ExpenseCategory cat, float newEffective)
    {
        float baseline = GetCategoryBaseline(cat);
        if (baseline <= 0f) return;

        var state = GetCategoryState(cat);
        if (state.cutAmount <= 0.01f) return;

        newEffective = Mathf.Min(baseline, newEffective);

        float currentEffective = GetCategoryEffective(cat);
        float restoreBy = newEffective - currentEffective;
        if (restoreBy <= 0.01f) return;

        restoreBy = Mathf.Min(restoreBy, state.cutAmount);
        float fractionRestored = restoreBy / state.cutAmount;
        float debtRestored = state.accruedMoraleDebt * fractionRestored;

        float recovered = debtRestored * BudgetRestoreRecoveryFraction;
        float scarred = debtRestored - recovered;

        state.cutAmount -= restoreBy;
        state.accruedMoraleDebt -= debtRestored;
        state.scar += scarred;

        PlayerDataManager.Instance.ModifyFamilyMorale(recovered);

        Debug.Log($"[BudgetRestore] {cat}: effective ${currentEffective:F0} → ${newEffective:F0} " +
                  $"(restored ${restoreBy:F0}, {fractionRestored:P0} of cut). " +
                  $"Recovered +{recovered:F1} morale, scarred {scarred:F1} (total scar {state.scar:F1}). " +
                  $"Remaining cut ${state.cutAmount:F0}.");
    }

    public void SetCategoryProvision(ExpenseCategory cat, float newEffective)
    {
        float baseline = GetCategoryBaseline(cat);
        if (baseline <= 0f) return;

        float floor = GetCategoryFloor(cat);
        newEffective = Mathf.Max(floor, newEffective);

        float currentEffective = GetCategoryEffective(cat);
        float reduction = currentEffective - newEffective;
        if (reduction <= 0.01f) return;

        var state = GetCategoryState(cat);
        state.cutAmount += reduction;
        state.monthsSinceCut = 0;

        float moraleHit = (reduction / baseline) * BudgetCutMoraleK;
        state.accruedMoraleDebt += moraleHit;
        if (state.originalCutHit <= 0.01f)state.originalCutHit = moraleHit;
        PlayerDataManager.Instance.ModifyFamilyMorale(-moraleHit);

        Debug.Log($"[BudgetCut] {cat}: effective ${currentEffective:F0} → ${newEffective:F0} " +
                  $"(cut ${reduction:F0}, total ${state.cutAmount:F0} of ${baseline:F0} baseline). " +
                  $"FamilyMorale -{moraleHit:F1}.");
    }

    private void AdvanceCategoryMonths()
    {
        foreach (var s in categoryStates.Values)
        {
            if (s.cutAmount > 0.01f)
            {
                s.monthsSinceCut++;
                s.monthsSinceLastRaise++;
            }
        }
    }

    public void OpenExpenseAdjustmentFromReport()
    {
        uiManager.ShowExpenseAdjustment(() =>
        {
            uiManager.ShowReportPanel(
                CurrentLedger != null ? CurrentLedger.GetMonthlyBreakdown() : "");
        });
    }

    // Handles the death of a household adult earner.
    public void ApplyAdultEarnerDeath(EventData ev, int currentMonth)
    {
        var pdm = PlayerDataManager.Instance;
        int originalAdults = pdm.OriginalAdults;

        float perDeathLoss = 80f / Mathf.Max(1, originalAdults);
        ApplyIncomeEffect(-perDeathLoss, -1);

        pdm.RemoveAdult();
        int adultsRemaining = pdm.RawAdults;

        Debug.Log($"[Death] Adult earner died. Loss -{perDeathLoss:F1}% (80/{originalAdults}). " +
                  $"Adults remaining: {adultsRemaining}");

        if (adultsRemaining > 0 && ev.startsChain && ev.followUpEvents != null)
        {
            foreach (var next in ev.followUpEvents)
            {
                if (next == null) continue;
                eventManager.ScheduleFollowUp(next, currentMonth + ev.followUpDelay);
            }
            Debug.Log("[Death] Recovery chain scheduled (adult remains).");
        }
        else if (adultsRemaining == 0)
        {
            Debug.Log("[Death] No adults remain — household on income floor, no recovery.");
        }
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

        if (categoryStates.TryGetValue(category, out var state))
        {
            total -= state.cutAmount;
            total += state.boostAmount;
        }

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

    public bool TryHandleAsAdultEarnerDeath(EventData ev, int month)
    {
        if (ev.familyMemberType != FamilyMemberType.AdultEarner)
            return false;
        if (!ev.affectsHousehold || ev.adultsLost <= 0)
            return false;

        ApplyAdultEarnerDeath(ev, month);
        return true;
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
        loanManager.IsLoanUnlocked;

    bool canShowSavings =
        financeManager != null &&
        (financeManager.CashOnHand > 0f ||
         financeManager.generalSavingsBalance > 0f);

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
            ShowOrChooseEvent(currentEvent);
            return;
        }

        if (pendingEvents.Count > 0)
        {
            SetPhase(GamePhase.Simulation);
            ProcessNextEvent();
            return;
        }

        if (CurrentPhase != GamePhase.Report && !monthResolutionFinished)
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
        if (IsGuidedMode)
            uiManager.ShowSetupPanelAtReview();
        else
            uiManager.ShowSetupPanel();
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
        uiManager?.UpdateMoneyText(financeManager.CashOnHand);
    }

    private string BuildEventResultText(ResolvedEvent ev)
    {
        string text = ev.description;

        if (Mathf.Abs(ev.moneyChange) >= 1f)
        {
            string sign = ev.moneyChange > 0 ? "+" : "-";
            float actual = Mathf.Abs(ev.moneyChange);
            text += $"\n\nMoney: {sign}${actual:F0}";
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

        if (ev.affectsExpenses && ev.expenseFlatChange != 0f)
        {
            string sign = ev.expenseFlatChange > 0 ? "+" : "-";
            string duration = ev.expenseEffectMonths == -1
                ? "permanent"
                : ev.expenseEffectMonths == 1
                    ? "this month"
                    : $"{ev.expenseEffectMonths} months";
            text += $"\n{ev.expenseCategoryName} cost: {sign}${Mathf.Abs(ev.expenseFlatChange):F0} ({duration})";
        }

        return text;
    }

    private void ShowOrChooseEvent(ResolvedEvent ev)
    {
        if (IsHeadlessSimulation)
        {
            if (ev.pendingClaimDecision)
            {
                ApplyClaimChoice(ev, true);
                OnEventPopupClosed();
                return;
            }

            if (!ev.hasChoices || ev.choices == null || ev.choices.Count == 0)
            {
                OnEventPopupClosed();
                return;
            }

            int choiceIndex;
            float roll = Random.value;
            int count = ev.choices.Count;

            if (count == 2)
                choiceIndex = roll < 0.5f ? 0 : 1;
            else
                choiceIndex = roll < 0.33f ? 0 : roll < 0.66f ? 1 : 2;
            ApplyEventChoice(ev, choiceIndex);
            OnEventPopupClosed();
            return;
        }

        if (IsHeadlessSimulation) { /* skip UI checks in headless */ }
        else if(uiManager.IsPopupActive)
            return;

        if (ev.pendingClaimDecision)
        {
            ShowEventThenClaim(ev);
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

        if (ev.title == "Funeral Society Invitation")
        {
            burialSocietyUnlocked = true;
            Debug.Log("[Insurance] Burial Society unlocked via Funeral Society Invitation.");
        }

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

        if (choice.moraleChange != 0f)
        {
            switch (choice.moraleType)
            {
                case "Family":
                case "Self":
                    PlayerDataManager.Instance.ModifyFamilyMorale(choice.moraleChange);
                    break;
                case "Social":
                case "Community":
                    PlayerDataManager.Instance.ModifySocialMorale(choice.moraleChange);
                    break;
            }
        }

        if (choice.incomePercentChange != 0f)
            ApplyIncomeEffect(choice.incomePercentChange, choice.incomeEffectMonths);

        if (choice.affectsLoan && loanManager != null)
            loanManager.ModifyBorrowingPower(choice.borrowingPowerChange);
    }

    private void ShowInsuranceClaimChoice(ResolvedEvent ev)
    {
        string planName = insuranceManager.GetPlan(ev.type)?.planName ?? ev.type.ToString();
        float rawLoss = ev.intendedLoss;
        float payout = ev.claimPayout;
        float deductible = ev.claimDeductible;

        var choices = new List<EventData.ChoiceOption>
    {
        new EventData.ChoiceOption
        {
            label = deductible > 0.5f
                ? $"Claim — pay ${deductible:F0} excess, recover ${payout:F0}"
                : $"Claim — recover ${payout:F0}",
            resultDescription = $"Claim approved. {planName} covers ${payout:F0} of this loss.",
            moneyChange = 0f, momentumChange = 0f
        },
        new EventData.ChoiceOption
        {
            label = $"Cover it myself — ${rawLoss:F0} total",
            resultDescription = "No claim made. The full loss comes out of your pocket.",
            moneyChange = 0f, momentumChange = 0f
        }
    };

        uiManager.ShowChoicePopup(
            "Insurance Claim Available",
            $"Your <b>{planName}</b> covers this event.\n\nWith claim: pay ${deductible:F0} excess, recover ${payout:F0}.\nWithout: pay ${rawLoss:F0} in full.",
            planName,
            "Insurance",
            choices,
            index =>
            {
                ApplyClaimChoice(ev, index == 0);
                ShowEvent(ev);
            }
        );
    }

    private void ShowEventThenClaim(ResolvedEvent ev)
    {
        if (IsHeadlessSimulation)
        {
            ShowInsuranceClaimChoice(ev);
            return;
        }

        string text = ev.description;

        UIManager.Instance.ShowEventPopupWithCallback(
            ev.title,
            text,
            ev.pool,
            () => ShowInsuranceClaimChoice(ev)
        );
    }

    private void ApplyClaimChoice(ResolvedEvent ev, bool claimed)
    {
        ev.pendingClaimDecision = false;

        if (claimed)
        {
            float netLoss = Mathf.Max(0f, ev.intendedLoss - ev.claimPayout);
            float cappedNetLoss = ApplyMonthlyDamage(netLoss);

            float grossLossToRecord = cappedNetLoss + ev.claimPayout;

            if (grossLossToRecord > 0f)
                ApplyMoneyChange(FinancialEntry.EntryType.EventLoss, ev.title, grossLossToRecord, false);

            if (ev.claimPayout > 0f)
            {
                ApplyMoneyChange(FinancialEntry.EntryType.InsurancePayout, "Insurance Payout", ev.claimPayout, true);
                insuranceManager.RecordClaimBookkeeping(ev.type, ev.claimPayout);
                totalInsurancePayoutAmount += ev.claimPayout;
                insuredEventsCount++;
            }

            totalRawEventDamage += cappedNetLoss;
            ev.moneyChange = -cappedNetLoss;
            ev.insurancePayout = ev.claimPayout;
            mentorMemory_hasEverClaimed = true;
            Debug.Log($"[Claim] Player claimed — gross loss recorded: ${grossLossToRecord:F0}, payout: ${ev.claimPayout:F0}, net cash: ${-cappedNetLoss:F0}");
        }
        else
        {
            float cappedLoss = ApplyMonthlyDamage(ev.intendedLoss);
            if (cappedLoss > 0f)
                ApplyMoneyChange(FinancialEntry.EntryType.EventLoss, ev.title, cappedLoss, false);
            totalRawEventDamage += cappedLoss;
            ev.moneyChange = -cappedLoss;
            ev.insurancePayout = 0f;
            Debug.Log($"[Claim] Player declined — full loss: ${cappedLoss:F0}");
        }
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

        monthHistory.Clear();
        foreach (var s in save.snapshots)
            monthHistory.Add(new MonthSnapshot
            {
                month = s.month,
                income = s.income,
                expenses = s.expenses,
                cashOnHand = s.cashOnHand,
                savingsBalance = s.savingsBalance,
                eventLoss = s.eventLoss,
                hadEvent = s.hadEvent,
                eventWasInsured = s.eventWasInsured
            });

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
        PlayerDataManager.Instance.SetFamilyMorale(save.familyMorale);
        PlayerDataManager.Instance.SetSocialMorale(save.socialMorale);
        PlayerDataManager.Instance.SetOriginalAdults(save.originalAdults);

        savingsStreak = save.savingsStreak;
        overBudgetStreak = save.overBudgetStreak;
        patternWarningIssued = save.patternWarningIssued;
        recoveryAcknowledged = save.recoveryAcknowledged;
        lastMomentumZone = save.lastMomentumZone;
        previousMomentum = save.previousMomentum;
        monthsSinceMajorEvent = save.monthsSinceMajorEvent;
        eventManager.SetEventPressure(save.eventPressure);
        burialSocietyUnlocked = save.burialSocietyUnlocked;

        uiManager.UpdateMonthText(currentMonth, totalMonths);
        uiManager.UpdateMoneyText(financeManager.CashOnHand);

        StartNewMonth();
    }

    public void FullRestart()
    {
        _sessionId++;
        Debug.Log("=== FULL GAME RESET ===");
        uiManager.SwitchPanel(UIManager.UIPanelState.None);
        financeManager?.ResetFinance();
        eventManager?.ResetAll();
        uiManager.UpdateMoneyText(financeManager.CashOnHand);
        loanManager?.ResetAll();
        insuranceManager.ResetAll();
        PlayerDataManager.Instance?.ResetPlayerData();
        activeExpenseEffects.Clear();
        categoryStates.Clear();
        monthHistory.Clear();
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
        monthResolutionFinished = false;
        recoveryAcknowledged = false;
        patternWarningIssued = false;
        mentorMemory_hasEverClaimed = false;
        mentorMemory_consecutiveLowSavingsMonths = 0;
        burialSocietyUnlocked = false;
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
                setupData.schoolFeesAmount = 80f;

                setupData.minIncome = 280f;
                setupData.maxIncome = 450f;

                financeManager.rentCost = 80f;
                financeManager.groceries = 90f;
                financeManager.transport = 25f;
                financeManager.utilities = 15f;
                financeManager.generalSavingsMonthly = 0f;

                financeManager.assets = new PlayerAssets();

                PlayerDataManager.Instance.SetInitialHousehold(setupData.adults, setupData.children);
                break;

            case ProfileType.Formal:

                setupData.adults = 2;
                setupData.children = 2;
                setupData.isIncomeStable = false;
                setupData.housing = HousingType.Renting;
                setupData.ownsCar = true;
                setupData.hasSchoolFees = true;
                setupData.schoolFeesAmount = 80f;

                setupData.minIncome = 420f;
                setupData.maxIncome = 850f;

                financeManager.rentCost = 150f;
                financeManager.groceries = 140f;
                financeManager.transport = 50f;
                financeManager.utilities = 40f;
                financeManager.generalSavingsMonthly = 0f;

                financeManager.assets = new PlayerAssets
                {
                    hasMotor = true
                };

                financeManager.motorInsuredValue = 6000f;

                PlayerDataManager.Instance.SetInitialHousehold(setupData.adults, setupData.children);
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
                setupData.maxIncome = 650f;  // harvest months

                financeManager.rentCost = 0f;
                financeManager.groceries = 100f;
                financeManager.transport = 40f;
                financeManager.utilities = 25f;
                financeManager.generalSavingsMonthly = 50f;

                financeManager.assets = new PlayerAssets
                {
                    hasCrops = true,
                    hasLivestock = true
                };

                financeManager.cropsInsuredValue = 4000f;
                financeManager.livestockInsuredValue = 6000f;

                PlayerDataManager.Instance.SetInitialHousehold(setupData.adults, setupData.children);
                break;

                /*case ProfileType.HighClass:

                    setupData.adults = 2;
                    setupData.children = 2;
                    setupData.isIncomeStable = false;
                    setupData.housing = HousingType.OwnsHouse;
                    setupData.ownsCar = true;
                    setupData.hasSchoolFees = true;
                    setupData.schoolFeesAmount = 5000f;

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
        Debug.Log($"[PROFILE CHECK] Savings = {financeManager.generalSavingsMonthly}");
        uiManager.UpdateMoneyText(financeManager.CashOnHand);
    }

    private void ResetYearTotals()
    {
        yearIncome = 0f;
        yearExpenses = 0f;
        yearPremiums = 0f;
        yearPayouts = 0f;
        yearEventLosses = 0f;
        totalUnexpectedEvents = 0;
        insuredEventsCount = 0;
        totalRawEventDamage = 0f;
        totalInsurancePayoutAmount = 0f;
        forcedLoanCount = 0;
        monthsUnderFinancialPressure = 0;
        Debug.Log("[Year] Year 1 totals reset for Year 2.");
    }

#if UNITY_EDITOR
    private void RunHeadlessLoop(string testName)
    {
        //financeManager.generalSavingsMonthly = 0f;
        //financeManager.InitializeFromSetup();

        if (CurrentPhase == GamePhase.Idle)
            StartNewMonth();

        bool basicPlanEnabled = false;
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
                case GamePhase.Insurance:
                    if (!basicPlanEnabled)
                    {
                        basicPlanEnabled = true;
                        insuranceManager?.EnableBasicPlan();
                    }
                    OnInsuranceConfirmed();
                    break;
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
        categoryStates.Clear();
        savingsStreak = 0;
        overBudgetStreak = 0;
        skipHistory.Clear();
        forcedLoanHistory.Clear();
        previousMomentum = 0f;
        recoveryAcknowledged = false;
        patternWarningIssued = false;
        mentorMemory_hasEverClaimed = false;
        mentorMemory_consecutiveLowSavingsMonths = 0;
        burialSocietyUnlocked = false;
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

    private void RunStressTestUsingProfile(ProfileType profile, string testName)
    {
        if (!StressTestPreCheck(testName))
            return;

        Debug.Log($"===== STARTING {testName} =====");
        Debug.Log($"Using live ApplyProfile data: {profile}");

        ApplyProfile(profile);

        float savedSavings = financeManager.generalSavingsMonthly;
        financeManager.InitializeFromSetup();
        financeManager.generalSavingsMonthly = savedSavings;

        RunHeadlessLoop(testName);
    }

    // ============================================================
    // TEST Game profiles (Informal, Formal, Farmer)
    // ============================================================

    [ContextMenu("DEBUG_StressTest_Profile_Informal")]
    public void DEBUG_StressTest_Profile_Informal()
    {
        RunStressTestUsingProfile(
            ProfileType.Informal,
            "StressTest [PROFILE INFORMAL]"
        );
    }

    [ContextMenu("DEBUG_StressTest_Profile_Formal")]
    public void DEBUG_StressTest_Profile_Formal()
    {
        RunStressTestUsingProfile(
            ProfileType.Formal,
            "StressTest [PROFILE FORMAL]"
        );
    }

    [ContextMenu("DEBUG_StressTest_Profile_Farmer")]
    public void DEBUG_StressTest_Profile_Farmer()
    {
        RunStressTestUsingProfile(
            ProfileType.Farmer,
            "StressTest [PROFILE FARMER]"
        );
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
        setupData.schoolFeesAmount = 2000f; //Lomagundi College
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

    // ============================================================
    // TEST 5 — BUDGET SYSTEMS
    // ============================================================
    [ContextMenu("DEBUG_TestBudgetSystems")]
    public void DEBUG_TestBudgetSystems()
    {
        const string NAME = "BudgetSystems";
        if (!StressTestPreCheck(NAME)) return;
        ApplyProfile(ProfileType.Formal);
        var pdm = PlayerDataManager.Instance;

        Debug.Log("===== BUDGET SYSTEMS TEST =====");
        Debug.Log($"[T] Start FamilyMorale = {pdm.FamilyMorale:F1} (expect 0)");

        Debug.Log("[T] --- Cut groceries to floor (70) ---");
        SetCategoryProvision(ExpenseCategory.Groceries, 70f);
        Debug.Log($"[T] FamilyMorale = {pdm.FamilyMorale:F1} (expect -10.0)");
        var g = GetCategoryState(ExpenseCategory.Groceries);
        Debug.Log($"[T] cut=${g.cutAmount:F0} debt={g.accruedMoraleDebt:F1} scar={g.scar:F1} (expect cut 70, debt 10, scar 0)");

        Debug.Log("[T] --- Restore groceries halfway (to 105) ---");
        RestoreCategoryProvision(ExpenseCategory.Groceries, 105f);
        Debug.Log($"[T] FamilyMorale = {pdm.FamilyMorale:F1} (expect -6.25 = -10 +3.75)");
        Debug.Log($"[T] cut=${g.cutAmount:F0} debt={g.accruedMoraleDebt:F1} scar={g.scar:F1} (expect cut 35, debt 5.0, scar 1.25)");

        Debug.Log("[T] --- Restore groceries fully (to 140) ---");
        RestoreCategoryProvision(ExpenseCategory.Groceries, 140f);
        Debug.Log($"[T] FamilyMorale = {pdm.FamilyMorale:F1} (expect -2.5 = -6.25 +3.75)");
        Debug.Log($"[T] cut=${g.cutAmount:F0} debt={g.accruedMoraleDebt:F1} scar={g.scar:F1} (expect cut 0, debt 0, scar 2.5)");

        Debug.Log("===== BUDGET SYSTEMS TEST COMPLETE =====");
        IsHeadlessSimulation = false;

        Debug.Log("[T] --- Boost groceries +28 (immediate, absorbs scar 2.5) ---");
        SetCategoryBoost(ExpenseCategory.Groceries, 28f);
        Debug.Log($"[T] FamilyMorale = {pdm.FamilyMorale:F1} (expect -1.0 = -2.5 +1.5)");
        Debug.Log($"[T] boost=${g.boostAmount:F0} cycle={g.boostMonthsInCycle} scar={g.scar:F1} (expect boost 28, cycle 0, scar 0)");

        Debug.Log("[T] --- Simulate 3 months sustained ---");
        ProcessBudgetBoosts();
        ProcessBudgetBoosts();
        ProcessBudgetBoosts();
        Debug.Log($"[T] FamilyMorale = {pdm.FamilyMorale:F1} (expect +3.0 = -1.0 +4.0)");
        Debug.Log($"[T] cycle={g.boostMonthsInCycle} (expect 0 after payout)");

        Debug.Log("[T] --- Drop groceries boost (1/3 consolation) ---");
        SetCategoryBoost(ExpenseCategory.Groceries, 0f);
        Debug.Log($"[T] FamilyMorale = {pdm.FamilyMorale:F1} (expect +4.3 = +3.0 +1.33)");
        Debug.Log("[T] === FAMILY PROMPTS (Transport) ===");

        Debug.Log("[T] --- Cut transport to floor (25) ---");
        SetCategoryProvision(ExpenseCategory.Transport, 25f);
        var t = GetCategoryState(ExpenseCategory.Transport);
        float before = pdm.FamilyMorale;
        Debug.Log($"[T] originalCutHit={t.originalCutHit:F1} (expect 10.0), morale={before:F1}");

        Debug.Log("[T] --- Family raises, player declines (nag #1) ---");
        RaiseCategory(ExpenseCategory.Transport, 2);
        Debug.Log($"[T] morale={pdm.FamilyMorale:F1} (expect {before - 3.0f:F1}), timesRaised={t.timesRaised} (expect 1)");

        Debug.Log("[T] --- Decline again (nag #2) ---");
        RaiseCategory(ExpenseCategory.Transport, 2);
        Debug.Log($"[T] morale={pdm.FamilyMorale:F1} (expect {before - 7.0f:F1}), timesRaised={t.timesRaised} (expect 2)");

        Debug.Log("[T] --- Decline again (nag #3) ---");
        RaiseCategory(ExpenseCategory.Transport, 2);
        Debug.Log($"[T] morale={pdm.FamilyMorale:F1} (expect {before - 12.0f:F1}), timesRaised={t.timesRaised} (expect 3)");

        Debug.Log("[T] --- Family gives up (4th raise ignored) ---");
        bool raised = RaiseCategory(ExpenseCategory.Transport, 2);
        Debug.Log($"[T] raised={raised} (expect False), morale={pdm.FamilyMorale:F1} (unchanged)");
    }
#endif
}