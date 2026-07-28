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
    private int mentorMemory_familyStrainStreak = 0;
    private bool mentorMemory_familyStrainMentioned = false; // resets when morale climbs back above the threshold, so a NEW stretch can fire again
    private bool mentorMemory_communityHighMentioned = false; // once per game
    private bool mentorMemory_communityLowMentioned = false;  // once per game
    private bool mentorMemory_goalBuiltMentioned = false;     // once per game, fires the month AFTER goalBuiltMonth
    private bool mentorMemory_scarAckPending = false;         // set by RestoreCategoryProvision, delivered next CheckMentorMemory pass
    private bool mentorMemory_bufferLineShown = false;        // once per game - the low-savings-buffer line fires at most once so the wording never repeats

    public int MentorMemory_FamilyStrainStreak => mentorMemory_familyStrainStreak;
    public bool MentorMemory_FamilyStrainMentioned => mentorMemory_familyStrainMentioned;
    public bool MentorMemory_CommunityHighMentioned => mentorMemory_communityHighMentioned;
    public bool MentorMemory_CommunityLowMentioned => mentorMemory_communityLowMentioned;
    public bool MentorMemory_GoalBuiltMentioned => mentorMemory_goalBuiltMentioned;
    public bool MentorMemory_ScarAckPending => mentorMemory_scarAckPending;
    public bool MentorMemory_BufferLineShown => mentorMemory_bufferLineShown;
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
    public ProfileType CurrentProfileType { get; private set; } = ProfileType.Formal;

    [Header("Personal Goal")]
    public bool GoalBuilt { get; private set; }
    private bool goalReachedOnce;      // first crossing announced (also drives the +1 "pass but not really" score)
    private int goalMonthsSinceOffer;  // re-offer pacing - offered again once this hits 2
    private int goalMilestoneReached;  // 0/25/50/75/100 - highest mentor milestone already fired
    private int freeGoalIndex = -1;    // Free Mode pool pick, rolled at setup confirm
    private float freeGoalTarget;
    private int goalBuiltMonth = -1;   // month number GoalBuilt flipped true, for the year-end line

    public bool HasGoalBeenReached => goalReachedOnce;
    public int FreeGoalIndex => freeGoalIndex;
    public float FreeGoalTarget => freeGoalTarget;
    public int GoalBuiltMonth => goalBuiltMonth;
    public int GoalMonthsSinceOffer => goalMonthsSinceOffer;
    public int GoalMilestoneReached => goalMilestoneReached;
    private List<IncomeBenefit> activeIncomeBenefits = new();
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
    public List<IncomeBenefit> ActiveIncomeBenefits => activeIncomeBenefits;
    public List<ExpenseEffect> ActiveExpenseEffects => activeExpenseEffects;

    private const float BudgetBoostCeilingFraction = 0.80f;
    private const int BudgetBoostCycleMonths = 3;
    private const float BudgetBoostEarlyExitFraction = 1f / 3f;

    [ContextMenu("DEV - Full Reset (Save + Prefs)")]
    public void DEV_FullReset()
    {
        SaveSystem.DeleteAllSaves();
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
        // No auto-load here - per-profile save slots mean the decision happens per
        // profile, not globally. UIManager.HandleProfileOrFreeModeSelection checks
        // SaveSystem.SaveExists(profile, guided) when a profile/Free Mode is clicked
        // and offers Continue/Start-over there. Auto-loading used to run before
        // UIManager.Start forced MainMenu, so clicking through profile select would
        // call ApplyProfile → InitializeFromSetup and wipe the loaded state.
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
        FindFirstObjectByType<CloudSpawner>()?.UpdateForMonth(currentMonth, hasWeatherEvent);
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
        CropsOrLivestock,
        NoMotor,
    }

    public enum ProfileType
    {
        Informal,
        Formal,
        Farmer
    }

    [System.Serializable]
    public class IncomeBenefit
    {
        public float amount;         // flat $ per month
        public int remainingMonths;  // -1 = permanent
    }

    [System.Serializable]
    public class IncomeEffect
    {
        public float reductionPercent;
        public int remainingMonths; // Remember -1 = permanent
    }

    public void StartNewMonth()
    {
        if (!IsHeadlessSimulation && currentMonth > 1)
            SaveSystem.SaveGame(this);

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
        UpdateIncomeBenefits();
        AdvanceCategoryMonths();
        uiManager?.UpdateGoalProgressText();
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
        FindFirstObjectByType<CloudSpawner>()?.UpdateForMonth(currentMonth, hasWeatherEvent);
        Debug.Log($"[Events] Generated: {monthlyEvents.Count} events for month {currentMonth}");
        Debug.Log($"[Background] Month {currentMonth} | Weather Event: {hasWeatherEvent}");
        var combined = new List<ResolvedEvent>(monthlyEvents);
        foreach (var prompt in BuildFamilyPrompts())
        {
            int insertAt = Random.Range(0, combined.Count + 1);
            combined.Insert(insertAt, prompt);
        }

        float activeGoalTarget = GoalDefs.GetActiveGoalTarget();
        if (!GoalBuilt && activeGoalTarget > 0f &&
            financeManager.generalSavingsBalance >= activeGoalTarget &&
            (!goalReachedOnce || goalMonthsSinceOffer >= 2))
        {
            goalReachedOnce = true;
            int insertAt = Random.Range(0, combined.Count + 1);
            combined.Insert(insertAt, BuildGoalPromptEvent(activeGoalTarget));
        }

        pendingEvents.Clear();
        foreach (var ev in combined)
            pendingEvents.Enqueue(ev);

        if (IsHeadlessSimulation)
        {
            ProcessNextEvent();
            return;
        }

        // Play the month as a watched sequence - income lands, expenses drain -
        // then the events interrupt. Events wait until the ticker finishes.
        isMonthTickerPlaying = true;
        UpdateTopButtons();
        uiManager.PlayMonthTicker(CurrentLedger, () =>
        {
            isMonthTickerPlaying = false;
            UpdateTopButtons();
            ProcessNextEvent();
        });
    }

    private bool isMonthTickerPlaying = false;
    public bool IsMonthTickerPlaying => isMonthTickerPlaying;

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
            Debug.LogWarning("[GameManager] OnEventPopupClosed ignored - stale callback.");
            return;
        }

        UpdateTopButtons();
        isWaitingForEventConfirmation = false;

        if (CurrentPhase != GamePhase.Simulation)
            return;

        // Event Ticker Beat
        bool hasMoneyMove = !IsHeadlessSimulation && currentEvent != null &&
            (Mathf.Abs(currentEvent.moneyChange) >= 1f || currentEvent.insurancePayout > 0f);

        if (hasMoneyMove)
        {
            float delta = currentEvent.moneyChange + currentEvent.insurancePayout;
            float fromBalance = financeManager.CashOnHand - delta;
            uiManager.PlayMoneyBeat(currentEvent.title, delta, fromBalance, ProcessNextEvent);
        }
        else
        {
            ProcessNextEvent();
        }
        // Remove asset
        if (currentEvent != null && currentEvent.destroysAsset != AssetRequirement.None && financeManager != null)
        {
            var a = financeManager.assets;
            switch (currentEvent.destroysAsset)
            {
                case AssetRequirement.Motor: a.hasMotor = false; break;
                case AssetRequirement.House: a.hasHouse = false; break;
                case AssetRequirement.Crops: a.hasCrops = false; break;
                case AssetRequirement.Livestock: a.hasLivestock = false; break;
            }
            financeManager.assets = a;
            financeManager.RecalculateAssetValues();
            Debug.Log($"[Assets] '{currentEvent.destroysAsset}' destroyed by {currentEvent.title}. Payout was {currentEvent.insurancePayout:F0}.");
        }
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
            Debug.Log($"[MENTOR] EvaluateMentor SKIPPED - month={currentMonth}, alreadySpoke={mentorSpokeThisMonth}");
            return;
        }

        float momentum = PlayerDataManager.Instance.FinancialMomentum;
        Debug.Log($"[MENTOR] EvaluateMentor running - momentum={momentum:F1}");

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
            Debug.Log("[MENTOR] Headless - skipping display");
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

        string baseLine;
        if (momentum >= 20f)
            baseLine = MentorLines.YearEndStrong[Random.Range(0, MentorLines.YearEndStrong.Length)];
        else if (momentum >= 5f)
            baseLine = MentorLines.YearEndPositive[Random.Range(0, MentorLines.YearEndPositive.Length)];
        else if (momentum >= -4f)
            baseLine = MentorLines.YearEndNeutral[Random.Range(0, MentorLines.YearEndNeutral.Length)];
        else if (momentum >= -19f)
            baseLine = MentorLines.YearEndWarning[Random.Range(0, MentorLines.YearEndWarning.Length)];
        else
            baseLine = MentorLines.YearEndNegative[Random.Range(0, MentorLines.YearEndNegative.Length)];

        string goalLine = GetGoalCheckupLine();
        return string.IsNullOrEmpty(goalLine) ? baseLine : $"{baseLine} {goalLine}";
    }

    private string GetMidYearMentorReflection()
    {
        float momentum = PlayerDataManager.Instance.FinancialMomentum;

        string baseLine;
        if (momentum >= 15f)
            baseLine = MentorLines.MidYearStrong[Random.Range(0, MentorLines.MidYearStrong.Length)];
        else if (momentum >= 5f)
            baseLine = MentorLines.MidYearPositive[Random.Range(0, MentorLines.MidYearPositive.Length)];
        else if (momentum >= -4f)
            baseLine = MentorLines.MidYearNeutral[Random.Range(0, MentorLines.MidYearNeutral.Length)];
        else if (momentum >= -14f)
            baseLine = MentorLines.MidYearWarning[Random.Range(0, MentorLines.MidYearWarning.Length)];
        else
            baseLine = MentorLines.MidYearNegative[Random.Range(0, MentorLines.MidYearNegative.Length)];

        string goalLine = GetGoalCheckupLine();
        return string.IsNullOrEmpty(goalLine) ? baseLine : $"{baseLine} {goalLine}";
    }

    // Appended to both the mid-year and year-end mentor reflections - a quick
    // "where's the goal fund at" checkup. Guided profiles only, per spec.
    // Note: titles are quoted rather than following "the {title}" literally, since
    // "the Her own market stall" / "the A second cow" read wrong - quoting sidesteps
    // the grammar regardless of which goal is active.
    private string GetGoalCheckupLine()
    {
        if (!IsGuidedMode) return "";

        float target = GoalDefs.GetActiveGoalTarget();
        if (target <= 0f) return "";

        string title = GoalDefs.GetActiveGoalTitle();

        if (GoalBuilt)
            return $"And \"{title}\" is already working for you.";

        float pct = Mathf.Clamp01(financeManager.generalSavingsBalance / target) * 100f;
        return pct >= 50f
            ? $"\"{title}\" is within reach. Protect that fund."
            : $"\"{title}\" is still far off. Small, steady amounts get there; heroic months don't come.";
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

        // Save moved to StartNewMonth (see there for rationale) - saving here, before
        // ProcessBudgetBoosts/currentMonth++, meant a resume would replay the finished
        // month and lose boost dividends.
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
            SettleBoostsAtGameEnd();
            SaveSystem.DeleteSave(CurrentProfileType, IsGuidedMode); // this slot only - other profiles' saves are untouched
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
                SaveSystem.DeleteSave(CurrentProfileType, IsGuidedMode); // unreachable in the current 24-month config (caught above), kept for safety
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
        if (IsSavingsDecisionActive || IsLoanDecisionActive)
            return;

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
                currentEvent.isReward,
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
        if (CurrentLedger == null || !monthResolutionStarted) return;
        if (monthResolutionFinished)
        {
            Debug.LogWarning("[Month] EndMonthlyResolution called twice - ignoring.");
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
            Debug.LogWarning("[Month] EndMonthlyResolution called twice - ignoring.");
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
            return "Protection reduced the impact. That wasn't accidental.";

        if (currentEvent.moneyChange < 0f)
            return "Losses rarely arrive alone. Stay aware of the pattern.";

        return MentorLines.Neutral[
            Random.Range(0, MentorLines.Neutral.Length)
        ];
    }

    private GamePhase phaseBeforeLoan = GamePhase.Simulation;

    public void BeginLoanDecision()
    {
        if (IsLoanDecisionActive)
            return;
        phaseBeforeLoan = CurrentPhase;
        SetPhase(GamePhase.Loan);
        IsLoanDecisionActive = true;
        if (isMonthTickerPlaying) uiManager.PauseMonthTicker();
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

        System.Action afterSimStartTutorial = () =>
        {
            string goalTitle = !IsHeadlessSimulation ? GoalDefs.GetActiveGoalTitle() : "";
            if (!string.IsNullOrEmpty(goalTitle) && TutorialManager.Instance != null)
                TutorialManager.Instance.OnGoalIntro(goalTitle, ConfirmMonthAndResolve);
            else
                ConfirmMonthAndResolve();
        };

        // First-ever simulation start: show tutorial before continuing begins
        if (!IsHeadlessSimulation && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnSimulationFirstStart(afterSimStartTutorial);
        }
        else
        {
            afterSimStartTutorial();
        }
    }

    public void OnLoanDecisionFinished()
    {
        IsLoanDecisionActive = false;

        if (phaseBeforeLoan == GamePhase.Report)
        {
            phaseBeforeLoan = GamePhase.Simulation;
            SetPhase(GamePhase.Report);
            uiManager.ShowReportPanel(
                CurrentLedger != null ? CurrentLedger.GetMonthlyBreakdown() : "");
            return;
        }

        SetPhase(GamePhase.Simulation);

        if (isMonthTickerPlaying)
        {
            uiManager.ResumeMonthTicker();
            return;
        }

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
            return;

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
                         "This is the debt spiral. Borrowing to survive creates the debt that makes survival harder. " +
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
        // ForceBorrow now waterfalls Mukando then Moneylender internally and caps to
        // whatever's actually available from each, so there's no need to pre-shrink the
        // request here the way the single-account version did.
        loanManager.ForceBorrow(amount);
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
                label = $"Borrow ${coverAmount:F0}, just enough",
                resultDescription = $"Borrowed ${coverAmount:F0}. Covers the gap. Pay it back as fast as you can.",
                moneyChange = 0f, momentumChange = 0f
            },
            new EventData.ChoiceOption
            {
                label = $"Borrow ${bufferAmount:F0}, with a buffer",
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
            "Your expenses came to more than you had. A money lender can cover you, but every dollar borrowed comes back with interest. You can also trim your household spending to lean on debt less.",
            "Ndlovu",
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

        // Family strain streak - advances regardless of whether the mentor speaks this
        // month, same as the low-savings counter above.
        if (PlayerDataManager.Instance.FamilyMorale < -10f)
        {
            mentorMemory_familyStrainStreak++;
        }
        else
        {
            mentorMemory_familyStrainStreak = 0;
            mentorMemory_familyStrainMentioned = false; // stretch over - a future stretch can fire again
        }

        if (mentorSpokeThisMonth) return;

        // All state above still advances in headless runs; everything below this line
        // only opens real UI, so it skips in headless the same way CheckGoalMilestone does.
        // (This also fixes a pre-existing gap: the low-savings/18-month checks below had
        // no headless guard before, so stress tests were already popping real mentor
        // popups - just less often than the goal milestones did.)
        if (IsHeadlessSimulation) return;

        if (mentorMemory_consecutiveLowSavingsMonths >= 3 && !mentorMemory_bufferLineShown)
        {
            uiManager.ShowMentorMessage(GetBufferLine(financeManager.CashOnHand));
            mentorSpokeThisMonth = true;
            mentorMemory_consecutiveLowSavingsMonths = 0;
            mentorMemory_bufferLineShown = true;
            return;
        }

        if (currentMonth == 18 && !mentorMemory_hasEverClaimed)
        {
            bool hasInsurance = insuranceManager.allPlans.Exists(p => p.isSubscribed && !p.isLapsed);
            string line = hasInsurance
                ? "Eighteen months of premiums, no claims. That's not money wasted. That's the cost of protection you were fortunate not to need. Stay consistent."
                : "Eighteen months in without insurance. Think about what a single serious event would cost you right now with nothing in place.";
            uiManager.ShowMentorMessage(line);
            mentorSpokeThisMonth = true;
        }

        if (!mentorSpokeThisMonth && mentorMemory_familyStrainStreak >= 2 && !mentorMemory_familyStrainMentioned)
        {
            mentorMemory_familyStrainMentioned = true;
            uiManager.ShowMentorMessage(
                "The house has been heavy for a while now. Money troubles pass. How you treated people during them is what gets remembered.");
            mentorSpokeThisMonth = true;
        }

        float socialMorale = PlayerDataManager.Instance.SocialMorale;

        if (!mentorSpokeThisMonth && !mentorMemory_communityHighMentioned && socialMorale >= 10f)
        {
            mentorMemory_communityHighMentioned = true;
            uiManager.ShowMentorMessage(
                "People speak well of you around here. That goodwill is worth more than it looks. Communities carry their own through the hard months.");
            mentorSpokeThisMonth = true;
        }

        if (!mentorSpokeThisMonth && !mentorMemory_communityLowMentioned && socialMorale <= -8f)
        {
            mentorMemory_communityLowMentioned = true;
            uiManager.ShowMentorMessage(
                "You've been saying no to everyone lately. Sometimes that's necessary. But the day you need help, the answers may sound a lot like yours.");
            mentorSpokeThisMonth = true;
        }

        if (!mentorSpokeThisMonth && GoalBuilt && !mentorMemory_goalBuiltMentioned && currentMonth > goalBuiltMonth)
        {
            mentorMemory_goalBuiltMentioned = true;
            uiManager.ShowMentorMessage(
                "You didn't just save money. You turned it into something that works for you. That's the whole lesson, right there.");
            mentorSpokeThisMonth = true;
        }

        if (!mentorSpokeThisMonth && mentorMemory_scarAckPending)
        {
            mentorMemory_scarAckPending = false;
            uiManager.ShowMentorMessage(
                "Things are back to normal at home, on paper. You'll notice it doesn't feel quite like before. Normal isn't the same as mended. Give more than you took, and it can be.");
            mentorSpokeThisMonth = true;
        }

        if (!mentorSpokeThisMonth)
            CheckGoalMilestone();
    }

    // Balance-keyed variant for the once-per-game low-savings-buffer line. Picking the
    // wording off the live cash position (rather than one fixed string) is what keeps it
    // from reading as tone-deaf when the player is already negative by the time it fires.
    private string GetBufferLine(float cashOnHand)
    {
        if (cashOnHand < 0f)
        {
            return "Three months without a meaningful buffer, and you're already behind. Right now the goal isn't $20 in savings - it's stopping the gap from growing. Look at what's costing more than it's worth.";
        }
        else if (cashOnHand < 100f)
        {
            return "Three months without a meaningful buffer. You're sitting right at the edge - one surprise expense away from being in the hole. This is the moment to start closing that gap.";
        }
        else
        {
            return "Three months without a meaningful buffer. That's not bad luck. That's a gap in the plan. Even $20 set aside consistently changes what you can survive.";
        }
    }

    // Fires once per milestone (25/50/75/100%) the first time savings cross it,
    // through the same mentorSpokeThisMonth slot as the rest of CheckMentorMemory.
    private void CheckGoalMilestone()
    {
        float target = GoalDefs.GetActiveGoalTarget();
        if (target <= 0f) return;

        float pct = Mathf.Clamp01(financeManager.generalSavingsBalance / target) * 100f;
        int milestone = pct >= 100f ? 100 : pct >= 75f ? 75 : pct >= 50f ? 50 : pct >= 25f ? 25 : 0;

        if (milestone == 0 || milestone <= goalMilestoneReached) return;
        goalMilestoneReached = milestone;

        if (IsHeadlessSimulation) return; // state above still advances; only the popup skips

        if (GoalBuilt) return; // already built - the milestone nudge no longer applies

        string title = GoalDefs.GetActiveGoalTitle();
        string line = milestone switch
        {
            25 => $"A quarter of the way to \"{title}\". Most people never start. You did.",
            50 => "Halfway there. This is where it gets tempting to dip in. Don't.",
            75 => "So close you can see it. Protect that fund like it's already yours.",
            100 => "The money is sitting there. A dream you don't act on is just a number in a book.",
            _ => null
        };
        if (line == null) return;

        uiManager.ShowMentorMessage(line);
        mentorSpokeThisMonth = true;
    }

    private void FinalizeLedgerAndShowReport()
    {
        if (CurrentLedger == null)
        {
            Debug.LogWarning("[Ledger] FinalizeLedgerAndShowReport called with no active ledger - stale callback from a previous game. Ignoring.");
            return;
        }

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
        BuildForecastReview();

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
        float baseline = GetCategoryBaseline(cat);
        if (baseline <= 0f) return;
        var state = GetCategoryState(cat);

        float targetBoost = Mathf.Max(0f, newEffective - baseline);
        float targetCut = Mathf.Max(0f, baseline - newEffective);

        if (targetBoost > 0.01f)
        {
            if (state.cutAmount > 0.01f)
                RestoreCategoryProvision(cat, baseline);
            SetCategoryBoost(cat, targetBoost);
            return;
        }

        if (state.boostAmount > 0.01f)
            SetCategoryBoost(cat, 0f);

        if (targetCut > state.cutAmount + 0.01f)
            SetCategoryProvision(cat, newEffective);
        else if (targetCut < state.cutAmount - 0.01f)
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

    private List<ResolvedEvent> BuildFamilyPrompts()
    {
        var prompts = new List<ResolvedEvent>();
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

        if (candidates.Count == 0) return prompts;

        candidates.Sort((a, b) => b.monthsSinceCut.CompareTo(a.monthsSinceCut));
        int raiseCount = Mathf.Min(2, candidates.Count);

        for (int i = 0; i < raiseCount; i++)
            prompts.Add(BuildFamilyPromptEvent(candidates[i].category));

        return prompts;
    }

    private ResolvedEvent BuildFamilyPromptEvent(ExpenseCategory cat)
    {
        var s = GetCategoryState(cat);
        int tier = Mathf.Clamp(s.timesRaised, 0, NagFractions.Length - 1);
        string body = GetFamilyPromptLine(cat, tier);
        string baseName = cat.ToString();
        string senderName = GetFamilySenderName();

        var choices = new List<EventData.ChoiceOption>
        {
            new EventData.ChoiceOption { label = $"Restore {baseName} fully",  resultDescription = $"{senderName}'s face changes the moment they see it's back to normal. Nobody says thank you out loud. They don't have to.", moneyChange = 0f, momentumChange = 0f },
            new EventData.ChoiceOption { label = $"Restore {baseName} halfway", resultDescription = $"It's not everything, but {senderName} notices. \"It's something,\" they say.", moneyChange = 0f, momentumChange = 0f },
            new EventData.ChoiceOption { label = "Not now",                     resultDescription = "You keep things as they are for now.", moneyChange = 0f, momentumChange = 0f }
        };

        return new ResolvedEvent
        {
            title = "A word at home",
            description = body,
            senderName = senderName,
            senderRelation = GetFamilySenderRelation(),
            pool = EventPool.Choice,
            hasChoices = true,
            choices = choices,
            isFamilyPrompt = true,
            familyPromptCategory = cat
        };
    }

    // Folded into the year-end score before grading (see UIManager.BuildScoreSummary).
    // Built = full pass, reached-but-never-built = "pass but not really", never reached = 0.
    public float GetGoalScoreBonus()
    {
        if (GoalBuilt) return 3f;
        if (goalReachedOnce) return 1f;
        return 0f;
    }

    // HUD / report line, e.g. "Her own market stall: $240 / $500" or "...: built ✔".
    // Empty string means no active goal to show (target not yet known - e.g.
    // Free Mode before setup confirm).
    public string GetGoalProgressLine()
    {
        float target = GoalDefs.GetActiveGoalTarget();
        if (target <= 0f) return "";

        string title = GoalDefs.GetActiveGoalTitle();
        if (GoalBuilt)
            return $"{title}: built";

        float current = financeManager != null ? financeManager.generalSavingsBalance : 0f;
        return $"{title}: {GameUtils.FormatMoney(current)} / {GameUtils.FormatMoney(target)}";
    }

    private ResolvedEvent BuildGoalPromptEvent(float target)
    {
        string title = GoalDefs.GetActiveGoalTitle();

        var choices = new List<EventData.ChoiceOption>
        {
            new EventData.ChoiceOption
            {
                label = $"Build it now, spend {GameUtils.FormatMoney(target)}",
                resultDescription = $"It's done. {title} is real now, not just a number in the savings book.",
                moneyChange = 0f, momentumChange = 0f
            },
            new EventData.ChoiceOption
            {
                label = "Not yet, keep saving",
                resultDescription = "The money stays where it is. The dream can wait a little longer.",
                moneyChange = 0f, momentumChange = 0f
            }
        };

        return new ResolvedEvent
        {
            title = "You've saved enough, build it now?",
            description = $"Your savings have reached {GameUtils.FormatMoney(target)}, enough for {title}. " +
                           "Building now means it starts earning for you sooner. Waiting costs nothing but time.",
            senderName = "Yourself",
            senderRelation = "Savings goal",
            pool = EventPool.Choice,
            hasChoices = true,
            choices = choices,
            isGoalPrompt = true
        };
    }

    private void HandleGoalChoice(int choiceIndex)
    {
        float target = GoalDefs.GetActiveGoalTarget();

        if (choiceIndex == 0)
        {
            financeManager.generalSavingsBalance -= target;
            financeManager.generalSavingsBalance = Mathf.Max(0f, financeManager.generalSavingsBalance);
            GoalDefs.ApplyActiveGoalBenefit();
            GoalBuilt = true;
            goalBuiltMonth = currentMonth;
            PlayerDataManager.Instance.ModifyFamilyMorale(3f);
            PlayerDataManager.Instance.ModifyMomentum(2f);
            uiManager?.UpdateGoalProgressText();
            Debug.Log($"[Goal] Built: {GoalDefs.GetActiveGoalTitle()} in month {currentMonth}");
        }
        else
        {
            goalMonthsSinceOffer = 0;
            Debug.Log("[Goal] Keep saving - re-offered in 2 months if still above target.");
        }
    }

    private string GetFamilySenderName()
    {
        switch (CurrentProfileType)
        {
            case ProfileType.Informal: return IsGuidedMode ? "Nyasha" : GetGenericSenderName();
            case ProfileType.Formal: return IsGuidedMode ? "Farai" : GetGenericSenderName();
            case ProfileType.Farmer: return IsGuidedMode ? "Ambuya Moyo" : GetGenericSenderName();
            default: return GetGenericSenderName();
        }
    }

    private string GetFamilySenderRelation()
    {
        if (!IsGuidedMode) return "Home";
        return CurrentProfileType switch
        {
            ProfileType.Informal => "Your spouse",
            ProfileType.Formal => "Your spouse",
            ProfileType.Farmer => "Your wife",
            _ => "Home"
        };
    }

    private string GetGenericSenderName()
    {
        if (setupData.adults >= 2) return "Your partner";
        if (setupData.children >= 1) return "The kids";
        return "Your family";
    }

    private string GetFamilyPromptLine(ExpenseCategory cat, int tier)
    {
        if (!IsGuidedMode)
            return GetGenericLine(cat, tier);

        return CurrentProfileType switch
        {
            ProfileType.Informal => GetInformalLine(cat, tier),
            ProfileType.Formal => GetFormalLine(cat, tier),
            ProfileType.Farmer => GetFarmerLine(cat, tier),
            _ => GetGenericLine(cat, tier)
        };
    }

    private string GetInformalLine(ExpenseCategory cat, int tier)
    {
        switch (cat)
        {
            case ExpenseCategory.Groceries:
                if (tier == 0) return "Nyasha mentions the sadza's been thinner this week. She's not upset, just noticing.";
                if (tier == 1) return "Nyasha asks again about food. \"The kids finish their plates too fast now,\" she says.";
                return "Nyasha doesn't ask this time. She just looks at the empty pot longer than she needs to.";

            case ExpenseCategory.Transport:
                if (tier == 0) return "Your son says the walk to school is longer since you stopped the fare.";
                if (tier == 1) return "He's stopped mentioning the walk. He just leaves earlier now.";
                return "He missed the morning register twice this week. He didn't tell you why.";

            case ExpenseCategory.Utilities:
                if (tier == 0) return "Nyasha asks if you can put the lights back on at night. \"Just for a bit,\" she says.";
                if (tier == 1) return "The kids do homework by phone-light now. Nyasha hasn't complained, but you've noticed.";
                return "Nyasha stopped asking about the lights. She started boiling water on a fire outside instead.";

            default:
                return GetGenericLine(cat, tier);
        }
    }

    // Chido's family - spouse Farai, two kids in school.
    private string GetFormalLine(ExpenseCategory cat, int tier)
    {
        switch (cat)
        {
            case ExpenseCategory.Groceries:
                if (tier == 0) return "Farai asks, half-joking, if you're \"on a diet plan\" the whole family didn't agree to.";
                if (tier == 1) return "Farai isn't joking anymore. \"The kids ask why lunch is smaller,\" she says.";
                return "Farai stopped bringing it up. She's started packing her own lunch smaller too, so the kids don't notice.";

            case ExpenseCategory.Transport:
                if (tier == 0) return "Farai mentions the car's sitting more than it used to.";
                if (tier == 1) return "She's started taking combis to work. She hasn't said it's because of you, but you know.";
                return "Farai stopped mentioning the car at all. She's made her own arrangement, without you.";

            case ExpenseCategory.Utilities:
                if (tier == 0) return "Farai asks if the water heater's broken, or if you turned it down.";
                if (tier == 1) return "The kids are showering cold and not saying anything. Farai has noticed that they've noticed.";
                return "Farai stopped asking. She just makes sure the kids don't complain in front of you.";

            default:
                return GetGenericLine(cat, tier);
        }
    }

    // Sekuru Moyo's family - grandson Tapiwa.
    private string GetFarmerLine(ExpenseCategory cat, int tier)
    {
        switch (cat)
        {
            case ExpenseCategory.Groceries:
                if (tier == 0) return "Tapiwa asks if there's more mealie-meal in the store room, or if that's all there is now.";
                if (tier == 1) return "Tapiwa's stopped asking for seconds. He says he's not that hungry.";
                return "Tapiwa's doing more work than usual, saying less. Sekuru Moyo hasn't said anything, but he's watching you.";

            case ExpenseCategory.Transport:
                if (tier == 0) return "Tapiwa asks if you can still afford the trips to market, or if he should start walking the produce in.";
                if (tier == 1) return "Tapiwa's legs are tired most evenings now. He hasn't complained. He just goes to bed earlier.";
                return "Tapiwa asked Sekuru Moyo, not you, if things were going to get easier. Sekuru Moyo didn't have an answer.";

            case ExpenseCategory.Utilities:
                if (tier == 0) return "Sekuru Moyo asks if the paraffin's run low, or if you're rationing it on purpose.";
                if (tier == 1) return "The homework gets done by firelight now. Nobody's said it's a problem. It clearly is.";
                return "Sekuru Moyo has stopped asking about anything. He's just quieter at dinner than he used to be.";

            default:
                return GetGenericLine(cat, tier);
        }
    }

    // Fallback for Free Mode or any category not covered above.
    private string GetGenericLine(ExpenseCategory cat, int tier)
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
            case 1: return $"It's come up again. {thing} has been tight for a while now, and they're really hoping you can restore it.";
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

        // Fully restored but carries a scar - queue the mentor's acknowledgement for
        // next month's CheckMentorMemory pass rather than interrupting this popup flow.
        if (state.cutAmount <= 0.01f && state.scar > 0f)
            mentorMemory_scarAckPending = true;

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

        if (!GoalBuilt)
            goalMonthsSinceOffer++;
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
            Debug.Log("[Death] No adults remain - household on income floor, no recovery.");
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

        if (percent < 0f)
        {
            bool hasAccidentCover =
                insuranceManager != null &&
                (insuranceManager.CanClaimForEvent(InsuranceManager.InsuranceType.PersonalAccident) ||
                 insuranceManager.CanClaimForEvent(InsuranceManager.InsuranceType.Health));

            if (hasAccidentCover)
            {
                float lostIncomeEstimate = financeManager.currentIncome * Mathf.Abs(percent) / 100f;
                float benefitAmount = lostIncomeEstimate * 0.5f;

                activeIncomeBenefits.Add(new IncomeBenefit
                {
                    amount = benefitAmount,
                    remainingMonths = months <= 0 ? -1 : months
                });

                Debug.Log($"[Insurance] Accident/Health cover registered income benefit: ${benefitAmount:F0}/month for {(months <= 0 ? "permanent" : months + " months")}");
            }
        }

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
        float eventInflation = 0f;
        foreach (var effect in activeExpenseEffects)
            if (effect.category == category)
                eventInflation += effect.flatIncrease;

        float total = 0f;
        if (categoryStates.TryGetValue(category, out var state))
        {
            total -= state.cutAmount;

            float spillover = Mathf.Max(0f, eventInflation - state.boostAmount);

            total += state.boostAmount;
            total += spillover;
        }
        else
        {
            total += eventInflation;
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

    private void UpdateIncomeBenefits()
    {
        for (int i = activeIncomeBenefits.Count - 1; i >= 0; i--)
        {
            if (activeIncomeBenefits[i].remainingMonths == -1)
                continue;

            activeIncomeBenefits[i].remainingMonths--;

            if (activeIncomeBenefits[i].remainingMonths <= 0)
                activeIncomeBenefits.RemoveAt(i);
        }
    }

    public void PayActiveIncomeBenefits()
    {
        foreach (var benefit in activeIncomeBenefits)
        {
            if (benefit.amount > 0.01f)
                ApplyMoneyChange(FinancialEntry.EntryType.InsurancePayout, "Accident cover benefit", benefit.amount, true);
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
        if (isMonthTickerPlaying) uiManager.PauseMonthTicker();
        uiManager.ShowSavingsPanel();
    }

    public void OnSavingsDecisionFinished()
    {
        IsSavingsDecisionActive = false;
        SetPhase(GamePhase.Simulation);
        if (isMonthTickerPlaying)
        {
            uiManager.ResumeMonthTicker();
            return;
        }

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
        uiManager?.UpdateGoalProgressText();
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
    bool isCredit,
    bool suppressHudUpdate = false)
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
        if (!suppressHudUpdate)
            uiManager?.UpdateMoneyText(financeManager.CashOnHand);
    }

    // ---------------- Forecast bet → payoff ----------------
    // The forecast is a wager the player places (read warnings → pick insurance).
    // These hooks close the loop: every risk event explicitly settles the bet.

    /// <summary>Set at month resolution; shown on NEXT month's forecast panel. Not persisted in saves.</summary>
    public string LastMonthForecastReview { get; private set; } = "";

    private static readonly (ForecastManager.ForecastCategory cat, EventPool pool)[] ForecastPoolMap =
    {
        (ForecastManager.ForecastCategory.Weather,   EventPool.Weather),
        (ForecastManager.ForecastCategory.Health,    EventPool.Health),
        (ForecastManager.ForecastCategory.Economic,  EventPool.Economic),
        (ForecastManager.ForecastCategory.Crime,     EventPool.Crime),
        (ForecastManager.ForecastCategory.Crops,     EventPool.Agriculture),
        (ForecastManager.ForecastCategory.Livestock, EventPool.Agriculture),
    };

    private bool WasPoolWarned(EventPool pool)
    {
        if (forecastManager == null) return false;
        foreach (var (cat, p) in ForecastPoolMap)
            if (p == pool && forecastManager.IsCategoryWarned(cat))
                return true;
        return false;
    }

    private static string ForecastCategoryLabel(ForecastManager.ForecastCategory cat) => cat switch
    {
        ForecastManager.ForecastCategory.Health    => "health trouble",
        ForecastManager.ForecastCategory.Livestock => "livestock trouble",
        ForecastManager.ForecastCategory.Crops     => "crop trouble",
        ForecastManager.ForecastCategory.Economic  => "economic squeeze",
        ForecastManager.ForecastCategory.Crime     => "crime",
        ForecastManager.ForecastCategory.Weather   => "bad weather",
        _ => "trouble"
    };

    private void BuildForecastReview()
    {
        LastMonthForecastReview = "";
        if (forecastManager == null || IsHeadlessSimulation) return;

        var lines = new List<string>();
        var seenPools = new HashSet<EventPool>();

        foreach (var article in forecastManager.SelectedArticles)
        {
            EventPool pool = EventPool.Choice;
            foreach (var (cat, p) in ForecastPoolMap)
                if (cat == article.category) { pool = p; break; }
            if (pool == EventPool.Choice || !seenPools.Add(pool)) continue;

            ResolvedEvent hit = monthlyEvents.Find(e =>
                e.pool == pool && (e.moneyChange < -0.5f || e.intendedLoss > 0.5f));

            string label = ForecastCategoryLabel(article.category);
            if (hit != null && hit.insurancePayout > 0.5f)
                lines.Add($"The warned {label} struck. Your cover paid ${hit.insurancePayout:F0}. You saw it coming, and you were ready.");
            else if (hit != null)
                lines.Add($"The warned {label} struck with no cover in place, ${Mathf.Abs(hit.moneyChange):F0} out of pocket.");
            else if (CurrentLedger != null && CurrentLedger.TotalInsurancePremiums > 0.5f)
                lines.Add($"The warned {label} never came. Premiums buy peace of mind, not refunds.");
            else
                lines.Add($"The warned {label} never came, this time.");
        }

        // Hits are the story; misses are the footnote. Cap at two lines.
        lines.Sort((a, b) => b.Contains("struck").CompareTo(a.Contains("struck")));
        if (lines.Count > 2)
            lines = lines.GetRange(0, 2);

        LastMonthForecastReview = string.Join("\n", lines);
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

        // Settle the forecast bet on the spot - this is the payoff moment.
        if (!ev.isFamilyPrompt && (ev.moneyChange < -0.5f || ev.insurancePayout > 0.5f))
        {
            bool warned = WasPoolWarned(ev.pool);
            if (warned && ev.insurancePayout > 0.5f)
                text += "\n\n<i>The news warned of this, and your cover was ready. That's what planning looks like.</i>";
            else if (warned)
                text += "\n\n<i>The news warned of this one. The forecast is worth a second look next month.</i>";
            else if (ev.insurancePayout > 0.5f)
                text += "\n\n<i>No warning this time, but your cover caught it anyway.</i>";
        }

        return text;
    }

    private void ShowOrChooseEvent(ResolvedEvent ev)
    {
        if (ev.isGoalPrompt)
        {
            if (IsHeadlessSimulation)
            {
                HandleGoalChoice(1); // deterministic: auto "Keep saving"
                OnEventPopupClosed();
                return;
            }

            UIManager.Instance.ShowChoicePopup(
                ev.title,
                ev.description,
                ev.senderName,
                ev.senderRelation,
                ev.choices,
                index =>
                {
                    HandleGoalChoice(index);
                    OnEventPopupClosed();
                }
            );
            return;
        }

        if (ev.isFamilyPrompt)
        {
            if (IsHeadlessSimulation)
            {
                RaiseCategory(ev.familyPromptCategory, 2);
                OnEventPopupClosed();
                return;
            }

            System.Action showFamilyPromptPopup = () =>
            {
                UIManager.Instance.ShowChoicePopup(
                    ev.title,
                    ev.description,
                    ev.senderName,
                    ev.senderRelation,
                    ev.choices,
                    index =>
                    {
                        RaiseCategory(ev.familyPromptCategory, index);
                        OnEventPopupClosed();
                    }
                );
            };

            // Same wrapping pattern as OnFirstEvent - tutorial line closes, then the
            // actual popup shows. Seen()-gated internally so this is a no-op after the first time.
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnFirstFamilyPrompt(showFamilyPromptPopup);
            else
                showFamilyPromptPopup();

            return;
        }

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

        if (ev.title == "Mukando Invitation" && choiceIndex == 0)
        {
            loanManager.JoinMukando();
            Debug.Log("[Loan] Mukando joined via Mukando Invitation.");
        }

        if (choice.moneyChange != 0f)
        {
            bool isCredit = choice.moneyChange > 0f;
            ApplyMoneyChange(
                isCredit ? FinancialEntry.EntryType.EventReward
                         : FinancialEntry.EntryType.EventLoss,
                $"{ev.title}: {choice.label}",
                Mathf.Abs(choice.moneyChange),
                isCredit
            );
            totalRawEventDamage += Mathf.Max(0f, -choice.moneyChange);
        }
        // Record on the event itself so OnEventPopupClosed's money beat picks up
        // choice-driven changes (ev.moneyChange otherwise stays 0 for choice events).
        ev.moneyChange = choice.moneyChange;

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
        if (!string.IsNullOrEmpty(choice.grantsAsset) && financeManager != null)
        {
            var a = financeManager.assets;
            switch (choice.grantsAsset.Trim().ToLower())
            {
                case "motor": a.hasMotor = true; break;
                case "house": a.hasHouse = true; break;
                case "crops": a.hasCrops = true; break;
                case "livestock": a.hasLivestock = true; break;
            }
            financeManager.assets = a;
            financeManager.RecalculateAssetValues();
            Debug.Log($"[Assets] Granted '{choice.grantsAsset}' via choice '{choice.label}'.");
        }
    }

    private void ShowInsuranceClaimChoice(ResolvedEvent ev)
    {
        if (ev.intendedLoss < 20f)
        {
            ApplyClaimChoice(ev, true);
            ShowEvent(ev);
            return;
        }

        string planName = insuranceManager.GetPlan(ev.type)?.planName ?? ev.type.ToString();
        float rawLoss = ev.intendedLoss;
        float payout = ev.claimPayout;
        float deductible = ev.claimDeductible;

        var choices = new List<EventData.ChoiceOption>
    {
        new EventData.ChoiceOption
        {
            label = deductible > 0.5f
                ? $"Claim: pay ${deductible:F0} excess, recover ${payout:F0}"
                : $"Claim: recover ${payout:F0}",
            resultDescription = $"Claim approved. {planName} covers ${payout:F0} of this loss.",
            moneyChange = 0f, momentumChange = 0f
        },
        new EventData.ChoiceOption
        {
            label = $"Cover it myself: ${rawLoss:F0} total",
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
                ApplyMoneyChange(FinancialEntry.EntryType.EventLoss, ev.title, grossLossToRecord, false, suppressHudUpdate: true);

            if (ev.claimPayout > 0f)
            {
                ApplyMoneyChange(FinancialEntry.EntryType.InsurancePayout, "Insurance Payout", ev.claimPayout, true, suppressHudUpdate: true);
                insuranceManager.RecordClaimBookkeeping(ev.type, ev.claimPayout);
                totalInsurancePayoutAmount += ev.claimPayout;
                insuredEventsCount++;
            }

            uiManager?.UpdateMoneyText(financeManager.CashOnHand);

            totalRawEventDamage += cappedNetLoss;
            ev.moneyChange = -cappedNetLoss;
            ev.insurancePayout = ev.claimPayout;
            mentorMemory_hasEverClaimed = true;
            Debug.Log($"[Claim] Player claimed - gross loss recorded: ${grossLossToRecord:F0}, payout: ${ev.claimPayout:F0}, net cash: ${-cappedNetLoss:F0}");
        }
        else
        {
            float cappedLoss = ApplyMonthlyDamage(ev.intendedLoss);
            if (cappedLoss > 0f)
                ApplyMoneyChange(FinancialEntry.EntryType.EventLoss, ev.title, cappedLoss, false);
            totalRawEventDamage += cappedLoss;
            ev.moneyChange = -cappedLoss;
            ev.insurancePayout = 0f;
            Debug.Log($"[Claim] Player declined - full loss: ${cappedLoss:F0}");
        }
    }

    public struct BudgetBarState
    {
        public float cap;
        public float baseLine;
        public float current;
        public float belowBase;   // one colour: you're providing less than base
        public float aboveBase;   // one colour: you're providing more than base
        public bool atCap;
    }

    private float ComputeCushionedCost(ExpenseCategory cat, float hypotheticalProvision)
    {
        float baseline = GetCategoryBaseline(cat);
        float boost = Mathf.Max(0f, hypotheticalProvision - baseline);
        float cut = Mathf.Max(0f, baseline - hypotheticalProvision);
        float eventInflation = GetCategoryEventInflation(cat);
        float spillover = Mathf.Max(0f, eventInflation - boost);
        return baseline - cut + boost + spillover;
    }

    public BudgetBarState GetBudgetBarState(float groceriesProvision, float transportProvision, float utilitiesProvision)
    {
        float housing = financeManager.GetHousingCost();
        float school = 0f;
        if (setupData.hasSchoolFees)
        {
            int childCount = Mathf.Max(0, PlayerDataManager.Instance?.Children ?? 0);
            school = ((setupData.schoolFeesAmount * childCount) * 3f) / 12f;
        }

        float baseLine = housing + school
            + GetCategoryBaseline(ExpenseCategory.Groceries)
            + GetCategoryBaseline(ExpenseCategory.Transport)
            + GetCategoryBaseline(ExpenseCategory.Utilities);

        float current = housing + school
            + ComputeCushionedCost(ExpenseCategory.Groceries, groceriesProvision)
            + ComputeCushionedCost(ExpenseCategory.Transport, transportProvision)
            + ComputeCushionedCost(ExpenseCategory.Utilities, utilitiesProvision);

        float cap = GetAverageIncome() * BudgetBoostCeilingFraction;

        return new BudgetBarState
        {
            cap = cap,
            baseLine = baseLine,
            current = current,
            belowBase = Mathf.Max(0f, baseLine - current),
            aboveBase = Mathf.Max(0f, current - baseLine),
            atCap = current >= cap - 0.01f
        };
    }

    public void ContinueFromSave(GameSaveData save)
    {
        if (save == null)
        {
            Debug.LogWarning("[GameManager] ContinueFromSave called with a null save.");
            return;
        }
        LoadFromSave(save);
    }

    public void LoadFromSave(GameSaveData save)
    {
        if (setupData != null)
        {
            setupData.adults = save.setupAdults;
            setupData.children = save.setupChildren;
            setupData.isIncomeStable = save.setupIsIncomeStable;
            setupData.housing = (HousingType)save.setupHousing;
            setupData.ownsCar = save.setupOwnsCar;
            setupData.hasSchoolFees = save.setupHasSchoolFees;
            setupData.schoolFeesAmount = save.setupSchoolFeesAmount;
            setupData.minIncome = save.setupMinIncome;
            setupData.maxIncome = save.setupMaxIncome;
            setupData.houseValue = save.setupHouseValue;
        }

        if (financeManager != null)
        {
            financeManager.minIncome = save.setupMinIncome;
            financeManager.maxIncome = save.setupMaxIncome;
            financeManager.isIncomeStable = save.setupIsIncomeStable;
            financeManager.rentCost = save.financeRentCost;
            financeManager.houseMaintenanceCost = save.financeHouseMaintenanceCost;
            financeManager.groceries = save.financeGroceries;
            financeManager.transport = save.financeTransport;
            financeManager.utilities = save.financeUtilities;

            financeManager.assets = new PlayerAssets
            {
                hasHouse = save.assetHasHouse,
                hasMotor = save.assetHasMotor,
                hasCrops = save.assetHasCrops,
                hasLivestock = save.assetHasLivestock
            };
            financeManager.houseInsuredValue = save.houseInsuredValue;
            financeManager.motorInsuredValue = save.motorInsuredValue;
            financeManager.cropsInsuredValue = save.cropsInsuredValue;
            financeManager.livestockInsuredValue = save.livestockInsuredValue;

            // RollMonthlyIncome() re-rolls currentIncome fresh every month from
            // minIncome/maxIncome/isIncomeStable, so currentIncome itself doesn't need
            // restoring - but schoolFeesPerTerm does, since nothing else derives it
            // without re-running InitializeFromSetup (which this resume path avoids).
            financeManager.schoolFeesPerTerm = save.setupHasSchoolFees ? save.setupSchoolFeesAmount : 0f;
        }

        if (loanManager != null)
        {
            loanManager.RestoreFromSave(
                save.moneylender, save.mukando,
                save.loanUnlocked, save.mukandoJoined,
                save.mukandoConsecutiveMisses, save.mukandoRecoveryMonthsNeeded);
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
        // Current household size, not just original - without this a resumed game
        // resurrects any adults/children lost to events before the save point.
        PlayerDataManager.Instance.SetCurrentHousehold(save.currentAdults, save.currentChildren);

        savingsStreak = save.savingsStreak;
        overBudgetStreak = save.overBudgetStreak;
        patternWarningIssued = save.patternWarningIssued;
        recoveryAcknowledged = save.recoveryAcknowledged;
        lastMomentumZone = save.lastMomentumZone;
        previousMomentum = save.previousMomentum;
        monthsSinceMajorEvent = save.monthsSinceMajorEvent;
        eventManager.SetEventPressure(save.eventPressure);
        burialSocietyUnlocked = save.burialSocietyUnlocked;
        IsGuidedMode = save.isGuidedMode;
        CurrentProfileType = (ProfileType)save.profileType;

        GoalBuilt = save.goalBuilt;
        goalReachedOnce = save.goalReachedOnce;
        goalMonthsSinceOffer = save.goalMonthsSinceOffer;
        goalMilestoneReached = save.goalMilestoneReached;
        freeGoalIndex = save.freeGoalIndex;
        freeGoalTarget = save.freeGoalTarget;
        goalBuiltMonth = save.goalBuiltMonth;

        mentorMemory_familyStrainStreak = save.mentorMemory_familyStrainStreak;
        mentorMemory_familyStrainMentioned = save.mentorMemory_familyStrainMentioned;
        mentorMemory_communityHighMentioned = save.mentorMemory_communityHighMentioned;
        mentorMemory_communityLowMentioned = save.mentorMemory_communityLowMentioned;
        mentorMemory_goalBuiltMentioned = save.mentorMemory_goalBuiltMentioned;
        mentorMemory_scarAckPending = save.mentorMemory_scarAckPending;
        mentorMemory_bufferLineShown = save.mentorMemory_bufferLineShown;

        categoryStates.Clear();
        if (save.categoryStates != null)
            foreach (var s in save.categoryStates)
                if (s != null)
                    categoryStates[s.category] = s;

        activeIncomeEffects.Clear();

        if (save.incomeBenefits != null)
            foreach (var b in save.incomeBenefits)
                activeIncomeBenefits.Add(new IncomeBenefit
                {
                    amount = b.amount,
                    remainingMonths = b.remainingMonths
                });
        activeIncomeBenefits.Clear();
        if (save.incomeEffects != null)
            foreach (var e in save.incomeEffects)
                activeIncomeEffects.Add(new IncomeEffect
                {
                    reductionPercent = e.reductionPercent,
                    remainingMonths = e.remainingMonths
                });

        activeExpenseEffects.Clear();
        if (save.expenseEffects != null)
            foreach (var e in save.expenseEffects)
                activeExpenseEffects.Add(new ExpenseEffect
                {
                    category = (ExpenseCategory)e.category,
                    flatIncrease = e.flatIncrease,
                    remainingMonths = e.remainingMonths
                });

        uiManager.UpdateMonthText(currentMonth, totalMonths);
        uiManager.UpdateMoneyText(financeManager.CashOnHand);

        StartNewMonth();
    }

    public void FullRestart()
    {
        Debug.Log("=== FULL GAME RESET ===");
        CurrentLedger = null;
        monthResolutionStarted = false;
        monthResolutionFinished = false;

        uiManager.SwitchPanel(UIManager.UIPanelState.None);
        financeManager?.ResetFinance();
        eventManager?.ResetAll();
        uiManager.UpdateMoneyText(financeManager.CashOnHand);
        loanManager?.ResetAll();
        loanManager?.AnnounceLoanSystem();

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

        // Mukando Invitation is scripted-only now (excluded from the random pool in
        // EventManager), so every reset path that clears pendingEvents must reschedule
        // it or the player can never be offered Mukando in that playthrough.
        var mukandoEvent = eventManager?.EventDatabase?.events?.Find(e => e.eventName == "Mukando Invitation");
        if (mukandoEvent != null)
            eventManager.ScheduleFollowUp(mukandoEvent, 2);
        else
            Debug.LogWarning("[Loan] Mukando Invitation event not found in EventDatabase - opt-in prompt will not fire.");

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
        activeIncomeBenefits.Clear();

        mentorSpokeThisMonth = false;
        loanIntroShown = false;
        monthResolutionStarted = false;
        monthResolutionFinished = false;
        recoveryAcknowledged = false;
        patternWarningIssued = false;
        mentorMemory_hasEverClaimed = false;
        mentorMemory_consecutiveLowSavingsMonths = 0;
        mentorMemory_familyStrainStreak = 0;
        mentorMemory_familyStrainMentioned = false;
        mentorMemory_communityHighMentioned = false;
        mentorMemory_communityLowMentioned = false;
        mentorMemory_goalBuiltMentioned = false;
        mentorMemory_scarAckPending = false;
        mentorMemory_bufferLineShown = false;
        burialSocietyUnlocked = false;
        isMonthTickerPlaying = false;
        LastMonthForecastReview = "";
        IsLoanDecisionActive = false;
        IsSavingsDecisionActive = false;
        forcedLoanThisMonth = false;
        isWaitingForEventConfirmation = false;
        lastMomentumZone = int.MinValue;

        GoalBuilt = false;
        goalReachedOnce = false;
        goalMonthsSinceOffer = 0;
        goalMilestoneReached = 0;
        freeGoalIndex = -1;
        freeGoalTarget = 0f;
        goalBuiltMonth = -1;

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

        SaveSystem.DeleteAllSaves();

        Debug.Log("=== GAME RESET COMPLETE ===");
    }

    private void ResetForNewGame()
    {
        eventManager?.ResetAll();
        loanManager?.ResetAll();
        insuranceManager?.ResetAll();
        TutorialManager.Instance?.ResetRunState();

        // Ndlovu (Moneylender) is available from month 1 now - reveal the loan button
        // and one-time intro right after tutorial state is cleared, not gated behind
        // months of Mukando saving the way the old single-account system was.
        loanManager?.AnnounceLoanSystem();

        var mukandoEvent = eventManager?.EventDatabase?.events?.Find(e => e.eventName == "Mukando Invitation");
        if (mukandoEvent != null)
            eventManager.ScheduleFollowUp(mukandoEvent, 2);
        else
            Debug.LogWarning("[Loan] Mukando Invitation event not found in EventDatabase - opt-in prompt will not fire.");
    }

    public bool HasMonthResolutionStarted()
    {
        return monthResolutionStarted;
    }

    // Free Mode has no fixed goal def (defs are per guided profile), so the
    // pool pick + target are rolled once income is known. Call this at setup
    // confirm, after setupData.minIncome/maxIncome are set - guided profiles
    // no-op since ApplyProfile already gives them a static GoalDefs entry.
    public void RollFreeGoalIfNeeded()
    {
        if (IsGuidedMode) return;
        if (freeGoalIndex >= 0) return; // already rolled this run

        freeGoalIndex = Random.Range(0, GoalDefs.FreeModePool.Length);
        float avgIncome = (setupData.minIncome + setupData.maxIncome) / 2f;
        float raw = avgIncome * 1.5f;
        freeGoalTarget = Mathf.Max(300f, Mathf.Round(raw / 50f) * 50f);
        Debug.Log($"[Goal] Free Mode goal rolled: {GoalDefs.FreeModePool[freeGoalIndex].title} target ${freeGoalTarget:F0}");
    }

    public void ClearProfile()
    {
        IsGuidedMode = false;
        freeGoalIndex = -1; // re-roll on next setup confirm (RollFreeGoalIfNeeded)
        freeGoalTarget = 0f;
        if (setupData == null || financeManager == null)
            return;

        Debug.Log("[Profile] Free Mode selected");
        ResetForNewGame();

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
        CurrentProfileType = profile;
        if (setupData == null || financeManager == null)
        {
            Debug.LogError("SetupData or FinanceManager missing.");
            return;
        }

        Debug.Log($"[Profile] Applying: {profile}");
        ResetForNewGame();

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
                setupData.isIncomeStable = true;
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

        Debug.Log($"✅ {testName} COMPLETE - " +
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
        activeIncomeBenefits.Clear();
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
        mentorMemory_familyStrainStreak = 0;
        mentorMemory_familyStrainMentioned = false;
        mentorMemory_communityHighMentioned = false;
        mentorMemory_communityLowMentioned = false;
        mentorMemory_goalBuiltMentioned = false;
        mentorMemory_scarAckPending = false;
        mentorMemory_bufferLineShown = false;
        burialSocietyUnlocked = false;
        isMonthTickerPlaying = false;
        LastMonthForecastReview = "";
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

        // Without this, GoalBuilt/goalMilestoneReached etc. leak into the next
        // stress test run in the same play session - e.g. running Informal then
        // Formal back-to-back would have Formal start with GoalBuilt already true.
        GoalBuilt = false;
        goalReachedOnce = false;
        goalMonthsSinceOffer = 0;
        goalMilestoneReached = 0;
        freeGoalIndex = -1;
        freeGoalTarget = 0f;
        goalBuiltMonth = -1;

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
        // Informal/Formal start at $0/month savings by design (see ApplyProfile) - real
        // gameplay, not a bug. But that means their goal ($500 / $700) would never be
        // reached in a headless run, silently skipping every goal code path this test is
        // meant to cover. Force a modest contribution here for test purposes only -
        // doesn't touch the real profile defaults players see.
        if (savedSavings <= 0f)
            savedSavings = 40f;

        financeManager.InitializeFromSetup();
        financeManager.generalSavingsMonthly = savedSavings;

        RunHeadlessLoop(testName);

        Debug.Log($"[Goal] {testName} result - Built:{GoalBuilt} ReachedOnce:{HasGoalBeenReached} " +
                  $"Milestone:{goalMilestoneReached} BuiltMonth:{goalBuiltMonth} " +
                  $"FinalSavings:${financeManager.generalSavingsBalance:F0} Target:${GoalDefs.GetActiveGoalTarget():F0}");
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
    // TEST Free Mode Goal - none of the profile/generic stress tests above
    // roll a Free Mode goal (they all bypass SetupPanelController.ConfirmAndStart,
    // the only place RollFreeGoalIfNeeded is normally called). These force each
    // pool entry directly so all three get headless coverage.
    // ============================================================
    private void RunStressTestFreeModeGoal(int forcedPoolIndex, string testName)
    {
        if (!StressTestPreCheck(testName))
            return;

        Debug.Log($"===== STARTING {testName} =====");

        ClearProfile();

        setupData.adults = 1;
        setupData.children = 0;
        setupData.isIncomeStable = false;
        setupData.minIncome = 400f;
        setupData.maxIncome = 700f;

        financeManager.rentCost = 100f;
        financeManager.groceries = 80f;
        financeManager.transport = 40f;
        financeManager.utilities = 30f;

        // Bypasses RollFreeGoalIfNeeded's randomness so each pool entry gets
        // deterministic coverage - same math the real roll uses.
        freeGoalIndex = forcedPoolIndex;
        float avgIncome = (setupData.minIncome + setupData.maxIncome) / 2f;
        freeGoalTarget = Mathf.Max(300f, Mathf.Round(avgIncome * 1.5f / 50f) * 50f);
        Debug.Log($"[Goal] Forced Free Mode goal: {GoalDefs.FreeModePool[forcedPoolIndex].title} target ${freeGoalTarget:F0}");

        financeManager.InitializeFromSetup();
        financeManager.generalSavingsMonthly = 40f; // see RunStressTestUsingProfile - same test-only override

        RunHeadlessLoop(testName);

        Debug.Log($"[Goal] {testName} result - Built:{GoalBuilt} ReachedOnce:{HasGoalBeenReached} " +
                  $"Milestone:{goalMilestoneReached} BuiltMonth:{goalBuiltMonth} " +
                  $"FinalSavings:${financeManager.generalSavingsBalance:F0} Target:${freeGoalTarget:F0}");
    }

    [ContextMenu("DEBUG_StressTest_FreeGoal_SewingMachine")]
    public void DEBUG_StressTest_FreeGoal_SewingMachine()
    {
        RunStressTestFreeModeGoal(0, "StressTest [FREE GOAL: Sewing Machine]");
    }

    [ContextMenu("DEBUG_StressTest_FreeGoal_Bicycle")]
    public void DEBUG_StressTest_FreeGoal_Bicycle()
    {
        RunStressTestFreeModeGoal(1, "StressTest [FREE GOAL: Bicycle]");
    }

    [ContextMenu("DEBUG_StressTest_FreeGoal_EmergencyFund")]
    public void DEBUG_StressTest_FreeGoal_EmergencyFund()
    {
        RunStressTestFreeModeGoal(2, "StressTest [FREE GOAL: Emergency Fund]");
    }

    // ============================================================
    // TEST 1 - DEFAULT (original middle-class baseline, now fixed)
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
    // TEST 2 - ZIMBABWE LOW CLASS
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
    // TEST 3 - ZIMBABWE MIDDLE CLASS
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
    // TEST 4 - ZIMBABWE HIGH CLASS
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
    // TEST 5 - BUDGET SYSTEMS
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
        Debug.Log($"[T] Prompt line (Formal, Transport, tier0) = \"{GetFamilyPromptLine(ExpenseCategory.Transport, 0)}\"");
        Debug.Log($"[T] Sender name (Formal) = \"{GetFamilySenderName()}\"");
    }

    [ContextMenu("DEBUG_TestSaveLoadBudget")]
    public void DEBUG_TestSaveLoadBudget()
    {
        const string NAME = "SaveLoadBudget";
        if (!StressTestPreCheck(NAME)) return;
        ApplyProfile(ProfileType.Formal);
        var pdm = PlayerDataManager.Instance;

        Debug.Log("===== SAVE/LOAD BUDGET TEST =====");

        SetCategoryProvision(ExpenseCategory.Groceries, 70f);
        RestoreCategoryProvision(ExpenseCategory.Groceries, 140f);
        RaiseCategory(ExpenseCategory.Groceries, 2);
        SetCategoryProvision(ExpenseCategory.Transport, 25f);
        RaiseCategory(ExpenseCategory.Transport, 2);
        SetCategoryBoost(ExpenseCategory.Utilities, 12f);

        var gBefore = GetCategoryState(ExpenseCategory.Groceries);
        var tBefore = GetCategoryState(ExpenseCategory.Transport);
        var uBefore = GetCategoryState(ExpenseCategory.Utilities);
        float moraleBefore = pdm.FamilyMorale;

        Debug.Log($"[SL] BEFORE - morale={moraleBefore:F2}");
        Debug.Log($"[SL] BEFORE - Groceries scar={gBefore.scar:F2} cut={gBefore.cutAmount:F0}");
        Debug.Log($"[SL] BEFORE - Transport cut={tBefore.cutAmount:F0} timesRaised={tBefore.timesRaised} origHit={tBefore.originalCutHit:F1}");
        Debug.Log($"[SL] BEFORE - Utilities boost={uBefore.boostAmount:F0} cycle={uBefore.boostMonthsInCycle}");

        SaveSystem.SaveGame(this);
        categoryStates.Clear();
        Debug.Log("[SL] --- categoryStates cleared, reloading from save ---");

        GameSaveData save = SaveSystem.LoadGame(CurrentProfileType, IsGuidedMode);
        if (save?.categoryStates == null) { Debug.LogError("[SL] FAIL: no categoryStates in save."); IsHeadlessSimulation = false; return; }
        foreach (var s in save.categoryStates)
            categoryStates[s.category] = s;

        var gAfter = GetCategoryState(ExpenseCategory.Groceries);
        var tAfter = GetCategoryState(ExpenseCategory.Transport);
        var uAfter = GetCategoryState(ExpenseCategory.Utilities);

        Debug.Log($"[SL] AFTER - Groceries scar={gAfter.scar:F2} cut={gAfter.cutAmount:F0} (expect scar {gBefore.scar:F2}, cut {gBefore.cutAmount:F0})");
        Debug.Log($"[SL] AFTER - Transport cut={tAfter.cutAmount:F0} timesRaised={tAfter.timesRaised} origHit={tAfter.originalCutHit:F1} (expect cut {tBefore.cutAmount:F0}, raised {tBefore.timesRaised}, hit {tBefore.originalCutHit:F1})");
        Debug.Log($"[SL] AFTER - Utilities boost={uAfter.boostAmount:F0} cycle={uAfter.boostMonthsInCycle} (expect boost {uBefore.boostAmount:F0}, cycle {uBefore.boostMonthsInCycle})");

        bool pass =
            Mathf.Approximately(gAfter.scar, gBefore.scar) &&
            Mathf.Approximately(tAfter.cutAmount, tBefore.cutAmount) &&
            tAfter.timesRaised == tBefore.timesRaised &&
            Mathf.Approximately(tAfter.originalCutHit, tBefore.originalCutHit) &&
            Mathf.Approximately(uAfter.boostAmount, uBefore.boostAmount);

        Debug.Log(pass ? "[SL] ✅ PASS - all budget state round-tripped." : "[SL] ❌ FAIL - state mismatch after load.");

        SaveSystem.DeleteSave(CurrentProfileType, IsGuidedMode);
        IsHeadlessSimulation = false;
    }

    [ContextMenu("DEBUG_TestSaveLoadGoal")]
    public void DEBUG_TestSaveLoadGoal()
    {
        const string NAME = "SaveLoadGoal";
        if (!StressTestPreCheck(NAME)) return;

        ApplyProfile(ProfileType.Informal); // target $500, +8% income benefit when built

        // Fake a mid-goal state without running the full 24-month loop -
        // same "poke fields directly, save, clear, reload" shape as DEBUG_TestSaveLoadBudget.
        currentMonth = 7;
        financeManager.generalSavingsBalance = 260f; // 52% of $500 - past the 50% milestone
        goalReachedOnce = true;
        goalMonthsSinceOffer = 1;
        goalMilestoneReached = 50;

        bool builtBefore = GoalBuilt;
        bool reachedBefore = goalReachedOnce;
        int offerBefore = goalMonthsSinceOffer;
        int milestoneBefore = goalMilestoneReached;
        float balanceBefore = financeManager.generalSavingsBalance;

        Debug.Log($"[SL-Goal] BEFORE - Built:{builtBefore} Reached:{reachedBefore} Offer:{offerBefore} " +
                  $"Milestone:{milestoneBefore} Savings:${balanceBefore:F0}");

        SaveSystem.SaveGame(this);

        // Clear in-memory state the way a fresh session would have it before reloading.
        GoalBuilt = false;
        goalReachedOnce = false;
        goalMonthsSinceOffer = 0;
        goalMilestoneReached = 0;
        freeGoalIndex = -1;
        freeGoalTarget = 0f;
        goalBuiltMonth = -1;
        financeManager.generalSavingsBalance = 0f;
        Debug.Log("[SL-Goal] --- goal fields cleared, reloading from save ---");

        GameSaveData save = SaveSystem.LoadGame(CurrentProfileType, IsGuidedMode);
        if (save == null) { Debug.LogError("[SL-Goal] FAIL: no save found."); IsHeadlessSimulation = false; return; }

        GoalBuilt = save.goalBuilt;
        goalReachedOnce = save.goalReachedOnce;
        goalMonthsSinceOffer = save.goalMonthsSinceOffer;
        goalMilestoneReached = save.goalMilestoneReached;
        freeGoalIndex = save.freeGoalIndex;
        freeGoalTarget = save.freeGoalTarget;
        goalBuiltMonth = save.goalBuiltMonth;
        financeManager.generalSavingsBalance = save.generalSavingsBalance;

        Debug.Log($"[SL-Goal] AFTER - Built:{GoalBuilt} Reached:{goalReachedOnce} Offer:{goalMonthsSinceOffer} " +
                  $"Milestone:{goalMilestoneReached} Savings:${financeManager.generalSavingsBalance:F0} " +
                  $"(expect Built:{builtBefore} Reached:{reachedBefore} Offer:{offerBefore} Milestone:{milestoneBefore} Savings:${balanceBefore:F0})");

        bool pass =
            GoalBuilt == builtBefore &&
            goalReachedOnce == reachedBefore &&
            goalMonthsSinceOffer == offerBefore &&
            goalMilestoneReached == milestoneBefore &&
            Mathf.Approximately(financeManager.generalSavingsBalance, balanceBefore);

        Debug.Log(pass ? "[SL-Goal] ✅ PASS - goal state round-tripped." : "[SL-Goal] ❌ FAIL - state mismatch after load.");

        SaveSystem.DeleteSave(CurrentProfileType, IsGuidedMode);
        IsHeadlessSimulation = false;
    }

    [ContextMenu("DEBUG_TestFamilyPromptQueue")]
    public void DEBUG_TestFamilyPromptQueue()
    {
        const string NAME = "FamilyPromptQueue";
        if (!StressTestPreCheck(NAME)) return;
        ApplyProfile(ProfileType.Formal);
        var pdm = PlayerDataManager.Instance;

        Debug.Log("===== FAMILY PROMPT QUEUE TEST =====");

        // Cut transport so it's eligible, and force the chance to fire by aging it.
        SetCategoryProvision(ExpenseCategory.Transport, 25f);
        var t = GetCategoryState(ExpenseCategory.Transport);
        t.monthsSinceCut = 5;          // pushes chance to the cap
        t.monthsSinceLastRaise = 5;    // triggers safety-net (0.90)

        var prompts = BuildFamilyPrompts();
        Debug.Log($"[FQ] prompts built = {prompts.Count} (expect >=1)");
        if (prompts.Count == 0) { Debug.Log("[FQ] (RNG didn't fire - re-run; chance is 0.90)"); IsHeadlessSimulation = false; return; }

        var p = prompts[0];
        Debug.Log($"[FQ] isFamilyPrompt={p.isFamilyPrompt} cat={p.familyPromptCategory} choices={p.choices.Count} (expect True, Transport, 3)");

        // Simulate splice into a 2-event list.
        var combined = new List<ResolvedEvent>
        {
            new ResolvedEvent { title = "FakeEventA" },
            new ResolvedEvent { title = "FakeEventB" }
        };
        foreach (var prompt in prompts)
            combined.Insert(Random.Range(0, combined.Count + 1), prompt);
        Debug.Log($"[FQ] combined queue size = {combined.Count} (expect {2 + prompts.Count})");

        // Resolve the prompt with decline → nag #1 = 30% of 10 = 3.
        float before = pdm.FamilyMorale;
        RaiseCategory(p.familyPromptCategory, 2);
        Debug.Log($"[FQ] declined → morale {before:F1} → {pdm.FamilyMorale:F1} (expect -3.0 change), timesRaised={t.timesRaised} (expect 1)");

        Debug.Log("===== FAMILY PROMPT QUEUE TEST COMPLETE =====");
        IsHeadlessSimulation = false;
    }

    [ContextMenu("DEBUG_TestBudgetBar")]
    public void DEBUG_TestBudgetBar()
    {
        const string NAME = "BudgetBar";
        if (!StressTestPreCheck(NAME)) return;
        ApplyProfile(ProfileType.Formal);

        Debug.Log("===== BUDGET BAR TEST =====");

        var a = GetBudgetBarState(140, 50, 40);
        Debug.Log($"[BB] A base={a.baseLine:F0} current={a.current:F0} cap={a.cap:F0} below={a.belowBase:F0} above={a.aboveBase:F0} atCap={a.atCap}");
        Debug.Log($"[BB] A expect base=420 current=420 cap≈508 below=0 above=0");

        var b = GetBudgetBarState(100, 50, 40);
        Debug.Log($"[BB] B current={b.current:F0} below={b.belowBase:F0} (expect current 380, below 40)");

        var c = GetBudgetBarState(180, 50, 40);
        Debug.Log($"[BB] C current={c.current:F0} above={c.aboveBase:F0} (expect current 460, above 40)");

        // Boost $40 on groceries + a $25 event on groceries.
        // Under Option A, the event is absorbed by the boost: cost stays at the boosted level (460),
        // not 485. Only inflation exceeding the boost would spill over.
        ApplyExpenseEffect(ExpenseCategory.Groceries, 25f, 2);
        var d = GetBudgetBarState(180, 50, 40);
        Debug.Log($"[BB] D current={d.current:F0} above={d.aboveBase:F0} (expect current 460, above 40 - event absorbed, no spillover)");

        var e = GetBudgetBarState(300, 50, 40);
        Debug.Log($"[BB] E current={e.current:F0} cap={e.cap:F0} atCap={e.atCap} (expect current>cap, atCap True)");

        Debug.Log("===== BUDGET BAR TEST COMPLETE =====");
        IsHeadlessSimulation = false;
    }
#endif
}