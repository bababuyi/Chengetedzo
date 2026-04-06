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
    public BudgetPieChart budgetPieChart;

    [Header("Savings")]
    public GameObject savingsSection;
    public Slider savingsSlider;
    public TMP_Text savingsValueText;

    public GameObject schoolFeesAmountGroup;

    [Header("Navigation Buttons")]
    public GameObject nextButton;
    public GameObject backButton;
    public GameObject confirmAndStartButton;

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
        ShowStep(1);

        stableIncomeToggle.onValueChanged.RemoveAllListeners();
        stableIncomeToggle.onValueChanged.AddListener(OnStableIncomeToggled);
        OnStableIncomeToggled(stableIncomeToggle.isOn);

        schoolFeesToggle.onValueChanged.RemoveAllListeners();
        schoolFeesToggle.onValueChanged.AddListener(OnSchoolFeesToggled);
        OnSchoolFeesToggled(schoolFeesToggle.isOn);

        hasHouseToggle.onValueChanged.RemoveAllListeners();
        hasLivestockToggle.onValueChanged.RemoveAllListeners();
        hasMotorToggle.onValueChanged.RemoveAllListeners();
        hasCropsToggle.onValueChanged.RemoveAllListeners();

        hasHouseToggle.onValueChanged.AddListener(OnHouseToggleChanged);
        hasHouseToggle.onValueChanged.AddListener(_ => UpdateAssetsFromToggles());

        hasLivestockToggle.onValueChanged.AddListener(_ => UpdateAssetsFromToggles());
        hasMotorToggle.onValueChanged.AddListener(_ => UpdateAssetsFromToggles());
        hasCropsToggle.onValueChanged.AddListener(_ => UpdateAssetsFromToggles());

        UpdateAssetsFromToggles();

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

        warningText.text = "";
        warningText.gameObject.SetActive(false);
    }

    public enum SetupMode
    {
        NormalSetup,
        ReviewFromProfile,
        Savings
    }

    private SetupMode currentMode = SetupMode.NormalSetup;

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

        var currentFinance = GameManager.Instance?.financeManager;
        if (currentFinance == null) return;

        currentFinance.assets = new PlayerAssets
        {
            hasHouse = hasHouseToggle.isOn,
            hasLivestock = hasLivestockToggle.isOn,
            hasMotor = hasMotorToggle.isOn,
            hasCrops = hasCropsToggle.isOn
        };

        if (GameManager.Instance.insuranceManager != null)
            GameManager.Instance.insuranceManager.RefreshEligibility();
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
        savingsSection.SetActive(step == 3);
        reviewSection.SetActive(step == 4);

        backButton.SetActive(step > 1);
        nextButton.SetActive(step < 4);
        if (confirmAndStartButton != null)
            confirmAndStartButton.SetActive(step == 4);

        currentStep = step;

        if (step == 1)
            UnlockSetupUI();

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
            InitSavingsStep();

        if (step == 4)
            BuildReviewSummary();
    }

    private void InitSavingsStep()
    {
        float.TryParse(maxIncomeInput.text, out float maxIncome);
        if (maxIncome <= 0)
            float.TryParse(minIncomeInput.text, out maxIncome);

        savingsSlider.minValue = 0;
        savingsSlider.maxValue = maxIncome;

        float suggested = Mathf.Round(maxIncome * 0.1f / 10f) * 10f;
        savingsSlider.SetValueWithoutNotify(suggested);

        savingsSlider.onValueChanged.RemoveAllListeners();
        savingsSlider.onValueChanged.AddListener(_ => UpdateSavingsDisplay());

        UpdateSavingsDisplay();
    }

    private void UpdateSavingsDisplay()
    {
        float snapped = Mathf.Round(savingsSlider.value / 10f) * 10f;
        savingsSlider.SetValueWithoutNotify(snapped);
        if (savingsValueText != null)
            savingsValueText.text = $"${snapped:F0} / month";
    }

    public void NextStep()
    {
        if (!CanProceed())
            return;

        if (isReviewMode && currentStep == 3)
        {
            UIManager.Instance.ShowForecastPanel();
            return;
        }

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

            case 3:
                return true;

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
        if (currentStep != 4)
            return;

        if (GameManager.Instance == null ||
            PlayerDataManager.Instance == null ||
            UIManager.Instance == null)
        {
            Debug.LogError("[SetupPanelController] Required manager missing.");
            return;
        }

        if (!ValidateFullSetup())
            return;

        GameManager gm = GameManager.Instance;

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

        ConfirmSetup();
        LockSetupUI();

        FinanceManager finance = gm.financeManager;
        expensesPanelController.ApplyExpensesToFinance(finance);
        finance.InitializeFromSetup();

        float savings = Mathf.Round(savingsSlider.value / 10f) * 10f;
        finance.generalSavingsMonthly = savings;
        gm.OnSavingsSetupConfirmed(savings);
    }

    private void BuildReviewSummary()
    {
        float.TryParse(minIncomeInput.text, out float minIncome);
        float.TryParse(maxIncomeInput.text, out float maxIncome);

        if (minIncome <= 0) minIncome = 0;
        if (maxIncome <= 0) maxIncome = minIncome;

        float averageIncome = (minIncome + maxIncome) * 0.5f;

        int adults = 1;
        int children = 0;

        if (!int.TryParse(adultsInput.text, out adults) || adults < 1)
            adults = 1;

        if (!int.TryParse(childrenInput.text, out children) || children < 0)
            children = 0;

        bool stableIncome = stableIncomeToggle.isOn;
        bool hasSchoolFees = schoolFeesToggle.isOn;

        float monthlyExpenses = 0f;

        if (expensesPanelController != null)
            monthlyExpenses = expensesPanelController.GetEstimatedMonthlyExpenses();

        float surplus = averageIncome - monthlyExpenses;

        string summary = "<b>Your Starting Situation</b>\n\n";

        summary += $"Income Range: ${minIncome:F0} – ${maxIncome:F0}\n";
        summary += $"Estimated Monthly Income: ${averageIncome:F0}\n\n";

        summary += $"Living Expenses: -${monthlyExpenses:F0}\n";

        if (hasSchoolFees)
        {
            float.TryParse(schoolFeesAmountInput.text, out float fees);
            summary += $"School Fees: -${fees:F0}\n";
        }

        summary += "\n";

        float savingsAmount = Mathf.Round(savingsSlider.value / 10f) * 10f;
        float netSurplus = surplus - savingsAmount;

        summary += $"Monthly Savings: -${savingsAmount:F0}\n";

        if (netSurplus >= 0)
            summary += $"<color=#3CB371>Estimated Surplus: +${netSurplus:F0}</color>\n\n";
        else
            summary += $"<color=#E74C3C>Estimated Shortfall: -${Mathf.Abs(netSurplus):F0}</color>\n\n";

        summary += $"Household: {adults} adult{(adults != 1 ? "s" : "")}";
        if (children > 0)
            summary += $", {children} child{(children != 1 ? "ren" : "")}";
        summary += "\n\n";

        summary += "<i>This is your financial baseline. The year will test it.</i>";

        reviewSummaryText.text = summary;

        if (reflectionLineText != null)
            reflectionLineText.text = GetReflectionLine(stableIncome, hasSchoolFees, surplus);

        float feesForChart = 0f;
        if (hasSchoolFees)
            float.TryParse(schoolFeesAmountInput.text, out feesForChart);

        float housing = expensesPanelController?.GetHousingCost() ?? 0f;
        float groceries = expensesPanelController?.GetGroceriesCost() ?? 0f;
        float transport = expensesPanelController?.GetTransportCost() ?? 0f;
        float utilities = expensesPanelController?.GetUtilitiesCost() ?? 0f;

        budgetPieChart?.Render(averageIncome, housing, groceries, transport, utilities, feesForChart, savingsAmount);
    }

    private string GetReflectionLine(bool stableIncome, bool hasSchoolFees, float surplus)
    {
        if (surplus < 0)
            return "Your expenses already exceed your income. Stability will require difficult choices.";

        if (surplus < 200)
            return "You have little breathing room. Small shocks may feel large.";

        if (!stableIncome && surplus > 0)
            return "Your flexibility will matter more than your margin.";

        if (hasSchoolFees && surplus > 0)
            return "Regular commitments reward consistent planning.";

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
        hasLivestockToggle?.onValueChanged.RemoveAllListeners();
        hasMotorToggle?.onValueChanged.RemoveAllListeners();
        hasCropsToggle?.onValueChanged.RemoveAllListeners();
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

    public void OnPanelOpened()
    {
        UnlockSetupUI();
        ShowStep(1);
    }

    private void UpdateAssetsFromToggles()
    {
        var currentFinance = GameManager.Instance?.financeManager;

        if (currentFinance == null)
            return;

        currentFinance.assets = new PlayerAssets
        {
            hasHouse = hasHouseToggle.isOn,
            hasLivestock = hasLivestockToggle.isOn,
            hasMotor = hasMotorToggle.isOn,
            hasCrops = hasCropsToggle.isOn
        };

        if (GameManager.Instance.insuranceManager != null)
            GameManager.Instance.insuranceManager.RefreshEligibility();
    }

    private bool isReviewMode = false;

    public void EnterReviewMode()
    {
        currentMode = SetupMode.ReviewFromProfile;

        currentStep = 3;
        ShowStep(currentStep);

        backButton.SetActive(false);
        nextButton.SetActive(true);

        var txt = nextButton.GetComponentInChildren<TMP_Text>();
        if (txt != null)
            txt.text = "Continue";

        DisableSavingsUI();
    }

    private void DisableSavingsUI()
    {
        if (savingsSlider != null)
            savingsSlider.gameObject.SetActive(false);
    }

    /// <summary>
    /// Called by guided profile flow. Pre-fills everything from the applied
    /// profile and lands on step 4 (Review) with the confirm button ready.
    /// </summary>
    public void JumpToReviewStep()
    {
        var gm = GameManager.Instance;
        var finance = gm?.financeManager;
        var setup = gm?.setupData;
        if (gm == null || finance == null || setup == null) return;

        // Populate income fields so ConfirmAndStart can parse them
        minIncomeInput.text = setup.minIncome.ToString("F0");
        maxIncomeInput.text = setup.maxIncome.ToString("F0");
        stableIncomeToggle.isOn = setup.isIncomeStable;
        maxIncomeInput.gameObject.SetActive(!setup.isIncomeStable);

        // Household
        adultsInput.text = setup.adults.ToString();
        childrenInput.text = setup.children.ToString();

        // School fees
        schoolFeesToggle.isOn = setup.hasSchoolFees;
        if (setup.hasSchoolFees)
            schoolFeesAmountInput.text = setup.schoolFeesAmount.ToString("F0");
        OnSchoolFeesToggled(setup.hasSchoolFees);

        // Assets
        hasHouseToggle.isOn = finance.assets.hasHouse;
        hasMotorToggle.isOn = finance.assets.hasMotor;
        hasLivestockToggle.isOn = finance.assets.hasLivestock;
        hasCropsToggle.isOn = finance.assets.hasCrops;

        // Savings slider — default 10% of max income
        float maxIncome = setup.maxIncome > 0 ? setup.maxIncome : setup.minIncome;
        savingsSlider.minValue = 0;
        savingsSlider.maxValue = maxIncome;
        float suggested = Mathf.Round(maxIncome * 0.1f / 10f) * 10f;
        savingsSlider.SetValueWithoutNotify(suggested);
        UpdateSavingsDisplay();

        // Lock toggles — player is not configuring, just reviewing
        LockSetupUI();

        // Show step 4 but use the finance-aware summary builder
        incomeSection.SetActive(false);
        expensesSection.SetActive(false);
        savingsSection.SetActive(false);
        reviewSection.SetActive(true);

        backButton.SetActive(false);
        nextButton.SetActive(false);
        if (confirmAndStartButton != null)
            confirmAndStartButton.SetActive(true);

        currentStep = 4;

        BuildReviewSummaryFromProfile();
    }

    private void BuildReviewSummaryFromProfile()
    {
        var gm = GameManager.Instance;
        var finance = gm?.financeManager;
        var setup = gm?.setupData;
        if (finance == null || setup == null) return;

        float minIncome = setup.minIncome;
        float maxIncome = setup.maxIncome > 0 ? setup.maxIncome : minIncome;
        float averageIncome = (minIncome + maxIncome) * 0.5f;

        float housing = finance.rentCost;
        float groceries = finance.groceries;
        float transport = finance.transport;
        float utilities = finance.utilities;
        float fees = setup.hasSchoolFees ? setup.schoolFeesAmount : 0f;

        float monthlyExpenses = housing + groceries + transport + utilities + fees;
        float surplus = averageIncome - monthlyExpenses;

        float savingsAmount = Mathf.Round(savingsSlider.value / 10f) * 10f;
        float netSurplus = surplus - savingsAmount;

        string summary = "<b>Your Starting Situation</b>\n\n";
        summary += $"Income Range: ${minIncome:F0} – ${maxIncome:F0}\n";
        summary += $"Estimated Monthly Income: ${averageIncome:F0}\n\n";
        summary += $"Housing: -${housing:F0}\n";
        summary += $"Food: -${groceries:F0}\n";
        summary += $"Transport: -${transport:F0}\n";
        summary += $"Utilities: -${utilities:F0}\n";
        if (fees > 0) summary += $"School Fees: -${fees:F0}\n";
        summary += "\n";
        summary += $"Monthly Savings: -${savingsAmount:F0}\n";

        if (netSurplus >= 0)
            summary += $"<color=#3CB371>Estimated Surplus: +${netSurplus:F0}</color>\n\n";
        else
            summary += $"<color=#E74C3C>Estimated Shortfall: -${Mathf.Abs(netSurplus):F0}</color>\n\n";

        int adults = setup.adults;
        int children = setup.children;
        summary += $"Household: {adults} adult{(adults != 1 ? "s" : "")}";
        if (children > 0)
            summary += $", {children} child{(children != 1 ? "ren" : "")}";
        summary += "\n\n";
        summary += "<i>This is your financial baseline. The year will test it.</i>";

        reviewSummaryText.text = summary;

        if (reflectionLineText != null)
            reflectionLineText.text = GetReflectionLine(setup.isIncomeStable, setup.hasSchoolFees, surplus);

        budgetPieChart?.Render(averageIncome, housing, groceries, transport, utilities, fees, savingsAmount);
    }
}
