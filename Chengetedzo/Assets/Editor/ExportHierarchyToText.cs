using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System;

public class ExportHierarchyToText : EditorWindow
{
    [MenuItem("Window/Export Hierarchy to Text")]
    public static void ShowWindow()
    {
        GetWindow<ExportHierarchyToText>("Export Hierarchy");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Export Current Scene Hierarchy"))
        {
            ExportHierarchy();
        }
    }

    static void ExportHierarchy()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("=== Scene Hierarchy Export ===");
        sb.AppendLine("Scene: " + SceneManager.GetActiveScene().name);
        sb.AppendLine();

        foreach (GameObject go in rootObjects)
        {
            AppendObjectAndChildren(go.transform, sb, 0);
        }

        string path = EditorUtility.SaveFilePanel(
            "Save Hierarchy Text",
            "",
            "Hierarchy.txt",
            "txt"
        );

        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log("Hierarchy exported to: " + path);
        }
    }

    static void AppendObjectAndChildren(Transform transform, StringBuilder sb, int level)
    {
        string indent = new string('-', level * 2);

        sb.AppendLine($"{indent}{transform.name}");
        sb.AppendLine($"{indent}  Transform:");
        sb.AppendLine($"{indent}    Position: {transform.localPosition}");
        sb.AppendLine($"{indent}    Rotation: {transform.localEulerAngles}");
        sb.AppendLine($"{indent}    Scale:    {transform.localScale}");

        Component[] components = transform.GetComponents<Component>();
        sb.AppendLine($"{indent}  Components:");

        foreach (Component component in components)
        {
            if (component == null)
            {
                sb.AppendLine($"{indent}    - Missing Script");
                continue;
            }

            if (component is MonoBehaviour mono)
            {
                sb.AppendLine($"{indent}    - {mono.GetType().Name} (Script)");
                AppendInspectorFields(mono, sb, indent + "      ");
            }
            else
            {
                sb.AppendLine($"{indent}    - {component.GetType().Name}");
            }
        }

        sb.AppendLine();

        for (int i = 0; i < transform.childCount; i++)
        {
            AppendObjectAndChildren(transform.GetChild(i), sb, level + 1);
        }
    }

    static void AppendInspectorFields(MonoBehaviour mono, StringBuilder sb, string indent)
    {
        FieldInfo[] fields = mono.GetType().GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic
        );

        foreach (FieldInfo field in fields)
        {
            // Skip hidden fields
            if (Attribute.IsDefined(field, typeof(HideInInspector)))
                continue;

            // Unity shows fields if:
            // - public
            // - OR private with [SerializeField]
            bool isPublic = field.IsPublic;
            bool isSerializedPrivate = Attribute.IsDefined(field, typeof(SerializeField));

            if (!isPublic && !isSerializedPrivate)
                continue;

            object value = field.GetValue(mono);
            string valueString = FormatValue(value);

            sb.AppendLine($"{indent}{field.Name} = {valueString}");
        }
    }

    static string FormatValue(object value)
    {
        if (value == null)
            return "null";

        Type type = value.GetType();

        if (type.IsPrimitive || value is string || type.IsEnum)
            return value.ToString();

        if (value is UnityEngine.Object unityObj)
        {
            try
            {
                // Unity "fake null" safety
                if (unityObj == null)
                    return "null (Unassigned Reference)";

                return unityObj.name + $" ({type.Name})";
            }
            catch
            {
                return "(Unassigned Reference)";
            }
        }

        if (value is IEnumerable enumerable)
        {
            int count = 0;
            foreach (var _ in enumerable) count++;
            return $"[Size: {count}]";
        }

        return $"({type.Name})";
    }
}