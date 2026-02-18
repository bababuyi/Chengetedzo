using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BudgetPanelController : MonoBehaviour
{
    [Header("Income Display")]
    public TMP_Text incomeDisplayText;

    [Header("Allocation")]
    public Slider savingsSlider;
    public TMP_Text savingsValueText;

    [Header("Navigation")]
    public Button backButton;

    [Header("Confirm")]
    public Button confirmButton;
    public TMP_Text confirmButtonText;

    [Header("Savings Withdraw (Simulation Only)")]
    public GameObject savingsWithdrawGroup;

    public Button withdraw10Button;
    public Button withdraw20Button;
    public Button withdraw50Button;
    public Button withdraw100Button;

    public TMP_Text savingsBalanceText;

    [Header("Savings Allocation (Setup Only)")]
    public GameObject savingsAllocationGroup;

    private FinanceManager finance;
    private GameManager.GamePhase currentPhase;

    private void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackPressed);

        if (GameManager.Instance == null)
        {
            Debug.LogError("[BudgetPanelController] GameManager not ready.");
            return;
        }

        finance = GameManager.Instance.financeManager;

        if (finance == null || GameManager.Instance == null)

        {
            Debug.LogError("[BudgetPanelController] FinanceManager not ready.");
            return;
        }

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmPressed);

        if (savingsSlider != null)
            savingsSlider.onValueChanged.AddListener(_ => UpdateValues());

        SetupWithdrawButtons();

        ConfigureForPhase(GameManager.GamePhase.Idle);

        UpdateValues();
    }

    public void LoadDefaultsFromSetup()
    {
        if (finance == null || GameManager.Instance == null)

        {
            Debug.LogError("[BudgetPanelController] FinanceManager not found.");
            return;
        }

        var setup = GameManager.Instance.setupData;

        incomeDisplayText.text = $"Monthly Income: ${finance.currentIncome:F0}";
        ConfigureSliderBounds();

        float maxIncome = GameManager.Instance.setupData.maxIncome;
        savingsSlider.value = Mathf.Round((maxIncome * 0.1f) / 10f) * 10f;


        UpdateValues();
    }

    private void UpdateValues()
    {
        if (savingsSlider == null) return;

        // Snap to increments of 10
        float snapped = Mathf.Round(savingsSlider.value / 10f) * 10f;
        savingsSlider.SetValueWithoutNotify(snapped);

        savingsValueText.text = $"${snapped:F0}";

        UpdateSavingsColour(snapped);
    }

    private void OnConfirmPressed()
    {
        if (currentPhase == GameManager.GamePhase.Idle)
        {
            // SETUP CONFIRM
            float savings = savingsSlider.value;

            float monthlyExpenses = finance.GetProjectedMonthlyExpenses();
            float maxSurplus = Mathf.Max(0f,
                GameManager.Instance.setupData.maxIncome - monthlyExpenses);

            if (savings > maxSurplus)
            {
                Debug.Log("[Budget] Cannot allocate savings beyond projected surplus.");
                return; // Block progression
            }

            finance.generalSavingsMonthly = savings;

            GameManager.Instance.SetPhase(GameManager.GamePhase.Forecast);  // ADD THIS

            UIManager.Instance.ShowForecastPanel();
            GameManager.Instance.forecastManager.GenerateForecast();

            gameObject.SetActive(false);
        }
        else
        {
            // SIMULATION CONFIRM (done borrowing)
            gameObject.SetActive(false);

            GameManager.Instance.OnSavingsDecisionFinished();
            // This already resumes the simulation flow safely
        }
    }

    private void SetupWithdrawButtons()
    {
        withdraw10Button.onClick.AddListener(() => Withdraw(10));
        withdraw20Button.onClick.AddListener(() => Withdraw(20));
        withdraw50Button.onClick.AddListener(() => Withdraw(50));
        withdraw100Button.onClick.AddListener(() => Withdraw(100));
    }

    private void Withdraw(float amount)
    {
        if (finance == null || GameManager.Instance == null)
            return;

        if (finance.WithdrawFromSavings(amount))
        {
            RefreshSavingsDisplay();
        }
        else
        {
            Debug.Log("[Budget] Withdrawal failed.");
        }
    }

    private void RefreshSavingsDisplay()
    {
        if (savingsBalanceText != null)
            savingsBalanceText.text =
                $"Savings Balance: ${finance.generalSavingsBalance:F0}";

        float balance = finance.generalSavingsBalance;

        withdraw10Button.interactable = balance >= 10;
        withdraw20Button.interactable = balance >= 20;
        withdraw50Button.interactable = balance >= 50;
        withdraw100Button.interactable = balance >= 100;
    }

    public void ConfigureForPhase(GameManager.GamePhase phase)
    {
        currentPhase = phase;
        bool isSetup = phase == GameManager.GamePhase.Idle;

        // Setup UI
        savingsAllocationGroup.SetActive(isSetup);
        savingsValueText.gameObject.SetActive(isSetup);

        // Confirm is always visible
        confirmButton.gameObject.SetActive(true);

        // Change confirm text dynamically
        if (confirmButtonText != null)
            confirmButtonText.text = isSetup ? "Confirm Savings" : "Continue";

        // Simulation UI
        savingsWithdrawGroup.SetActive(!isSetup);

        if (!isSetup)
            RefreshSavingsDisplay();

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    private void OnBackPressed()
    {
        if (currentPhase != GameManager.GamePhase.Idle)
            return; // only allow back during setup phase

        UIManager.Instance.ShowSetupPanel();
        gameObject.SetActive(false);
    }

    private void ConfigureSliderBounds()
    {
        float maxIncome = GameManager.Instance.setupData.maxIncome;

        savingsSlider.minValue = 0;
        savingsSlider.maxValue = maxIncome;
        savingsSlider.wholeNumbers = false;
    }

    private void UpdateSavingsColour(float savingsAmount)
    {
        if (GameManager.Instance == null || GameManager.Instance.setupData == null)
            return;

        var setup = GameManager.Instance.setupData;

        float minIncome = setup.minIncome;
        float maxIncome = setup.maxIncome;

        float monthlyExpenses = 0f;

        if (GameManager.Instance.financeManager != null)
        {
            monthlyExpenses =
                GameManager.Instance.financeManager.GetProjectedMonthlyExpenses();
        }

        // Calculate surplus AFTER getting expenses
        float minSurplus = minIncome - monthlyExpenses;
        float maxSurplus = maxIncome - monthlyExpenses;

        // Clamp negatives
        minSurplus = Mathf.Max(0f, minSurplus);
        maxSurplus = Mathf.Max(0f, maxSurplus);

        if (maxSurplus <= 0f)
        {
            savingsValueText.color = Color.red;
            return;
        }

        if (savingsAmount <= minSurplus)
        {
            savingsValueText.color = Color.white;
        }
        else if (savingsAmount <= maxSurplus)
        {
            savingsValueText.color = new Color(1f, 0.5f, 0f); // orange
        }
        else
        {
            savingsValueText.color = Color.red;
        }
        confirmButton.interactable = savingsAmount <= maxSurplus;
    }
}
