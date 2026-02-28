using System.Collections;
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

    [Header("Monthly Report")]
    public TextMeshProUGUI monthlyReportText;

    [Header("Event Popup")]
    public GameObject eventPopup;
    public Image eventIcon;
    public TextMeshProUGUI eventTitleText;
    public TextMeshProUGUI eventDescriptionText;
    public Button continueButton;

    [Header("Mentor Popup")]
    public GameObject mentorPopup;
    public TextMeshProUGUI mentorText;
    public Button mentorContinueButton;

    // Popup state property (correct, single version)
    private UIPanelState currentPanelState = UIPanelState.None;
    // ================================
    // POPUP CONTROLLER (SEALED)
    // ================================

    private GameObject activePopup;
    private Button activeContinueButton;
    private System.Action activeOnClose;

    public bool IsPopupActive { get; private set; }

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

        HideLoanTopButton();
        HideSavingsTopButton();
    }

    private void Start()
    {
        SwitchPanel(UIPanelState.None);
        ShowSetupPanel();
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
        moneyText.text = $"Money: ${amount:F2}";
    }

    // Hides all simulation and flow panels (does NOT hide setup, top HUD, or popups)
    private void HideAllPanelsInternal()
    {
        setupPanel.SetActive(false);
        budgetPanel.SetActive(false);
        forecastPanel.SetActive(false);
        insurancePanel.SetActive(false);
        loanPanel.SetActive(false);
        reportPanel.SetActive(false);
        endOfYearScreen.SetActive(false);
    }

    public void HideAllPanels()
    {
        if (setupPanel != null) setupPanel.SetActive(false);
        if (forecastPanel != null) forecastPanel.SetActive(false);
        if (insurancePanel != null) insurancePanel.SetActive(false);
        if (loanPanel != null) loanPanel.SetActive(false);
        if (reportPanel != null) reportPanel.SetActive(false);
        if (budgetPanel != null) budgetPanel.SetActive(false);
        if (endOfYearScreen != null) endOfYearScreen.SetActive(false);
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

        // Close popup safely if open
        if (IsPopupActive)
            CloseActivePopup();

        HideAllPanelsInternal();

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
    }

    public void ShowInsurancePanel()
    {
        SwitchPanel(UIPanelState.Insurance);
    }

    public void ShowReportPanel(string reportText)
    {
        if (IsPopupActive)
            CloseActivePopup();

        eventPopup?.SetActive(false);
        mentorPopup?.SetActive(false);
        IsPopupActive = false;

        SwitchPanel(UIPanelState.Report);
        monthlyReportText.text = reportText;

        Debug.Log("Entering Report Phase.");
        Debug.Log("IsPopupActive at report: " + IsPopupActive);
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

    // ===== End-of-Year Screen =====
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

        resultsText.text =
        "<b>Year Complete</b>\n\n" +
        $"Income: ${gm.YearIncome:F0}\n" +
        $"Expenses: ${gm.YearExpenses:F0}\n" +
        $"Insurance Premiums: ${gm.YearPremiums:F0}\n" +
        $"Insurance Payouts: ${gm.YearPayouts:F0}\n" +
        $"Event Losses: ${gm.YearEventLosses:F0}\n\n" +
        $"Net Result: ${net:F0}\n" +
        $"Final Cash: ${gm.financeManager.CashOnHand:F0}\n\n" +
        $"<i>{mentorReflection}</i>";
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void ShowMentorMessage(string message)
    {
        mentorText.text = message;

        ShowPopup(
            mentorPopup,
            mentorContinueButton,
            null
        );
    }

    private void CloseMentorPopup()
    {
        mentorPopup.SetActive(false);
        IsPopupActive = false;
    }

    public void ShowLoanPanel()
    {
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
        SwitchPanel(UIPanelState.None);

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
        SwitchPanel(UIPanelState.None);
        GameManager.Instance.OnSavingsDecisionFinished();
    }
    public void ShowSavingsPanel()
    {
        SwitchPanel(UIPanelState.Budget);

        var controller = budgetPanel.GetComponent<BudgetPanelController>();
        controller?.ConfigureForPhase(GameManager.GamePhase.Savings);
    }
}