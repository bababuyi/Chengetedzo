using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class SetupPanelController : MonoBehaviour
{
    [Header("Sections")]
    public GameObject incomeSection;
    public GameObject expensesSection;
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
    
    [Header("Panels")]
    public ExpensesPanelController expensesPanelController;

    [Header("Asset Toggles")]
    public Toggle hasHouseToggle;
    public Toggle hasLivestockToggle;
    public Toggle hasMotorToggle;
    public Toggle hasCropsToggle;

    private int currentStep = 1;

    private void Start()
    {
        finance = GameManager.Instance?.financeManager;

        if (finance == null)
        {
            Debug.LogError("[SetupPanelController] FinanceManager not ready.");
            return;
        }

        ShowStep(1);

        stableIncomeToggle.onValueChanged.RemoveAllListeners();
        stableIncomeToggle.onValueChanged.AddListener(OnStableIncomeToggled);
        OnStableIncomeToggled(stableIncomeToggle.isOn);

        schoolFeesToggle.onValueChanged.RemoveAllListeners();
        schoolFeesToggle.onValueChanged.AddListener(OnSchoolFeesToggled);
        OnSchoolFeesToggled(schoolFeesToggle.isOn);

        hasHouseToggle.onValueChanged.RemoveAllListeners();
        hasHouseToggle.onValueChanged.AddListener(OnHouseToggleChanged);

        OnHouseToggleChanged(hasHouseToggle.isOn);

        minIncomeInput.onValueChanged.AddListener(_ =>
        {
            if (stableIncomeToggle.isOn)
                maxIncomeInput.text = minIncomeInput.text;
        });

        adultsInput.onEndEdit.AddListener(value =>
        {
            if (!int.TryParse(value, out int a) || a < 1)
                adultsInput.text = "1";
        });

        if (string.IsNullOrWhiteSpace(adultsInput.text))
            adultsInput.text = "1";

        if (string.IsNullOrWhiteSpace(childrenInput.text))
            childrenInput.text = "0";
    }


    private FinanceManager finance;

    public void ConfirmSetup()
    {
        if (!float.TryParse(minIncomeInput.text, out float minIncome))
        {
            ShowWarning("Invalid minimum income.");
            return;
        }

        float maxIncome = minIncome;

        if (!stableIncomeToggle.isOn)
        {
            if (!float.TryParse(maxIncomeInput.text, out maxIncome))
            {
                ShowWarning("Invalid maximum income.");
                return;
            }
        }

        //if (finance == null)
        //  return;

        finance.assets = new PlayerAssets
        {
            hasHouse = hasHouseToggle != null && hasHouseToggle.isOn,
            hasLivestock = hasLivestockToggle != null && hasLivestockToggle.isOn,
            hasMotor = hasMotorToggle != null && hasMotorToggle.isOn,
            hasCrops = hasCropsToggle != null && hasCropsToggle.isOn
        };

        if (InsuranceManager.Instance != null)
            InsuranceManager.Instance.RefreshEligibility();

        LockSetupUI();
    }

    private void LockSetupUI()
    {
        hasHouseToggle.interactable = false;
        hasLivestockToggle.interactable = false;
        hasMotorToggle.interactable = false;
        hasCropsToggle.interactable = false;
    }
    public void ShowStep(int step)
    {
        incomeSection.SetActive(step == 1);
        expensesSection.SetActive(step == 2);
        reviewSection.SetActive(step == 3);

        backButton.SetActive(step > 1);
        nextButton.SetActive(step < 3);

        currentStep = step;

        if (step == 2)
        {
            if (expensesPanelController == null)
            {
                Debug.LogError("[SetupPanel] ExpensesPanelController missing.");
                return;
            }

            expensesPanelController.Init();
            expensesPanelController.SetHousingMode(hasHouseToggle.isOn);
            OnSchoolFeesToggled(schoolFeesToggle.isOn);
        }

        if (step == 3)
        {
            BuildReviewSummary();
        }
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

            case 2:
                return ValidateSchoolFees();

            default:
                return true;
        }
    }
    private bool ValidateIncome()
    {
        if (!float.TryParse(minIncomeInput.text, out float min))
        {
            ShowWarning("Please enter a valid minimum income.");
            return false;
        }

        if (min <= 0)
        {
            ShowWarning("Income must be greater than zero.");
            return false;
        }

        if (stableIncomeToggle.isOn)
            return true;

        if (!float.TryParse(maxIncomeInput.text, out float max))
        {
            ShowWarning("Please enter a valid maximum income.");
            return false;
        }

        if (max < min)
        {
            ShowWarning("Maximum income cannot be less than minimum income.");
            return false;
        }

        return true;
    }

    private bool ValidateSchoolFees()
    {
        if (!schoolFeesToggle.isOn)
            return true;

        if (!float.TryParse(schoolFeesAmountInput.text, out float fees))
        {
            ShowWarning("Invalid school fee amount.");
            return false;
        }

        GameManager.Instance.setupData.schoolFeesAmount = fees;
        return true;
    }

    private void ShowWarning(string message)
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
    }

    public void OnSchoolFeesToggled(bool enabled)
    {
        if (schoolFeesAmountGroup == null)
        {
            Debug.LogError("[SetupPanelController] schoolFeesAmountGroup is not assigned!");
            return;
        }

        schoolFeesAmountGroup.SetActive(enabled);
    }

    public void ConfirmAndStart()
    {
        if (GameManager.Instance == null ||
            PlayerDataManager.Instance == null ||
            UIManager.Instance == null)
        {
            Debug.LogError("[SetupPanelController] Required manager missing.");
            return;
        }

        GameManager gm = GameManager.Instance;

        ConfirmSetup();

        // Save setup data
        gm.setupData.minIncome = float.Parse(minIncomeInput.text);
        gm.setupData.maxIncome = float.Parse(maxIncomeInput.text);
        gm.setupData.isIncomeStable = stableIncomeToggle.isOn;

        int totalAdults = 1;
        int totalChildren = 0;

        if (!int.TryParse(adultsInput.text, out totalAdults) || totalAdults < 1)
            totalAdults = 1;

        if (!int.TryParse(childrenInput.text, out totalChildren) || totalChildren < 0)
            totalChildren = 0;

        PlayerDataManager.Instance.Adults = totalAdults;
        PlayerDataManager.Instance.Children = totalChildren;

        gm.setupData.hasSchoolFees = schoolFeesToggle.isOn;
        if (schoolFeesToggle.isOn)
            gm.setupData.schoolFeesAmount = float.Parse(schoolFeesAmountInput.text);

        FinanceManager finance = GameManager.Instance.financeManager;

        // Apply expense choices FIRST
        expensesPanelController.ApplyExpensesToFinance(finance);

        // Initialize finance (income, starting cash)
        finance.InitializeFromSetup();

        // SHOW BUDGET PANEL (do NOT start simulation yet)
        UIManager.Instance.ShowBudgetPanel();
        gameObject.SetActive(false);

        if (expensesPanelController != null)
            expensesPanelController.ApplyExpensesToFinance(finance);

        if (!ValidateFullSetup())
            return;
    }

    private void BuildReviewSummary()
    {
        float.TryParse(minIncomeInput.text, out float minIncome);
        float.TryParse(maxIncomeInput.text, out float maxIncome);

        if (minIncome <= 0) minIncome = 0;
        if (maxIncome <= 0) maxIncome = minIncome;

        int adults = 1;
        int children = 0;

        if (!int.TryParse(adultsInput.text, out adults) || adults < 1)
            adults = 1;

        if (!int.TryParse(childrenInput.text, out children) || children < 0)
            children = 0;

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
            float.TryParse(schoolFeesAmountInput.text, out float fees);
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

    public void OnHouseToggleChanged(bool ownsHouse)
    {
        expensesPanelController.SetHousingMode(ownsHouse);
    }

    public void OnStableIncomeToggled(bool isStable)
    {
        maxIncomeInput.gameObject.SetActive(!isStable);

        if (isStable)
        {
            maxIncomeInput.text = minIncomeInput.text;
        }
    }

    private void OnDestroy()
    {
        stableIncomeToggle?.onValueChanged.RemoveAllListeners();
        schoolFeesToggle?.onValueChanged.RemoveAllListeners();
        hasHouseToggle?.onValueChanged.RemoveAllListeners();
        minIncomeInput?.onValueChanged.RemoveAllListeners();
        adultsInput?.onEndEdit.RemoveAllListeners();
    }

    private bool ValidateFullSetup()
    {
        return ValidateIncome() &&
               ValidateSchoolFees();
    }

    public void UnlockSetupUI()
    {
        hasHouseToggle.interactable = true;
        hasLivestockToggle.interactable = true;
        hasMotorToggle.interactable = true;
        hasCropsToggle.interactable = true;
    }
}
