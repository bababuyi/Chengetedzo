using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BudgetPieChart : MonoBehaviour
{
    [Header("Slices (Radial 360 Images, stacked in this order)")]
    public Image housingSlice;
    public Image groceriesSlice;
    public Image transportSlice;
    public Image utilitiesSlice;
    public Image schoolFeesSlice;
    public Image savingsSlice;
    public Image surplusSlice;

    [Header("Colors")]
    public Color housingColor = new Color(0.85f, 0.33f, 0.24f); // red
    public Color groceriesColor = new Color(0.93f, 0.58f, 0.20f); // orange
    public Color transportColor = new Color(0.95f, 0.80f, 0.10f); // yellow
    public Color utilitiesColor = new Color(0.60f, 0.40f, 0.80f); // purple
    public Color schoolFeesColor = new Color(0.20f, 0.60f, 0.86f); // blue
    public Color savingsColor = new Color(0.24f, 0.70f, 0.44f); // green
    public Color surplusColor = new Color(0.60f, 0.85f, 0.60f); // light green
    public Color shortfallColor = new Color(0.55f, 0.10f, 0.10f); // dark red

    [Header("Legend Rows")]
    public LegendRow housingRow;
    public LegendRow groceriesRow;
    public LegendRow transportRow;
    public LegendRow utilitiesRow;
    public LegendRow schoolFeesRow;
    public LegendRow savingsRow;
    public LegendRow surplusRow;

    [System.Serializable]
    public class LegendRow
    {
        public Image colorSwatch;
        public TMP_Text label;
    }

    public void Render(
        float income,
        float housing,
        float groceries,
        float transport,
        float utilities,
        float schoolFees,
        float savings)
    {
        if (income <= 0f)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        float totalOutgoings = housing + groceries + transport + utilities + schoolFees + savings;
        float surplus = Mathf.Max(0f, income - totalOutgoings);
        float shortfall = Mathf.Max(0f, totalOutgoings - income);
        float total = income + shortfall; // expands pie if over budget so fractions still sum to 1

        float cursor = 0f;
        cursor = SetSlice(housingSlice, cursor, housing / total, housingColor);
        cursor = SetSlice(groceriesSlice, cursor, groceries / total, groceriesColor);
        cursor = SetSlice(transportSlice, cursor, transport / total, transportColor);
        cursor = SetSlice(utilitiesSlice, cursor, utilities / total, utilitiesColor);
        cursor = SetSlice(schoolFeesSlice, cursor, schoolFees / total, schoolFeesColor);
        cursor = SetSlice(savingsSlice, cursor, savings / total, savingsColor);
        cursor = SetSlice(surplusSlice, cursor, (surplus > 0 ? surplus : shortfall) / total,
                          surplus > 0 ? surplusColor : shortfallColor);

        SetRow(housingRow, housingColor, "Housing", housing, housing > 0);
        SetRow(groceriesRow, groceriesColor, "Food", groceries, groceries > 0);
        SetRow(transportRow, transportColor, "Transport", transport, transport > 0);
        SetRow(utilitiesRow, utilitiesColor, "Utilities", utilities, utilities > 0);
        SetRow(schoolFeesRow, schoolFeesColor, "School Fees", schoolFees, schoolFees > 0);
        SetRow(savingsRow, savingsColor, "Savings", savings, savings > 0);

        if (surplus > 0)
            SetRow(surplusRow, surplusColor, "Surplus", surplus, true);
        else
            SetRow(surplusRow, shortfallColor, "Shortfall", shortfall, shortfall > 0);
    }

    private float SetSlice(Image slice, float startFrac, float sizeFrac, Color color)
    {
        if (slice == null) return startFrac + sizeFrac;

        bool visible = sizeFrac > 0.005f;
        slice.gameObject.SetActive(visible);

        if (visible)
        {
            slice.color = color;
            slice.fillAmount = sizeFrac;

            slice.transform.localEulerAngles = new Vector3(0f, 0f, -startFrac * 360f);
        }

        return startFrac + sizeFrac;
    }

    private void SetRow(LegendRow row, Color color, string categoryName, float amount, bool visible)
    {
        if (row == null) return;

        if (row.colorSwatch != null) row.colorSwatch.gameObject.SetActive(visible);
        if (row.label != null) row.label.gameObject.SetActive(visible);

        if (!visible) return;

        if (row.colorSwatch != null) row.colorSwatch.color = color;
        if (row.label != null)
        {
            row.label.text = $"{categoryName}   <b>${amount:F0}</b>";
            row.label.color = Color.white;
        }
    }
}