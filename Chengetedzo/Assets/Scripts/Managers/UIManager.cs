using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using static GameManager;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Top Info")]
    public TextMeshProUGUI monthText;
    public TextMeshProUGUI moneyText;

    [Header("Personal Goal")]
    // Optional - small line under the balance, e.g. "Her own market stall: $240 / $500".
    // Safe to leave unassigned; UpdateGoalProgressText no-ops if null.
    public TextMeshProUGUI goalProgressText;

    [Header("Top HUD Buttons")]
    public GameObject loanButton;
    public GameObject savingsButton;

    [Header("Panels")]
    public GameObject budgetPanel;
    public GameObject loanPanel;
    public GameObject insurancePanel;
    public GameObject reportPanel;

    [Header("Setup Flow")]
    public GameObject setupPanel;
    public GameObject forecastPanel;

    [Header("Simulation HUD")]
    public GameObject topHUD;
    public GameObject financialHUD;

    [Header("Persistent Navigation")]
    public GameObject exitButtonHUD;

    [Header("Other Screens")]
    public GameObject endOfYearScreen;
    public TextMeshProUGUI resultsText;

    [Header("Glossary")]
    public GameObject glossaryPanel;

    [Header("End Of Year Controls")]
    public Button endOfYearContinueButton;
    public Button restartButton;

    [Header("Year End Graph")]
    public YearEndGraph yearEndGraph;

    private int yearEndPage = 0;
    private string yearPartOneText;

    private string yearPartTwoText;
    private const float CONTINUE_X = 700f;
    private const float BACK_X = -700f;

    [Header("Event Popup")]
    public GameObject eventPopup;
    public RectTransform eventContent;
    public Image eventIcon;
    public TextMeshProUGUI eventTitleText;
    public TextMeshProUGUI eventDescriptionText;
    public Button continueButton;

    [Header("Event Pool Headers")]
    public Sprite headerWeather;
    public Sprite headerAgriculture;
    public Sprite headerEconomic;
    public Sprite headerHealth;
    public Sprite headerCrime;
    public Sprite headerOpportunity;
    public Sprite headerDefault;

    [Header("Event Animation")]
    public RectTransform eventNotificationRect;
    public float eventSlideDuration = 0.4f;

    // One row per sender name that has portrait art. senderName must match the string
    // passed into ShowChoicePopup/ShowMessagePopup exactly - this is keyed on sender name
    // only, never on senderRelation, since two senders can share a relation (e.g. two
    // "Friend" contacts) but not a portrait.
    [System.Serializable]
    public class SenderPortrait
    {
        public string senderName;
        public Sprite icon;
    }

    [Header("Choice Popup")]
    public TextMeshProUGUI choiceSenderNameText;
    public TextMeshProUGUI choiceSenderRelationText;

    // Icon slot at Canvas/PopUpLayer/EventChoicePopup/Icon. senderPortraits is the sender
    // name -> sprite mapping (fill in the Inspector); defaultSenderIcon is shown for any
    // sender not in the list - new senders added later, organisations with no art yet, and
    // the three deliberately-unassigned professionals. Applied fresh on every popup call,
    // never left over from the previous message.
    public Image choiceSenderIcon;
    public List<SenderPortrait> senderPortraits = new List<SenderPortrait>();
    public Sprite defaultSenderIcon;
    private Dictionary<string, Sprite> _senderPortraitLookup;

    public GameObject choiceResultBubble;
    public TextMeshProUGUI choiceResultText;
    public Button choiceContinueButton;
    public GameObject choiceButtonsContainer;

    public GameObject choiceEventPopup;
    public TextMeshProUGUI choiceTitleText;
    public TextMeshProUGUI choiceDescriptionText;
    public RectTransform choiceSenderBubbleRect;
    public GameObject choiceButtonPrefab;
    public Transform choiceButtonsParent;

    // Built once from senderPortraits on first use. Later duplicate senderName rows are
    // ignored (first one wins) rather than throwing, so a typo'd duplicate in the Inspector
    // doesn't take the popup down.
    private Sprite GetSenderIcon(string senderName)
    {
        if (_senderPortraitLookup == null)
        {
            _senderPortraitLookup = new Dictionary<string, Sprite>();
            foreach (var entry in senderPortraits)
            {
                if (string.IsNullOrEmpty(entry.senderName) || entry.icon == null) continue;
                if (!_senderPortraitLookup.ContainsKey(entry.senderName))
                    _senderPortraitLookup[entry.senderName] = entry.icon;
            }
        }

        return _senderPortraitLookup.TryGetValue(senderName, out Sprite sprite) ? sprite : defaultSenderIcon;
    }

    // Always assigns something (a match or the fallback), so the icon never carries over
    // from whichever sender was shown last.
    private void ApplySenderIcon(string senderName)
    {
        if (choiceSenderIcon == null) return;
        choiceSenderIcon.sprite = GetSenderIcon(senderName);
    }

    public bool IsEventPopupShowing()
    {
        return eventPopup != null && eventPopup.activeSelf;
    }

    public bool IsChoicePopupShowing()
    {
        return choiceEventPopup != null && choiceEventPopup.activeSelf;
    }

    private System.Action<int> _onChoicePicked;

    private System.Action _budgetAdjustOnDone;

    public void ShowExpenseAdjustment(System.Action onDone)
    {
        _budgetAdjustOnDone = onDone;
        SwitchPanel(UIPanelState.Setup);
        setupPanel.GetComponent<SetupPanelController>()?.EnterExpenseAdjustmentMode();
        TutorialManager.Instance?.OnFirstBudgetCut();
    }

    public void OnBudgetAdjustmentConfirmed()
    {
        var cb = _budgetAdjustOnDone;
        _budgetAdjustOnDone = null;
        SwitchPanel(UIPanelState.Simulation);
        cb?.Invoke();
    }

    [Header("Mentor Popup")]
    public GameObject mentorPopup;
    public TextMeshProUGUI mentorText;
    public Button mentorContinueButton;
    public UnityEngine.UI.Image mentorBackground;
    public GameObject mentorFaceContainer;
    public UnityEngine.UI.Image mentorPortraitImage;
    public GameObject mentorChatBubble;
    public TextMeshProUGUI mentorChatText;

    [Header("Main Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject profileSelectPanel;
    public UnityEngine.UI.Button freeModeButton;

    [Header("Settings")]
    public GameObject settingsPanel;

    [Header("Input Blocker")]
    public GameObject inputBlockerPanel;
    private UIPanelState _stateBeforeSettings = UIPanelState.None;

    private UIPanelState currentPanelState = UIPanelState.None;

    private GameObject activePopup;
    private Button activeContinueButton;
    private System.Action activeOnClose;

    public bool IsPopupActive { get; private set; }
    public bool IsGuidedMode { get; private set; }
    private enum SuspendedContext { None, EventPopup, ChoicePopup }
    private SuspendedContext _suspendedContext = SuspendedContext.None;

    private void ShowPopup(
        GameObject popupObject,
        Button continueBtn,
        System.Action onClose = null)
    {
        if (IsPopupActive)
        {
            Debug.LogWarning("Popup already active. Ignoring new popup.");
            return;
        }

        IsPopupActive = true;

        activePopup = popupObject;
        activeContinueButton = continueBtn;
        activeOnClose = onClose;

        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(true);
        popupObject.SetActive(true);

        continueBtn.onClick.RemoveAllListeners();
        continueBtn.onClick.AddListener(() =>
        {
            CloseActivePopup();
        });

    }

    public void CloseActivePopup()
    {
        if (!IsPopupActive)
            return;

        // Second layer of defence: the choice popup (regular choice events AND the
        // insurance claim choice, which is shown through this same popup) only applies
        // its consequence once OnChoiceSelected fires - a generic force-close here would
        // hide it without ever calling that, silently dropping the decision and its
        // money. Refuse instead of closing while it's the active popup.
        if (activePopup == choiceEventPopup)
        {
            Debug.LogWarning("[UI] CloseActivePopup refused - choice popup has an unresolved decision.");
            return;
        }

        activePopup.SetActive(false);

        var callback = activeOnClose;

        activePopup = null;
        activeContinueButton = null;
        activeOnClose = null;
        IsPopupActive = false;

        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);

        Debug.Log("Popup closed safely.");

        callback?.Invoke();
    }

    public enum UIPanelState
    {
        None,
        MainMenu,
        ProfileSelect,
        Setup,
        Budget,
        Forecast,
        Insurance,
        Loan,
        Report,
        EndOfYear,
        Simulation
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        HideAllPanels();

        eventPopup?.SetActive(false);
        mentorPopup?.SetActive(false);
        choiceEventPopup?.SetActive(false);

        HideLoanTopButton();
        HideSavingsTopButton();
    }

    private async void Start()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    // UNITY_WEBGL covers every WebGL host (Facebook, itch.io, etc. are all the same Unity
    // platform) - this runtime check is what actually tells them apart. Only Facebook's own
    // template loads window.FBInstant; calling into it anywhere else throws a ReferenceError
    // that happens inside a native dynCall and is NOT catchable here, so it must be skipped
    // before ever touching FBInstant, not caught after the fact.
    if (WebGLBridge.IsAvailable())
    {
        try
        {
            var initTask = Meta.InstantGames.FBInstant.InitializeAsync();
            if (await Task.WhenAny(initTask, Task.Delay(5000)) == initTask)
                await Meta.InstantGames.FBInstant.StartGameAsync();
            else
                Debug.LogWarning("[UIManager] FBInstant init timed out - continuing anyway.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[UIManager] FBInstant init failed: {ex.Message}");
        }
    }
    else
    {
        Debug.Log("[UIManager] FBInstant not present on this host - skipping Facebook init.");
    }
#endif
        SwitchPanel(UIPanelState.MainMenu);
    }

    void ApplySafeArea(RectTransform panel)
    {
        Rect safeArea = Screen.safeArea;
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;
        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;
    }

    public void ShowSetupPanel()
    {
        SwitchPanel(UIPanelState.Setup);
        setupPanel.GetComponent<SetupPanelController>()?.OnPanelOpened();
    }

    public void ShowGlossary()
    {
        if (glossaryPanel != null)
            glossaryPanel.SetActive(true);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
    }

    public void HideGlossary()
    {
        if (glossaryPanel != null)
            glossaryPanel.SetActive(false);

        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }

    public void UpdateMonthText(int currentMonth, int totalMonths)
    {
        int displayMonth = ((currentMonth - 1) % 12) + 1;

        string monthName = System.Globalization.CultureInfo
            .CurrentCulture
            .DateTimeFormat
            .GetMonthName(displayMonth);

        monthText.text = $"{monthName} ({currentMonth}/{totalMonths})";
    }

    public void UpdateMoneyText(float amount)
    {
        Debug.Log("Updating TOP HUD TEXT to: " + amount);
        moneyText.text = $"Balance: {GameUtils.FormatMoney(amount)}";
    }

    public void UpdateGoalProgressText()
    {
        if (goalProgressText == null) return;
        string line = GameManager.Instance?.GetGoalProgressLine() ?? "";
        goalProgressText.gameObject.SetActive(!string.IsNullOrEmpty(line));
        if (!string.IsNullOrEmpty(line))
            goalProgressText.text = line;
    }

    // Silent variant for the month ticker - called every frame while lerping,
    // so no Debug.Log (it would flood the console).
    private void SetMoneyDisplay(float amount)
    {
        if (moneyText != null)
            moneyText.text = $"Balance: {GameUtils.FormatMoney(amount)}";
    }

    // ---------------- Month Ticker ----------------
    // Plays the month's pre-event ledger as a watched sequence: balance resets to
    // the opening amount, income lands, then each expense drains it - with the
    // current line item shown in simTickerText. Events fire only after it finishes.
    // Uses only HUD text, never the popup system.

    [Header("Month Ticker")]
    public TextMeshProUGUI simTickerText;      // optional - ticker still works (silently) if unassigned
    public Image tickerIcon;                   // optional - money icon shown beside the ticker line
    public Sprite tickerIncomeSprite;          // shown for entries that add money
    public Sprite tickerExpenseSprite;         // shown for entries that remove money
    public float tickerCountSeconds = 0.30f;   // lerp time per entry
    public float tickerHoldSeconds = 0.30f;    // pause on each entry after the lerp

    private static bool IsTickerEntry(FinancialEntry.EntryType t) => t switch
    {
        // Event outcomes are revealed by their popups, not spoiled by the ticker.
        FinancialEntry.EntryType.EventReward => false,
        FinancialEntry.EntryType.EventLoss => false,
        FinancialEntry.EntryType.InsurancePayout => false,
        _ => true
    };

    public void PlayMonthTicker(MonthlyFinancialLedger ledger, System.Action onComplete)
    {
        if (ledger == null || ledger.EntryCount == 0)
        {
            onComplete?.Invoke();
            return;
        }
        StartCoroutine(MonthTickerRoutine(ledger, onComplete));
    }

    private bool tickerPaused;
    private float tickerRunningBalance;
    private float cashAtPause;
    private float tickerCurrentTarget;

    public void PauseMonthTicker()
    {
        tickerPaused = true;
        cashAtPause = GameManager.Instance.financeManager.CashOnHand;
        SetMoneyDisplay(cashAtPause);
        if (simTickerText != null) simTickerText.text = "End of month balance";
    }

    public void ResumeMonthTicker()
    {
        float delta = GameManager.Instance.financeManager.CashOnHand - cashAtPause;
        tickerRunningBalance += delta;
        tickerCurrentTarget += delta;
        tickerPaused = false;
    }

    private IEnumerator MonthTickerRoutine(MonthlyFinancialLedger ledger, System.Action onComplete)
    {
        var entries = new List<FinancialEntry>();
        foreach (var e in ledger.Entries)
            if (IsTickerEntry(e.entryType))
                entries.Add(e);

        if (entries.Count == 0)
        {
            onComplete?.Invoke();
            yield break;
        }

        // Busy months tighten up so the sequence never drags.
        float count = tickerCountSeconds;
        float hold = tickerHoldSeconds;
        if (entries.Count > 8) { count *= 0.6f; hold *= 0.6f; }

        RectTransform tickerRect = null;
        Vector2 tickerBasePos = Vector2.zero;
        if (simTickerText != null)
        {
            tickerRect = simTickerText.rectTransform;
            tickerBasePos = tickerRect.anchoredPosition;
            simTickerText.gameObject.SetActive(true);
            simTickerText.text = "";
            simTickerText.alpha = 0f;
        }
        if (tickerIcon != null)
            tickerIcon.gameObject.SetActive(false); // shown once the first entry lands

        tickerRunningBalance = ledger.OpeningBalance;
        SetMoneyDisplay(tickerRunningBalance);
        yield return new WaitForSeconds(0.35f);

        var gm = GameManager.Instance;
        int lastWeek = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            float signed = entry.SignedAmount();
            tickerCurrentTarget = tickerRunningBalance + signed;

            // Week counter woven into the month text: "January, Week 2"
            if (gm != null && monthText != null && entries.Count > 0)
            {
                int week = 1 + Mathf.Min(3, (i * 4) / entries.Count);
                if (week != lastWeek)
                {
                    lastWeek = week;
                    int displayMonth = ((gm.currentMonth - 1) % 12) + 1;
                    string monthName = System.Globalization.CultureInfo
                        .CurrentCulture.DateTimeFormat.GetMonthName(displayMonth);
                    monthText.text = $"{monthName}, Week {week}";
                }
            }

            if (simTickerText != null)
            {
                string sign = signed >= 0f ? "+" : "-";
                simTickerText.text = $"{entry.description}   {sign}${Mathf.Abs(signed):F0}";
            }

            if (tickerIcon != null)
            {
                Sprite s = signed >= 0f ? tickerIncomeSprite : tickerExpenseSprite;
                tickerIcon.sprite = s;
                tickerIcon.gameObject.SetActive(s != null);
            }

            if (signed >= 0f) AudioManager.Instance?.OnMoneyGain();
            else AudioManager.Instance?.OnMoneyLoss();

            float total = count + hold;
            float t = 0f;
            while (t < total)
            {
                while (tickerPaused) yield return null;
                t += Time.deltaTime;
                SetMoneyDisplay(Mathf.Lerp(tickerRunningBalance, tickerCurrentTarget, Mathf.Clamp01(t / count)));

                if (tickerRect != null)
                {
                    float p = Mathf.Clamp01(t / total);
                    float fadeIn = Mathf.Clamp01(t / 0.12f);
                    float fadeOut = Mathf.Clamp01((1f - p) / 0.30f);
                    float a = Mathf.Min(fadeIn, fadeOut);

                    simTickerText.alpha = a;
                    tickerRect.anchoredPosition = tickerBasePos + new Vector2(0f, 14f * p);

                    if (tickerIcon != null && tickerIcon.gameObject.activeSelf)
                    {
                        var c = tickerIcon.color;
                        c.a = a;
                        tickerIcon.color = c;
                    }
                }
                yield return null;
            }
            tickerRunningBalance = tickerCurrentTarget;
            SetMoneyDisplay(tickerRunningBalance);
        }

        while (tickerPaused) yield return null;
        yield return new WaitForSeconds(0.2f);
        if (simTickerText != null)
        {
            simTickerText.gameObject.SetActive(false);
            simTickerText.alpha = 1f;
            if (tickerRect != null) tickerRect.anchoredPosition = tickerBasePos;
        }
        if (tickerIcon != null)
        {
            tickerIcon.gameObject.SetActive(false);
            var ic = tickerIcon.color;
            ic.a = 1f;
            tickerIcon.color = ic;
        }
        if (gm != null && monthText != null)
            UpdateMonthText(gm.currentMonth, gm.totalMonths);

        while (tickerPaused) yield return null;
        onComplete?.Invoke();
    }

    public void PlayMoneyBeat(string label, float signedAmount, float fromBalance, System.Action onDone)
    {
        StartCoroutine(MoneyBeatRoutine(label, signedAmount, fromBalance, onDone));
    }

    private IEnumerator MoneyBeatRoutine(string label, float signedAmount, float fromBalance, System.Action onDone)
    {
        float target = fromBalance + signedAmount;

        RectTransform tickerRect = null;
        Vector2 tickerBasePos = Vector2.zero;
        if (simTickerText != null)
        {
            tickerRect = simTickerText.rectTransform;
            tickerBasePos = tickerRect.anchoredPosition;
            simTickerText.gameObject.SetActive(true);
            string sign = signedAmount >= 0f ? "+" : "-";
            simTickerText.text = $"{label}   {sign}${Mathf.Abs(signedAmount):F0}";
            simTickerText.alpha = 0f;
        }

        if (tickerIcon != null)
        {
            Sprite s = signedAmount >= 0f ? tickerIncomeSprite : tickerExpenseSprite;
            tickerIcon.sprite = s;
            tickerIcon.gameObject.SetActive(s != null);
        }

        if (signedAmount >= 0f) AudioManager.Instance?.OnMoneyGain();
        else AudioManager.Instance?.OnMoneyLoss();

        float count = tickerCountSeconds;
        float hold = tickerHoldSeconds;
        float total = count + hold;
        float t = 0f;
        while (t < total)
        {
            t += Time.deltaTime;
            SetMoneyDisplay(Mathf.Lerp(fromBalance, target, Mathf.Clamp01(t / count)));

            if (tickerRect != null)
            {
                float p = Mathf.Clamp01(t / total);
                float fadeIn = Mathf.Clamp01(t / 0.12f);
                float fadeOut = Mathf.Clamp01((1f - p) / 0.30f);
                float a = Mathf.Min(fadeIn, fadeOut);

                simTickerText.alpha = a;
                tickerRect.anchoredPosition = tickerBasePos + new Vector2(0f, 14f * p);

                if (tickerIcon != null && tickerIcon.gameObject.activeSelf)
                {
                    var c = tickerIcon.color;
                    c.a = a;
                    tickerIcon.color = c;
                }
            }
            yield return null;
        }

        SetMoneyDisplay(target);

        if (simTickerText != null)
        {
            simTickerText.gameObject.SetActive(false);
            simTickerText.alpha = 1f;
            if (tickerRect != null) tickerRect.anchoredPosition = tickerBasePos;
        }
        if (tickerIcon != null)
        {
            tickerIcon.gameObject.SetActive(false);
            var ic = tickerIcon.color;
            ic.a = 1f;
            tickerIcon.color = ic;
        }

        onDone?.Invoke();
    }

    private void HideAllPanels()
    {
        if (setupPanel != null) setupPanel.SetActive(false);
        if (budgetPanel != null) budgetPanel.SetActive(false);
        if (forecastPanel != null) forecastPanel.SetActive(false);
        if (insurancePanel != null) insurancePanel.SetActive(false);
        if (loanPanel != null) loanPanel.SetActive(false);
        if (reportPanel != null) reportPanel.SetActive(false);
        if (endOfYearScreen != null) endOfYearScreen.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (profileSelectPanel != null) profileSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void ShowPanel(UIPanelState state)
    {
        HideAllPanels();

        switch (state)
        {
            case UIPanelState.Setup:
                setupPanel.SetActive(true);
                break;

            case UIPanelState.Budget:
                budgetPanel.SetActive(true);
                break;

            case UIPanelState.Forecast:
                forecastPanel.SetActive(true);
                break;

            case UIPanelState.Loan:
                loanPanel.SetActive(true);
                break;

            case UIPanelState.Report:
                reportPanel.SetActive(true);
                break;

            case UIPanelState.EndOfYear:
                endOfYearScreen.SetActive(true);
            break;
        }
    }

    public void SwitchPanel(UIPanelState newState)
    {
        if (currentPanelState == newState)
            return;
        //------------------------------------//
        // FUTURE BARAKA. BE VERY CAREFUL WITH EVENT AND MENTOR POPUPS. YOU WILL REGRET TOUCHING ANYTHING. DOUBLE CHECK STATES IF YOU DO. CRASH OUT COUNT = 13
        //-----------------------------------//
        if (IsPopupActive)
        {
            Debug.Log("Closing existing popup before opening new one.");
            CloseActivePopup();
        }

        HideAllPanels();

        currentPanelState = newState;

        switch (newState)
        {
            case UIPanelState.Setup:
                setupPanel.SetActive(true);
                topHUD?.SetActive(true);
                financialHUD?.SetActive(false);
                break;

            case UIPanelState.Budget:
                budgetPanel.SetActive(true);
                topHUD?.SetActive(true);
                financialHUD?.SetActive(false);
                break;

            case UIPanelState.Forecast:
                forecastPanel.SetActive(true);
                topHUD?.SetActive(true);
                financialHUD?.SetActive(false);
                break;

            case UIPanelState.Insurance:
                insurancePanel.SetActive(true);
                topHUD?.SetActive(true);
                financialHUD?.SetActive(false);
                break;

            case UIPanelState.Loan:
                loanPanel.SetActive(true);
                topHUD?.SetActive(true);
                financialHUD?.SetActive(false);
                break;

            case UIPanelState.Report:
                reportPanel.SetActive(true);
                topHUD?.SetActive(false);
                financialHUD?.SetActive(false);
                break;

            case UIPanelState.EndOfYear:
                endOfYearScreen.SetActive(true);
                topHUD?.SetActive(false);
                financialHUD?.SetActive(false);
                break;

            case UIPanelState.Simulation:
                topHUD?.SetActive(true);
                financialHUD?.SetActive(true);
                break;

            case UIPanelState.MainMenu:
                mainMenuPanel.SetActive(true);
                topHUD?.SetActive(false);
                financialHUD?.SetActive(false);
                break;

            case UIPanelState.ProfileSelect:
                profileSelectPanel.SetActive(true);
                topHUD.SetActive(false);
                break;

            case UIPanelState.None:
                topHUD?.SetActive(false);
                financialHUD?.SetActive(false);
                break;
        }
    }

    public void ShowBudgetPanel()
    {
        SwitchPanel(UIPanelState.Budget);
    }

    public void ShowSetupPanelAtReview()
    {
        SwitchPanel(UIPanelState.Setup);
        setupPanel.GetComponent<SetupPanelController>()?.JumpToReviewStep();
    }

    public void ShowForecastPanel()
    {
        SwitchPanel(UIPanelState.Forecast);
        TutorialManager.Instance?.OnForecastOpened();
    }

    public void ShowInsurancePanel()
    {
        SwitchPanel(UIPanelState.Insurance);
        TutorialManager.Instance?.OnInsuranceOpened();
    }

    public void ShowReportPanel(string reportText)
    {
        if (IsPopupActive)
            CloseActivePopup();

        SwitchPanel(UIPanelState.Report);

        var visualPanel = reportPanel.GetComponent<MonthlyReportPanel>();
        visualPanel?.Populate(GameManager.Instance?.CurrentLedger);

        TutorialManager.Instance?.OnReportOpened();
    }

    public void ForceCloseAllPopups()
    {
        if (activePopup != null)
            activePopup.SetActive(false);

        if (mentorPopup != null)
            mentorPopup.SetActive(false);

        if (choiceEventPopup != null)
            choiceEventPopup.SetActive(false);

        if (eventPopup != null)
            eventPopup.SetActive(false);

        _onChoicePicked = null;

        activePopup = null;
        activeContinueButton = null;
        activeOnClose = null;
        IsPopupActive = false;

        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);
        Debug.Log("[UI] ForceCloseAllPopups called");
    }

    public void ShowEventPopup(string title, string description, EventPool pool = EventPool.Economic, Sprite icon = null)
    {
        eventTitleText.text = title;
        eventDescriptionText.text = description;

        Sprite header = pool switch
        {
            EventPool.Weather => headerWeather,
            EventPool.Agriculture => headerAgriculture,
            EventPool.Economic => headerEconomic,
            EventPool.Health => headerHealth,
            EventPool.Crime => headerCrime,
            EventPool.Opportunity => headerOpportunity,
            _ => headerDefault,
        };

        eventIcon.sprite = header != null ? header : icon;
        eventIcon.enabled = true;

        ShowPopup(eventPopup, continueButton, () =>
        {
            GameManager.Instance.OnEventPopupClosed();
        });

        StartCoroutine(SlideEvent(true));
    }

    public void ShowEventPopupWithCallback(string title, string description, EventPool pool, System.Action onClose)
    {
        eventTitleText.text = title;
        eventDescriptionText.text = description;

        Sprite header = pool switch
        {
            EventPool.Weather => headerWeather,
            EventPool.Agriculture => headerAgriculture,
            EventPool.Economic => headerEconomic,
            EventPool.Health => headerHealth,
            EventPool.Crime => headerCrime,
            EventPool.Opportunity => headerOpportunity,
            _ => headerDefault,
        };

        eventIcon.sprite = header != null ? header : null;
        eventIcon.enabled = true;

        ShowPopup(eventPopup, continueButton, onClose);
        StartCoroutine(SlideEvent(true));
    }

    private IEnumerator SlideEvent(bool slideIn)
    {
        if (eventNotificationRect == null) yield break;

        float restY = eventNotificationRect.anchoredPosition.y;
        float offscreenY = restY + 600f;

        float startY = slideIn ? offscreenY : restY;
        float endY = slideIn ? restY : offscreenY;

        eventNotificationRect.anchoredPosition = new Vector2(
            eventNotificationRect.anchoredPosition.x, startY);

        float time = 0f;
        while (time < eventSlideDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, time / eventSlideDuration);
            eventNotificationRect.anchoredPosition = new Vector2(
                eventNotificationRect.anchoredPosition.x,
                Mathf.Lerp(startY, endY, t));
            yield return null;
        }

        eventNotificationRect.anchoredPosition = new Vector2(
            eventNotificationRect.anchoredPosition.x, endY);
    }

    public void ShowChoicePopup(
    string title,
    string description,
    string senderName,
    string senderRelation,
    List<EventData.ChoiceOption> choices,
    System.Action<int> onChoicePicked)
    {
        if (IsPopupActive)
        {
            Debug.LogWarning("[UI] Choice popup requested while another popup is active.");
            return;
        }

        _onChoicePicked = onChoicePicked;

        choiceTitleText.text = title;
        choiceDescriptionText.text = description;

        if (choiceSenderNameText != null)
            choiceSenderNameText.text = senderName;

        if (choiceSenderRelationText != null)
            choiceSenderRelationText.text = senderRelation;

        ApplySenderIcon(senderName);

        choiceResultBubble?.SetActive(false);
        choiceButtonsContainer?.SetActive(true);
        choiceContinueButton?.gameObject.SetActive(false);

        foreach (Transform child in choiceButtonsParent)
            Destroy(child.gameObject);

        for (int i = 0; i < choices.Count; i++)
        {
            GameObject btn = Instantiate(choiceButtonPrefab, choiceButtonsParent);

            TMP_Text label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = choices[i].label;

            int captured = i;
            btn.GetComponent<Button>().onClick.AddListener(
                () => ShowChoiceResult(captured, choices)
            );
        }

        ShowPopup(choiceEventPopup, choiceContinueButton, null);

        if (choiceSenderBubbleRect != null)
            UIAnimator.Instance?.ScaleBubbleIn(choiceSenderBubbleRect);

        StartCoroutine(EqualizeButtonHeights());
    }

    // Sibling to ShowChoicePopup for a message with no decision to make, only Continue -
    // insurance denial letters use this instead of ShowEventPopupWithCallback (the full
    // event card, whose `pool` argument picks a header sprite - wrong for a letter that
    // isn't illustrating a new event, and reads as a second event when the actual event
    // already had its own card a moment earlier). Same body as ShowChoicePopup down to
    // the sender fields, then lines 912-914 above are inverted: buttons off, Continue on,
    // and the per-choice button loop is skipped entirely.
    public void ShowMessagePopup(
    string title,
    string description,
    string senderName,
    string senderRelation,
    System.Action onClose)
    {
        if (IsPopupActive)
        {
            Debug.LogWarning("[UI] Message popup requested while another popup is active.");
            return;
        }

        choiceTitleText.text = title;
        choiceDescriptionText.text = description;

        if (choiceSenderNameText != null)
            choiceSenderNameText.text = senderName;

        if (choiceSenderRelationText != null)
            choiceSenderRelationText.text = senderRelation;

        ApplySenderIcon(senderName);

        choiceResultBubble?.SetActive(false);
        choiceButtonsContainer?.SetActive(false);
        choiceContinueButton?.gameObject.SetActive(true);

        ShowPopup(choiceEventPopup, choiceContinueButton, null);

        // ShowPopup just wired continueBtn.onClick to CloseActivePopup, which refuses to
        // close choiceEventPopup outright (see the guard at the top of CloseActivePopup -
        // it assumes an unresolved multi-choice decision still needs OnChoiceSelected to
        // fire). Override with a direct close, the same way ShowChoiceResult overrides it
        // once a choice is actually picked.
        choiceContinueButton.onClick.RemoveAllListeners();
        choiceContinueButton.onClick.AddListener(() => CloseMessagePopup(onClose));

        if (choiceSenderBubbleRect != null)
            UIAnimator.Instance?.ScaleBubbleIn(choiceSenderBubbleRect);
    }

    private void CloseMessagePopup(System.Action onClose)
    {
        choiceEventPopup.SetActive(false);
        IsPopupActive = false;

        if (activePopup == choiceEventPopup)
        {
            activePopup = null;
            activeContinueButton = null;
            activeOnClose = null;
        }

        if (inputBlockerPanel != null) inputBlockerPanel.SetActive(false);

        onClose?.Invoke();
    }

    private IEnumerator EqualizeButtonHeights()
    {
        yield return null;
        yield return null;

        if (choiceButtonsParent == null) yield break;

        LayoutRebuilder.ForceRebuildLayoutImmediate(choiceButtonsParent as RectTransform);

        float maxHeight = 0f;
        var rects = new List<RectTransform>();

        foreach (Transform child in choiceButtonsParent)
        {
            var rect = child.GetComponent<RectTransform>();
            if (rect == null) continue;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            rects.Add(rect);
            maxHeight = Mathf.Max(maxHeight, rect.rect.height);
        }

        foreach (var rect in rects)
        {
            var csf = rect.GetComponent<UnityEngine.UI.ContentSizeFitter>();
            if (csf != null) csf.enabled = false;

            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxHeight);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(choiceButtonsParent as RectTransform);
    }

    private void ShowChoiceResult(int index, List<EventData.ChoiceOption> choices)
    {
        var choice = choices[index];

        choiceButtonsContainer?.SetActive(false);

        if (choiceResultBubble != null)
        {
            string resultMsg = choice.resultDescription;

            if (choice.moneyChange != 0f)
            {
                string sign = choice.moneyChange > 0f ? "+" : "-";
                resultMsg += $"\n\n{sign}${Mathf.Abs(choice.moneyChange):F0}";
            }

            choiceResultText.text = resultMsg;
            choiceResultBubble.SetActive(true);
            var rect = choiceResultBubble.GetComponent<RectTransform>();
            if (rect != null)
                UIAnimator.Instance?.ScaleBubbleIn(rect);
        }

        if (choiceContinueButton != null)
        {
            choiceContinueButton.gameObject.SetActive(true);
            choiceContinueButton.onClick.RemoveAllListeners();
            choiceContinueButton.onClick.AddListener(() => OnChoiceSelected(index));
        }
    }

    private void OnChoiceSelected(int index)
    {
        choiceEventPopup.SetActive(false);
        IsPopupActive = false;

        if (activePopup == choiceEventPopup)
        {
            activePopup = null;
            activeContinueButton = null;
            activeOnClose = null;
        }

        var callback = _onChoicePicked;
        _onChoicePicked = null;
        callback?.Invoke(index);
    }

    // End-of-Year Screen
    public void ShowEndOfYearSummary(string mentorReflection)
    {
        if (IsPopupActive && activePopup != null)
            CloseActivePopup();

        HideAllPanels();
        currentPanelState = UIPanelState.EndOfYear;
        if (endOfYearScreen != null) endOfYearScreen.SetActive(true);
        if (topHUD != null) topHUD.SetActive(false);

        var gm = GameManager.Instance;

        float net =
            gm.YearIncome
            - gm.YearExpenses
            - gm.YearPremiums
            - gm.YearEventLosses
            + gm.YearPayouts;

        // PART 1
        yearPartOneText =
            "<b>Year Complete</b>\n\n" +
            $"Income: {GameUtils.FormatMoney(gm.YearIncome)}\n" +
            $"Expenses: {GameUtils.FormatMoney(gm.YearExpenses)}\n" +
            $"Insurance Premiums: {GameUtils.FormatMoney(gm.YearPremiums)}\n" +
            $"Insurance Payouts: {GameUtils.FormatMoney(gm.YearPayouts)}\n" +
            $"Event Losses: {GameUtils.FormatMoney(gm.YearEventLosses)}\n\n" +
            $"Net Result: {GameUtils.FormatMoney(net)}\n" +
            $"Final Cash: {GameUtils.FormatMoney(gm.financeManager.CashOnHand)}\n\n" +
            $"<i>{mentorReflection}</i>";

        // PART 2
        // SECTION 0 - Score
        yearPartTwoText = BuildScoreSummary();

        // SECTION 1 - Insurance
        yearPartTwoText += "<b>Insurance</b>\n";

        if (gm.YearPremiums == 0f)
        {
            yearPartTwoText += $"You carried no insurance this year. Your {GameUtils.FormatMoney(gm.YearEventLosses)} in event losses came entirely out of pocket.\n\n";
        }
        else
        {
            float netInsuranceBenefit = gm.TotalInsurancePayoutAmount - gm.YearPremiums;

            if (gm.TotalInsurancePayoutAmount == 0f)
            {
                yearPartTwoText += $"You paid {GameUtils.FormatMoney(gm.YearPremiums)} in premiums and made no claims. That's not money wasted. That's the cost of protection you fortunately didn't need.\n\n";
            }
            else if (netInsuranceBenefit >= 0f)
            {
                yearPartTwoText += $"Your insurance paid out {GameUtils.FormatMoney(gm.TotalInsurancePayoutAmount)} against ${gm.YearPremiums:F0} in premiums, a net benefit of ${netInsuranceBenefit:F0}.\n\n";
            }
            else
            {
                yearPartTwoText += $"You paid {GameUtils.FormatMoney(gm.YearPremiums)} in premiums and received {GameUtils.FormatMoney(gm.TotalInsurancePayoutAmount)} back. You came out {GameUtils.FormatMoney(Mathf.Abs(netInsuranceBenefit))} behind, but that coverage was there if something serious had hit.\n\n";
            }
        }

        // SECTION 2 - Resilience
        yearPartTwoText += "<b>Resilience</b>\n";
        yearPartTwoText += $"You faced {gm.TotalUnexpectedEvents} unexpected events. {gm.InsuredEventsCount} were covered by insurance.\n";

        if (gm.ForcedLoanCount > 0)
            yearPartTwoText += $"You needed emergency loans in {gm.ForcedLoanCount} months, a signal that the gap between income and expenses was too thin.\n";

        if (gm.MonthsUnderFinancialPressure > 0)
            yearPartTwoText += $"Your cash went negative in {gm.MonthsUnderFinancialPressure} months.\n";

        if (gm.ForcedLoanCount == 0 && gm.MonthsUnderFinancialPressure == 0)
            yearPartTwoText += "You never went negative and never needed an emergency loan. That's genuine financial resilience.\n";

        yearPartTwoText += "\n";

        // SECTION 3 - One takeaway
        yearPartTwoText += "<b>Key Takeaway</b>\n";

        float netBenefit = gm.TotalInsurancePayoutAmount - gm.YearPremiums;

        if (netBenefit > 0f)
            yearPartTwoText += "Insurance paid for itself this year. The lesson: start early, stay consistent.";
        else if (gm.ForcedLoanCount >= 3)
            yearPartTwoText += "Repeated forced loans point to one gap: an emergency fund of even one month's expenses would have broken the cycle.";
        else if (gm.TotalUnexpectedEvents > 0 && gm.InsuredEventsCount == 0)
            yearPartTwoText += "Every loss this year was uninsured. Even basic cover would have reduced the damage.";
        else if (gm.financeManager.CashOnHand > 0f && gm.ForcedLoanCount == 0)
            yearPartTwoText += "You finished with cash in hand and no emergency debt. Build on that.";
        else
            yearPartTwoText += "Every year teaches something. The question is whether next year's decisions reflect what this one showed you.";

        yearEndPage = 0;
        resultsText.text = yearPartOneText;
        resultsText.gameObject.SetActive(true);
        SetEndOfYearButtonPosition(CONTINUE_X);
        endOfYearContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Key Takeaways";
        yearEndGraph?.HideGraph();
        StartCoroutine(RenderGraphNextFrame());
        restartButton.interactable = false;
    }
    private IEnumerator RenderGraphNextFrame()
    {
        yield return null;
        yearEndGraph?.Render(GameManager.Instance.monthHistory);
    }

    public void OnEndOfYearContinueClicked()
    {
        if (endOfYearScreen != null && !endOfYearScreen.activeSelf)
            endOfYearScreen.SetActive(true);

        yearEndPage++;

        switch (yearEndPage)
        {
            case 1: // Key Takeaways
                resultsText.text = yearPartTwoText;
                resultsText.gameObject.SetActive(true);
                yearEndGraph?.HideGraph();

                // If this is a yearly review (not final), wire continue to proceed
                if (_yearlyReviewOnContinue != null)
                {
                    endOfYearContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Year 2";
                    restartButton.gameObject.SetActive(false);
                    endOfYearContinueButton.onClick.RemoveAllListeners();
                    endOfYearContinueButton.onClick.AddListener(() =>
                    {
                        var cb = _yearlyReviewOnContinue;
                        _yearlyReviewOnContinue = null;
                        restartButton.gameObject.SetActive(true);
                        cb?.Invoke();
                    });
                }
                else
                {
                    endOfYearContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "View Graph";
                    restartButton.interactable = true;
                    SetEndOfYearButtonPosition(CONTINUE_X);
                }
                break;

            case 2: // Graph
                resultsText.gameObject.SetActive(false);
                yearEndGraph?.ShowGraph();
                endOfYearContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Back";
                SetEndOfYearButtonPosition(BACK_X);
                break;

            default: // Back to page 1
                yearEndPage = 0;
                resultsText.text = yearPartOneText;
                resultsText.gameObject.SetActive(true);
                yearEndGraph?.HideGraph();
                endOfYearContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Key Takeaways";
                restartButton.interactable = false;
                SetEndOfYearButtonPosition(CONTINUE_X);
                break;
        }
    }

    public void RestartGame()
    {
        GameManager.Instance.FullRestart();
    }

    public void ResetPanelState()
    {
        currentPanelState = UIPanelState.None;
    }

    public void ShowMentorMessage(string message, System.Action onClose = null)
    {
        Debug.Log($"[UI-MENTOR] ShowMentorMessage called | IsPopupActive={IsPopupActive} | msg=\"{message}\"");
        Debug.Log($"[UI-MENTOR] ShowMentorMessage stack: {System.Environment.StackTrace}");
        if (IsPopupActive)
        {
            StartCoroutine(WaitThenShowMentor(message, onClose));
            return;
        }

        if (mentorBackground != null) mentorBackground.gameObject.SetActive(true);
        if (mentorFaceContainer != null) mentorFaceContainer.SetActive(false);
        if (mentorChatBubble != null) mentorChatBubble.SetActive(true);
        if (mentorChatText != null) mentorChatText.text = message;

        ShowPopup(mentorPopup, mentorContinueButton, onClose);
        var rect = mentorPopup.GetComponent<RectTransform>();
        UIAnimator.Instance?.SlideUpChat(rect);
        AudioManager.Instance?.OnMentorMessage();
    }

    private IEnumerator WaitThenShowMentor(string message, System.Action onClose)
    {
        Debug.Log($"[UI-MENTOR] WaitThenShowMentor queued: \"{message}\"");
        yield return new WaitUntil(() => !IsPopupActive);
        Debug.Log($"[UI-MENTOR] WaitThenShowMentor now showing: \"{message}\"");
        ShowMentorMessage(message, onClose);
    }

    public void ShowMentorMessageTransparent(string message, System.Action onClose = null)
    {
        Debug.Log($"[UI-MENTOR] ShowMentorMessageTransparent called | IsPopupActive={IsPopupActive} | msg=\"{message}\"");
        if (IsPopupActive)
        {
            StartCoroutine(WaitThenShowTransparent(message, onClose));
            return;
        }

        if (mentorBackground != null) mentorBackground.gameObject.SetActive(false);
        if (mentorFaceContainer != null) mentorFaceContainer.SetActive(true);
        if (mentorChatBubble != null) mentorChatBubble.SetActive(false);

        mentorText.text = message;

        ShowPopup(mentorPopup, mentorContinueButton, () =>
        {
            if (mentorBackground != null) mentorBackground.gameObject.SetActive(true);
            if (mentorFaceContainer != null) mentorFaceContainer.SetActive(false);
            onClose?.Invoke();
        });
        UIAnimator.Instance?.FadeIn(mentorPopup);
    }

    private System.Collections.IEnumerator WaitThenShowTransparent(string message, System.Action onClose)
    {
        Debug.Log($"[UI-MENTOR] WaitThenShowTransparent queued: \"{message}\"");
        float waited = 0f;
        while (IsPopupActive && waited < 8f)
        {
            waited += UnityEngine.Time.deltaTime;
            yield return null;
        }
        if (waited >= 8f)
        {
            Debug.LogWarning("[UI-MENTOR] Timeout waiting for popup - force-clearing IsPopupActive.");
            ForceCloseAllPopups();
        }
        Debug.Log($"[UI-MENTOR] WaitThenShowTransparent now showing: \"{message}\"");
        ShowMentorMessageTransparent(message, onClose);
    }

    public void ShowLoanPanel()
    {
        if (IsPopupActive && activePopup == eventPopup)
            _suspendedContext = SuspendedContext.EventPopup;
        else if (IsPopupActive && activePopup == null) // choice popup manages itself
            _suspendedContext = SuspendedContext.ChoicePopup;
        else
            _suspendedContext = SuspendedContext.None;

        SwitchPanel(UIPanelState.Loan);
        loanPanel.GetComponent<LoanPanelController>()?.RefreshUI();
    }

    public void ShowLoanTopButton()
    {
        if (loanButton != null)
            loanButton.SetActive(true);
    }

    public void HideLoanTopButton()
    {
        if (loanButton != null)
            loanButton.SetActive(false);
    }

    public void OnLoanButtonClicked()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (IsPopupActive) return;
        gm.BeginLoanDecision();
    }

    public void CloseLoanPanel()
    {
        var context = _suspendedContext;
        _suspendedContext = SuspendedContext.None;

        if (context == SuspendedContext.EventPopup || context == SuspendedContext.ChoicePopup)
        {
            loanPanel.SetActive(false);
            currentPanelState = UIPanelState.Simulation;
        }
        else
        {
            SwitchPanel(UIPanelState.Simulation);
        }

        GameManager.Instance.OnLoanDecisionFinished();
    }

    public void ShowSavingsTopButton()
    {
        if (savingsButton != null)
            savingsButton.SetActive(true);
    }

    public void HideSavingsTopButton()
    {
        if (savingsButton != null)
            savingsButton.SetActive(false);
    }

    public void OnSavingsButtonClicked()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        if (IsPopupActive) return;
        gm.BeginSavingsDecision();
    }

    public void CloseSavingsPanel()
    {
        var context = _suspendedContext;
        _suspendedContext = SuspendedContext.None;

        if (context == SuspendedContext.EventPopup || context == SuspendedContext.ChoicePopup)
        {
            budgetPanel.SetActive(false);
            currentPanelState = UIPanelState.Simulation;
        }
        else
        {
            SwitchPanel(UIPanelState.Simulation);
        }

        GameManager.Instance.OnSavingsDecisionFinished();
    }

    public void ShowSavingsPanel()
    {
        if (IsPopupActive && activePopup == eventPopup)
            _suspendedContext = SuspendedContext.EventPopup;
        else if (IsPopupActive && activePopup == null)
            _suspendedContext = SuspendedContext.ChoicePopup;
        else
            _suspendedContext = SuspendedContext.None;

        SwitchPanel(UIPanelState.Budget);
    }

    public void ClearReportPanel()
    {
        resultsText.text = "";
    }

    private void SetEndOfYearButtonPosition(float xPos)
    {
        RectTransform rect = endOfYearContinueButton.GetComponent<RectTransform>();
        Vector2 pos = rect.anchoredPosition;
        pos.x = xPos;
        rect.anchoredPosition = pos;
    }

    public void OnStartGameClicked()
    {
        SwitchPanel(UIPanelState.ProfileSelect);
    }

    // Per-profile save slots - no global Continue button. Clicking a profile (or Free
    // Mode) checks that slot: if it has a save, offer "Continue (Month N)" / "Start
    // over" before doing anything else; if empty, go straight to the normal fresh flow.
    // `profile` is ignored by SaveSystem when guided=false (Free Mode is one shared slot).
    private void HandleProfileOrFreeModeSelection(GameManager.ProfileType profile, bool guided, System.Action startFreshAction)
    {
        if (SaveSystem.SaveExists(profile, guided))
        {
            // Load once here, for both the "Month N" label and (if they pick Continue)
            // the actual resume - SaveSystem.LoadGame isn't called a second time.
            GameSaveData peek = SaveSystem.LoadGame(profile, guided);
            if (peek == null)
            {
                // SaveExists said yes but the file didn't read cleanly - don't strand
                // the player on a dead click.
                startFreshAction?.Invoke();
                return;
            }

            var choices = new List<EventData.ChoiceOption>
            {
                new EventData.ChoiceOption
                {
                    label = $"Continue (Month {peek.currentMonth})",
                    resultDescription = $"Loading your save from Month {peek.currentMonth}..."
                },
                new EventData.ChoiceOption
                {
                    label = "Start over (erases this save)",
                    resultDescription = "Save erased. Starting fresh..."
                }
            };

            ShowChoicePopup(
                "Welcome back",
                $"You have a save for this profile at Month {peek.currentMonth}. Continue where you left off, or start over?",
                "", "",
                choices,
                index =>
                {
                    if (index == 0)
                        GameManager.Instance.ContinueFromSave(peek);
                    else
                    {
                        SaveSystem.DeleteSave(profile, guided);
                        startFreshAction?.Invoke();
                    }
                }
            );
            return;
        }

        startFreshAction?.Invoke();
    }

    public void OnSelectInformalWorker()
    {
        if (GameManager.Instance == null) return;

        HandleProfileOrFreeModeSelection(GameManager.ProfileType.Informal, true, () =>
        {
            GameManager.Instance.ApplyProfile(GameManager.ProfileType.Informal);

            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnProfileSelected(GameManager.ProfileType.Informal);
            else
                ShowSetupPanelAtReview();
        });
    }

    public void OnSelectFormalWorker()
    {
        if (GameManager.Instance == null) return;

        HandleProfileOrFreeModeSelection(GameManager.ProfileType.Formal, true, () =>
        {
            GameManager.Instance.ApplyProfile(GameManager.ProfileType.Formal);

            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnProfileSelected(GameManager.ProfileType.Formal);
            else
                ShowSetupPanelAtReview();
        });
    }

    public void OnSelectFarmer()
    {
        if (GameManager.Instance == null) return;

        HandleProfileOrFreeModeSelection(GameManager.ProfileType.Farmer, true, () =>
        {
            GameManager.Instance.ApplyProfile(GameManager.ProfileType.Farmer);

            if (TutorialManager.Instance != null)
                TutorialManager.Instance.OnProfileSelected(GameManager.ProfileType.Farmer);
            else
                ShowSetupPanelAtReview();
        });
    }

    public void OnFreeModeClicked()
    {
        if (GameManager.Instance == null) return;

        HandleProfileOrFreeModeSelection(GameManager.ProfileType.Informal, false, () =>
        {
            TutorialManager.Instance?.OnFreeModeSelected();
            GameManager.Instance.ClearProfile();
            SwitchPanel(UIPanelState.Setup);

            TutorialManager.Instance?.OnFreeSetupOpened();
        });
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void ShowYearlyReview(string mentorReflection, System.Action onContinue)
    {
        if (IsPopupActive && activePopup != null)
            CloseActivePopup();

        HideAllPanels();
        currentPanelState = UIPanelState.EndOfYear;
        if (endOfYearScreen != null) endOfYearScreen.SetActive(true);
        if (topHUD != null) topHUD.SetActive(false);

        var gm = GameManager.Instance;

        float net =
            gm.YearIncome - gm.YearExpenses - gm.YearPremiums
            - gm.YearEventLosses + gm.YearPayouts;

        yearPartOneText =
            "<b>Year 1 Complete</b>\n\n" +
            $"Income: ${gm.YearIncome:F0}\n" +
            $"Expenses: ${gm.YearExpenses:F0}\n" +
            $"Insurance Premiums: ${gm.YearPremiums:F0}\n" +
            $"Insurance Payouts: ${gm.YearPayouts:F0}\n" +
            $"Event Losses: ${gm.YearEventLosses:F0}\n\n" +
            $"Net Result: ${net:F0}\n" +
            $"Current Balance: ${gm.financeManager.CashOnHand:F0}\n\n" +
            $"<i>{mentorReflection}</i>";

        yearPartTwoText = BuildScoreSummary() + BuildInsuranceSummary(gm);

        yearEndPage = 0;
        resultsText.text = yearPartOneText;
        resultsText.gameObject.SetActive(true);
        SetEndOfYearButtonPosition(CONTINUE_X);

        if (endOfYearContinueButton != null)
            endOfYearContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Key Takeaways";

        if (restartButton != null)
            restartButton.gameObject.SetActive(false);

        yearEndGraph?.HideGraph();
        StartCoroutine(RenderGraphNextFrame());

        _yearlyReviewOnContinue = onContinue;
    }

    private System.Action _yearlyReviewOnContinue;

    private string BuildScoreSummary()
    {
        var p = PlayerDataManager.Instance;
        if (p == null) return "";

        var gm = GameManager.Instance;

        float momentum = p.FinancialMomentum;
        float morale = p.CompositeMorale;
        float score = p.FinalScore + (gm != null ? gm.GetGoalScoreBonus() : 0f);

        string grade = GetGrade(score);
        string overallLabel = GetScoreLabel(score);
        string momentumWord = GetAxisWord(momentum);
        string moraleWord = GetAxisWord(morale);

        string text = "<b>Your Year</b>\n";
        text += $"Grade: <b>{grade}</b>\n\n";
        text += $"Financial Discipline:  {momentum:+0;-0}  ({momentumWord})\n";
        text += $"Family & Community:  {morale:+0;-0}  ({moraleWord})\n\n";

        string goalLine = BuildGoalSummaryLine(gm);
        if (!string.IsNullOrEmpty(goalLine))
            text += $"{goalLine}\n\n";

        text += $"<i>{overallLabel}</i>\n\n";
        return text;
    }

    private string BuildGoalSummaryLine(GameManager gm)
    {
        if (gm == null) return "";
        float target = GoalDefs.GetActiveGoalTarget();
        if (target <= 0f) return "";

        string title = GoalDefs.GetActiveGoalTitle();

        if (gm.GoalBuilt)
            return $"<b>Goal:</b> You built {title} in Month {gm.GoalBuiltMonth}. It earned for you every month after.";

        if (gm.HasGoalBeenReached)
            return $"<b>Goal:</b> The {title} money sat safe, but it was never built.";

        return $"<b>Goal:</b> The {title} fund ended at {GameUtils.FormatMoney(gm.financeManager.generalSavingsBalance)} of {GameUtils.FormatMoney(target)}. The dream waits.";
    }

    private string GetGrade(float score)
    {
        if (score >= 25f) return "A";
        if (score >= 15f) return "B";
        if (score >= 5f) return "C";
        if (score >= -10f) return "D";
        return "F";
    }

    private string GetScoreLabel(float score)
    {
        if (score >= 25f) return "An excellent year, disciplined with money and good to the people around you.";
        if (score >= 15f) return "A strong, balanced year. You managed money well and kept your relationships steady.";
        if (score >= 5f) return "A steady year. Room to grow, but you held things together.";
        if (score >= -10f) return "A tough year. The choices were hard, and it showed.";
        return "A very hard year. Both your finances and your relationships took a heavy toll.";
    }

    private string GetAxisWord(float value)
    {
        if (value >= 20f) return "Strong";
        if (value >= 5f) return "Steady";
        if (value >= -5f) return "Mixed";
        if (value >= -20f) return "Strained";
        return "Struggling";
    }

    private string BuildInsuranceSummary(GameManager gm)
    {
        string text = "<b>Insurance</b>\n";
        if (gm.YearPremiums == 0f)
            text += $"You carried no insurance this year. ${gm.YearEventLosses:F0} in losses came out of pocket.\n\n";
        else
        {
            float net = gm.TotalInsurancePayoutAmount - gm.YearPremiums;
            if (gm.TotalInsurancePayoutAmount == 0f)
                text += $"You paid ${gm.YearPremiums:F0} in premiums with no claims. Protection you didn't need to use.\n\n";
            else if (net >= 0f)
                text += $"Insurance paid ${gm.TotalInsurancePayoutAmount:F0} against ${gm.YearPremiums:F0} in premiums, net benefit of ${net:F0}.\n\n";
            else
                text += $"Premiums: ${gm.YearPremiums:F0}. Payouts: ${gm.TotalInsurancePayoutAmount:F0}. Cover was there if something serious had hit.\n\n";
        }

        text += "<b>Year 2 Begins Now</b>\n";
        text += "Your cash balance carries over. Your decisions this year set the foundation. Now build on it.";
        return text;
    }

    // Wired to SimulationExitBtn - the in-game Exit button a player uses to leave mid-
    // session. Confirms first: exiting mid-month puts the player back at the top of that
    // month with whatever they did since undone (nothing autosaves mid-month, only at
    // StartNewMonth), and without a warning that reads as the game eating a turn. Only
    // proceeds to the save-preserving reset (ExitSimulationKeepingSave) if they confirm -
    // that's what lets the per-profile Continue prompt (HandleProfileOrFreeModeSelection)
    // find their save afterward. FullRestart itself is reserved for paths that should
    // genuinely start over (end-of-run Play Again, explicit Start Over).
    public void ReturnToMainMenu()
    {
        int leaveMonth = GameManager.Instance != null ? GameManager.Instance.currentMonth : 1;

        var choices = new List<EventData.ChoiceOption>
        {
            new EventData.ChoiceOption
            {
                label = "Leave",
                resultDescription = $"Leaving. Month {leaveMonth} will start over next time."
            },
            new EventData.ChoiceOption
            {
                label = "Keep playing",
                resultDescription = "Back to it."
            }
        };

        ShowChoicePopup(
            "Leave now?",
            $"You will start again from the beginning of Month {leaveMonth}. Anything you have done this month will not be saved.",
            "", "",
            choices,
            index =>
            {
                if (index == 0)
                    GameManager.Instance.ExitSimulationKeepingSave();
            }
        );
    }

    public void OnBackToMenu()
    {
        SwitchPanel(UIPanelState.MainMenu);
    }

    public void ShowSettings()
    {
        if (settingsPanel == null) return;

        if (IsPopupActive && activePopup != null)
            CloseActivePopup();

        _stateBeforeSettings = currentPanelState;
        HideAllPanels();
        settingsPanel.SetActive(true);  
    }

    public void OnAdjustBudgetFromReportClicked()
    {
        GameManager.Instance.OpenExpenseAdjustmentFromReport();
    }

    public void HideSettings()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(false);

        if (_stateBeforeSettings != UIPanelState.None)
        {
            var restore = _stateBeforeSettings;
            _stateBeforeSettings = UIPanelState.None;
            currentPanelState = UIPanelState.None;
            SwitchPanel(restore);
        }
    }
}