using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BudgetPanelController : MonoBehaviour
{
    [Header("Step Navigation")]
    public GameObject incomeSection;
    public GameObject expenseSection;
    public GameObject allocationSection;

    private int currentStep = 1;

    [Header("Income Input")]
    public TMP_InputField incomeInput;

    [Header("Expense Sliders")]
    public Slider rentSlider;
    public TMP_Text rentValueText;

    public Slider groceriesSlider;
    public TMP_Text groceriesValueText;

    public Slider transportSlider;
    public TMP_Text transportValueText;

    public Slider schoolFeesSlider;
    public TMP_Text schoolFeesValueText;

    [Header("Allocation Sliders")]
    public Slider savingsSlider;
    public TMP_Text savingsValueText;

    public Slider loanRepaymentSlider;
    public TMP_Text loanRepaymentValueText;

    [Header("Buttons & Text")]
    public Button confirmButton;
    public TMP_Text summaryText;
    public TMP_Text warningText;

    private float totalIncome;

    private void Start()
    {
        confirmButton.onClick.AddListener(OnConfirmAndStartSimulation);

        // Always begin at Step 1 (Income)
        ShowStep(1);

        // Add listeners
        incomeInput.onValueChanged.AddListener(_ => UpdateCalculations());
        rentSlider.onValueChanged.AddListener(_ => UpdateCalculations());
        groceriesSlider.onValueChanged.AddListener(_ => UpdateCalculations());
        transportSlider.onValueChanged.AddListener(_ => UpdateCalculations());
        schoolFeesSlider.onValueChanged.AddListener(_ => UpdateCalculations());
        savingsSlider.onValueChanged.AddListener(_ => UpdateCalculations());
        loanRepaymentSlider.onValueChanged.AddListener(_ => UpdateCalculations());

        warningText.gameObject.SetActive(false);
        summaryText.text = "";
    }

    public void ShowStep(int step)
    {
        incomeSection.SetActive(step == 1);
        expenseSection.SetActive(step == 2);
        allocationSection.SetActive(step == 3);
        currentStep = step;
    }

    public void NextStep()
    {
        if (currentStep == 1)
        {
            if (!float.TryParse(incomeInput.text, out totalIncome) || totalIncome <= 0)
            {
                warningText.text = "Please enter a valid income.";
                warningText.gameObject.SetActive(true);
                return;
            }
        }

        if (currentStep < 3)
            ShowStep(currentStep + 1);
    }

    public void PrevStep()
    {
        if (currentStep > 1)
            ShowStep(currentStep - 1);
    }

    private void UpdateCalculations()
    {
        rentValueText.text = $"${rentSlider.value:F0}";
        groceriesValueText.text = $"${groceriesSlider.value:F0}";
        transportValueText.text = $"${transportSlider.value:F0}";
        schoolFeesValueText.text = $"${schoolFeesSlider.value:F0}";
        savingsValueText.text = $"${savingsSlider.value:F0}";
        loanRepaymentValueText.text = $"${loanRepaymentSlider.value:F0}";

        if (!float.TryParse(incomeInput.text, out totalIncome))
            return;

        float totalExpenses = rentSlider.value + groceriesSlider.value + transportSlider.value + schoolFeesSlider.value;
        float totalAllocations = savingsSlider.value + loanRepaymentSlider.value;
        float totalOutflow = totalExpenses + totalAllocations;
        float remaining = totalIncome - totalOutflow;

        summaryText.text = $"Total Outflow: ${totalOutflow:F0}\nRemaining: ${remaining:F0}";

        if (remaining < 0)
        {
            summaryText.color = Color.red;
            warningText.text = "Your budget exceeds your income!";
            warningText.gameObject.SetActive(true);
            confirmButton.interactable = false;
        }
        else
        {
            summaryText.color = Color.green;
            warningText.gameObject.SetActive(false);
            confirmButton.interactable = true;
        }
    }

    private void OnConfirmAndStartSimulation()
    {
        if (!float.TryParse(incomeInput.text, out totalIncome) || totalIncome <= 0)
        {
            summaryText.text = "Please enter a valid income.";
            summaryText.color = Color.red;
            return;
        }

        float totalExpenses = rentSlider.value + groceriesSlider.value + transportSlider.value + schoolFeesSlider.value;
        float totalAllocations = savingsSlider.value + loanRepaymentSlider.value;
        float totalOutflow = totalExpenses + totalAllocations;

        float remaining = totalIncome - totalOutflow;

        if (remaining < 0)
        {
            summaryText.text = "Your budget exceeds your income.";
            summaryText.color = Color.red;
            return;
        }

        summaryText.text = $"Budget Confirmed!\nOutflow: ${totalOutflow:F0}\nRemaining: ${remaining:F0}";
        summaryText.color = Color.green;

        // Apply the budget to FinanceManager
        FinanceManager finance = FindFirstObjectByType<FinanceManager>();
        if (finance != null)
        {
            finance.SetPlayerIncome(totalIncome);
            finance.ApplyBudget(totalIncome, totalExpenses, totalAllocations);
        }

        Debug.Log("Budget confirmed — showing forecast...");

        // Move to forecast
        gameObject.SetActive(false);
        FindFirstObjectByType<ForecastManager>()?.GenerateForecast();
    }
}
