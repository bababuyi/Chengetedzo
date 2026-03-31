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
        float scale = income > 0f ? maxBarHeight / income : 0f;

        //Income
        SetBarHeight(incomeColumn.barRoot, income * scale);
        if (incomeColumn.valueText != null) incomeColumn.valueText.text = $"${Mathf.RoundToInt(income)}";

        //Expense bar
        SetStackedBar(totalExpenses * scale,
            housing, groceries, transport, utilities, schoolFees, insurance, eventLosses,
            totalExpenses, scale);
        if (expensesColumn.valueText != null) expensesColumn.valueText.text = $"${Mathf.RoundToInt(totalExpenses)}";

        //Leftover bar
        SetBarHeight(leftoverColumn.barRoot, leftover * scale);
        if (leftoverColumn.valueText != null) leftoverColumn.valueText.text = $"${Mathf.RoundToInt(leftover)}";

        // Legend
        SetLegendRow(housingLegend, housingLegendValue, housing, "Housing");
        SetLegendRow(groceriesLegend, groceriesLegendValue, groceries, "Groceries");
        SetLegendRow(transportLegend, transportLegendValue, transport, "Transport");
        SetLegendRow(utilitiesLegend, utilitiesLegendValue, utilities, "Utilities");
        SetLegendRow(schoolFeesLegend, schoolFeesLegendValue, schoolFees, "School fees");
        SetLegendRow(insuranceLegend, insuranceLegendValue, insurance, "Insurance");
        SetLegendRow(eventLegend, eventLegendValue, eventLosses, "Events");
    }

    private void SetBarHeight(RectTransform bar, float height)
    {
        if (bar == null) return;
        var sd = bar.sizeDelta;
        sd.y = Mathf.Max(0f, height);
        bar.sizeDelta = sd;
    }

    private void SetStackedBar(
        float totalBarHeight,
        float housing, float groceries, float transport, float utilities,
        float schoolFees, float insurance, float eventLosses,
        float totalExpenses, float scale)
    {
        SetSegment(housingSegment, housing, totalExpenses, scale);
        SetSegment(groceriesSegment, groceries, totalExpenses, scale);
        SetSegment(transportSegment, transport, totalExpenses, scale);
        SetSegment(utilitiesSegment, utilities, totalExpenses, scale);
        SetSegment(schoolFeesSegment, schoolFees, totalExpenses, scale);
        SetSegment(insuranceSegment, insurance, totalExpenses, scale);
        SetSegment(eventSegment, eventLosses, totalExpenses, scale);
    }

    private void SetSegment(Image seg, float value, float total, float scale)
    {
        if (seg == null) return;
        bool visible = value > 0.01f;
        seg.gameObject.SetActive(visible);
        if (!visible) return;
        var sd = seg.rectTransform.sizeDelta;
        sd.y = value * scale;
        seg.rectTransform.sizeDelta = sd;
    }

    private void SetLegendRow(GameObject row, TMP_Text valueText, float amount, string label)
    {
        if (row == null) return;
        row.SetActive(amount > 0.01f);
        if (valueText != null && amount > 0.01f)
            valueText.text = $"${Mathf.RoundToInt(amount)}";
    }
}