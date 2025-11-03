using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public GameObject eventPopup;
    public GameObject endOfYearScreen;
    public TextMeshProUGUI eventText;
    public TextMeshProUGUI resultsText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        ShowBudgetPanel(); // start on the budget screen
    }

    // ===== Basic Updates =====
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

    public void ShowSavingsPanel()
    {
        HideAllPanels();
        savingsPanel.SetActive(true);
    }

    public void ShowLoanPanel()
    {
        HideAllPanels();
        loanPanel.SetActive(true);
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

    // ===== Events & End-Year =====
    public void ShowEventPopup(string message)
    {
        eventPopup.SetActive(true);
        eventText.text = message;
        Invoke(nameof(HideEventPopup), 3f);
    }

    private void HideEventPopup()
    {
        eventPopup.SetActive(false);
    }

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
