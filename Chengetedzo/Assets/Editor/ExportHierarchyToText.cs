using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

public class ExportHierarchyToText : EditorWindow
{
    private bool filterByName = false;
    private string nameFilter = "";
    private bool filterByComponent = false;
    private string componentFilter = "";
    private bool onlyFlagged = false;
    private bool exportAllScenes = false;

    private bool showTransform = true;
    private bool showPosition = true;
    private bool showRotation = true;
    private bool showScale = true;
    private bool showComponents = true;
    private bool showBuiltinComponents = true;
    private bool showScripts = true;
    private bool showPublicFields = true;
    private bool showSerializedFields = true;

    private bool foldFilters = true;
    private bool foldColumns = true;
    private bool foldTransSub = true;
    private Vector2 scroll;

    private static int totalObjects;
    private static int totalComponents;
    private static List<string> auditIssues;

    private static readonly string[] PresetNames =
    {
        "Full", "Names Only", "Names + Transform",
        "Names + Components", "Names + Scripts", "Scripts + Fields",
    };

    [MenuItem("Window/Export Hierarchy to Text")]
    public static void ShowWindow()
    {
        var win = GetWindow<ExportHierarchyToText>("Export Hierarchy");
        win.minSize = new Vector2(300, 440);
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Label("Scene Scope", EditorStyles.boldLabel);
        exportAllScenes = EditorGUILayout.Toggle("All Open Scenes", exportAllScenes);

        EditorGUILayout.Space(6);
        GUILayout.Label("Quick Presets", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i < 3; i++)
            if (GUILayout.Button(PresetNames[i], GUILayout.Height(22))) ApplyPreset(i);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        for (int i = 3; i < PresetNames.Length; i++)
            if (GUILayout.Button(PresetNames[i], GUILayout.Height(22))) ApplyPreset(i);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        foldFilters = EditorGUILayout.Foldout(foldFilters, "Object Filters", true, EditorStyles.foldoutHeader);
        if (foldFilters)
        {
            EditorGUI.indentLevel++;
            filterByName = EditorGUILayout.Toggle("Name Contains", filterByName);
            if (filterByName) nameFilter = EditorGUILayout.TextField(nameFilter);

            filterByComponent = EditorGUILayout.Toggle("Has Component", filterByComponent);
            if (filterByComponent) componentFilter = EditorGUILayout.TextField(componentFilter);

            onlyFlagged = EditorGUILayout.Toggle("Issues Only ⚠", onlyFlagged);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(6);

        foldColumns = EditorGUILayout.Foldout(foldColumns, "Data Columns", true, EditorStyles.foldoutHeader);
        if (foldColumns)
        {
            EditorGUI.indentLevel++;

            foldTransSub = EditorGUILayout.Foldout(foldTransSub, "Transform", true);
            if (foldTransSub)
            {
                EditorGUI.indentLevel++;
                bool newTrans = EditorGUILayout.Toggle("Include Transform", showTransform);
                if (newTrans != showTransform)
                {
                    showTransform = newTrans;
                    if (!showTransform) showPosition = showRotation = showScale = false;
                }
                EditorGUI.BeginDisabledGroup(!showTransform);
                showPosition = EditorGUILayout.Toggle("  Position", showPosition);
                showRotation = EditorGUILayout.Toggle("  Rotation", showRotation);
                showScale = EditorGUILayout.Toggle("  Scale", showScale);
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(2);

            bool newComp = EditorGUILayout.Toggle("Components", showComponents);
            if (newComp != showComponents)
            {
                showComponents = newComp;
                if (!showComponents)
                    showBuiltinComponents = showScripts = showPublicFields = showSerializedFields = false;
            }
            EditorGUI.BeginDisabledGroup(!showComponents);
            EditorGUI.indentLevel++;
            showBuiltinComponents = EditorGUILayout.Toggle("Built-in Components", showBuiltinComponents);

            bool newScripts = EditorGUILayout.Toggle("Scripts (MonoBehaviour)", showScripts);
            if (newScripts != showScripts)
            {
                showScripts = newScripts;
                if (!showScripts) showPublicFields = showSerializedFields = false;
            }
            EditorGUI.BeginDisabledGroup(!showScripts);
            EditorGUI.indentLevel++;
            showPublicFields = EditorGUILayout.Toggle("Public Fields [pub]", showPublicFields);
            showSerializedFields = EditorGUILayout.Toggle("[SerializeField] [ser]", showSerializedFields);
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
            EditorGUI.EndDisabledGroup();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(12);

        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.55f, 0.75f, 1f);
        if (GUILayout.Button("Preview", GUILayout.Height(34)))
            OpenPreview();

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Export", GUILayout.Height(34)))
            RunExport(saveToFile: true);

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    void OpenPreview()
    {
        string content = RunExport(saveToFile: false);
        HierarchyPreviewWindow.Open(content);
    }

    string RunExport(bool saveToFile)
    {
        totalObjects = 0;
        totalComponents = 0;
        auditIssues = new List<string>();

        ExportConfig cfg = BuildConfig();

        List<Scene> scenes = new List<Scene>();
        if (exportAllScenes)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.isLoaded) scenes.Add(s);
            }
        }
        else
        {
            scenes.Add(SceneManager.GetActiveScene());
        }

        StringBuilder body = new StringBuilder();
        foreach (Scene scene in scenes)
        {
            body.AppendLine("╔══════════════════════════════════════════╗");
            body.AppendLine($"  Scene: {scene.name}  ({scene.path})");
            body.AppendLine("╚══════════════════════════════════════════╝");
            body.AppendLine();
            foreach (GameObject go in scene.GetRootGameObjects())
                AppendObjectAndChildren(go.transform, body, 0, cfg);
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Scene Hierarchy Export ===");
        sb.AppendLine($"Exported : {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Columns  : {ActiveColumnSummary(cfg)}");
        sb.AppendLine();
        sb.AppendLine("── Audit Summary ──────────────────────────────");
        sb.AppendLine($"  GameObjects : {totalObjects}");
        sb.AppendLine($"  Components  : {totalComponents}");
        sb.AppendLine($"  Issues      : {auditIssues.Count}");
        if (auditIssues.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("  ⚠ Issues:");
            foreach (string issue in auditIssues)
                sb.AppendLine($"    • {issue}");
        }
        sb.AppendLine("───────────────────────────────────────────────");
        sb.AppendLine();
        sb.Append(body);

        string result = sb.ToString();

        if (saveToFile)
        {
            string defaultName = exportAllScenes
                ? "AllScenes_Hierarchy.txt"
                : $"{SceneManager.GetActiveScene().name}_Hierarchy.txt";

            string path = EditorUtility.SaveFilePanel("Save Hierarchy Text", "", defaultName, "txt");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, result);
                Debug.Log($"Exported: {path}  ({totalObjects} objects, {auditIssues.Count} issues)");
            }
        }

