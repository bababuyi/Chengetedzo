#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor utility to scan all EventData assets in the project and
/// add any missing ones to the EventDatabase.
///
/// Place this file anywhere inside an Editor/ folder, e.g.:
///   Assets/Editor/EventDatabasePopulator.cs
///
/// Usage:
///   Tools ▶ Chengetedzo ▶ Populate Event Database
/// </summary>
public static class EventDatabasePopulator
{
    private const string MENU_PATH = "Tools/Chengetedzo/Populate Event Database";

    [MenuItem(MENU_PATH)]
    public static void PopulateEventDatabase()
    {
        // ── 1. Find the EventDatabase asset ──────────────────────────────────
        string[] dbGuids = AssetDatabase.FindAssets("t:EventDatabase");

        if (dbGuids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Event Database Populator",
                "No EventDatabase asset found in the project.\n\n" +
                "Create one via: Assets ▶ Create ▶ Chengetedzo ▶ Event Database",
                "OK");
            return;
        }

        if (dbGuids.Length > 1)
        {
            // Let the user pick if there are multiple
            string paths = string.Join("\n", dbGuids.Select(g =>
                AssetDatabase.GUIDToAssetPath(g)));

            bool proceed = EditorUtility.DisplayDialog(
                "Event Database Populator",
                $"Multiple EventDatabase assets found:\n\n{paths}\n\n" +
                "The first one will be used. Continue?",
                "Continue", "Cancel");

            if (!proceed) return;
        }

        string dbPath = AssetDatabase.GUIDToAssetPath(dbGuids[0]);
        EventDatabase database = AssetDatabase.LoadAssetAtPath<EventDatabase>(dbPath);

        if (database == null)
        {
            EditorUtility.DisplayDialog(
                "Event Database Populator",
                $"Failed to load EventDatabase at:\n{dbPath}",
                "OK");
            return;
        }

        // ── 2. Find all EventData assets ─────────────────────────────────────
        string[] eventGuids = AssetDatabase.FindAssets("t:EventData");

        if (eventGuids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Event Database Populator",
                "No EventData assets found in the project.",
                "OK");
            return;
        }

        // ── 3. Determine which are already registered ─────────────────────────
        if (database.events == null)
            database.events = new List<EventData>();

        // Use a HashSet for O(1) duplicate checks
        HashSet<EventData> existing = new HashSet<EventData>(database.events
            .Where(e => e != null));

        List<EventData> added = new List<EventData>();
        List<EventData> skipped = new List<EventData>();   // already present
        List<string> nulls = new List<string>();      // load failures

        foreach (string guid in eventGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            EventData ev = AssetDatabase.LoadAssetAtPath<EventData>(path);

            if (ev == null)
            {
                nulls.Add(path);
                continue;
            }

            if (existing.Contains(ev))
            {
                skipped.Add(ev);
                continue;
            }

            database.events.Add(ev);
            existing.Add(ev);   // prevent duplicates within this run
            added.Add(ev);
        }

        // ── 4. Remove null entries left over from deleted assets ──────────────
        int nullsRemoved = database.events.RemoveAll(e => e == null);

        // ── 5. Save ───────────────────────────────────────────────────────────
        if (added.Count > 0 || nullsRemoved > 0)
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        // ── 6. Report ─────────────────────────────────────────────────────────
        string report =
            $"EventDatabase updated: {dbPath}\n\n" +
            $"Added:          {added.Count}\n" +
            $"Already present: {skipped.Count}\n" +
            $"Null entries removed: {nullsRemoved}\n" +
            $"Load failures:  {nulls.Count}\n\n" +
            $"Total events in database: {database.events.Count}";

        if (added.Count > 0)
        {
            report += "\n\n── Newly added ──\n";
            report += string.Join("\n", added.Select(e => $"  • {e.eventName} ({e.name})"));
        }

        if (nulls.Count > 0)
        {
            report += "\n\n── Load failures (check asset type) ──\n";
            report += string.Join("\n", nulls.Select(p => $"  • {p}"));
        }

        Debug.Log("[EventDatabasePopulator]\n" + report);
        EditorUtility.DisplayDialog("Event Database Populator", report, "OK");
    }

    // ── Validate menu item (greyed-out when no DB exists) ────────────────────
    [MenuItem(MENU_PATH, true)]
    private static bool ValidatePopulate()
    {
        return AssetDatabase.FindAssets("t:EventDatabase").Length > 0;
    }
}
#endif