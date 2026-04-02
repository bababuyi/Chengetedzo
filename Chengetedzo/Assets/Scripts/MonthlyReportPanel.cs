using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonthlyReportPanel : MonoBehaviour
{
    [Header("Navigation")]
    public Button continueButton;

    [Header("Left Column - Text")]
    public TMP_Text monthHeaderText;
    public TMP_Text incomeText;
    public Transform expenseLineContainer;
    public GameObject expenseLinePrefab;
    public TMP_Text totalExpensesText;
    public TMP_Text savingsLineText;
    public GameObject savingsLineRoot;
    public TMP_Text endBalanceText;
    public Transform eventRecapContainer;
    public GameObject eventRecapPrefab;

    [Header("Right Column - Chart")]
    public MonthlyBarChart barChart;
    public TMP_Text savingsBalanceText;

    private void OnEnable()
    {
        if (continueButton == null) return;
        continueButton.interactable =
            GameManager.Instance?.CurrentPhase == GameManager.GamePhase.Report;
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinue);
    }

    private void OnDisable()
    {
        continueButton?.onClick.RemoveListener(OnContinue);
    }

    public void Populate(MonthlyFinancialLedger ledger)
    {
        Debug.Log($"[ReportPanel] Populate called. Ledger null: {ledger == null}");
        if (ledger == null) return;

        int displayMonth = ((ledger.MonthNumber - 1) % 12) + 1;
        string monthName = System.Globalization.CultureInfo.CurrentCulture
            .DateTimeFormat.GetMonthName(displayMonth);
        if (monthHeaderText != null)
            monthHeaderText.text = $"Month {ledger.MonthNumber} - {monthName}";

        float income = 0f;
        float housing = 0f;
        float groceries = 0f;
        float transport = 0f;
        float utilities = 0f;
        float schoolFees = 0f;
        float insurance = 0f;
        float eventLosses = 0f;
        float savingsContrib = 0f;

        var eventLines = new List<(string name, float amount, bool positive)>();

        foreach (var entry in ledger.Entries)
        {
            float abs = Mathf.Abs(entry.SignedAmount());
            switch (entry.entryType)
            {
                case FinancialEntry.EntryType.Income:
                case FinancialEntry.EntryType.EventReward:
                    income += entry.SignedAmount();
                    if (entry.entryType == FinancialEntry.EntryType.EventReward)
                        eventLines.Add((entry.description, abs, true));
                    break;

                case FinancialEntry.EntryType.Expense:
                    switch (entry.description)
                    {
                        case "Housing": housing += abs; break;
                        case "Groceries": groceries += abs; break;
                        case "Transport": transport += abs; break;
                        case "Utilities": utilities += abs; break;
                        case "School Fees": schoolFees += abs; break;
                        default: housing += abs; break;
                    }
                    break;

                case FinancialEntry.EntryType.InsurancePremium:
                    insurance += abs;
                    break;

                case FinancialEntry.EntryType.InsurancePayout:
                    eventLines.Add(($"Insurance payout", abs, true));
                    break;

                case FinancialEntry.EntryType.EventLoss:
                    eventLosses += abs;
                    eventLines.Add((entry.description, abs, false));
                    break;

                case FinancialEntry.EntryType.SavingsContribution:
                    savingsContrib += abs;
                    break;
            }
        }

        float totalExpenses = housing + groceries + transport + utilities
                              + schoolFees + insurance + eventLosses;
        float leftover = Mathf.Max(0f, income - totalExpenses);

        if (incomeText != null)
            incomeText.text = $"+${Mathf.RoundToInt(income)}";

        BuildExpenseLines(housing, groceries, transport, utilities, schoolFees, insurance);

        if (totalExpensesText != null)
            totalExpensesText.text = $"-${Mathf.RoundToInt(totalExpenses)}";

        bool hasSavings = savingsContrib > 0.01f;
        if (savingsLineRoot != null) savingsLineRoot.SetActive(hasSavings);
        if (hasSavings && savingsLineText != null)
            savingsLineText.text = $"+${Mathf.RoundToInt(savingsContrib)}";

        if (endBalanceText != null)
            endBalanceText.text = $"${Mathf.RoundToInt(ledger.ClosingBalance)}";

        BuildEventRecap(eventLines);

        barChart?.Render(income, housing, groceries, transport, utilities,
                         schoolFees, insurance, eventLosses);

        float savingsBalance = GameManager.Instance?.financeManager?.generalSavingsBalance ?? 0f;
        if (savingsBalanceText != null)
        {
            savingsBalanceText.gameObject.SetActive(savingsBalance > 0.01f);
            savingsBalanceText.text = $"Savings balance: ${Mathf.RoundToInt(savingsBalance)}";
        }
    }

    private void BuildExpenseLines(float housing, float groceries, float transport,
                                   float utilities, float schoolFees, float insurance)
    {
        if (expenseLineContainer == null || expenseLinePrefab == null) return;

        foreach (Transform child in expenseLineContainer)
            Destroy(child.gameObject);

        AddExpenseLine("Housing", housing);
        AddExpenseLine("Groceries", groceries);
        AddExpenseLine("Transport", transport);
        AddExpenseLine("Utilities", utilities);
        AddExpenseLine("School fees", schoolFees);
        AddExpenseLine("Insurance", insurance);
    }

    private void AddExpenseLine(string label, float amount)
    {
        if (amount < 0.01f) return;
        var go = Instantiate(expenseLinePrefab, expenseLineContainer);
        var texts = go.GetComponentsInChildren<TMP_Text>();
        if (texts.Length >= 2)
        {
            texts[0].text = label;
            texts[1].text = $"-${Mathf.RoundToInt(amount)}";
        }
    }

    private void BuildEventRecap(List<(string name, float amount, bool positive)> events)
    {
        if (eventRecapContainer == null || eventRecapPrefab == null) return;

        EnsureEventRecapLayout();

        foreach (Transform child in eventRecapContainer)
            Destroy(child.gameObject);

        foreach (var ev in events)
        {
            var go = Instantiate(eventRecapPrefab, eventRecapContainer);
            NormalizeUIChild(go.transform);
            var texts = go.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = ev.name;
                texts[1].text = ev.positive
                    ? $"+${Mathf.RoundToInt(ev.amount)}"
                    : $"-${Mathf.RoundToInt(ev.amount)}";
                texts[1].color = ev.positive
                    ? new Color(0.23f, 0.55f, 0.13f)
                    : new Color(0.64f, 0.17f, 0.17f);
            }
            var bg = go.GetComponent<Image>();
            if (bg != null)
                bg.color = ev.positive
                    ? new Color(0.91f, 0.95f, 0.87f, 1f)
                    : new Color(0.98f, 0.92f, 0.92f, 1f);
        }
    }

    private void EnsureEventRecapLayout()
    {
        var layout = eventRecapContainer.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = eventRecapContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;
        }

        var fitter = eventRecapContainer.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = eventRecapContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private static void NormalizeUIChild(Transform child)
    {
        child.localScale = Vector3.one;

        if (child is RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.localRotation = Quaternion.identity;
        }
    }

    private void OnContinue()
    {
        if (GameManager.Instance?.CurrentPhase != GameManager.GamePhase.Report) return;
        if (continueButton != null) continueButton.interactable = false;
        GameManager.Instance.EndMonthAndAdvance();
    }
}
