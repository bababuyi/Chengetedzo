using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameManager;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Top Info")]
    public TextMeshProUGUI monthText;
    public TextMeshProUGUI moneyText;

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
    public GameObject topHUD; // month + money bar

    [Header("Other Screens")]
    public GameObject endOfYearScreen;
    public TextMeshProUGUI resultsText;

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
    public Image eventIcon;
    public TextMeshProUGUI eventTitleText;
    public TextMeshProUGUI eventDescriptionText;
    public Button continueButton;

    [Header("Choice Popup")]
    public TextMeshProUGUI choiceSenderNameText;
    public TextMeshProUGUI choiceSenderRelationText;

    public GameObject choiceResultBubble;
    public TextMeshProUGUI choiceResultText;
    public Button choiceContinueButton;
    public GameObject choiceButtonsContainer;

    public GameObject choiceEventPopup;
    public TextMeshProUGUI choiceTitleText;
    public TextMeshProUGUI choiceDescriptionText;
    public GameObject choiceButtonPrefab;
    public Transform choiceButtonsParent;

    public bool IsEventPopupShowing()
    {
        return eventPopup != null && eventPopup.activeSelf;
    }

    public bool IsChoicePopupShowing()
    {
        return choiceEventPopup != null && choiceEventPopup.activeSelf;
    }

    private System.Action<int> _onChoicePicked;

    [Header("Mentor Popup")]
    public GameObject mentorPopup;
    public TextMeshProUGUI mentorText;
    public Button mentorContinueButton;
    public UnityEngine.UI.Image mentorBackground;

    [Header("Main Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject profileSelectPanel;
    public UnityEngine.UI.Button freeModeButton;

    // Popup state property (correct, single version)
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

        popupObject.SetActive(true);

        continueBtn.onClick.RemoveAllListeners();
        continueBtn.onClick.AddListener(CloseActivePopup);
    }

    private void CloseActivePopup()
    {
        if (!IsPopupActive)
            return;

        activePopup.SetActive(false);

        var callback = activeOnClose;

        activePopup = null;
        activeContinueButton = null;
        activeOnClose = null;
        IsPopupActive = false;

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

        eventPopup?.SetActive(false);
        mentorPopup?.SetActive(false);
        choiceEventPopup?.SetActive(false);

        HideLoanTopButton();
        HideSavingsTopButton();
    }

    private void Start()
    {
        SwitchPanel(UIPanelState.MainMenu);
    }

    public void ShowSetupPanel()
    {
        SwitchPanel(UIPanelState.Setup);
        setupPanel.GetComponent<SetupPanelController>()?.OnPanelOpened();
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
        moneyText.text = $"${amount:F0}";
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
        // FUTURE BARAKA. BE VERY CAREFUL WITH EVENT AND MENTOR POPUPS. YOU WILL REGRET TOUCHING ANYTHING. DOUBLE CHECK STATES IF YOU DO
        //-----------------------------------//
        if (IsPopupActive && activePopup != null && activePopup != eventPopup)
            CloseActivePopup();

        HideAllPanels();

        currentPanelState = newState;

        switch (newState)
        {
            case UIPanelState.Setup:
                setupPanel.SetActive(true);
                topHUD.SetActive(false);
                break;

            case UIPanelState.Budget:
                budgetPanel.SetActive(true);
                topHUD.SetActive(true);
                break;

            case UIPanelState.Forecast:
                forecastPanel.SetActive(true);
                topHUD.SetActive(true);
                break;

            case UIPanelState.Insurance:
                insurancePanel.SetActive(true);
                topHUD.SetActive(true);
                break;

            case UIPanelState.Loan:
                loanPanel.SetActive(true);
                topHUD.SetActive(true);
                break;

            case UIPanelState.Report:
                reportPanel.SetActive(true);
                topHUD.SetActive(true);
                break;

            case UIPanelState.EndOfYear:
                endOfYearScreen.SetActive(true);
                topHUD.SetActive(false);
                break;

            case UIPanelState.Simulation:
                topHUD.SetActive(true);
                break;
            case UIPanelState.MainMenu:
                mainMenuPanel.SetActive(true);
                topHUD.SetActive(false);
                break;

            case UIPanelState.ProfileSelect:
                profileSelectPanel.SetActive(true);
                topHUD.SetActive(false);
                
                if (freeModeButton != null)
                    freeModeButton.interactable = TutorialManager.HasAttemptedGuided;
                break;
        }
    }

    public void ShowBudgetPanel()
    {
        SwitchPanel(UIPanelState.Budget);
        budgetPanel.GetComponent<BudgetPanelController>()?.LoadDefaultsFromSetup();
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

    public void ShowEventPopup(string title, string description, Sprite icon = null)
    {
        eventTitleText.text = title;
        eventDescriptionText.text = description;

        eventIcon.sprite = icon;
        eventIcon.enabled = icon != null;

        ShowPopup(
            eventPopup,
            continueButton,
            () => GameManager.Instance.OnEventPopupClosed()
        );
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

        choiceEventPopup.SetActive(true);
        IsPopupActive = true;
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

                if (choice.momentumChange != 0f)
                {
                    string msign = choice.momentumChange > 0f ? "+" : "";
                    resultMsg += $"  ·  Morale: {msign}{choice.momentumChange:F0}";
                }
            }

            choiceResultText.text = resultMsg;
            choiceResultBubble.SetActive(true);
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

        var callback = _onChoicePicked;
        _onChoicePicked = null;
        callback?.Invoke(index);
    }

    // End-of-Year Screen
    public void ShowEndOfYearSummary(string mentorReflection)
    {
        SwitchPanel(UIPanelState.EndOfYear);

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
            $"Income: ${gm.YearIncome:F0}\n" +
            $"Expenses: ${gm.YearExpenses:F0}\n" +
            $"Insurance Premiums: ${gm.YearPremiums:F0}\n" +
            $"Insurance Payouts: ${gm.YearPayouts:F0}\n" +
            $"Event Losses: ${gm.YearEventLosses:F0}\n\n" +
            $"Net Result: ${net:F0}\n" +
            $"Final Cash: ${gm.financeManager.CashOnHand:F0}\n\n" +
            $"<i>{mentorReflection}</i>";

        // PART 2
        // SECTION 1 — Insurance
        yearPartTwoText = "<b>Insurance</b>\n";

        if (gm.YearPremiums == 0f)
        {
            yearPartTwoText += $"You carried no insurance this year. Your ${gm.YearEventLosses:F0} in event losses came entirely out of pocket.\n\n";
        }
        else
        {
            float netInsuranceBenefit = gm.TotalInsurancePayoutAmount - gm.YearPremiums;

            if (gm.TotalInsurancePayoutAmount == 0f)
            {
                yearPartTwoText += $"You paid ${gm.YearPremiums:F0} in premiums and made no claims. That's not money wasted — that's the cost of protection you fortunately didn't need.\n\n";
            }
            else if (netInsuranceBenefit >= 0f)
            {
                yearPartTwoText += $"Your insurance paid out ${gm.TotalInsurancePayoutAmount:F0} against ${gm.YearPremiums:F0} in premiums — a net benefit of ${netInsuranceBenefit:F0}.\n\n";
            }
            else
            {
                yearPartTwoText += $"You paid ${gm.YearPremiums:F0} in premiums and received ${gm.TotalInsurancePayoutAmount:F0} back. You came out ${Mathf.Abs(netInsuranceBenefit):F0} behind — but that coverage was there if something serious had hit.\n\n";
            }
        }

        // SECTION 2 — Resilience
        yearPartTwoText += "<b>Resilience</b>\n";
        yearPartTwoText += $"You faced {gm.TotalUnexpectedEvents} unexpected events. {gm.InsuredEventsCount} were covered by insurance.\n";

        if (gm.ForcedLoanCount > 0)
            yearPartTwoText += $"You needed emergency loans in {gm.ForcedLoanCount} months — a signal that the gap between income and expenses was too thin.\n";

        if (gm.MonthsUnderFinancialPressure > 0)
            yearPartTwoText += $"Your cash went negative in {gm.MonthsUnderFinancialPressure} months.\n";

        if (gm.ForcedLoanCount == 0 && gm.MonthsUnderFinancialPressure == 0)
            yearPartTwoText += "You never went negative and never needed an emergency loan. That's genuine financial resilience.\n";

        yearPartTwoText += "\n";

        // SECTION 3 — One takeaway
        yearPartTwoText += "<b>Key Takeaway</b>\n";

        float netBenefit = gm.TotalInsurancePayoutAmount - gm.YearPremiums;

        if (netBenefit > 0f)
            yearPartTwoText += "Insurance paid for itself this year. The lesson: start early, stay consistent.";
        else if (gm.ForcedLoanCount >= 3)
            yearPartTwoText += "Repeated forced loans point to one gap — an emergency fund of even one month's expenses would have broken the cycle.";
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
        endOfYearContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Key Takeaways →";
        if (yearEndGraph != null) yearEndGraph.gameObject.SetActive(false);
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
        yearEndPage++;

        switch (yearEndPage)
        {
            case 1: // Key Takeaways
                resultsText.text = yearPartTwoText;
                resultsText.gameObject.SetActive(true);
                if (yearEndGraph != null) yearEndGraph.gameObject.SetActive(false);
                endOfYearContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "View Graph →";
                restartButton.interactable = true;
                SetEndOfYearButtonPosition(CONTINUE_X);
                break;

            case 2: // Graph
                resultsText.gameObject.SetActive(false);
                if (yearEndGraph != null) yearEndGraph.gameObject.SetActive(true);
                endOfYearContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "← Back";
                SetEndOfYearButtonPosition(BACK_X);
                break;

            default: // Back to page 1
                yearEndPage = 0;
                resultsText.text = yearPartOneText;
                resultsText.gameObject.SetActive(true);
                if (yearEndGraph != null) yearEndGraph.gameObject.SetActive(false);
                endOfYearContinueButton.GetComponentInChildren<TextMeshProUGUI>().text = "Key Takeaways →";
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
        mentorText.text = message;
        ShowPopup(mentorPopup, mentorContinueButton, onClose);
        var rect = mentorPopup.GetComponent<RectTransform>();
        UIAnimator.Instance?.SlideUpChat(rect);
        AudioManager.Instance?.OnMentorMessage();
    }

    public void ShowMentorMessageTransparent(string message, System.Action onClose = null)
    {
        if (IsPopupActive)
        {
            StartCoroutine(WaitThenShowTransparent(message, onClose));
            return;
        }

        if (mentorBackground != null)
            mentorBackground.enabled = false;

        mentorText.text = message;
        ShowPopup(mentorPopup, mentorContinueButton, () =>
        {
            if (mentorBackground != null)
                mentorBackground.enabled = true;
            onClose?.Invoke();
        });
        UIAnimator.Instance?.FadeIn(mentorPopup);
    }

    private System.Collections.IEnumerator WaitThenShowTransparent(
        string message, System.Action onClose)
    {
        yield return new UnityEngine.WaitUntil(() => !IsPopupActive);
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
        GameManager.Instance.BeginLoanDecision();
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
        GameManager.Instance.BeginSavingsDecision();
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
        var controller = budgetPanel.GetComponent<BudgetPanelController>();
        controller?.ConfigureForPhase(GameManager.GamePhase.Savings);
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

    public void OnSelectInformalWorker()
    {
        GameManager.Instance.ApplyProfile(ProfileType.Informal);
        ShowBudgetPanel();
        TutorialManager.Instance?.OnProfileSelected(ProfileType.Informal);
    }

    public void OnSelectFormalWorker()
    {
        GameManager.Instance.ApplyProfile(ProfileType.Formal);
        ShowBudgetPanel();
        TutorialManager.Instance?.OnProfileSelected(ProfileType.Formal);
    }

    public void OnSelectFarmer()
    {
        GameManager.Instance.ApplyProfile(ProfileType.Farmer);
        ShowBudgetPanel();
        TutorialManager.Instance?.OnProfileSelected(ProfileType.Farmer);
    }

    public void OnFreeModeClicked()
    {
        TutorialManager.Instance?.OnFreeModeSelected();
        GameManager.Instance.ClearProfile();
        SwitchPanel(UIPanelState.Setup);

        TutorialManager.Instance?.OnFreeSetupOpened();
    }

    public void OnBackToMenu()
    {
        SwitchPanel(UIPanelState.MainMenu);
    }
}