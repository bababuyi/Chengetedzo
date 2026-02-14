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
        if (loanManager == null)
        {
            Debug.LogError("[LoanPanel] LoanManager not assigned.");
            enabled = false;
            return;
        }

        if (repaymentSlider == null ||
            borrow100Button == null ||
            borrow250Button == null ||
            borrow500Button == null ||
            continueButton == null)
        {
            Debug.LogError("[LoanPanel] UI references missing.");
            enabled = false;
            return;
        }

        // Slider setup (prefer manager config if available)
        repaymentSlider.minValue = loanManager.minRepaymentRate;
        repaymentSlider.maxValue = loanManager.maxRepaymentRate;
        repaymentSlider.value = loanManager.repaymentRate;

        repaymentSlider.onValueChanged.AddListener(OnRepaymentChanged);

        borrow100Button.onClick.AddListener(() => TryBorrow(100));
        borrow250Button.onClick.AddListener(() => TryBorrow(250));
        borrow500Button.onClick.AddListener(() => TryBorrow(500));
        continueButton.onClick.AddListener(OnContinueClicked);

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (repaymentSlider != null)
            repaymentSlider.onValueChanged.RemoveListener(OnRepaymentChanged);

        if (borrow100Button != null)
            borrow100Button.onClick.RemoveAllListeners();

        if (borrow250Button != null)
            borrow250Button.onClick.RemoveAllListeners();

        if (borrow500Button != null)
            borrow500Button.onClick.RemoveAllListeners();

        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    private void OnRepaymentChanged(float value)
    {
        loanManager.SetRepaymentRate(value);

        repaymentValueText.text =$"Repayment Rate: {loanManager.repaymentRate * 100f:F0}%";

        UpdateRepaymentPreview();
    }

    private void TryBorrow(float amount)
    {
        borrow100Button.interactable = false;
        borrow250Button.interactable = false;
        borrow500Button.interactable = false;

        if (loanManager == null)
            return;

        bool success = loanManager.Borrow(amount);

        if (!success)
        {
            repaymentAmountText.text = "Borrow request denied.";
            return;
        }

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (loanManager == null)
            return;

        if (borrowingPowerText != null)
            borrowingPowerText.text =
                $"Borrowing Power: ${loanManager.borrowingPower:F0}";

        if (loanBalanceText != null)
            loanBalanceText.text =
                $"Loan Balance: ${loanManager.loanBalance:F0}";

        if (borrow100Button != null)
            borrow100Button.interactable = loanManager.borrowingPower >= 100;

        if (borrow250Button != null)
            borrow250Button.interactable = loanManager.borrowingPower >= 250;

        if (borrow500Button != null)
            borrow500Button.interactable = loanManager.borrowingPower >= 500;

        if (repaymentSlider != null)
            repaymentSlider.SetValueWithoutNotify(loanManager.repaymentRate);

        if (repaymentValueText != null)
            repaymentValueText.text =
                $"Repayment Rate: {loanManager.repaymentRate * 100f:F0}%";

        UpdateRepaymentPreview();
    }

    private void OnContinueClicked()
    {
        if (GameManager.Instance == null)
            return;

        if (GameManager.Instance.CurrentPhase != GameManager.GamePhase.Loan)
            return;

        if (UIManager.Instance != null)
            UIManager.Instance.HideAllPanels();

        GameManager.Instance.BeginMonthlySimulation();
    }

    private void UpdateRepaymentPreview()
    {
        if (loanManager == null)
            return;

        if (loanManager.loanBalance <= 0f)
        {
            repaymentAmountText.text = "No active loan";
            return;
        }

        float amount =
            loanManager.loanBalance * loanManager.repaymentRate;

        repaymentAmountText.text =
            $"Monthly Repayment ({loanManager.repaymentRate * 100f:F0}%): ${amount:F0}";
    }
}
