using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class SetupPanelController : MonoBehaviour
{
    [Header("Sections")]
    public GameObject incomeSection;
    public GameObject expensesSection;
    public GameObject schoolFeesSection;
    public GameObject reviewSection;

    [Header("Income Inputs")]
    public TMP_InputField minIncomeInput;
    public TMP_InputField maxIncomeInput;

    [Header("School Fees")]
    public Toggle schoolFeesToggle;
    public TMP_InputField schoolFeesAmountInput;

    [Header("Warnings")]
    public TMP_Text warningText;

    [Header("Household")]
    public TMP_InputField adultsInput;
    public TMP_InputField childrenInput;

    [Header("Income")]
    public Toggle stableIncomeToggle;

    [Header("Review")]
    public TMP_Text reviewSummaryText;
    public TMP_Text reflectionLineText;

    public GameObject schoolFeesAmountGroup;

    [Header("Navigation Buttons")]
    public GameObject nextButton;
    public GameObject backButton;

    private int currentStep = 1;

    private void Start()
    {
        ShowStep(1);
    }

    public void ShowStep(int step)
    {
        incomeSection.SetActive(step == 1);
        expensesSection.SetActive(step == 2);
        schoolFeesSection.SetActive(step == 3);
        reviewSection.SetActive(step == 4);

        // Navigation visibility
        backButton.SetActive(step > 1);
        nextButton.SetActive(step < 4);

        currentStep = step;

        if (step == 4)
            BuildReviewSummary();
    }


    public void NextStep()
    {
        if (!CanProceed())
            return;

        currentStep++;
        ShowStep(currentStep);
    }

    public void PreviousStep()
    {
        currentStep--;
        ShowStep(currentStep);
    }

    private bool CanProceed()
    {
        warningText.text = "";
        warningText.gameObject.SetActive(false);

        switch (currentStep)
        {
            case 1:
                return ValidateIncome();

            case 3:
                return ValidateSchoolFees();

            default:
                return true;
        }
    }

    private bool ValidateIncome()
    {
        if (!float.TryParse(minIncomeInput.text, out float min) ||
            !float.TryParse(maxIncomeInput.text, out float max))
        {
            ShowWarning("Please enter valid income amounts.");
            return false;
        }

        if (min <= 0 || max <= 0 || max < min)
        {
            ShowWarning("Income range must be positive and logical.");
            return false;
        }

        return true;
    }
    private bool ValidateSchoolFees()
    {
        if (!schoolFeesToggle.isOn)
            return true;

        if (!float.TryParse(schoolFeesAmountInput.text, out float fees) || fees <= 0)
        {
            ShowWarning("Please enter a valid school fee amount.");
            return false;
        }

        return true;
    }

    private void ShowWarning(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
    }

    public void OnSchoolFeesToggled(bool enabled)
    {
        schoolFeesAmountGroup.SetActive(enabled);
    }

    public void ConfirmAndStart()
    {
        GameManager gm = GameManager.Instance;

        // Save setup data
        gm.setupData.minIncome = float.Parse(minIncomeInput.text);
        gm.setupData.maxIncome = float.Parse(maxIncomeInput.text);
        gm.setupData.isIncomeStable = stableIncomeToggle.isOn;

        PlayerDataManager.Instance.adults = int.Parse(adultsInput.text);
        PlayerDataManager.Instance.children = int.Parse(childrenInput.text);

        gm.setupData.hasSchoolFees = schoolFeesToggle.isOn;
        if (schoolFeesToggle.isOn)
            gm.setupData.schoolFeesAmount = float.Parse(schoolFeesAmountInput.text);

        // Initialize finance (income range, school fees)
        FindFirstObjectByType<FinanceManager>()?.InitializeFromSetup();

        // SHOW BUDGET PANEL (do NOT start simulation yet)
        UIManager.Instance.HideAllPanels();
        UIManager.Instance.ShowBudgetPanel();

        // Hide setup panel
        gameObject.SetActive(false);
    }

    private void BuildReviewSummary()
    {
        float minIncome = float.Parse(minIncomeInput.text);
        float maxIncome = float.Parse(maxIncomeInput.text);

        int adults = int.Parse(adultsInput.text);
        int children = int.Parse(childrenInput.text);

        bool stableIncome = stableIncomeToggle.isOn;
        bool hasSchoolFees = schoolFeesToggle.isOn;

        string summary = "<b>Your Starting Situation</b>\n\n";

        summary += $"Income: ${minIncome:F0} – ${maxIncome:F0} per month\n";
        summary += $"Stability: {(stableIncome ? "Stable income" : "Variable income")}\n\n";

        summary += $"Household: {adults} adult{(adults != 1 ? "s" : "")}";
        if (children > 0)
            summary += $", {children} child{(children != 1 ? "ren" : "")}";
        summary += "\n\n";

        if (hasSchoolFees)
        {
            float fees = float.Parse(schoolFeesAmountInput.text);
            summary += $"School Fees: ${fees:F0} per term\n\n";
        }

        summary += "<i>Nothing here is permanent — but patterns will begin here.</i>";

        reviewSummaryText.text = summary;

        if (reflectionLineText != null)
            reflectionLineText.text = GetReflectionLine(stableIncome, hasSchoolFees);
    }

    private string GetReflectionLine(bool stableIncome, bool hasSchoolFees)
    {
        if (!stableIncome && hasSchoolFees)
            return "Periods of uncertainty may test consistency.";

        if (!stableIncome)
            return "Flexibility will matter more than precision.";

        if (hasSchoolFees)
            return "Regular commitments reward planning.";

        return "A steady base gives you room to adapt.";
    }

    public void OnStableIncomeToggled(bool isStable)
    {
        maxIncomeInput.interactable = !isStable;

        if (isStable)
        {
            maxIncomeInput.text = minIncomeInput.text;
        }
    }

}
