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
        finance = GameManager.Instance?.financeManager;

        if (finance == null)
        {
            Debug.LogError("[BudgetPanelController] FinanceManager not ready.");
            return;
        }

        confirmButton.onClick.AddListener(OnConfirmPressed);
        savingsSlider.onValueChanged.AddListener(_ => UpdateValues());

        SetupWithdrawButtons();

        // Default: setup phase
        ConfigureForPhase(GameManager.GamePhase.Idle);

        UpdateValues();
    }


    public void LoadDefaultsFromSetup()
    {
        if (finance == null)
        {
            Debug.LogError("[BudgetPanelController] FinanceManager not found.");
            return;
        }

        var setup = GameManager.Instance.setupData;

        incomeDisplayText.text = $"Monthly Income: ${finance.currentIncome:F0}";
        savingsSlider.value = finance.currentIncome * 0.1f;

        UpdateValues();
    }

    private void UpdateValues()
    {
        savingsValueText.text = $"${savingsSlider.value:F0}";
    }

    private void OnConfirmPressed()
    {
        if (currentPhase == GameManager.GamePhase.Idle)
        {
            // SETUP CONFIRM
            finance.generalSavingsMonthly = savingsSlider.value;

            UIManager.Instance.ShowForecastPanel();
            GameManager.Instance.forecastManager.GenerateForecast();

            gameObject.SetActive(false);
        }
        else
        {
            // SIMULATION CONFIRM (done borrowing)
            gameObject.SetActive(false);

            GameManager.Instance.OnLoanDecisionFinished();
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
        if (finance.WithdrawFromSavings(amount))
        {
            RefreshSavingsDisplay();
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
    }
}
