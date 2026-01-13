using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BudgetPanelController : MonoBehaviour
{
    [Header("Income Display")]
    public TMP_Text incomeDisplayText;

    [Header("Step Navigation")]
    public GameObject incomeSection;
    public GameObject expenseSection;
    public GameObject allocationSection;

    private int currentStep = 1;

    [Header("Expense Sliders")]
    public Slider rentSlider;
    public TMP_Text rentValueText;

    public Slider groceriesSlider;
    public TMP_Text groceriesValueText;

    public Slider transportSlider;
    public TMP_Text transportValueText;

    public Slider utilitiesSlider;
    public TMP_Text utilitiesValueText;

    [Header("Allocation Sliders")]
    public Slider savingsSlider;
    public TMP_Text savingsValueText;

    public Slider loanRepaymentSlider;
    public TMP_Text loanRepaymentValueText;

    [Header("Buttons & Text")]
    public Button confirmButton;
    public TMP_Text summaryText;
    public TMP_Text warningText;

    [Header("School Fees Savings")]
    public Slider schoolFeeSavingsSlider;
    public TMP_Text schoolFeeSavingsValueText;

    [Header("Read-Only Info")]
    public TMP_Text householdSummaryText;

    [Header("School Fees (Read Only)")]
    public GameObject schoolFeesGroup;
    public TMP_Text schoolFeesAmountText;


    private float totalIncome;

    private void Start()
{
    confirmButton.onClick.AddListener(OnConfirmAndStartSimulation);

    // Allocation-only mode (early game)
    incomeSection.SetActive(false);
    expenseSection.SetActive(false);
    allocationSection.SetActive(true);

    currentStep = 3;

    // Add listeners
    savingsSlider.onValueChanged.AddListener(_ => UpdateCalculations());
    schoolFeeSavingsSlider.onValueChanged.AddListener(_ => UpdateCalculations());
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
        savingsValueText.text = $"${savingsSlider.value:F0}";
        loanRepaymentValueText.text = $"${loanRepaymentSlider.value:F0}";
        utilitiesValueText.text = $"${utilitiesSlider.value:F0}";
        schoolFeeSavingsValueText.text = $"${schoolFeeSavingsSlider.value:F0}";

        float schoolFees = 0f;
        if (GameManager.Instance != null && GameManager.Instance.setupData != null)
        {
            schoolFees = GameManager.Instance.setupData.hasSchoolFees
                ? GameManager.Instance.setupData.schoolFeesAmount
                : 0f;
        }

        float totalExpenses =
            rentSlider.value +
            groceriesSlider.value +
            transportSlider.value +
            utilitiesSlider.value +
            schoolFees;
        float totalAllocations = savingsSlider.value + loanRepaymentSlider.value + schoolFeeSavingsSlider.value; ;
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
        if (totalIncome <= 0)
        {
            summaryText.text = "Income not available.";
            summaryText.color = Color.red;
            return;
        }

        float schoolFees = GameManager.Instance.setupData.hasSchoolFees
    ? GameManager.Instance.setupData.schoolFeesAmount
    : 0f;

        float totalExpenses =
            rentSlider.value +
            groceriesSlider.value +
            transportSlider.value +
            utilitiesSlider.value +
            schoolFees;
        float totalAllocations = savingsSlider.value + loanRepaymentSlider.value;
        float schoolFeeSavings = schoolFeeSavingsSlider.value;
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
            //finance.SetPlayerIncome(totalIncome);
            finance.rent = rentSlider.value;
            finance.groceries = groceriesSlider.value;
            finance.transport = transportSlider.value;
            finance.utilities = utilitiesSlider.value;
            finance.ApplyBudget(totalIncome, totalExpenses, totalAllocations);
            finance.SetSchoolFeeSavings(schoolFeeSavings);
        }

        Debug.Log("Budget confirmed — showing forecast...");

        UIManager.Instance.ShowForecastPanel();
        FindFirstObjectByType<ForecastManager>()?.GenerateForecast();
    }

    public void LoadDefaultsFromSetup()
    {
        totalIncome = FindFirstObjectByType<FinanceManager>().currentIncome;

        if (totalIncome <= 0f)
        {
            Debug.LogError("[BudgetPanel] totalIncome is 0 — cannot allocate budget.");
        }

        if (GameManager.Instance == null || GameManager.Instance.setupData == null)
        {
            Debug.LogWarning("[BudgetPanel] Setup data not ready.");
            return;
        }

        var setup = GameManager.Instance.setupData;

        totalIncome = FindFirstObjectByType<FinanceManager>().currentIncome;
        incomeDisplayText.text = $"Monthly Income: ${totalIncome:F0}";

        int householdSize = setup.adults + setup.children;

        rentSlider.value = Mathf.Clamp(totalIncome * 0.3f, rentSlider.minValue, rentSlider.maxValue);
        groceriesSlider.value = Mathf.Clamp(40f * householdSize, groceriesSlider.minValue, groceriesSlider.maxValue);
        transportSlider.value = Mathf.Clamp(25f * setup.adults, transportSlider.minValue, transportSlider.maxValue);
        utilitiesSlider.value = Mathf.Clamp(20f + householdSize * 10f, utilitiesSlider.minValue, utilitiesSlider.maxValue);
        savingsSlider.value = Mathf.Clamp(totalIncome * 0.1f, savingsSlider.minValue, savingsSlider.maxValue);

        // 3. School fees
        schoolFeesGroup.SetActive(setup.hasSchoolFees);

        float schoolFees = setup.hasSchoolFees ? setup.schoolFeesAmount : 0f;
        schoolFeesAmountText.text = $"${schoolFees:F0}";

        // 4. Default allocations (gentle, not optimal)
        loanRepaymentSlider.value = 0f;
        schoolFeeSavingsSlider.value = setup.hasSchoolFees ? setup.schoolFeesAmount / 3f : 0f;

        householdSummaryText.text =
        $"Household: {setup.adults} adult{(setup.adults != 1 ? "s" : "")}, " +
        $"{setup.children} child{(setup.children != 1 ? "ren" : "")}";

        UpdateCalculations();
    }
}
