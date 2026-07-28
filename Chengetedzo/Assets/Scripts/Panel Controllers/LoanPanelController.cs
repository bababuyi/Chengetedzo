using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Two-channel loan panel: Mukando (cheap, contribution-gated, join/leave toggle) and
// Moneylender/Ndlovu (always available, expensive, no toggle) shown side by side.
// Term-based instalment model: the player picks an amount and a term (1..maxTermMonths)
// per channel; the live preview shows both the total repaid and the fixed monthly
// instalment, since the cost comparison between channels is the entire point.
//
// NOTE for the editor pass: the public fields below need UI objects created and wired
// in the Inspector - amount input, term slider, and preview text per account, plus the
// Mukando join toggle. There is no longer a repayment-rate slider; rates are fixed per
// channel and the new term slider replaces it.
public class LoanPanelController : MonoBehaviour
{
    [Header("References")]
    public LoanManager loanManager;

    [Header("Mukando")]
    public TextMeshProUGUI mukandoBalanceText;
    public TextMeshProUGUI mukandoPowerText;
    public TextMeshProUGUI mukandoStatusText; // "Not joined" / "Building standing (1/3 months)" / "Active"
    public Toggle mukandoJoinToggle;
    public TMP_InputField mukandoAmountInput;
    public Slider mukandoTermSlider;
    public TextMeshProUGUI mukandoTermValueText;   // "8 months"
    public TextMeshProUGUI mukandoPreviewText;     // "Borrow $600 over 8 months. You repay $787.50 total, $98 a month."
    public Button mukandoBorrowButton;

    [Header("Moneylender")]
    public TextMeshProUGUI moneylenderBalanceText;
    public TextMeshProUGUI moneylenderPowerText;
    public TMP_InputField moneylenderAmountInput;
    public Slider moneylenderTermSlider;
    public TextMeshProUGUI moneylenderTermValueText;
    public TextMeshProUGUI moneylenderPreviewText;
    public Button moneylenderBorrowButton;

    [Header("Feedback / Navigation")]
    public TextMeshProUGUI feedbackText;
    public Button continueButton;

    private void Start()
    {
        if (loanManager == null)
        {
            Debug.LogError("[LoanPanel] LoanManager not assigned.");
            enabled = false;
            return;
        }

        if (continueButton == null)
        {
            Debug.LogError("[LoanPanel] UI references missing.");
            enabled = false;
            return;
        }

        if (mukandoJoinToggle != null)
            mukandoJoinToggle.onValueChanged.AddListener(OnMukandoToggleChanged);

        SetupAccountControls(
            loanManager.mukando, mukandoAmountInput, mukandoTermSlider,
            mukandoTermValueText, mukandoPreviewText, mukandoBorrowButton);

        SetupAccountControls(
            loanManager.moneylender, moneylenderAmountInput, moneylenderTermSlider,
            moneylenderTermValueText, moneylenderPreviewText, moneylenderBorrowButton);

        continueButton.onClick.AddListener(OnContinueClicked);

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (mukandoJoinToggle != null)
            mukandoJoinToggle.onValueChanged.RemoveListener(OnMukandoToggleChanged);

        mukandoAmountInput?.onValueChanged.RemoveAllListeners();
        mukandoTermSlider?.onValueChanged.RemoveAllListeners();
        mukandoBorrowButton?.onClick.RemoveAllListeners();

        moneylenderAmountInput?.onValueChanged.RemoveAllListeners();
        moneylenderTermSlider?.onValueChanged.RemoveAllListeners();
        moneylenderBorrowButton?.onClick.RemoveAllListeners();

        if (continueButton != null)
            continueButton.onClick.RemoveListener(OnContinueClicked);
    }

    private void SetupAccountControls(
        LoanAccount account, TMP_InputField amountInput, Slider termSlider,
        TextMeshProUGUI termValueText, TextMeshProUGUI previewText, Button borrowButton)
    {
        if (termSlider != null)
        {
            termSlider.minValue = 1;
            termSlider.maxValue = Mathf.Max(1, account.maxTermMonths);
            termSlider.wholeNumbers = true;
            termSlider.value = 1;
            termSlider.onValueChanged.AddListener(_ => RefreshPreview(account, amountInput, termSlider, termValueText, previewText));
        }

        if (amountInput != null)
            amountInput.onValueChanged.AddListener(_ => RefreshPreview(account, amountInput, termSlider, termValueText, previewText));

        if (borrowButton != null)
            borrowButton.onClick.AddListener(() => TryBorrow(account, amountInput, termSlider));

        RefreshPreview(account, amountInput, termSlider, termValueText, previewText);
    }

    private void OnMukandoToggleChanged(bool isOn)
    {
        if (loanManager == null) return;

        if (isOn) loanManager.JoinMukando();
        else loanManager.LeaveMukando();

        RefreshUI();
    }

