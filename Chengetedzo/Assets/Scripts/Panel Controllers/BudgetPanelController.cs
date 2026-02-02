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

    public GameObject schoolFeesGroup;
    public Slider schoolFeeSavingsSlider;
    public TMP_Text schoolFeeSavingsValueText;

    [Header("Confirm")]
    public Button confirmButton;

    private FinanceManager finance;

    private void Start()
    {
        finance = GameManager.Instance?.financeManager;

        if (finance == null)
        {
            Debug.LogError("[BudgetPanelController] FinanceManager not ready.");
            return;
        }

        confirmButton.onClick.AddListener(ConfirmAndStart);

        savingsSlider.onValueChanged.AddListener(_ => UpdateValues());
        schoolFeeSavingsSlider.onValueChanged.AddListener(_ => UpdateValues());

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

        schoolFeesGroup.SetActive(setup.hasSchoolFees);

        if (setup.hasSchoolFees)
        {
            schoolFeeSavingsSlider.value = setup.schoolFeesAmount / 3f;
        }
        else
        {
            schoolFeeSavingsSlider.value = 0f;
        }

        UpdateValues();
    }

    private void UpdateValues()
    {
        savingsValueText.text = $"${savingsSlider.value:F0}";
        schoolFeeSavingsValueText.text = $"${schoolFeeSavingsSlider.value:F0}";
    }

    private void ConfirmAndStart()
    {
        finance.SetSchoolFeeSavings(schoolFeeSavingsSlider.value);

        UIManager.Instance.ShowForecastPanel();
        // Show forecast instead of starting simulation
        GameManager.Instance.forecastManager.GenerateForecast();
        gameObject.SetActive(false);
    }
}
