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

        // Get FinanceManager safely
        FinanceManager finance = FindFirstObjectByType<FinanceManager>();
        if (finance != null)
            incomeInput.text = finance.currentIncome.ToString("F0");

        ShowStep(1);

        // Recalculate live if user edits income manually
        incomeInput.onValueChanged.AddListener(_ => UpdateSliderText());

        // Add listeners for sliders
        rentSlider.onValueChanged.AddListener(_ => UpdateSliderText());
        groceriesSlider.onValueChanged.AddListener(_ => UpdateSliderText());
        transportSlider.onValueChanged.AddListener(_ => UpdateSliderText());
        schoolFeesSlider.onValueChanged.AddListener(_ => UpdateSliderText());
        savingsSlider.onValueChanged.AddListener(_ => UpdateSliderText());
        loanRepaymentSlider.onValueChanged.AddListener(_ => UpdateSliderText());
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
        if (currentStep < 3)
            ShowStep(currentStep + 1);
    }

    public void PrevStep()
    {
        if (currentStep > 1)
            ShowStep(currentStep - 1);
    }

    private void UpdateSliderText()
    {
        // Update slider value labels
        rentValueText.text = $"${rentSlider.value:F0}";
        groceriesValueText.text = $"${groceriesSlider.value:F0}";
        transportValueText.text = $"${transportSlider.value:F0}";
        schoolFeesValueText.text = $"${schoolFeesSlider.value:F0}";
        savingsValueText.text = $"${savingsSlider.value:F0}";
        loanRepaymentValueText.text = $"${loanRepaymentSlider.value:F0}";

        // --- Live total calculation ---
        if (float.TryParse(incomeInput.text, out totalIncome))
        {
            float totalExpenses = rentSlider.value + groceriesSlider.value + transportSlider.value + schoolFeesSlider.value;
            float totalAllocations = savingsSlider.value + loanRepaymentSlider.value;
            float totalOutflow = totalExpenses + totalAllocations;
            float remaining = totalIncome - totalOutflow;

            summaryText.text = $"Total Outflow: ${totalOutflow:F0}\nRemaining: ${remaining:F0}";

            // --- Warning & visual feedback ---
            if (remaining < 0)
            {
                summaryText.color = Color.red;
                warningText.gameObject.SetActive(true);
                warningText.text = "Your allocations exceed your income!";
                confirmButton.interactable = false;
            }
            else
            {
                summaryText.color = Color.green;
                warningText.gameObject.SetActive(false);
                confirmButton.interactable = true;
            }
        }
    }

    private void OnConfirmAndStartSimulation()
    {
        if (float.TryParse(incomeInput.text, out totalIncome))
        {
            float totalExpenses = rentSlider.value + groceriesSlider.value + transportSlider.value + schoolFeesSlider.value;
            float totalAllocations = savingsSlider.value + loanRepaymentSlider.value;
            float totalOutflow = totalExpenses + totalAllocations;
            float remaining = totalIncome - totalOutflow;

            if (remaining < 0)
            {
                summaryText.text = "Your allocations exceed your income.";
                summaryText.color = Color.red;
                return;
            }

            summaryText.text = $"Budget confirmed!\nOutflow: ${totalOutflow:F0}\nRemaining: ${remaining:F0}";
            summaryText.color = Color.green;

            // Apply to FinanceManager
            FinanceManager finance = FindFirstObjectByType<FinanceManager>();
            if (finance != null)
                finance.ApplyBudget(totalIncome, totalExpenses, totalAllocations);

            // Show Forecast before starting simulation
            Debug.Log("Budget confirmed — showing forecast...");
            gameObject.SetActive(false);
            FindFirstObjectByType<ForecastManager>()?.GenerateForecast();
        }
        else
        {
            summaryText.text = "Please enter a valid income amount.";
            summaryText.color = Color.red;
        }
    }
}
