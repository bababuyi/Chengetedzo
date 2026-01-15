using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class ExpenseTier
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

    public void Init()
    {
        // Clear old listeners
        rentSlider.onValueChanged.RemoveAllListeners();
        groceriesSlider.onValueChanged.RemoveAllListeners();
        transportSlider.onValueChanged.RemoveAllListeners();
        utilitiesSlider.onValueChanged.RemoveAllListeners();

        // Wire fresh listeners
        rentSlider.onValueChanged.AddListener(_ => UpdateRent());
        groceriesSlider.onValueChanged.AddListener(_ => UpdateGroceries());
        transportSlider.onValueChanged.AddListener(_ => UpdateTransport());
        utilitiesSlider.onValueChanged.AddListener(_ => UpdateUtilities());

        RefreshAll();
    }

    private void RefreshAll()
    {
        UpdateRent();
        UpdateGroceries();
        UpdateTransport();
        UpdateUtilities();
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

    private string GetTierLabel(float value, ExpenseTier tier)
    {
        if (tier == null) return "—";
        if (value <= tier.lowMax) return "Low";
        if (value <= tier.mediumMax) return "Medium";
        return "High";
    }
}