        return result;
    }

    void ApplyPreset(int index)
    {
        showTransform = showPosition = showRotation = showScale = true;
        showComponents = showBuiltinComponents = showScripts = true;
        showPublicFields = showSerializedFields = true;

        switch (index)
        {
            case 0: break;
            case 1: // Names Only
                showTransform = showPosition = showRotation = showScale = false;
                showComponents = showBuiltinComponents = showScripts = false;
                showPublicFields = showSerializedFields = false;
                break;
            case 2: // Names + Transform
                showComponents = showBuiltinComponents = showScripts = false;
                showPublicFields = showSerializedFields = false;
                break;
            case 3: // Names + Components (no field values)
                showPublicFields = showSerializedFields = false;
                break;
            case 4: // Names + Scripts (no transform, no built-ins, no field values)
                showTransform = showPosition = showRotation = showScale = false;
                showBuiltinComponents = false;
                showPublicFields = showSerializedFields = false;
                break;
            case 5: // Scripts + Fields (no transform, no built-ins)
                showTransform = showPosition = showRotation = showScale = false;
                showBuiltinComponents = false;
                break;
        }
        Repaint();
    }

    void AppendObjectAndChildren(Transform t, StringBuilder sb, int level, ExportConfig cfg)
    {
        if (filterByName && !string.IsNullOrEmpty(nameFilter))
        {
            if (t.name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0)
            {
                for (int i = 0; i < t.childCount; i++)
                    AppendObjectAndChildren(t.GetChild(i), sb, level + 1, cfg);
                return;
            }
        }

        if (filterByComponent && !string.IsNullOrEmpty(componentFilter))
        {
            bool has = t.GetComponents<Component>().Any(c =>
                c != null && c.GetType().Name.IndexOf(componentFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!has)
            {
                for (int i = 0; i < t.childCount; i++)
                    AppendObjectAndChildren(t.GetChild(i), sb, level + 1, cfg);
                return;
            }
        }

        Component[] components = t.GetComponents<Component>();
        bool objectHasIssue = components.Any(c => c == null);

        if (!objectHasIssue)
        {
            foreach (Component comp in components)
            {
                if (comp is MonoBehaviour mono)
                {
                    foreach (FieldInfo field in GetAuditableFields(mono, true, true))
                    {
                        object val = field.GetValue(mono);
                        if (val == null || (val is UnityEngine.Object uo && uo == null))
                        { objectHasIssue = true; break; }
                    }
                    if (objectHasIssue) break;
                }
            }
        }

        if (onlyFlagged && !objectHasIssue)
        {
            for (int i = 0; i < t.childCount; i++)
                AppendObjectAndChildren(t.GetChild(i), sb, level + 1, cfg);
            return;
        }

        totalObjects++;
        string indent = new string(' ', level * 4);
        sb.AppendLine($"{indent}▸ {t.name}{(objectHasIssue ? "  ⚠" : "")}");

        if (cfg.transform && (cfg.position || cfg.rotation || cfg.scale))
        {
            sb.AppendLine($"{indent}  [Transform]");
            if (cfg.position) sb.AppendLine($"{indent}    Position : {t.localPosition}");
            if (cfg.rotation) sb.AppendLine($"{indent}    Rotation : {t.localEulerAngles}");
            if (cfg.scale) sb.AppendLine($"{indent}    Scale    : {t.localScale}");
        }

        if (cfg.components)
        {
            bool headerWritten = false;
            foreach (Component comp in components)
            {
                totalComponents++;
                if (comp == null)
                {
                    EnsureComponentHeader(sb, indent, ref headerWritten);
                    sb.AppendLine($"{indent}    ⚠ Missing Script");
                    auditIssues.Add($"Missing script on: {GetGameObjectPath(t)}");
                    continue;
                }
                if (comp is MonoBehaviour mono)
                {
                    if (!cfg.scripts) continue;
                    EnsureComponentHeader(sb, indent, ref headerWritten);
                    sb.AppendLine($"{indent}    ◆ {mono.GetType().Name} (MonoBehaviour)");
                    AppendFields(mono, sb, indent + "        ", cfg, auditIssues);
                }
                else
                {
                    if (!cfg.builtins) continue;
                    EnsureComponentHeader(sb, indent, ref headerWritten);
                    sb.AppendLine($"{indent}    ◇ {comp.GetType().Name}");
                }
            }
        }

        sb.AppendLine();

        for (int i = 0; i < t.childCount; i++)
            AppendObjectAndChildren(t.GetChild(i), sb, level + 1, cfg);
    }

    static void EnsureComponentHeader(StringBuilder sb, string indent, ref bool written)
    {
        if (!written) { sb.AppendLine($"{indent}  [Components]"); written = true; }
    }

    static void AppendFields(MonoBehaviour mono, StringBuilder sb, string indent,
                             ExportConfig cfg, List<string> issues)
    {
        foreach (FieldInfo field in GetAuditableFields(mono, cfg.publicFields, cfg.serializedFields))
        {
            object val = field.GetValue(mono);
            string valueStr = FormatValue(val);
            string tag = field.IsPublic ? "pub" : "ser";
            bool isNull = val == null || (val is UnityEngine.Object uo && uo == null);

            sb.AppendLine($"{indent}[{tag}] {field.Name} = {valueStr}{(isNull ? "  ⚠ NULL" : "")}");

            if (isNull)
                issues.Add($"Null ref '{field.Name}' on {mono.GetType().Name} @ {GetGameObjectPath(mono.transform)}");
        }
    }

    static IEnumerable<FieldInfo> GetAuditableFields(MonoBehaviour mono, bool pub, bool ser)
    {
        Type type = mono.GetType();
        var result = Enumerable.Empty<FieldInfo>();

        if (pub)
            result = result.Concat(
                type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .Where(f => !Attribute.IsDefined(f, typeof(HideInInspector))));

        if (ser)
            result = result.Concat(
                type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(f => Attribute.IsDefined(f, typeof(SerializeField))
                              && !Attribute.IsDefined(f, typeof(HideInInspector))));

        return result;
    }

    static string FormatValue(object value)
    {
        if (value == null) return "null";
        Type type = value.GetType();
        if (type.IsPrimitive || value is string || type.IsEnum) return value.ToString();
        if (value is UnityEngine.Object uo) return uo != null ? $"{uo.name} ({type.Name})" : "null";
        if (value is IList list) return $"[{list.Count} elements]";
        if (value is IEnumerable ienu) { int n = 0; foreach (var _ in ienu) n++; return $"[{n} elements]"; }
        return $"({type.Name})";
    }

    static string GetGameObjectPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }

    ExportConfig BuildConfig() => new ExportConfig
    {
        transform = showTransform,
        position = showPosition,
        rotation = showRotation,
        scale = showScale,
        components = showComponents,
        builtins = showBuiltinComponents,
        scripts = showScripts,
        publicFields = showPublicFields,
        serializedFields = showSerializedFields,
    };

    static string ActiveColumnSummary(ExportConfig cfg)
    {
        var parts = new List<string>();
        if (cfg.transform)
        {
            var sub = new List<string>();
            if (cfg.position) sub.Add("pos");
            if (cfg.rotation) sub.Add("rot");
            if (cfg.scale) sub.Add("scale");
            parts.Add(sub.Count > 0 ? $"Transform({string.Join(",", sub)})" : "Transform");
        }
        if (cfg.builtins) parts.Add("BuiltinComponents");
        if (cfg.scripts)
        {
            var sub = new List<string>();
            if (cfg.publicFields) sub.Add("pub");
            if (cfg.serializedFields) sub.Add("ser");
            parts.Add(sub.Count > 0 ? $"Scripts({string.Join(",", sub)})" : "Scripts");
        }
        return parts.Count > 0 ? string.Join(" | ", parts) : "Names only";
    }

    private struct ExportConfig
    {
        public bool transform, position, rotation, scale;
        public bool components, builtins, scripts;
        public bool publicFields, serializedFields;
    }
}

