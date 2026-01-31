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
        repaymentValueText.text = $"{value * 100f:F0}%";
    }

    private void TryBorrow(float amount)
    {
        loanManager.Borrow(amount);
        RefreshUI();
    }

    public void RefreshUI()
    {
        borrowingPowerText.text = $"Borrowing Power: ${loanManager.borrowingPower:F0}";
        loanBalanceText.text = $"Loan Balance: ${loanManager.loanBalance:F0}";

        // Disable buttons if not eligible
        borrow100Button.interactable = loanManager.borrowingPower >= 100;
        borrow250Button.interactable = loanManager.borrowingPower >= 250;
        borrow500Button.interactable = loanManager.borrowingPower >= 500;
        repaymentValueText.text = $"{loanManager.repaymentRate * 100f:F0}%";

        bool canBorrow =
        !loanManager.BorrowedThisMonth &&
        loanManager.borrowingPower >= 100;

        borrow100Button.interactable = canBorrow;
        borrow250Button.interactable =
            !loanManager.BorrowedThisMonth &&
            loanManager.borrowingPower >= 250;

        borrow500Button.interactable =
            !loanManager.BorrowedThisMonth &&
            loanManager.borrowingPower >= 500;
    }

    private void OnContinueClicked()
    {
        UIManager.Instance.HideAllPanels();

        GameManager.Instance.BeginMonthlySimulation();
    }
}
