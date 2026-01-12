using UnityEngine;
using TMPro;

public class SetupController : MonoBehaviour
{
    [Header("Income")]
    public TMP_InputField minIncomeInput;
    public TMP_InputField maxIncomeInput;
    public UnityEngine.UI.Toggle stableIncomeToggle;

    [Header("Expenses")]
    //public TieredExpenseSlider rentSlider;
    //public TieredExpenseSlider groceriesSlider;
    //public TieredExpenseSlider transportSlider;
    //public TieredExpenseSlider utilitiesSlider;

    [Header("School Fees")]
    public UnityEngine.UI.Toggle schoolFeesToggle;
    public TMP_InputField feePerTermInput;
    public TMP_InputField savedAmountInput;

    public void OnConfirmSetup()
    {
        // --- Income ---
        float minIncome = float.Parse(minIncomeInput.text);
        float maxIncome = float.Parse(maxIncomeInput.text);

        var finance = GameManager.Instance.financeManager;

        finance.minIncome = minIncome;
        finance.maxIncome = maxIncome;
        finance.isIncomeStable = stableIncomeToggle.isOn;

        // --- Expenses ---
        //finance.rent = rentSlider.CurrentValue;
        //finance.groceries = groceriesSlider.CurrentValue;
        //finance.transport = transportSlider.CurrentValue;
        //finance.utilities = utilitiesSlider.CurrentValue;

        // --- School Fees ---
        if (schoolFeesToggle.isOn)
        {
            finance.schoolFeesPerTerm = float.Parse(feePerTermInput.text);
            finance.schoolFeeSavingsBalance = float.Parse(savedAmountInput.text);
        }
        else
        {
            finance.schoolFeesPerTerm = 0f;
            finance.schoolFeeSavingsBalance = 0f;
        }

        // Close setup start game
        UIManager.Instance.HideAllPanels();
        GameManager.Instance.BeginSimulation();
    }
}