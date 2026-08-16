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

    [Header("Savings Contribution")]
    // Savings isn't an expense being cut, it's an amount being chosen - no baseline to
    // measure against and no floor to protect, so this deliberately does not reuse
    // ExpensesPanelController's SetupAdjustSlider pattern (that one exists for the
    // Groceries/Transport/Utilities tradeoff sliders, which do have both). Range is
    // 0 to whatever GetSavingsSliderMax says the current committed budget can sustain.
    public Slider contributionSlider;
    public TMP_InputField contributionValueInput;
    public TMP_Text contributionValueText;

    private FinanceManager finance;
    private bool _listenersWired = false;

    private void EnsureFinance()
    {
        if (finance != null) return;
        if (GameManager.Instance == null)
        {
            Debug.LogError("[BudgetPanelController] GameManager not ready.");
            return;
        }

        finance = GameManager.Instance.financeManager;

        if (finance == null)
            Debug.LogError("[BudgetPanelController] FinanceManager not ready.");
    }

    private void Start()
    {
        EnsureFinance();
        if (finance == null) return;

        WireListenersOnce();

        // Covers the case where this component's Start happens to run after the panel's
        // first OnEnable (Unity normally does OnEnable then Start on first activation, but
        // don't depend on ordering) - make sure both displays are correct either way.
        RefreshSavingsDisplay();
        SetupContributionSlider();
    }

    // Panel object is toggled active/inactive by UIManager.SwitchPanel rather than being
    // recreated, so OnEnable - not just Start - is what needs to run every time Baba's
    // savings/withdraw numbers could have moved since the last visit (interest applied,
    // a withdrawal happened elsewhere, budget sliders changed on the Expenses panel, etc).
    private void OnEnable()
    {
        EnsureFinance();
        if (finance == null) return;

        RefreshSavingsDisplay();
        SetupContributionSlider();
    }

    private void WireListenersOnce()
    {
        if (_listenersWired) return;
        _listenersWired = true;

        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmPressed);

        SetupWithdrawButtons();

        if (contributionSlider != null)
            contributionSlider.onValueChanged.AddListener(OnContributionSliderChanged);

        if (contributionValueInput != null)
            contributionValueInput.onEndEdit.AddListener(OnContributionInputEndEdit);
    }

    private void OnConfirmPressed()
    {
        if (finance != null && contributionSlider != null)
            finance.generalSavingsMonthly = contributionSlider.value;

        UIManager.Instance.CloseSavingsPanel();
    }

    private void SetupContributionSlider()
    {
        if (contributionSlider == null || finance == null) return;

        float max = GameManager.Instance.GetSavingsSliderMax(
            finance.groceries, finance.transport, finance.utilities);

        contributionSlider.minValue = 0f;
        contributionSlider.maxValue = max;
        contributionSlider.SetValueWithoutNotify(
            Mathf.Clamp(finance.generalSavingsMonthly, 0f, max));

        RefreshContributionText(contributionSlider.value);
    }

    private void RefreshContributionText(float value)
    {
        if (contributionValueText != null)
            contributionValueText.text = $"{GameUtils.FormatMoney(value)} / month";

        if (contributionValueInput != null && !contributionValueInput.isFocused)
            contributionValueInput.SetTextWithoutNotify($"{value:F0}");
    }

    private void OnContributionSliderChanged(float value)
    {
        RefreshContributionText(value);
    }

    private void OnContributionInputEndEdit(string text)
    {
        if (contributionSlider == null) return;
        if (!float.TryParse(text, out float f)) return;

        contributionSlider.value = Mathf.Clamp(f, contributionSlider.minValue, contributionSlider.maxValue);
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