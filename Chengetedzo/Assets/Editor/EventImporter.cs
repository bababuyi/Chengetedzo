// Place this file in: Chengetedzo/Assets/Editor/RegularEventImporter.cs
// Usage:
//   1. Export RegularEvents_Template.xlsx as a CSV (File > Save As > CSV)
//      OR use an xlsx-reading library — this importer reads the CSV export.
//   2. Save the CSV as: Assets/GameData/Events/RegularEvents.csv
//   3. Unity menu → Tools → Import Regular Events
//
// NOTE: Follow-up event links (followUpEvents list) cannot be resolved from CSV
// because they require GUIDs.  After import, wire them up manually in the
// Inspector, just as you would for a brand-new event.

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EventImporter
{
    private const string CSV_PATH = "Assets/GameData/Events/RegularEvents.csv";
    private const string OUTPUT_FOLDER = "Assets/GameData/Events/Imported";

    // ── Column indices (0-based) matching the template ─────────────────────
    // Identity
    private const int C_NAME = 0;
    private const int C_DESC = 1;
    private const int C_LESSON = 2;
    private const int C_CATEGORY = 3;
    private const int C_POOL = 4;
    private const int C_REQ_ASSET = 5;
    private const int C_INSURANCE = 6;
    private const int C_OUTCOME = 7;
    private const int C_SEASON = 8;
    private const int C_SIGNAL = 9;
    private const int C_SEVERITY = 10;
    // Probability
    private const int C_PROBABILITY = 11;
    private const int C_WEIGHT = 12;
    // Loss
    private const int C_LOSS_TYPE = 13;
    private const int C_FIXED_LOSS = 14;
    private const int C_MIN_LOSS_PCT = 15;
    private const int C_MAX_LOSS_PCT = 16;
    // Income
    private const int C_AFFECTS_INCOME = 17;
    private const int C_INCOME_PCT = 18;
    private const int C_INCOME_MONTHS = 19;
    private const int C_CASH_REWARD = 20;
    private const int C_MOMENTUM_REWARD = 21;
    // Expense
    private const int C_AFFECTS_EXPENSE = 22;
    private const int C_EXPENSE_CAT = 23;
    private const int C_EXPENSE_FLAT = 24;
    private const int C_EXPENSE_MONTHS = 25;
    // Household
    private const int C_AFFECTS_HH = 26;
    private const int C_ADULTS_LOST = 27;
    private const int C_CHILDREN_LOST = 28;
    // Loan
    private const int C_AFFECTS_LOAN = 29;
    private const int C_BORROW_CHANGE = 30;
    // Chain
    private const int C_STARTS_CHAIN = 31;
    private const int C_FOLLOWUP_CHANCE = 32;
    private const int C_FOLLOWUP_DELAY = 33;

    // ── Menu entry ──────────────────────────────────────────────────────────
    [MenuItem("Tools/Import Events")]
    public static void Import()
    {
        string fullPath = Path.Combine(Application.dataPath, "../" + CSV_PATH);

        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("Import Failed",
                $"CSV not found at:\n{CSV_PATH}\n\n" +
                "Export RegularEvents_Template.xlsx as CSV and place it there.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
        {
            string parent = Path.GetDirectoryName(OUTPUT_FOLDER).Replace("\\", "/");
            string folder = Path.GetFileName(OUTPUT_FOLDER);
            AssetDatabase.CreateFolder(parent, folder);
        }

        string[] lines = File.ReadAllLines(fullPath);

        int created = 0, updated = 0, skipped = 0, errors = 0;

        // Skip header rows: row 0 = section headers, row 1 = col headers, row 2 = notes
        // Data starts at row index 3
        int startRow = 3;

        for (int i = startRow; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = ParseCSVLine(line);

            // Must have at minimum an eventName
            if (cols.Length <= C_NAME) continue;
            string eventName = cols[C_NAME].Trim();
            if (string.IsNullOrEmpty(eventName)) continue;

            // Skip the example rows by checking for known example names
            // (they'll be imported if you leave them in — which is fine)

            if (cols.Length < 20)
            {
                Debug.LogWarning($"[Importer] Line {i + 1}: '{eventName}' — too few columns ({cols.Length}). Skipped.");
                errors++;
                continue;
            }

            string safeName = SanitizeFileName(eventName);
            string assetPath = $"{OUTPUT_FOLDER}/{safeName}.asset";

            EventData ev = AssetDatabase.LoadAssetAtPath<EventData>(assetPath);
            bool isNew = ev == null;

            if (isNew)
            {
                ev = ScriptableObject.CreateInstance<EventData>();
                AssetDatabase.CreateAsset(ev, assetPath);
                created++;
            }
            else
            {
                updated++;
            }

            try
            {
                ApplyColumns(ev, cols);
                EditorUtility.SetDirty(ev);
                Debug.Log($"[Importer] {(isNew ? "Created" : "Updated")} '{eventName}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Importer] Line {i + 1}: '{eventName}' — {ex.Message}");
                errors++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Import Complete",
            $"Created : {created}\n" +
            $"Updated : {updated}\n" +
            $"Errors  : {errors}\n\n" +
            $"Assets saved to {OUTPUT_FOLDER}\n\n" +
            "Remember to wire followUpEvents manually in the Inspector!", "OK");
    }

    // ── Apply CSV columns to EventData ──────────────────────────────────────
    private static void ApplyColumns(EventData ev, string[] cols)
    {
        string Get(int idx) => idx < cols.Length ? cols[idx].Trim() : string.Empty;

        // ── Identity ───────────────────────────────────────────────────────
        ev.eventName = Get(C_NAME);
        ev.description = Get(C_DESC);
        ev.financialLesson = Get(C_LESSON);
        ev.category = ParseCategory(Get(C_CATEGORY));
        ev.pool = ParsePool(Get(C_POOL));
        ev.requiredAsset = ParseAssetRequirement(Get(C_REQ_ASSET));
        ev.insuranceType = ParseInsuranceType(Get(C_INSURANCE));
        ev.outcomeType = ParseOutcomeType(Get(C_OUTCOME));
        ev.season = ParseSeason(Get(C_SEASON));
        ev.signal = ParseSignal(Get(C_SIGNAL));
        ev.severity = ParseSeverity(Get(C_SEVERITY));

        // Choice-related — regular events never have choices
        ev.hasChoices = false;
        ev.senderName = string.Empty;
        ev.senderRelation = string.Empty;
        ev.choices = new List<EventData.ChoiceOption>();

        // ── Probability ────────────────────────────────────────────────────
        ev.probability = ParseFloat(Get(C_PROBABILITY));
        ev.weight = ParseInt(Get(C_WEIGHT), 10);

        // ── Loss ───────────────────────────────────────────────────────────
        ev.lossType = ParseLossType(Get(C_LOSS_TYPE));
        ev.fixedLossAmount = ParseFloat(Get(C_FIXED_LOSS));
        ev.minLossPercent = ParseFloat(Get(C_MIN_LOSS_PCT));
        ev.maxLossPercent = ParseFloat(Get(C_MAX_LOSS_PCT));

        // ── Income ─────────────────────────────────────────────────────────
        ev.affectsIncome = ParseBool(Get(C_AFFECTS_INCOME));
        ev.incomePercentChange = ParseFloat(Get(C_INCOME_PCT));
        ev.incomeEffectMonths = ParseInt(Get(C_INCOME_MONTHS), 0);
        ev.cashReward = ParseFloat(Get(C_CASH_REWARD));
        ev.momentumReward = ParseFloat(Get(C_MOMENTUM_REWARD));

        // ── Expense ────────────────────────────────────────────────────────
        ev.affectsExpenses = ParseBool(Get(C_AFFECTS_EXPENSE));
        ev.expenseCategory = ParseExpenseCategory(Get(C_EXPENSE_CAT));
        ev.expenseFlatChange = ParseFloat(Get(C_EXPENSE_FLAT));
        ev.expenseEffectMonths = ParseInt(Get(C_EXPENSE_MONTHS), 0);

        // ── Household ──────────────────────────────────────────────────────
        ev.affectsHousehold = ParseBool(Get(C_AFFECTS_HH));
        ev.adultsLost = ParseInt(Get(C_ADULTS_LOST), 0);
        ev.childrenLost = ParseInt(Get(C_CHILDREN_LOST), 0);

        // ── Loan ───────────────────────────────────────────────────────────
        ev.affectsLoan = ParseBool(Get(C_AFFECTS_LOAN));
        ev.borrowingPowerChange = ParseFloat(Get(C_BORROW_CHANGE));

        // ── Chain ──────────────────────────────────────────────────────────
        ev.startsChain = ParseBool(Get(C_STARTS_CHAIN));
        ev.followUpChance = ParseFloat(Get(C_FOLLOWUP_CHANCE));
        ev.followUpDelay = ParseInt(Get(C_FOLLOWUP_DELAY), 1);

        // Follow-up events list — cleared; wire manually in Inspector
        if (ev.followUpEvents == null)
            ev.followUpEvents = new List<EventData>();
    }

    // ── Enum parsers ────────────────────────────────────────────────────────

    private static ForecastManager.ForecastCategory ParseCategory(string s) => s.ToLower() switch
    {
        "health" => ForecastManager.ForecastCategory.Health,
        "livestock" => ForecastManager.ForecastCategory.Livestock,
        "crops" => ForecastManager.ForecastCategory.Crops,
        "economic" => ForecastManager.ForecastCategory.Economic,
        "crime" => ForecastManager.ForecastCategory.Crime,
        "weather" => ForecastManager.ForecastCategory.Weather,
        _ => ForecastManager.ForecastCategory.Economic,
    };

    private static EventPool ParsePool(string s) => s.ToLower() switch
    {
        "weather" => EventPool.Weather,
        "agriculture" => EventPool.Agriculture,
        "economic" => EventPool.Economic,
        "health" => EventPool.Health,
        "crime" => EventPool.Crime,
        "opportunity" => EventPool.Opportunity,
        "choice" => EventPool.Choice,
        _ => EventPool.Economic,
    };

    private static GameManager.AssetRequirement ParseAssetRequirement(string s) => s.ToLower() switch
    {
        "house" => GameManager.AssetRequirement.House,
        "motor" => GameManager.AssetRequirement.Motor,
        "crops" => GameManager.AssetRequirement.Crops,
        "livestock" => GameManager.AssetRequirement.Livestock,
        "cropsorlivestock" => GameManager.AssetRequirement.CropsOrLivestock,
        _ => GameManager.AssetRequirement.None,
    };

    private static InsuranceManager.InsuranceType ParseInsuranceType(string s) => s.ToLower() switch
    {
        "funeral" => InsuranceManager.InsuranceType.Funeral,
        "health" => InsuranceManager.InsuranceType.Health,
        "education" => InsuranceManager.InsuranceType.Education,
        "hospitalcash" => InsuranceManager.InsuranceType.HospitalCash,
        "personalaccident" => InsuranceManager.InsuranceType.PersonalAccident,
        "motor" => InsuranceManager.InsuranceType.Motor,
        "home" => InsuranceManager.InsuranceType.Home,
        "crop" => InsuranceManager.InsuranceType.Crop,
        _ => InsuranceManager.InsuranceType.None,
    };

    private static EventOutcomeType ParseOutcomeType(string s) =>
        s.Equals("Positive", StringComparison.OrdinalIgnoreCase)
            ? EventOutcomeType.Positive
            : EventOutcomeType.Negative;

    private static GameManager.Season ParseSeason(string s) => s.ToLower() switch
    {
        "summer" => GameManager.Season.Summer,
        "winter" => GameManager.Season.Winter,
        _ => GameManager.Season.Any,
    };

    private static ForecastManager.ForecastSignal ParseSignal(string s) => s.ToLower() switch
    {
        "dry" => ForecastManager.ForecastSignal.Dry,
        "wet" => ForecastManager.ForecastSignal.Wet,
        "heat" => ForecastManager.ForecastSignal.Heat,
        "disease" => ForecastManager.ForecastSignal.Disease,
        "economicstress" => ForecastManager.ForecastSignal.EconomicStress,
        "crimewave" => ForecastManager.ForecastSignal.CrimeWave,
        _ => ForecastManager.ForecastSignal.Neutral,
    };

    private static EventSeverity ParseSeverity(string s) => s.ToLower() switch
    {
        "moderate" => EventSeverity.Moderate,
        "major" => EventSeverity.Major,
        _ => EventSeverity.Minor,
    };

    private static LossCalculationType ParseLossType(string s) => s.ToLower() switch
    {
        "assetvalue" => LossCalculationType.AssetValue,
        "cashonhand" => LossCalculationType.CashOnHand,
        "fixedamount" => LossCalculationType.FixedAmount,
        _ => LossCalculationType.FixedAmount,
    };

    private static ExpenseCategory ParseExpenseCategory(string s)
    {
        if (int.TryParse(s, out int n))
            return (ExpenseCategory)n;
        return s.ToLower() switch
        {
            "transport" => ExpenseCategory.Transport,
            "groceries" => ExpenseCategory.Groceries,
            "housing" => ExpenseCategory.Housing,
            "utilities" => ExpenseCategory.Utilities,
            "schoolfees" => ExpenseCategory.SchoolFees,
            _ => ExpenseCategory.Transport,
        };
    }

    // ── Scalar parsers ──────────────────────────────────────────────────────

    private static float ParseFloat(string s) =>
        float.TryParse(s, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : 0f;

    private static int ParseInt(string s, int fallback = 0) =>
        int.TryParse(s, out int v) ? v : fallback;

    private static bool ParseBool(string s) =>
        s.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
        s.Equals("1") || s.Equals("yes", StringComparison.OrdinalIgnoreCase);

    // ── Utility ─────────────────────────────────────────────────────────────

    private static string SanitizeFileName(string name) =>
        name.Replace("/", "-").Replace("\\", "-")
            .Replace(":", "").Replace("*", "").Replace("?", "")
            .Replace("\"", "").Replace("<", "").Replace(">", "").Replace("|", "").Trim();

    /// <summary>RFC-4180-compatible CSV parser that handles quoted fields.</summary>
    private static string[] ParseCSVLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')
                inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
                current.Append(c);
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
#endif