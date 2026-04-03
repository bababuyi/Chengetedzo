using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class YearEndGraph : MonoBehaviour
{
    [Header("Graph Area")]
    public RectTransform graphArea;

    [Header("Line Prefabs")]
    public GameObject dotPrefab;
    public GameObject linePrefab;
    public GameObject eventDotPrefab;

    [Header("Axis Labels")]
    public Transform xAxisContainer;
    public GameObject xLabelPrefab;

    [Header("Y Axis Labels")]
    public TMP_Text yTopLabel;
    public TMP_Text yMidLabel;
    public TMP_Text yBottomLabel;

    [Header("Colors")]
    public Color incomeColor = new Color(0.39f, 0.60f, 0.13f);
    public Color expensesColor = new Color(0.89f, 0.29f, 0.29f);
    public Color cashColor = new Color(0.22f, 0.53f, 0.87f);
    public Color eventColor = new Color(0.73f, 0.46f, 0.09f);
    public Color insuredColor = new Color(0.39f, 0.60f, 0.13f);

    public void Render(List<GameManager.MonthSnapshot> snapshots)
    {
        if (snapshots == null || snapshots.Count == 0) return;

        foreach (Transform child in graphArea)
            Destroy(child.gameObject);
        if (xAxisContainer != null)
            foreach (Transform child in xAxisContainer)
                Destroy(child.gameObject);

        float graphW = graphArea.rect.width;
        float graphH = graphArea.rect.height;

        float maxVal = 1f;
        foreach (var s in snapshots)
        {
            maxVal = Mathf.Max(maxVal, s.income, s.expenses, s.cashOnHand);
        }
        maxVal *= 1.1f;

        int count = snapshots.Count;

        if (yTopLabel != null) yTopLabel.text = $"${Mathf.RoundToInt(maxVal)}";
        if (yMidLabel != null) yMidLabel.text = $"${Mathf.RoundToInt(maxVal * 0.5f)}";
        if (yBottomLabel != null) yBottomLabel.text = "$0";

        DrawGridLine(graphArea, 0f, graphW, graphH);
        DrawGridLine(graphArea, 0.5f, graphW, graphH);
        DrawGridLine(graphArea, 1.0f, graphW, graphH);

        DrawLine(snapshots, graphW, graphH, maxVal, count,
            s => s.income, incomeColor);
        DrawLine(snapshots, graphW, graphH, maxVal, count,
            s => s.expenses, expensesColor);
        DrawLine(snapshots, graphW, graphH, maxVal, count,
            s => s.cashOnHand, cashColor);

        for (int i = 0; i < snapshots.Count; i++)
        {
            var s = snapshots[i];
            if (!s.hadEvent) continue;

            float x = GetX(i, count, graphW);
            float y = GetY(s.expenses, maxVal, graphH);

            var dot = Instantiate(eventDotPrefab, graphArea);
            var rect = dot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);

            var img = dot.GetComponent<Image>();
            if (img != null)
                img.color = s.eventWasInsured ? insuredColor : eventColor;
        }

        if (xAxisContainer != null && xLabelPrefab != null)
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                float x = GetX(i, count, graphW);
                var label = Instantiate(xLabelPrefab, xAxisContainer);
                var rect = label.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
                rect.anchoredPosition = new Vector2(x, 0f);
                var txt = label.GetComponent<TMP_Text>();
                if (txt != null) txt.text = snapshots[i].month.ToString();
            }
        }
    }

    private void DrawLine(
        List<GameManager.MonthSnapshot> snapshots,
        float graphW, float graphH, float maxVal, int count,
        System.Func<GameManager.MonthSnapshot, float> valueSelector,
        Color color)
    {
        for (int i = 0; i < snapshots.Count; i++)
        {
            float x = GetX(i, count, graphW);
            float y = GetY(valueSelector(snapshots[i]), maxVal, graphH);

            var dot = Instantiate(dotPrefab, graphArea);
            var dRect = dot.GetComponent<RectTransform>();
            dRect.anchorMin = dRect.anchorMax = new Vector2(0f, 0f);
            dRect.pivot = new Vector2(0.5f, 0.5f);
            dRect.anchoredPosition = new Vector2(x, y);
            var dImg = dot.GetComponent<Image>();
            if (dImg != null) dImg.color = color;

            if (i < snapshots.Count - 1)
            {
                float x2 = GetX(i + 1, count, graphW);
                float y2 = GetY(valueSelector(snapshots[i + 1]), maxVal, graphH);

                var line = Instantiate(linePrefab, graphArea);
                var lRect = line.GetComponent<RectTransform>();
                lRect.anchorMin = lRect.anchorMax = new Vector2(0f, 0f);
                lRect.pivot = new Vector2(0f, 0.5f);

                float dx = x2 - x;
                float dy = y2 - y;
                float length = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

                lRect.anchoredPosition = new Vector2(x, y);
                lRect.sizeDelta = new Vector2(length, 3f);
                lRect.localEulerAngles = new Vector3(0f, 0f, angle);

                var lImg = line.GetComponent<Image>();
                if (lImg != null) lImg.color = color;
            }
        }
    }

    private void DrawGridLine(RectTransform parent, float yFrac, float w, float h)
    {
        var go = new GameObject("GridLine", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, yFrac * h);
        rect.sizeDelta = new Vector2(w, 1f);
        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);
    }

    private float GetX(int i, int count, float w) =>
        count <= 1 ? w * 0.5f : (i / (float)(count - 1)) * w;

    private float GetY(float val, float maxVal, float h) =>
        (val / maxVal) * h;
}