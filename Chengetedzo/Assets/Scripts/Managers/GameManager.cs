using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Simulation Settings")]
    public int currentMonth = 1;
    public int totalMonths = 12;
    public float monthDuration = 5f; // seconds per "month"

    [Header("Manager References")]
    public FinanceManager financeManager;
    public SavingsManager savingsManager;
    public LoanManager loanManager;
    public InsuranceManager insuranceManager;
    public EventManager eventManager;
    public UIManager uiManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        Debug.Log("Game Ready — Awaiting Start of Simulation.");
        uiManager.ShowBudgetPanel(); // starts on the budget screen
        uiManager.UpdateMoneyText(financeManager.cashOnHand);
        uiManager.UpdateMonthText(currentMonth, totalMonths);
    }

    public void BeginSimulation()
    {
        Debug.Log("Starting Life-Cycle Simulation...");
        uiManager.HideAllPanels();
        StartCoroutine(RunLifeCycle());
    }

    private IEnumerator RunLifeCycle()
    {
        Debug.Log("Simulation Running...");

        while (currentMonth <= totalMonths)
        {
            uiManager.UpdateMonthText(currentMonth, totalMonths);
            uiManager.UpdateMoneyText(financeManager.cashOnHand);

            // Process monthly systems
            financeManager.ProcessMonthlyBudget();
            insuranceManager.ProcessPremiums();
            loanManager?.ProcessContribution();
            eventManager?.CheckForMonthlyEvent(currentMonth);
            insuranceManager.ProcessClaims();
            savingsManager?.AccrueInterest();
            loanManager?.UpdateLoans();

            // Show visual feedback
            string monthlyReport = financeManager.GetMonthlySummary(currentMonth);
            uiManager.ShowReportPanel($"<b>Month {currentMonth}</b>\n\n{monthlyReport}");

            // Wait before moving to next month
            yield return new WaitForSeconds(monthDuration);
            currentMonth++;
        }

        uiManager.ShowEndOfYearSummary();
        Debug.Log("Simulation Ended - Year Complete");
    }
}