    private float GetRequestedAmount(TMP_InputField amountInput, LoanAccount account)
    {
        if (amountInput == null) return 0f;
        if (!float.TryParse(amountInput.text, out float amount)) return 0f;
        return Mathf.Clamp(amount, 0f, account.borrowingPower);
    }

    private void RefreshPreview(
        LoanAccount account, TMP_InputField amountInput, Slider termSlider,
        TextMeshProUGUI termValueText, TextMeshProUGUI previewText)
    {
        int term = termSlider != null ? Mathf.Max(1, (int)termSlider.value) : 1;
        float amount = GetRequestedAmount(amountInput, account);

        if (termValueText != null)
            termValueText.text = $"{term} month{(term == 1 ? "" : "s")}";

        if (previewText == null) return;

        if (amount <= 0f)
        {
            previewText.text = $"Enter an amount to borrow from {account.label} (up to {GameUtils.FormatMoney(account.borrowingPower)}).";
            return;
        }

        float owed = amount * (1f + account.interestRate);
        float instalment = owed / term;

        previewText.text =
            $"Borrow {GameUtils.FormatMoney(amount)} over {term} month{(term == 1 ? "" : "s")}. " +
            $"You repay {GameUtils.FormatMoney(owed)} total, {GameUtils.FormatMoney(instalment)} a month.";
    }

    private void TryBorrow(LoanAccount account, TMP_InputField amountInput, Slider termSlider)
    {
        if (loanManager == null) return;

        float amount = GetRequestedAmount(amountInput, account);
        int term = termSlider != null ? Mathf.Max(1, (int)termSlider.value) : 1;

        if (amount <= 0f)
        {
            if (feedbackText != null)
                feedbackText.text = "Enter an amount first.";
            return;
        }

        bool success = loanManager.Borrow(amount, account, term);

        if (!success)
        {
            if (feedbackText != null)
                feedbackText.text = $"Could not borrow from {account.label}.";
            RefreshUI();
            return;
        }

        if (amountInput != null)
            amountInput.text = "";

        if (feedbackText != null)
            feedbackText.text = $"Borrowed {GameUtils.FormatMoney(amount)} from {account.label}.";

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (loanManager == null)
            return;

        RefreshMukando();
        RefreshMoneylender();
    }

    private void RefreshMukando()
    {
        var mukando = loanManager.mukando;

        if (mukandoBalanceText != null)
            mukandoBalanceText.text = mukando.balance > 0f
                ? $"Balance: {GameUtils.FormatMoney(mukando.balance)} ({mukando.termMonths} months left, {GameUtils.FormatMoney(mukando.monthlyInstalment)}/month)"
                : "Balance: none";

        if (mukandoPowerText != null)
            mukandoPowerText.text = $"Available: {GameUtils.FormatMoney(mukando.borrowingPower)}";

        if (mukandoJoinToggle != null)
            mukandoJoinToggle.SetIsOnWithoutNotify(loanManager.mukandoJoined);

        if (mukandoStatusText != null)
        {
            if (!loanManager.mukandoJoined)
                mukandoStatusText.text = "Not joined";
            else if (mukando.monthsContributed < 3)
                mukandoStatusText.text = $"Building standing ({mukando.monthsContributed}/3 months)";
            else
                mukandoStatusText.text = "Active";
        }

        if (mukandoTermSlider != null)
        {
            mukandoTermSlider.maxValue = Mathf.Max(1, mukando.maxTermMonths);
            RefreshPreview(mukando, mukandoAmountInput, mukandoTermSlider, mukandoTermValueText, mukandoPreviewText);
        }

        if (mukandoBorrowButton != null)
            mukandoBorrowButton.interactable = mukando.borrowingPower > 0f;
    }

    private void RefreshMoneylender()
    {
        var moneylender = loanManager.moneylender;

        if (moneylenderBalanceText != null)
            moneylenderBalanceText.text = moneylender.balance > 0f
                ? $"Balance: {GameUtils.FormatMoney(moneylender.balance)} ({moneylender.termMonths} months left, {GameUtils.FormatMoney(moneylender.monthlyInstalment)}/month)"
                : "Balance: none";

        if (moneylenderPowerText != null)
            moneylenderPowerText.text = $"Available: {GameUtils.FormatMoney(moneylender.borrowingPower)}";

        if (moneylenderTermSlider != null)
        {
            moneylenderTermSlider.maxValue = Mathf.Max(1, moneylender.maxTermMonths);
            RefreshPreview(moneylender, moneylenderAmountInput, moneylenderTermSlider, moneylenderTermValueText, moneylenderPreviewText);
        }

        if (moneylenderBorrowButton != null)
            moneylenderBorrowButton.interactable = moneylender.borrowingPower > 0f;
    }

    private void OnContinueClicked()
    {
        UIManager.Instance.CloseLoanPanel();
    }
}
