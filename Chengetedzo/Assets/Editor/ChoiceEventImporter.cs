// Place this file in: Chengetedzo/Assets/Editor/ChoiceEventImporter.cs
// Usage:
//   1. Drop ChoiceEvents.csv into Assets/GameData/Events/ChoiceEvents/
//   2. Unity menu → Tools → Import Choice Events
//   3. All ScriptableObjects are created in that same folder

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ChoiceEventImporter
{
    private const string CSV_PATH      = "Assets/GameData/Events/ChoiceEvents/ChoiceEvents.csv";
    private const string OUTPUT_FOLDER = "Assets/GameData/Events/ChoiceEvents";

    [MenuItem("Tools/Import Choice Events")]
    public static void Import()
    {
        string fullPath = Path.Combine(Application.dataPath, "../" + CSV_PATH);

        if (!File.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("Import Failed",
                $"CSV not found at:\n{CSV_PATH}\n\nDrop ChoiceEvents.csv into that folder first.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder(OUTPUT_FOLDER))
            AssetDatabase.CreateFolder("Assets/GameData/Events", "ChoiceEvents");

        string[] lines = File.ReadAllLines(fullPath);

        int created = 0, skipped = 0;
        EventData current = null;
        var currentChoices = new List<EventData.ChoiceOption>();

        void FlushCurrent()
        {
            if (current == null) return;
            current.choices = currentChoices;
            EditorUtility.SetDirty(current);
            currentChoices = new List<EventData.ChoiceOption>();
            current = null;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] cols = ParseCSVLine(line);
            if (cols.Length < 2) continue;

            string type = cols[0].Trim().ToUpperInvariant();

            // ── EVENT row ──────────────────────────────────────────
            if (type == "EVENT")
            {
                FlushCurrent();

                if (cols.Length < 11)
                {
                    Debug.LogWarning($"[Importer] Line {i + 1}: EVENT row too short — skipped.");
                    continue;
                }

                string eventName = cols[1].Trim();
                string safeName  = eventName.Replace("/", "-").Replace("\\", "-");
                string assetPath = $"{OUTPUT_FOLDER}/{safeName}.asset";

                EventData existing = AssetDatabase.LoadAssetAtPath<EventData>(assetPath);
                if (existing != null)
                {
                    existing.pool = ParsePool(cols[6].Trim());
                    EditorUtility.SetDirty(existing);
                    Debug.Log($"[Importer] '{eventName}' already exists — updating pool.");
                    skipped++;
                    current = existing;
                    continue;
                }

                current = ScriptableObject.CreateInstance<EventData>();

                current.eventName        = eventName;
                current.description      = cols[2].Trim();
                current.senderName       = cols[3].Trim();
                current.senderRelation   = cols[4].Trim();
                current.category         = ParseCategory(cols[5].Trim());
                current.pool             = ParsePool(cols[6].Trim());
                current.requiredAsset    = ParseAssetRequirement(cols[7].Trim());
                current.probability      = ParseFloat(cols[8]);
                current.weight           = ParseInt(cols[9]);
                current.severity         = ParseSeverity(cols[10].Trim());

                // Fixed values for all choice events
                current.hasChoices       = true;
                current.outcomeType      = EventOutcomeType.Negative;
                current.insuranceType    = InsuranceManager.InsuranceType.None;
                current.affectsIncome    = false;
                current.startsChain      = false;
                current.season           = GameManager.Season.Any;
                current.signal           = ForecastManager.ForecastSignal.Neutral;

                AssetDatabase.CreateAsset(current, assetPath);
                created++;
                Debug.Log($"[Importer] Created '{eventName}'");
            }

            // ── CHOICE row ─────────────────────────────────────────
            else if (type == "CHOICE")
            {
                if (current == null)
                {
                    Debug.LogWarning($"[Importer] Line {i + 1}: CHOICE with no preceding EVENT — skipped.");
                    continue;
                }

                if (cols.Length < 9)
                {
                    Debug.LogWarning($"[Importer] Line {i + 1}: CHOICE row too short — skipped.");
                    continue;
                }

                currentChoices.Add(new EventData.ChoiceOption
                {
                    label                = cols[1].Trim(),
                    resultDescription    = cols[2].Trim(),
                    moneyChange          = ParseFloat(cols[3]),
                    momentumChange       = ParseFloat(cols[4]),
                    incomePercentChange  = ParseFloat(cols[5]),
                    incomeEffectMonths   = ParseInt(cols[6]),
                    affectsLoan          = cols[7].Trim().Equals("Yes", StringComparison.OrdinalIgnoreCase),
                    borrowingPowerChange = ParseFloat(cols[8]),
                });
            }
        }

        FlushCurrent();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Import Complete",
            $"Created: {created}\nSkipped (already exist): {skipped}\n\nCheck {OUTPUT_FOLDER}", "OK");
    }

    // ── Parsers ────────────────────────────────────────────────────

    private static float ParseFloat(string s) =>
        float.TryParse(s.Trim(), out float v) ? v : 0f;

    private static int ParseInt(string s) =>
        int.TryParse(s.Trim(), out int v) ? v : 0;

    private static ForecastManager.ForecastCategory ParseCategory(string s) => s.ToLower() switch
    {
        "health"    => ForecastManager.ForecastCategory.Health,
        "livestock" => ForecastManager.ForecastCategory.Livestock,
        "crops"     => ForecastManager.ForecastCategory.Crops,
        "economic"  => ForecastManager.ForecastCategory.Economic,
        "crime"     => ForecastManager.ForecastCategory.Crime,
        "weather"   => ForecastManager.ForecastCategory.Weather,
        _           => ForecastManager.ForecastCategory.Economic,
    };

    private static EventPool ParsePool(string s) => s.ToLower() switch
    {
        "weather"     => EventPool.Weather,
        "agriculture" => EventPool.Agriculture,
        "economic"    => EventPool.Economic,
        "health"      => EventPool.Health,
        "crime"       => EventPool.Crime,
        "opportunity" => EventPool.Opportunity,
        "choice" => EventPool.Choice,
        _ => EventPool.Economic,
    };

    private static GameManager.AssetRequirement ParseAssetRequirement(string s) => s.ToLower() switch
    {
        "house"            => GameManager.AssetRequirement.House,
        "motor"            => GameManager.AssetRequirement.Motor,
        "crops"            => GameManager.AssetRequirement.Crops,
        "livestock"        => GameManager.AssetRequirement.Livestock,
        "cropsorlivestock" => GameManager.AssetRequirement.CropsOrLivestock,
        _                  => GameManager.AssetRequirement.None,
    };

    private static EventSeverity ParseSeverity(string s) => s.ToLower() switch
    {
        "moderate" => EventSeverity.Moderate,
        "major"    => EventSeverity.Major,
        _          => EventSeverity.Minor,
    };

    // Handles quoted CSV fields containing commas
    private static string[] ParseCSVLine(string line)
    {
        var result  = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (char c in line)
        {
            if (c == '"')         { inQuotes = !inQuotes; }
            else if (c == ',' && !inQuotes) { result.Add(current.ToString()); current.Clear(); }
            else                  { current.Append(c); }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
#endif
