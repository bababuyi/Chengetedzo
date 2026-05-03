using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameManager;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    public const string KEY_GUIDED_ATTEMPTED = "Tut_GuidedAttempted";
    private const string KEY_INFORMAL_SEEN = "Tut_InformalSeen";
    private const string KEY_FORMAL_SEEN = "Tut_FormalSeen";
    private const string KEY_FARMER_SEEN = "Tut_FarmerSeen";
    private const string KEY_FORECAST_SEEN = "Tut_ForecastSeen";
    private const string KEY_INSURANCE_SEEN = "Tut_InsuranceSeen";
    private const string KEY_SIM_START_SEEN = "Tut_SimStartSeen";
    private const string KEY_LOAN_SEEN = "Tut_LoanSeen";
    private const string KEY_EVENT_SEEN = "Tut_EventSeen";
    private const string KEY_REPORT_SEEN = "Tut_ReportSeen";
    private const string KEY_COMPLETE_SEEN = "Tut_CompleteSeen";
    private const string KEY_FREE_FORECAST = "Tut_FreeForeSeen";
    private const string KEY_FREE_INSURANCE = "Tut_FreeInsSeen";
    private const string KEY_FREE_SETUP = "Tut_FreeSetupSeen";
    private const string KEY_DEDUCTIBLE_SEEN = "Tut_DeductibleSeen";

    public static bool HasAttemptedGuided
    {
        get => PlayerPrefs.GetInt(KEY_GUIDED_ATTEMPTED, 0) == 1;
        set { PlayerPrefs.SetInt(KEY_GUIDED_ATTEMPTED, value ? 1 : 0); PlayerPrefs.Save(); }
    }

    [Header("UI Elements to Pulse")]
    public RectTransform forecastListParent;
    public RectTransform insuranceToggleContainer;
    public RectTransform topHUDMoneyArea;
    public RectTransform loanTopButton;
    public RectTransform reportPanelRoot;
    public RectTransform savingsSlider;
    public RectTransform eventPopupRoot;
    public RectTransform budgetConfirmButton;

    [Header("Pulse Settings")]
    [Range(0.01f, 0.1f)]
    public float pulseAmount = 0.04f;
    [Range(1f, 6f)]
    public float pulseSpeed = 3f;

    private bool _isGuidedMode;
    private Coroutine _pulseCoroutine;
    private RectTransform _currentlyPulsing;


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy() => StopPulse();

    public void OnProfileSelected(ProfileType profile)
    {
        _isGuidedMode = true;
        HasAttemptedGuided = true;
        GameManager.Instance?.SetMentorSpokeThisMonth(true);

        string key = profile switch
        {
            ProfileType.Informal => KEY_INFORMAL_SEEN,
            ProfileType.Formal => KEY_FORMAL_SEEN,
            ProfileType.Farmer => KEY_FARMER_SEEN,
            _ => KEY_INFORMAL_SEEN
        };

        // Always hide profile select immediately
        UIManager.Instance.SwitchPanel(UIManager.UIPanelState.None);

        System.Action showSetup = () =>
        {
            UIManager.Instance.ForceCloseAllPopups();
            UIManager.Instance.ShowSetupPanel();
            var setup = UIManager.Instance.setupPanel
                .GetComponent<SetupPanelController>();
            setup?.EnterReviewMode();
            UIManager.Instance.ShowSetupPanelAtReview();
        };

        if (Seen(key))
        {
            showSetup();
            return;
        }

        ShowProfileIntroSequence(profile, () =>
        {
            Mark(key);
            showSetup();
        });
    }

    public void OnFreeModeSelected()
    {
        _isGuidedMode = false;
    }

    public void OnFreeSetupOpened()
    {
        if (Seen(KEY_FREE_SETUP)) return;

        ShowSequence(new[]
        {
            "Welcome to free mode. Here you define your own financial situation from scratch.",
            "Start with your income range. If your earnings vary month to month, enter a minimum and maximum. If they're stable, toggle that on and enter a single figure.",
            "Then set your living expenses — rent or house value, food, transport, and utilities. Try to be realistic. The game will use these numbers to simulate your actual monthly position.",
            "Finally, tell us about your household — how many adults and children depend on this income — and which assets you own. Assets determine which insurance products you can access.",
            "When you're ready, move through the steps and confirm. The year begins after that.",
        }, () => Mark(KEY_FREE_SETUP),
        new[] { null, null, null, null, budgetConfirmButton });
    }

    /// <summary>Call when the forecast panel opens for the first time.</summary>
    public void OnForecastOpened()
    {
        string key = _isGuidedMode ? KEY_FORECAST_SEEN : KEY_FREE_FORECAST;
        if (Seen(key)) return;
        ShowForecastIntroSequence(() => Mark(key));
    }

    /// <summary>Call when the insurance panel opens for the first time.</summary>
    public void OnInsuranceOpened()
    {
        string key = _isGuidedMode ? KEY_INSURANCE_SEEN : KEY_FREE_INSURANCE;
        if (Seen(key)) return;

        bool hasMotor = GameManager.Instance != null &&
                        GameManager.Instance.financeManager.assets.hasMotor;
        ShowInsuranceIntroSequence(hasMotor, () => Mark(key));
    }

    /// <summary>
    /// Call from OnInsuranceConfirmed, before ConfirmMonthAndResolve.
    /// Pass the actual resolve action as the callback so it runs after the popup closes.
    /// Only fires on the very first simulation start.
    /// </summary>
    public void OnSimulationFirstStart(System.Action onComplete)
    {
        if (Seen(KEY_SIM_START_SEEN)) { onComplete?.Invoke(); return; }
        ShowSimulationStartSequence(() => { Mark(KEY_SIM_START_SEEN); onComplete?.Invoke(); });
    }

    /// <summary>
    /// Call from LoanManager when loans first unlock.
    /// Explains the loan system and its consequences.
    /// </summary>
    public void OnLoanUnlocked()
    {
        if (Seen(KEY_LOAN_SEEN)) return;
        ShowLoanIntroSequence(() =>
        {
            Mark(KEY_LOAN_SEEN);
            CheckTutorialComplete();
        });
    }

    /// <summary>
    /// Call from GameManager.ProcessNextEvent when the very first event is about to show.
    /// Pass the actual ShowEvent call as the callback so it fires after the tutorial closes.
    /// </summary>
    public void OnFirstEvent(bool hadInsurance, float insurancePayout, System.Action showEventCallback)
    {
        if (Seen(KEY_EVENT_SEEN)) { showEventCallback?.Invoke(); return; }
        ShowEventIntroSequence(hadInsurance, insurancePayout, () =>
        {
            Mark(KEY_EVENT_SEEN);
            showEventCallback?.Invoke();
        });
    }

    public void OnReportOpened()
    {
        if (Seen(KEY_REPORT_SEEN)) return;
        ShowReportIntroSequence(() =>
        {
            Mark(KEY_REPORT_SEEN);
            CheckTutorialComplete();
        });
    }



    //For testing
    public void ResetAll()
    {
        string[] keys =
{
            KEY_GUIDED_ATTEMPTED, KEY_INFORMAL_SEEN, KEY_FORMAL_SEEN, KEY_FARMER_SEEN,
            KEY_FORECAST_SEEN, KEY_INSURANCE_SEEN, KEY_SIM_START_SEEN, KEY_LOAN_SEEN,
            KEY_EVENT_SEEN, KEY_REPORT_SEEN, KEY_COMPLETE_SEEN,
            KEY_FREE_FORECAST, KEY_FREE_INSURANCE, KEY_FREE_SETUP, KEY_DEDUCTIBLE_SEEN
        };
        foreach (var k in keys) PlayerPrefs.DeleteKey(k);
        PlayerPrefs.Save();
        Debug.Log("[Tutorial] All flags reset.");
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  SEQUENCE BUILDERS  (private content layer)
    // ═════════════════════════════════════════════════════════════════════════

    private void ShowProfileIntroSequence(ProfileType profile, System.Action onComplete)
    {
        float startCash = GameManager.Instance != null
            ? GameManager.Instance.financeManager.CashOnHand
            : 0f;

        var fm = GameManager.Instance?.financeManager;
        var sd = GameManager.Instance?.setupData;

        float rent = fm?.rentCost ?? 0f;
        float groceries = fm?.groceries ?? 0f;
        float transport = fm?.transport ?? 0f;
        float utilities = fm?.utilities ?? 0f;
        float schoolFees = sd?.schoolFeesAmount ?? 0f;
        float minIncome = sd?.minIncome ?? 0f;
        float maxIncome = sd?.maxIncome ?? 0f;
        int adults = sd?.adults ?? 1;
        int children = sd?.children ?? 0;

        string[] msgs;
        RectTransform[] pulses;

        switch (profile)
        {
            case ProfileType.Informal:
                msgs = new[]
                {
                    "You are Tendai — an informal trader working the markets in Mbare, Harare. Your income varies from week to week. Some months the stall does well. Others, the margins barely cover what you owe.",
                    $"You have {children} children and rent a room in a shared house. Your income ranges from ${minIncome:F0} to ${maxIncome:F0} a month. Rent is ${rent:F0}, groceries ${groceries:F0}, transport ${transport:F0}. You are starting with ${startCash:F0}.",
                    "You have no vehicle and no property. What you do have is resourcefulness. The next 24 months will test how well you can protect the small margin between you and an empty pocket.",
                    "Each month you will see your income arrive, your expenses leave, and sometimes an unexpected event will take something you weren't prepared to lose. Insurance, savings, and loans are your tools. Learn when to use them.",
                };
                pulses = new[] { null, topHUDMoneyArea, null, null };
                break;

            case ProfileType.Formal:
                msgs = new[]
                {
                    "You are Chido — an accounts clerk at a logistics company in Harare. You earn a fixed monthly salary, which puts you ahead of many. But steady income also means steady obligations.",
                    $"You have {children} {(children == 1 ? "child" : "children")} in school, and rent a house. Your salary runs ${minIncome:F0}–${maxIncome:F0} a month. Rent is ${rent:F0}, school fees ${schoolFees:F0}, groceries ${groceries:F0}. You are starting with ${startCash:F0}.",
                    "As a vehicle owner, third-party motor insurance is required by law in Zimbabwe. It is not optional — you will see it listed on the insurance screen. Not carrying it is not a choice you have.",
                    "Your income stability is your greatest advantage. The risk is complacency. Formal workers often underinsure because things feel manageable — until they aren't.",
                };
                pulses = new[] { null, topHUDMoneyArea, insuranceToggleContainer, null };
                break;

            case ProfileType.Farmer:
            default:
                msgs = new[]
                {
                    "You are Sekuru Moyo — a smallholder farmer in Mashonaland. You grow maize and keep cattle. You own your land and your home, which many do not. That security has a cost: when the land suffers, you suffer with it.",
                    $"Your income swings with the seasons — anywhere from ${minIncome:F0} to ${maxIncome:F0}. Monthly costs include groceries (${groceries:F0}), transport (${transport:F0}), and school fees (${schoolFees:F0}). You are starting with ${startCash:F0}.",
                    "Agriculture carries risks that most insurance products only partially cover. Pay close attention to the Monthly News forecast each month. A drought warning or disease alert is not just a headline — it is a signal.",
                    "Your home is an asset, your livestock is an asset, your crops are an asset. Each one is exposed to a different kind of risk. You will not be able to insure everything. Choose carefully.",
                };
                pulses = new[] { null, topHUDMoneyArea, forecastListParent, insuranceToggleContainer };
                break;
        }

        ShowSequence(msgs, onComplete, pulses);
    }

    private void ShowForecastIntroSequence(System.Action onComplete)
    {
        ShowSequence(new[]
        {
            "This is the Monthly News — your window into what the coming month may hold.",
            "These headlines are signals, not certainties. A disease warning doesn't mean your livestock will fall ill — but it means the risk is elevated. A drought warning doesn't guarantee crop failure — but it raises the probability.",
            "Read the headlines carefully. Then decide what protection you want to carry before the month begins. The cost of insurance is small. The cost of being uninsured when something happens is not.",
            "Once you continue past this screen, you will move to insurance selection. You cannot come back to the forecast after that.",
        }, onComplete,
        new[] { forecastListParent, forecastListParent, forecastListParent, null });
    }

    private void ShowInsuranceIntroSequence(bool hasMotor, System.Action onComplete)
    {
        var msgs = new List<string>
        {
            "Insurance is a monthly payment — called a premium — that you make in exchange for financial protection when something goes wrong.",
        };

        if (hasMotor)
        {
            msgs.Add("You own a vehicle. In Zimbabwe, third-party motor insurance is required by law. You will see it flagged on this screen. You must carry it.");
        }

        msgs.AddRange(new[]
        {
            "Most plans have a waiting period. This means you must pay the premium for a set number of months before you can make a claim. The earlier you start, the sooner that protection becomes active.",
            "Each plan shows its monthly cost, what it covers, and what its coverage limit is. Look at what risks you face based on the news you just read — and choose accordingly.",
            "You do not need every plan. But some cover is always better than none. A single serious event without insurance can cost more than months of premiums combined.",
        });

        var pulses = new RectTransform[msgs.Count];
        for (int i = 0; i < pulses.Length; i++)
            pulses[i] = insuranceToggleContainer;

        ShowSequence(msgs.ToArray(), onComplete, pulses);
    }

    private void ShowSimulationStartSequence(System.Action onComplete)
    {
        ShowSequence(new[]
        {
            "The month begins now. Your income will arrive, your expenses will leave, and any events that occur will be presented as popups.",
            "If something happens — a medical cost, a theft, storm damage — you will see a popup telling you what occurred and what it cost. If you have insurance that covers it, the payout will be shown there too.",
            "At the end of each month, you will see a financial report summarising everything that moved. Pay attention to it. That report is your feedback.",
            "Good luck.",
        }, onComplete,
        new[] { topHUDMoneyArea, eventPopupRoot, reportPanelRoot, null });
    }

    private void ShowLoanIntroSequence(System.Action onComplete)
    {
        ShowSequence(new[]
        {
            "You have contributed consistently to your savings pool. That consistency has unlocked access to borrowing.",
            "A loan gives you cash now when you need it. In return, a portion of your loan balance is repaid automatically each month until it is cleared.",
            "Miss a repayment and the repayment rate rises. Miss several in a row and the debt compounds faster than you can manage. Forced loans — taken automatically when your cash runs out — carry a momentum penalty.",
            "Loans are for genuine emergencies. They are not a substitute for savings or insurance. Use them carefully, and pay them back as quickly as you can.",
        }, onComplete,
        new[] { loanTopButton, loanTopButton, loanTopButton, loanTopButton });
    }

    private void ShowEventIntroSequence(bool hadInsurance, float payout, System.Action onComplete)
    {
        if (hadInsurance && payout > 0f)
        {
            ShowSequence(new[]
            {
                "Something happened this month. The popup below will show you the event and its full cost.",
                $"Your insurance covered ${payout:F0} of that loss. You can see the payout listed separately from the total damage. That is the difference between a difficult month and a potentially devastating one.",
                "This is why premiums matter. Every month you paid was building toward this moment.",
            }, onComplete,
            new[] { null, eventPopupRoot, null });
        }
        else
        {
            ShowSequence(new[]
            {
                "Something happened this month. The popup below will show you the event, what it cost, and where that money came from.",
                "This loss came directly from your available cash. There was no insurance in place to reduce it. That is what an uninsured event looks like in practice.",
                "After this month ends, consider whether any of the available insurance plans are worth carrying. A single event like this often costs more than several months of premiums.",
            }, onComplete,
            new[] { null, eventPopupRoot, insuranceToggleContainer });
        }
    }

    private void ShowReportIntroSequence(System.Action onComplete)
    {
        ShowSequence(new[]
        {
            "This is your monthly financial report. It shows everything that moved this month — income received, expenses paid, insurance premiums, any event losses, and what you saved.",
            "The closing balance is what you carry into next month. If it is lower than expected, look at where the gap appeared. If it is higher, consider whether more could have been put aside.",
            "This report is your feedback. The next forecast is your next opportunity to respond.",
        }, onComplete,
        new[] { reportPanelRoot, reportPanelRoot, null });
    }

    private void ShowTutorialCompleteSequence()
    {
        ShowSequence(new[]
        {
            "You have now seen the main mechanics. Forecasts, insurance, events, loans, reports — none of these should surprise you the same way again.",
            "From here, I will step back. But I will still check in when something worth noting happens — when patterns form, when you recover from a difficult stretch, or when a choice deserves reflection.",
            "The decisions are yours now. Make them count.",
        }, null, new RectTransform[] { null, null, null });
    }

    public void TriggerTutorial(string key)
    {
        Debug.Log($"[TUTORIAL-TRIGGER] TriggerTutorial called with key='{key}'");
        if (key == "insurance_deductible_explainer")
        {
            Debug.Log($"[TUTORIAL-TRIGGER] KEY_DEDUCTIBLE_SEEN already seen: {Seen(KEY_DEDUCTIBLE_SEEN)}");
            if (Seen(KEY_DEDUCTIBLE_SEEN)) return;
            UIManager.Instance.ShowMentorMessageTransparent(
                "A deductible is the amount you pay yourself before insurance covers the rest. " +
                "For example, if your deductible is $500 and your claim is $2,000, " +
                "you pay $500 and insurance covers $1,500.",
                () => Mark(KEY_DEDUCTIBLE_SEEN)
            );
        }
    }

    private bool _isSequenceRunning = false;

    private void ShowSequence(string[] messages, System.Action onComplete,
                              RectTransform[] pulseTargets = null)
    {
        if (messages == null || messages.Length == 0) { onComplete?.Invoke(); return; }
        if (_isSequenceRunning) return; // ← add this

        _isSequenceRunning = true;
        var msgList = new List<string>(messages);
        var pulseList = pulseTargets != null ? new List<RectTransform>(pulseTargets) : new List<RectTransform>();
        ShowSequenceStep(msgList, pulseList, () => { _isSequenceRunning = false; onComplete?.Invoke(); });
    }

    private void ShowSequenceStep(List<string> messages, List<RectTransform> pulses, System.Action onComplete)
    {
        Debug.Log($"[TUTORIAL] ShowSequenceStep — remaining={messages.Count}");

        if (GameManager.Instance != null)
            GameManager.Instance.SetMentorSpokeThisMonth(true);

        if (SettingsManager.Instance != null && !SettingsManager.Instance.MentorHints)
        {
            StopPulse();
            onComplete?.Invoke();
            return;
        }

        if (messages.Count == 0)
        {
            StopPulse();
            onComplete?.Invoke();
            return;
        }

        RectTransform pulse = pulses.Count > 0 ? pulses[0] : null;
        StartPulse(pulse);

        string message = messages[0];
        messages.RemoveAt(0);
        if (pulses.Count > 0) pulses.RemoveAt(0);

        UIManager.Instance.ShowMentorMessageTransparent(message, () =>
            ShowSequenceStep(messages, pulses, onComplete));
    }

    private void CheckTutorialComplete()
    {
        if (Seen(KEY_COMPLETE_SEEN)) return;

        bool coreComplete = Seen(KEY_EVENT_SEEN) &&
                            Seen(KEY_REPORT_SEEN) &&
                            Seen(KEY_LOAN_SEEN);

        if (!coreComplete) return;

        Mark(KEY_COMPLETE_SEEN);
        StartCoroutine(DelayThen(0.2f, ShowTutorialCompleteSequence));
    }

    private IEnumerator DelayThen(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        yield return new WaitUntil(() => !UIManager.Instance.IsPopupActive);
        action?.Invoke();
    }

    private void StartPulse(RectTransform target)
    {
        StopPulse();
        if (target == null) return;

        _currentlyPulsing = target;
        _pulseCoroutine = StartCoroutine(PulseLoop(target));
    }

    private void StopPulse()
    {
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }

        if (_currentlyPulsing != null)
        {
            _currentlyPulsing.localScale = Vector3.one;
            _currentlyPulsing = null;
        }
    }

    private IEnumerator PulseLoop(RectTransform target)
    {
        float t = 0f;

        while (true)
        {
            if (target == null) yield break;

            t += Time.deltaTime * pulseSpeed;
            float s = 1f + Mathf.Sin(t * Mathf.PI * 2f) * pulseAmount;
            target.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    private static bool Seen(string key) => PlayerPrefs.GetInt(key, 0) == 1;

    private static void Mark(string key)
    {
        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }
}