public class HierarchyPreviewWindow : EditorWindow
{
    private string _content = "";
    private string _searchTerm = "";
    private string _highlighted = "";
    private Vector2 _scroll;
    private bool _searchDirty = true;
    private int _matchCount = 0;
    private GUIStyle _textStyle;

    private string _statLine = "";

    public static void Open(string content)
    {
        var win = GetWindow<HierarchyPreviewWindow>("Hierarchy Preview");
        win.minSize = new Vector2(520, 500);
        win._content = content;
        win.ExtractStats(content);
        win._searchDirty = true;
        win.Repaint();
    }

    void OnGUI()
    {
        BuildStyleIfNeeded();

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        GUILayout.Label("Search:", GUILayout.Width(52));
        EditorGUI.BeginChangeCheck();
        _searchTerm = EditorGUILayout.TextField(_searchTerm, EditorStyles.toolbarSearchField,
                                                GUILayout.ExpandWidth(true));
        if (EditorGUI.EndChangeCheck()) _searchDirty = true;

        string matchLabel = string.IsNullOrEmpty(_searchTerm) ? "" : $"  {_matchCount} match{(_matchCount == 1 ? "" : "es")}";
        GUILayout.Label(matchLabel, GUILayout.Width(90));

        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Export to File", EditorStyles.toolbarButton, GUILayout.Width(100)))
            SaveToFile();
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_statLine))
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(_statLine, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        if (_searchDirty)
        {
            RebuildHighlight();
            _searchDirty = false;
        }

        string display = string.IsNullOrEmpty(_searchTerm) ? _content : _highlighted;
        EditorGUILayout.TextArea(display, _textStyle, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        EditorGUILayout.EndScrollView();
    }

    void RebuildHighlight()
    {
        if (string.IsNullOrEmpty(_searchTerm))
        {
            _highlighted = _content;
            _matchCount = 0;
            return;
        }

        var lines = _content.Split('\n');
        var sb = new StringBuilder();
        int count = 0;
        string term = _searchTerm.ToLowerInvariant();

        foreach (string line in lines)
        {
            if (line.ToLowerInvariant().Contains(term))
            {
                sb.AppendLine($">> {line.TrimEnd()}");
                count++;
            }
            else
            {
                sb.AppendLine(line.TrimEnd());
            }
        }

        _highlighted = sb.ToString();
        _matchCount = count;
    }

    void ExtractStats(string content)
    {
        _statLine = "";
        int goCount = 0;
        int compCount = 0;
        int issues = 0;
        bool gotGO = false, gotComp = false, gotIssues = false;

        foreach (string line in content.Split('\n'))
        {
            string trimmed = line.Trim();
            if (!gotGO && trimmed.StartsWith("GameObjects"))
            { int.TryParse(trimmed.Split(':').LastOrDefault()?.Trim(), out goCount); gotGO = true; }
            if (!gotComp && trimmed.StartsWith("Components"))
            { int.TryParse(trimmed.Split(':').LastOrDefault()?.Trim(), out compCount); gotComp = true; }
            if (!gotIssues && trimmed.StartsWith("Issues"))
            { int.TryParse(trimmed.Split(':').LastOrDefault()?.Trim(), out issues); gotIssues = true; }
            if (gotGO && gotComp && gotIssues) break;
        }

        _statLine = $"GameObjects: {goCount}   Components: {compCount}   Issues: {issues}";
    }

    void SaveToFile()
    {
        string path = EditorUtility.SaveFilePanel("Save Hierarchy Text", "", "Hierarchy.txt", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, _content);
            Debug.Log("Hierarchy exported to: " + path);
        }
    }

    void BuildStyleIfNeeded()
    {
        if (_textStyle != null) return;
        _textStyle = new GUIStyle(EditorStyles.textArea)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Consolas", "Courier New", "Menlo", "Courier" }, 11),
            fontSize = 11,
            wordWrap = false,
            richText = false,
        };
    }
}