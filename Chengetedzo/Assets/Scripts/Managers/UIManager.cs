using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public bool IsPopupActive { get; private set; } = false;

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
        ShowSetupPanel();
    }

    public void ShowSetupPanel()
    {
        // Setup only
        setupPanel.SetActive(true);

        budgetPanel.SetActive(false);
        forecastPanel.SetActive(false);
        endOfYearScreen.SetActive(false);

        HideAllPanels(); // hides sim panels
        topHUD.SetActive(false);
    }

    public void UpdateMonthText(int currentMonth, int totalMonths)
    {
        string monthName = System.Globalization.CultureInfo
            .CurrentCulture
            .DateTimeFormat
            .GetMonthName(currentMonth);

        monthText.text = $"{monthName} ({currentMonth}/{totalMonths})";
    }

    public void UpdateMoneyText(float amount)
    {
        moneyText.text = $"Money: ${amount:F2}";
    }

    // Hides all simulation and flow panels (does NOT hide setup, top HUD, or popups)
    public void HideAllPanels()
    {
        budgetPanel.SetActive(false);
        loanPanel.SetActive(false);
        insurancePanel.SetActive(false);
        reportPanel.SetActive(false);
        forecastPanel.SetActive(false);
    }

    public void ShowBudgetPanel()
    {
        setupPanel.SetActive(false);
        forecastPanel.SetActive(false);

        HideAllPanels();
        budgetPanel.SetActive(true);
        topHUD.SetActive(true);

        var controller = budgetPanel.GetComponent<BudgetPanelController>();
        controller?.LoadDefaultsFromSetup();
    }

    public void ShowForecastPanel()
    {
        HideAllPanels();
        budgetPanel.SetActive(false);
        setupPanel.SetActive(false);

        forecastPanel.SetActive(true);
        topHUD.SetActive(true);
    }

    public void ShowInsurancePanel()
    {
        HideAllPanels();

        GameManager.Instance.SetPhase(GameManager.GamePhase.Insurance);

        insurancePanel.SetActive(true);

        GameManager.Instance.insuranceManager.RefreshEligibility();
        insurancePanel.GetComponent<InsurancePanel>()?.RefreshUI();
    }

    public void ShowReportPanel(string reportText)
    {
        HideAllPanels();
        reportPanel.SetActive(true);

        if (monthlyReportText != null)
            monthlyReportText.text = reportText;
    }

    // ===== Event Popup =====
    public void ShowEventPopup(string title, string description, Sprite icon = null)
    {
        if (IsPopupActive) return;

        eventPopup.SetActive(true);
        eventTitleText.text = title;
        eventDescriptionText.text = description;

        eventIcon.sprite = icon;
        eventIcon.enabled = icon != null;
       
        // NOTE: Uses Time.timeScale = 0; GameManager relies on WaitForSecondsRealtime
        Time.timeScale = 0f;
        IsPopupActive = true;

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(CloseEventPopup);
    }

    private void CloseEventPopup()
    {
        eventPopup.SetActive(false);
        IsPopupActive = false;

        Time.timeScale = 1f;

        GameManager.Instance.OnEventPopupClosed();
    }

    // ===== End-of-Year Screen =====
    public void ShowEndOfYearSummary(string mentorReflection)
    {
        endOfYearScreen.SetActive(true);
        resultsText.text =
            "<b>Year Complete</b>\n\n" +
            $"<i>{mentorReflection}</i>";
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void ShowMentorMessage(string message)
    {
        if (IsPopupActive) return;

        mentorPopup.SetActive(true);
        mentorText.text = message;

        Time.timeScale = 0f;
        IsPopupActive = true;

        mentorContinueButton.onClick.RemoveAllListeners();
        mentorContinueButton.onClick.AddListener(CloseMentorPopup);
    }

    private void CloseMentorPopup()
    {
        mentorPopup.SetActive(false);
        IsPopupActive = false;
        Time.timeScale = 1f;
    }

    public void ShowLoanPanel()
    {
        HideAllPanels();
        loanPanel.SetActive(true);
        topHUD.SetActive(true);

        GameManager.Instance.BeginLoanDecision();

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
        UIManager.Instance.ShowLoanPanel();
    }

    public void CloseLoanPanel()
    {
        HideAllPanels();
        topHUD.SetActive(true);

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
        HideAllPanels();
        budgetPanel.SetActive(true);
        topHUD.SetActive(true);

        GameManager.Instance.PauseSimulation();

        budgetPanel
            .GetComponent<BudgetPanelController>()
            ?.OpenSavingsSection(); // optional but ideal
    }

    public void CloseSavingsPanel()
    {
        HideAllPanels();
        topHUD.SetActive(true);

        GameManager.Instance.ResumeSimulation();
    }
}