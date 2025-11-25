using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Simulation Settings")]
    public int currentMonth = 1;
    public int totalMonths = 12;
    public float monthDuration = 5f;

    [Header("Manager References")]
    public FinanceManager financeManager;
    public SavingsManager savingsManager;
    public LoanManager loanManager;
    public InsuranceManager insuranceManager;
    public EventManager eventManager;
    public UIManager uiManager;
    public VisualSimulationManager visualManager;   // <-- Added reference

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

        // Apply starting visuals
        visualManager?.UpdateVisuals();
    }

    public void BeginSimulation()
    {
        Debug.Log("Starting Life-Cycle Simulation...");
        uiManager.HideAllPanels();

        // Update visuals at the start of simulation
        visualManager?.UpdateVisuals();

        StartCoroutine(RunLifeCycle());
    }

    private IEnumerator RunLifeCycle()
    {
        Debug.Log("Simulation Running...");

        while (currentMonth <= totalMonths)
        {
            // UI
            uiManager.UpdateMonthText(currentMonth, totalMonths);
            uiManager.UpdateMoneyText(financeManager.cashOnHand);

            // Monthly logic
            financeManager.ProcessMonthlyBudget();
            insuranceManager.ProcessMonthlyPremiums();
            loanManager?.ProcessContribution();
            eventManager?.CheckForMonthlyEvent(currentMonth);
            insuranceManager.ProcessClaims();
            savingsManager?.AccrueInterest();
            loanManager?.UpdateLoans();
            financeManager.ProcessSchoolFees(currentMonth);

            // Visuals
            visualManager?.UpdateVisuals();

            // Report
            string monthlyReport = financeManager.GetMonthlySummary(currentMonth);
            uiManager.ShowReportPanel($"<b>Month {currentMonth}</b>\n\n{monthlyReport}");

            yield return new WaitUntil(() => !UIManager.Instance.IsPopupActive);
            yield return new WaitForSecondsRealtime(monthDuration);

            currentMonth++;
        }

        uiManager.ShowEndOfYearSummary();
        Debug.Log("Simulation Ended - Year Complete");
    }

    public enum Season
    {
        Summer,
        Winter
    }

    public Season GetSeasonForMonth(int month)
    {
        if (month == 11 || month == 12 || (month >= 1 && month <= 4))
            return Season.Summer;

        return Season.Winter;
    }

    public Season GetCurrentSeason()
    {
        return GetSeasonForMonth(currentMonth);
    }
    public void OnEventPopupClosed()
    {
        Debug.Log("[GameManager] Event popup closed — resuming simulation.");
    }

}
