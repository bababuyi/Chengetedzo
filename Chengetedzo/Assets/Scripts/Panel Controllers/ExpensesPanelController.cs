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

    [Header("Value Inputs")]
    public TMP_InputField rentValueInput;
    public TMP_InputField groceriesValueInput;
    public TMP_InputField transportValueInput;
    public TMP_InputField utilitiesValueInput;

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

    [Header("Adjustment Readout")]
    public TMP_Text incomeRangeText;
    public TMP_Text expensesTotalText;

    [Header("Budget Bar")]
    public GameObject budgetBarContainer; // parent of the whole budget-bar visual — shown/hidden by Enter/ExitAdjustmentMode
    public RectTransform budgetBarFill;
    public RectTransform belowBaseSegment;
    public RectTransform aboveBaseSegment;
    public RectTransform baseLineMarker;
    public Button confirmAdjustmentButton;

    [Header("Per-Category Baseline Ticks (optional)")]
    public RectTransform groceriesBaselineTick;
    public RectTransform transportBaselineTick;
    public RectTransform utilitiesBaselineTick;

    private void PositionBaselineTick(RectTransform tick, Slider slider, float baseline)
    {
        if (tick == null || slider == null) return;

        RectTransform track = slider.fillRect != null
            ? slider.fillRect.parent as RectTransform
            : slider.GetComponent<RectTransform>();
        if (track == null) return;

        float range = slider.maxValue - slider.minValue;
        if (range <= 0f) return;

        float fraction = Mathf.Clamp01((baseline - slider.minValue) / range);
        float trackWidth = track.rect.width;

        tick.anchoredPosition = new Vector2(fraction * trackWidth, tick.anchoredPosition.y);
    }

    private const float MIN_HOUSE_COST = 15000f;
    private bool isAdjustmentMode = false;

    // Normal-setup slider configuration, cached on EnterAdjustmentMode so ExitAdjustmentMode
    // can restore it exactly rather than guessing at defaults.
    private struct SliderRange { public float min; public float max; public float value; }
    private SliderRange _groceriesNormalRange;
    private SliderRange _transportNormalRange;
    private SliderRange _utilitiesNormalRange;
    private bool _normalRangeCached = false;

    public void EnterAdjustmentMode()
    {
        isAdjustmentMode = true;
        var gm = GameManager.Instance;

        if (rentSliderGroup != null) rentSliderGroup.SetActive(false);
        if (houseCostInputGroup != null) houseCostInputGroup.SetActive(false);
        if (schoolFeesToggle != null) schoolFeesToggle.gameObject.SetActive(false);

        if (budgetBarContainer != null) budgetBarContainer.SetActive(true);
        if (incomeRangeText != null) incomeRangeText.gameObject.SetActive(true);
        if (expensesTotalText != null) expensesTotalText.gameObject.SetActive(true);

        CacheNormalSliderRanges();

        SetupAdjustSlider(groceriesSlider, ExpenseCategory.Groceries, groceriesBaselineTick);
        SetupAdjustSlider(transportSlider, ExpenseCategory.Transport, transportBaselineTick);
        SetupAdjustSlider(utilitiesSlider, ExpenseCategory.Utilities, utilitiesBaselineTick);

        RefreshAll();
        RefreshBudgetReadout();
    }

    // Exact mirror of EnterAdjustmentMode: hides the budget bar and floor markers, hides the
    // adjustment readouts, and restores the sliders to their normal setup configuration.
    // Called from every path back into normal setup (SetupPanelController.OnPanelOpened,
    // JumpToReviewStep, ConfirmExpenseAdjustment) so adjustment-mode UI can't linger.
    public void ExitAdjustmentMode()
    {
        isAdjustmentMode = false;
        var gm = GameManager.Instance;

        bool hasHouse = gm != null && gm.financeManager != null && gm.financeManager.assets.hasHouse;
        SetHousingMode(hasHouse);
        if (schoolFeesToggle != null) schoolFeesToggle.gameObject.SetActive(true);

        if (budgetBarContainer != null) budgetBarContainer.SetActive(false);
        if (groceriesBaselineTick != null) groceriesBaselineTick.gameObject.SetActive(false);
        if (transportBaselineTick != null) transportBaselineTick.gameObject.SetActive(false);
        if (utilitiesBaselineTick != null) utilitiesBaselineTick.gameObject.SetActive(false);

        if (incomeRangeText != null) incomeRangeText.gameObject.SetActive(false);
        if (expensesTotalText != null) expensesTotalText.gameObject.SetActive(false);

        if (_normalRangeCached)
        {
            RestoreSliderRange(groceriesSlider, _groceriesNormalRange);
            RestoreSliderRange(transportSlider, _transportNormalRange);
            RestoreSliderRange(utilitiesSlider, _utilitiesNormalRange);
            _normalRangeCached = false;
        }
    }

    private void CacheNormalSliderRanges()
    {
        _groceriesNormalRange = new SliderRange { min = groceriesSlider.minValue, max = groceriesSlider.maxValue, value = groceriesSlider.value };
        _transportNormalRange = new SliderRange { min = transportSlider.minValue, max = transportSlider.maxValue, value = transportSlider.value };
        _utilitiesNormalRange = new SliderRange { min = utilitiesSlider.minValue, max = utilitiesSlider.maxValue, value = utilitiesSlider.value };
        _normalRangeCached = true;
    }

    private void RestoreSliderRange(Slider slider, SliderRange range)
    {
        if (slider == null) return;
        slider.minValue = range.min;
        slider.maxValue = range.max;
        slider.SetValueWithoutNotify(range.value);
    }

    private void SetupAdjustSlider(Slider slider, ExpenseCategory cat, RectTransform baselineTick = null)
    {
        if (slider == null) return;
        var gm = GameManager.Instance;
        float baseline = gm.GetCategoryBaseline(cat);
        float floor = gm.GetCategoryFloor(cat);
        float current = gm.GetCategoryEffective(cat);

        slider.minValue = floor;
        slider.maxValue = baseline * 2f;
        slider.SetValueWithoutNotify(Mathf.Clamp(current, floor, baseline * 2f));

        if (baselineTick != null) baselineTick.gameObject.SetActive(true);
        PositionBaselineTick(baselineTick, slider, baseline);
    }

    private void RefreshBudgetReadout()
    {
        if (!isAdjustmentMode) return;

        var gm = GameManager.Instance;
        var setup = gm.setupData;
        var finance = gm.financeManager;

        if (incomeRangeText != null)
        {
            float lo = setup.minIncome;
            float hi = setup.maxIncome > 0 ? setup.maxIncome : lo;
            incomeRangeText.text = setup.isIncomeStable
                ? $"Your income is about ${lo:F0} / month"
                : $"Your income is usually ${lo:F0} � ${hi:F0} / month";
        }

        if (expensesTotalText != null)
        {
            float housing = finance.GetHousingCost();
            float planned = housing
                + groceriesSlider.value
                + transportSlider.value
                + utilitiesSlider.value;

            if (setup.hasSchoolFees)
            {
                int childCount = Mathf.Max(0, PlayerDataManager.Instance?.Children ?? 0);
                planned += ((setup.schoolFeesAmount * childCount) * 3f) / 12f;
            }

            expensesTotalText.text = $"Planned spending: ${planned:F0} / month";
        }

        RefreshBudgetBar();
    }

    private void RefreshBudgetBar()
    {
        if (budgetBarFill == null) return;

        var gm = GameManager.Instance;
        var bar = gm.GetBudgetBarState(groceriesSlider.value, transportSlider.value, utilitiesSlider.value);

        float totalWidth = budgetBarFill.rect.width;
        float scale = bar.cap > 0f ? totalWidth / bar.cap : 0f;
        float baseLineX = bar.baseLine * scale;

        if (baseLineMarker != null)
            baseLineMarker.anchoredPosition = new Vector2(baseLineX, baseLineMarker.anchoredPosition.y);

        if (belowBaseSegment != null)
        {
            float belowWidth = bar.belowBase * scale;
            belowBaseSegment.sizeDelta = new Vector2(belowWidth, belowBaseSegment.sizeDelta.y);
            belowBaseSegment.anchoredPosition = new Vector2(baseLineX - belowWidth, belowBaseSegment.anchoredPosition.y);
        }

        if (aboveBaseSegment != null)
        {
            float aboveWidth = bar.aboveBase * scale;
            aboveBaseSegment.sizeDelta = new Vector2(aboveWidth, aboveBaseSegment.sizeDelta.y);
            aboveBaseSegment.anchoredPosition = new Vector2(baseLineX, aboveBaseSegment.anchoredPosition.y);
        }

        if (confirmAdjustmentButton != null)
            confirmAdjustmentButton.interactable = !bar.atCap;
    }

    public void ConfirmAdjustment()
    {
        if (!isAdjustmentMode)
            return;

        var gm = GameManager.Instance;
        gm.ApplyProvisionChange(ExpenseCategory.Groceries, groceriesSlider.value);
        gm.ApplyProvisionChange(ExpenseCategory.Transport, transportSlider.value);
        gm.ApplyProvisionChange(ExpenseCategory.Utilities, utilitiesSlider.value);

        isAdjustmentMode = false;
        UIManager.Instance.OnBudgetAdjustmentConfirmed();
    }

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

        if (rentValueInput != null)
            rentValueInput.onEndEdit.AddListener(v => { if (float.TryParse(v, out float f)) { rentSlider.value = Mathf.Clamp(f, rentSlider.minValue, rentSlider.maxValue); UpdateRent(); } });
        if (groceriesValueInput != null)
            groceriesValueInput.onEndEdit.AddListener(v => { if (float.TryParse(v, out float f)) { groceriesSlider.value = Mathf.Clamp(f, groceriesSlider.minValue, groceriesSlider.maxValue); UpdateGroceries(); } });
        if (transportValueInput != null)
            transportValueInput.onEndEdit.AddListener(v => { if (float.TryParse(v, out float f)) { transportSlider.value = Mathf.Clamp(f, transportSlider.minValue, transportSlider.maxValue); UpdateTransport(); } });
        if (utilitiesValueInput != null)
            utilitiesValueInput.onEndEdit.AddListener(v => { if (float.TryParse(v, out float f)) { utilitiesSlider.value = Mathf.Clamp(f, utilitiesSlider.minValue, utilitiesSlider.maxValue); UpdateUtilities(); } });

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
        if (rentValueInput != null && !rentValueInput.isFocused)
            rentValueInput.SetTextWithoutNotify($"{value:F0}");
        if (rentTierText != null) rentTierText.text = GetTierLabel(value, rentTier);
    }

    private void UpdateGroceries()
    {
        float value = groceriesSlider.value;
        if (groceriesValueInput != null && !groceriesValueInput.isFocused)
            groceriesValueInput.SetTextWithoutNotify($"{value:F0}");
        if (groceriesTierText != null) groceriesTierText.text = GetTierLabel(value, groceriesTier);
        if (isAdjustmentMode) RefreshBudgetReadout();
    }

    private void UpdateTransport()
    {
        float value = transportSlider.value;
        if (transportValueInput != null && !transportValueInput.isFocused)
            transportValueInput.SetTextWithoutNotify($"{value:F0}");
        if (transportTierText != null) transportTierText.text = GetTierLabel(value, transportTier);
        if (isAdjustmentMode) RefreshBudgetReadout();
    }

    private void UpdateUtilities()
    {
        float value = utilitiesSlider.value;
        if (utilitiesValueInput != null && !utilitiesValueInput.isFocused)
            utilitiesValueInput.SetTextWithoutNotify($"{value:F0}");
        if (utilitiesTierText != null) utilitiesTierText.text = GetTierLabel(value, utilitiesTier);
        if (isAdjustmentMode) RefreshBudgetReadout();
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
            houseCostValueText.text = "$�";
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

        // HOUSE OWNED � value is for insurance ONLY
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
    public float GetHousingCost() => houseCostInputGroup.activeSelf ? 0f : rentSlider.value;
    public float GetGroceriesCost() => groceriesSlider.value;
    public float GetTransportCost() => transportSlider.value;
    public float GetUtilitiesCost() => utilitiesSlider.value;

    public void ResetCategoryToBaseline(ExpenseCategory cat)
    {
        var gm = GameManager.Instance;
        float baseline = gm.GetCategoryBaseline(cat);

        switch (cat)
        {
            case ExpenseCategory.Groceries: groceriesSlider.value = baseline; break;
            case ExpenseCategory.Transport: transportSlider.value = baseline; break;
            case ExpenseCategory.Utilities: utilitiesSlider.value = baseline; break;
        }
    }
}
