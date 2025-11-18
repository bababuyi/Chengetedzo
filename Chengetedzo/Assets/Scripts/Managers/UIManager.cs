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

    [Header("Other Screens")]
    public GameObject endOfYearScreen;
    public TextMeshProUGUI resultsText;

    [Header("Event Popup")]
    public GameObject eventPopup;
    public Image eventIcon;
    public TextMeshProUGUI eventTitleText;
    public TextMeshProUGUI eventDescriptionText;
    public Button continueButton;

    // Popup state property (correct, single version)
    public bool IsPopupActive { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (eventPopup != null)
            eventPopup.SetActive(false);
    }

    private void Start()
    {
        ShowBudgetPanel();
    }

    // ===== Basic UI Updates =====
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
    }

    public void ShowBudgetPanel()
    {
        HideAllPanels();
        budgetPanel.SetActive(true);
    }

    public void ShowInsurancePanel()
    {
        HideAllPanels();
        insurancePanel.SetActive(true);
    }

    public void ShowReportPanel(string reportText)
    {
        HideAllPanels();
        reportPanel.SetActive(true);
        resultsText.text = reportText;
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
    public void ShowEndOfYearSummary()
    {
        endOfYearScreen.SetActive(true);
    }

    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
