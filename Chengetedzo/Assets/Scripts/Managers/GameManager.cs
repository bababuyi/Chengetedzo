using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Simulation Settings")]
    public int currentMonth = 1;
    public int totalMonths = 12;
    public float monthDuration = 5f; // seconds per "month" (real-time)

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
        uiManager.ShowBudgetPanel();
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
            // --- UI TOP BAR ---
            uiManager.UpdateMonthText(currentMonth, totalMonths);
            uiManager.UpdateMoneyText(financeManager.cashOnHand);

            // --- PROCESS MONTH ---
            financeManager.ProcessMonthlyBudget();
            insuranceManager.ProcessPremiums();
            loanManager?.ProcessContribution();
            eventManager?.CheckForMonthlyEvent(currentMonth);
            insuranceManager.ProcessClaims();
            savingsManager?.AccrueInterest();
            loanManager?.UpdateLoans();

            // --- MONTHLY REPORT ---
            string monthlyReport = financeManager.GetMonthlySummary(currentMonth);
            uiManager.ShowReportPanel($"<b>Month {currentMonth}</b>\n\n{monthlyReport}");

            // — Wait for popup to close BEFORE continuing
            yield return new WaitUntil(() => !UIManager.Instance.IsPopupActive);

            // — Wait for real time (not affected by timeScale)
            yield return new WaitForSecondsRealtime(monthDuration);

            currentMonth++;
        }

        // --- END OF YEAR ---
        uiManager.ShowEndOfYearSummary();
        Debug.Log("Simulation Ended - Year Complete");
    }

    public void OnEventPopupClosed()
    {
        Debug.Log("[GameManager] Event popup closed — resuming simulation.");
        // You may add extra logic later if needed.
    }
}
