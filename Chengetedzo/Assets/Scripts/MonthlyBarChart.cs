using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MonthlyBarChart : MonoBehaviour
{
    [System.Serializable]
    public class BarColumn
    {
        public RectTransform barRoot;
        public TMP_Text labelText;
        public TMP_Text valueText;
    }

    [Header("Columns")]
    public BarColumn incomeColumn;
    public BarColumn expensesColumn;
    public BarColumn leftoverColumn;

    [Header("Expense Segment Images (bottom to top order)")]
    public Image housingSegment;
    public Image groceriesSegment;
    public Image transportSegment;
    public Image utilitiesSegment;
    public Image schoolFeesSegment;
    public Image insuranceSegment;
    public Image eventSegment;

    [Header("Legend Rows")]
    public GameObject housingLegend;
    public GameObject groceriesLegend;
    public GameObject transportLegend;
    public GameObject utilitiesLegend;
    public GameObject schoolFeesLegend;
    public GameObject insuranceLegend;
    public GameObject eventLegend;

    [Header("Legend Value Texts")]
    public TMP_Text housingLegendValue;
    public TMP_Text groceriesLegendValue;
    public TMP_Text transportLegendValue;
    public TMP_Text utilitiesLegendValue;
    public TMP_Text schoolFeesLegendValue;
    public TMP_Text insuranceLegendValue;
    public TMP_Text eventLegendValue;

    [Header("Settings")]
    public float maxBarHeight = 140f;

    // Matching BudgetPieChart's palette exactly
    private static readonly Color HousingColor = new Color(0.85f, 0.33f, 0.24f);
    private static readonly Color GroceriesColor = new Color(0.93f, 0.58f, 0.20f);
    private static readonly Color TransportColor = new Color(0.95f, 0.80f, 0.10f);
    private static readonly Color UtilitiesColor = new Color(0.60f, 0.40f, 0.80f);
    private static readonly Color SchoolFeesColor = new Color(0.20f, 0.60f, 0.86f);
    private static readonly Color InsuranceColor = new Color(0.24f, 0.70f, 0.44f);
    private static readonly Color EventColor = new Color(0.64f, 0.17f, 0.17f);

    public void Render(
        float income,
        float housing,
        float groceries,
        float transport,
        float utilities,
        float schoolFees,
        float insurance,
        float eventLosses)
    {
        float totalExpenses = housing + groceries + transport + utilities + schoolFees + insurance + eventLosses;
        float leftover = Mathf.Max(0f, income - totalExpenses);
        float maxValue = Mathf.Max(income, totalExpenses, leftover);
        float scale = maxValue > 0f ? maxBarHeight / maxValue : 0f;

        // Income bar
        SetBarHeight(incomeColumn.barRoot, income * scale);
        if (incomeColumn.valueText != null)
            incomeColumn.valueText.text = $"${Mathf.RoundToInt(income)}";

        // Expense bar (stacked + coloured)
        SetStackedBar(housing, groceries, transport, utilities, schoolFees, insurance, eventLosses, totalExpenses, scale);
        if (expensesColumn.valueText != null)
            expensesColumn.valueText.text = $"${Mathf.RoundToInt(totalExpenses)}";

        // Leftover bar
        SetBarHeight(leftoverColumn.barRoot, leftover * scale);
        if (leftoverColumn.valueText != null)
            leftoverColumn.valueText.text = $"${Mathf.RoundToInt(leftover)}";

        // Legend rows — name + amount together
        SetLegendRow(housingLegend, housingLegendValue, HousingColor, "Housing", housing);
        SetLegendRow(groceriesLegend, groceriesLegendValue, GroceriesColor, "Groceries", groceries);
        SetLegendRow(transportLegend, transportLegendValue, TransportColor, "Transport", transport);
        SetLegendRow(utilitiesLegend, utilitiesLegendValue, UtilitiesColor, "Utilities", utilities);
        SetLegendRow(schoolFeesLegend, schoolFeesLegendValue, SchoolFeesColor, "School fees", schoolFees);
        SetLegendRow(insuranceLegend, insuranceLegendValue, InsuranceColor, "Insurance", insurance);
        SetLegendRow(eventLegend, eventLegendValue, EventColor, "Events", eventLosses);
    }

    private void SetBarHeight(RectTransform bar, float height)
    {
        if (bar == null) return;

        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchorMin = new Vector2(0.5f, 0.25f);
        bar.anchorMax = new Vector2(0.5f, 0.25f);
        bar.anchoredPosition = new Vector2(0f, 0f);

        var sd = bar.sizeDelta;
        sd.y = Mathf.Max(0f, height);
        bar.sizeDelta = sd;
    }

    private void SetStackedBar(
        float housing, float groceries, float transport, float utilities,
        float schoolFees, float insurance, float eventLosses,
        float totalExpenses, float scale)
    {
        SetSegment(housingSegment, housing, HousingColor, totalExpenses, scale);
        SetSegment(groceriesSegment, groceries, GroceriesColor, totalExpenses, scale);
        SetSegment(transportSegment, transport, TransportColor, totalExpenses, scale);
        SetSegment(utilitiesSegment, utilities, UtilitiesColor, totalExpenses, scale);
        SetSegment(schoolFeesSegment, schoolFees, SchoolFeesColor, totalExpenses, scale);
        SetSegment(insuranceSegment, insurance, InsuranceColor, totalExpenses, scale);
        SetSegment(eventSegment, eventLosses, EventColor, totalExpenses, scale);
    }

    private void SetSegment(Image seg, float value, Color color, float total, float scale)
    {
        if (seg == null) return;

        bool visible = value > 0.01f;
        seg.gameObject.SetActive(visible);
        if (!visible) return;

        seg.color = color;

        var sd = seg.rectTransform.sizeDelta;
        sd.y = value * scale;
        seg.rectTransform.sizeDelta = sd;
    }

    private void SetLegendRow(GameObject row, TMP_Text valueText, Color color, string label, float amount)
    {
        if (row == null) return;

        bool visible = amount > 0.01f;
        row.SetActive(visible);
        if (!visible) return;

        // Tint the row's swatch image if one exists
        var swatch = row.GetComponentInChildren<Image>();
        if (swatch != null)
            swatch.color = color;

        if (valueText != null)
            valueText.text = $"{label}   -${Mathf.RoundToInt(amount)}";
    }
}