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

        // Transform data
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
                AppendPublicFields(mono, sb, indent + "      ");
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

    static void AppendPublicFields(MonoBehaviour mono, StringBuilder sb, string indent)
    {
        FieldInfo[] fields = mono.GetType().GetFields(
            BindingFlags.Instance | BindingFlags.Public
        );

        foreach (FieldInfo field in fields)
        {
            // Skip hidden fields
            if (Attribute.IsDefined(field, typeof(HideInInspector)))
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

        // Primitive, string, enum
        if (type.IsPrimitive || value is string || type.IsEnum)
            return value.ToString();

        // Unity Object reference
        if (value is UnityEngine.Object unityObj)
            return unityObj.name + $" ({type.Name})";

        // Arrays & Lists
        if (value is IEnumerable enumerable)
        {
            int count = 0;
            foreach (var _ in enumerable) count++;
            return $"[Size: {count}]";
        }

        return $"({type.Name})";
    }
}