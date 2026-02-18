using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

[System.Serializable]
public struct ExpenseTier
{
    public float lowMax;
    public float mediumMax;
}

public class ExpensesPanelController : MonoBehaviour
{
    [Header("Sliders")]
    public Slider rentSlider;
    public Slider groceriesSlider;
    public Slider transportSlider;
    public Slider utilitiesSlider;

    [Header("House Cost (Input)")]
    public TMP_InputField houseCostInput;
    public TMP_Text houseCostValueText;

    [Header("Value Texts")]
    public TMP_Text rentValueText;
    public TMP_Text groceriesValueText;
    public TMP_Text transportValueText;
    public TMP_Text utilitiesValueText;

    [Header("Tier Labels")]
    public TMP_Text rentTierText;
    public TMP_Text groceriesTierText;
    public TMP_Text transportTierText;
    public TMP_Text utilitiesTierText;

    [Header("Expense Tiers")]
    public ExpenseTier rentTier;
    public ExpenseTier groceriesTier;
    public ExpenseTier transportTier;
    public ExpenseTier utilitiesTier;

    [Header("Housing UI")]
    public GameObject rentSliderGroup;
    public GameObject houseCostInputGroup;
    public TMP_Text houseCostWarningText;

    [Header("School Fees")]
    public Toggle schoolFeesToggle;
    public TMP_InputField schoolFeesInput;


    private const float MIN_HOUSE_COST = 15000f;

    public void Init()
    {
        if (rentSlider == null || groceriesSlider == null ||
    transportSlider == null || utilitiesSlider == null ||
    houseCostInput == null)
        {
            Debug.LogError("[ExpensesPanelController] Missing UI references.");
            return;
        }

        // Clear old listeners
        rentSlider.onValueChanged.RemoveAllListeners();
        groceriesSlider.onValueChanged.RemoveAllListeners();
        transportSlider.onValueChanged.RemoveAllListeners();
        utilitiesSlider.onValueChanged.RemoveAllListeners();
        houseCostInput.onValueChanged.RemoveAllListeners();

        // Sliders
        rentSlider.onValueChanged.AddListener(_ => UpdateRent());
        groceriesSlider.onValueChanged.AddListener(_ => UpdateGroceries());
        transportSlider.onValueChanged.AddListener(_ => UpdateTransport());
        utilitiesSlider.onValueChanged.AddListener(_ => UpdateUtilities());

        // Input field
        houseCostInput.onValueChanged.AddListener(_ => UpdateHouseCost());

        if (string.IsNullOrEmpty(houseCostInput.text))
            houseCostInput.text = MIN_HOUSE_COST.ToString("F0");

        RefreshAll();
    }

    private void Awake()
    {
        if (rentSliderGroup != null)
            rentSliderGroup.SetActive(true);

        if (houseCostInputGroup != null)
            houseCostInputGroup.SetActive(false);

        Init();
    }

    private void RefreshAll()
    {
        UpdateRent();
        UpdateGroceries();
        UpdateTransport();
        UpdateUtilities();
        UpdateHouseCost();
    }

    private void UpdateRent()
    {
        float value = rentSlider.value;
        rentValueText.text = $"${value:F0}";
        rentTierText.text = GetTierLabel(value, rentTier);
    }

    private void UpdateGroceries()
    {
        float value = groceriesSlider.value;
        groceriesValueText.text = $"${value:F0}";
        groceriesTierText.text = GetTierLabel(value, groceriesTier);
    }

    private void UpdateTransport()
    {
        float value = transportSlider.value;
        transportValueText.text = $"${value:F0}";
        transportTierText.text = GetTierLabel(value, transportTier);
    }

    private void UpdateUtilities()
    {
        float value = utilitiesSlider.value;
        utilitiesValueText.text = $"${value:F0}";
        utilitiesTierText.text = GetTierLabel(value, utilitiesTier);
    }

    private void UpdateHouseCost()
    {
        if (float.TryParse(houseCostInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            houseCostValueText.text = $"${value:F0}";

            if (value < MIN_HOUSE_COST)
            {
                houseCostWarningText.gameObject.SetActive(true);
                houseCostWarningText.text =
                    $"Suggested minimum is ${MIN_HOUSE_COST:F0}";
            }
            else
            {
                houseCostWarningText.gameObject.SetActive(false);
            }
        }
        else
        {
            houseCostValueText.text = "$—";
            houseCostWarningText.gameObject.SetActive(false);
        }
    }

    private string GetTierLabel(float value, ExpenseTier tier)
    {
        if (value <= tier.lowMax) return "Low";
        if (value <= tier.mediumMax) return "Medium";
        return "High";
    }

    public void ApplyExpensesToFinance(FinanceManager finance)
    {
        if (finance == null)
        {
            Debug.LogError("[ExpensesPanelController] FinanceManager is null.");
            return;
        }

        // HOUSE OWNED — value is for insurance ONLY
        if (finance.assets.hasHouse)
        {
            if (float.TryParse(houseCostInput.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float houseValue))
            {
                houseValue = Mathf.Max(MIN_HOUSE_COST, houseValue);
                finance.houseInsuredValue = houseValue;
            }
        }
        else
        {
            finance.rentCost = rentSlider.value;
        }

        if (GameManager.Instance.setupData.hasSchoolFees)
        {
            finance.schoolFeesPerTerm = GameManager.Instance.setupData.schoolFeesAmount;
        }
        else
        {
            finance.schoolFeesPerTerm = 0f;
        }

        // Monthly living costs
        finance.groceries = groceriesSlider.value;
        finance.transport = transportSlider.value;
        finance.utilities = utilitiesSlider.value;
    }

    public void SetHousingMode(bool ownsHouse)
    {
        if (rentSliderGroup != null)
            rentSliderGroup.SetActive(!ownsHouse);

        if (houseCostInputGroup != null)
            houseCostInputGroup.SetActive(ownsHouse);

        RefreshAll();
    }

    public float GetEstimatedMonthlyExpenses()
    {
        float total = 0f;

        if (houseCostInputGroup.activeSelf)
        {
            total += 0f;
        }
        else
        {
            total += rentSlider.value;
        }

        total += groceriesSlider.value;
        total += transportSlider.value;
        total += utilitiesSlider.value;
        if (GameManager.Instance.setupData.hasSchoolFees)
        {
            total += GameManager.Instance.setupData.schoolFeesAmount;
        }
        return total;
    }
}
