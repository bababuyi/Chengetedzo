using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BudgetPanelController : MonoBehaviour
{
    [Header("Income Display")]
    public TMP_Text incomeDisplayText;

    [Header("Confirm")]
    public Button confirmButton;

    [Header("Savings Withdraw")]
    public GameObject savingsWithdrawGroup;

    public Button withdraw10Button;
    public Button withdraw20Button;
    public Button withdraw50Button;
    public Button withdraw100Button;

    public TMP_Text savingsBalanceText;

    private FinanceManager finance;

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[BudgetPanelController] GameManager not ready.");
            return;
        }

        finance = GameManager.Instance.financeManager;

        if (finance == null)
        {
            Debug.LogError("[BudgetPanelController] FinanceManager not ready.");
            return;
        }

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmPressed);

        SetupWithdrawButtons();
        RefreshSavingsDisplay();
    }

    private void OnConfirmPressed()
    {
        UIManager.Instance.CloseSavingsPanel();
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
        if (finance == null) return;

        if (finance.WithdrawFromSavings(amount))
            RefreshSavingsDisplay();
        else
            Debug.Log("[Budget] Withdrawal failed.");
    }

    private void RefreshSavingsDisplay()
    {
        if (savingsBalanceText != null)
            savingsBalanceText.text = $"Savings Balance: {GameUtils.FormatMoney(finance.generalSavingsBalance)}";

        float balance = finance.generalSavingsBalance;

        withdraw10Button.interactable = balance >= 10;
        withdraw20Button.interactable = balance >= 20;
        withdraw50Button.interactable = balance >= 50;
        withdraw100Button.interactable = balance >= 100;
    }
}