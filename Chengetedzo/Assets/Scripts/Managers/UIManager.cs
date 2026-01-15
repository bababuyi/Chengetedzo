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

    [Header("Panels")]
    public GameObject budgetPanel;
    public GameObject savingsPanel;
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

        if (eventPopup != null)
            eventPopup.SetActive(false);

        if (mentorPopup != null)
            mentorPopup.SetActive(false);
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
        monthText.text = $"Month: {currentMonth}/{totalMonths}";
    }

    public void UpdateMoneyText(float amount)
    {
        moneyText.text = $"Money: ${amount:F2}";
    }

    // ===== Panel Switching =====
    public void HideAllPanels()
    {
        budgetPanel.SetActive(false);
        savingsPanel.SetActive(false);
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

        GameManager.Instance.SetPhase(GameManager.GamePhase.Forecast);
    }

    public void ShowInsurancePanel()
    {
        HideAllPanels();
        insurancePanel.SetActive(true);

        GameManager.Instance.SetPhase(GameManager.GamePhase.Insurance);
    }

    public void ShowReportPanel(string reportText)
    {
        HideAllPanels();
        reportPanel.SetActive(true);
        resultsText.text = reportText;

        GameManager.Instance.SetPhase(GameManager.GamePhase.Report);
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
}
