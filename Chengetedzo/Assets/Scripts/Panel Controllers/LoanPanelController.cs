using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoanPanelController : MonoBehaviour
{
    [Header("References")]
    public LoanManager loanManager;

    [Header("UI Elements")]
    public TextMeshProUGUI borrowingPowerText;
    public TextMeshProUGUI loanBalanceText;
    public TextMeshProUGUI repaymentAmountText;

    public Slider repaymentSlider;
    public TextMeshProUGUI repaymentValueText;

    [Header("UI Buttons")]
    public Button borrow100Button;
    public Button borrow250Button;
    public Button borrow500Button;

    public Button continueButton;

    private void Start()
    {
        // Slider setup
        repaymentSlider.minValue = 0.05f;
        repaymentSlider.maxValue = 0.25f;
        repaymentSlider.value = loanManager.repaymentRate;

        repaymentSlider.onValueChanged.AddListener(OnRepaymentChanged);

        // Borrow buttons
        borrow100Button.onClick.AddListener(() => TryBorrow(100));
        borrow250Button.onClick.AddListener(() => TryBorrow(250));
        borrow500Button.onClick.AddListener(() => TryBorrow(500));
        continueButton.onClick.AddListener(OnContinueClicked);

        RefreshUI();
    }

    private void OnRepaymentChanged(float value)
    {
        loanManager.repaymentRate = value;

        repaymentValueText.text =$"Repayment Rate: {loanManager.repaymentRate * 100f:F0}%";

        UpdateRepaymentPreview();
    }

    private void TryBorrow(float amount)
    {
        loanManager.Borrow(amount);
        RefreshUI();
    }

    public void RefreshUI()
    {
        borrowingPowerText.text =
            $"Borrowing Power: ${loanManager.borrowingPower:F0}";

        loanBalanceText.text =
            $"Loan Balance: ${loanManager.loanBalance:F0}";

        borrow100Button.interactable = loanManager.borrowingPower >= 100;
        borrow250Button.interactable = loanManager.borrowingPower >= 250;
        borrow500Button.interactable = loanManager.borrowingPower >= 500;

        repaymentSlider.SetValueWithoutNotify(loanManager.repaymentRate);
        repaymentValueText.text =
            $"{loanManager.repaymentRate * 100f:F0}%";

        UpdateRepaymentPreview();
    }

    private void OnContinueClicked()
    {
        UIManager.Instance.HideAllPanels();

        GameManager.Instance.BeginMonthlySimulation();
    }

    private void UpdateRepaymentPreview()
    {
        if (loanManager.loanBalance <= 0f)
        {
            repaymentAmountText.text = "No active loan";
            return;
        }

        float amount =
            loanManager.loanBalance * loanManager.repaymentRate;

        repaymentAmountText.text =
            $"Monthly Repayment: ${amount:F0}";
    }
}
