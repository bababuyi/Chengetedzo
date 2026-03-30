using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System;

public class ExportHierarchyToText : EditorWindow
{
    MonoScript targetScript;

    [MenuItem("Window/Export Hierarchy to Text")]
    public static void ShowWindow()
    {
        GetWindow<ExportHierarchyToText>("Export Hierarchy");
    }

    void OnGUI()
    {
        GUILayout.Label("Optional Script Filter", EditorStyles.boldLabel);

        targetScript = (MonoScript)EditorGUILayout.ObjectField(
            "Script To Copy Values From",
            targetScript,
            typeof(MonoScript),
            false
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Export Current Scene Hierarchy"))
        {
            ExportHierarchy(targetScript);
        }
    }

    static void ExportHierarchy(MonoScript filterScript)
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("=== Scene Hierarchy Export ===");
        sb.AppendLine("Scene: " + SceneManager.GetActiveScene().name);

        if (filterScript != null)
            sb.AppendLine("Filtered Script: " + filterScript.name);

        sb.AppendLine();

        foreach (GameObject go in rootObjects)
        {
            AppendObjectAndChildren(go.transform, sb, 0, filterScript);
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

    static void AppendObjectAndChildren(Transform transform, StringBuilder sb, int level, MonoScript filterScript)
    {
        string indent = new string('-', level * 2);

        sb.AppendLine($"{indent}{transform.name}");
        sb.AppendLine($"{indent}  Transform:");
        sb.AppendLine($"{indent}    Position: {transform.localPosition}");
        sb.AppendLine($"{indent}    Rotation: {transform.localEulerAngles}");
        sb.AppendLine($"{indent}    Scale:    {transform.localScale}");

        Component[] components = transform.GetComponents<Component>();
        sb.AppendLine($"{indent}  Components:");

        Type filterType = null;

        if (filterScript != null)
            filterType = filterScript.GetClass();

        foreach (Component component in components)
        {
            if (component == null)
            {
                sb.AppendLine($"{indent}    - Missing Script");
                continue;
            }

            if (filterType != null && component.GetType() != filterType)
                continue;

            if (component is MonoBehaviour mono)
            {
                sb.AppendLine($"{indent}    - {mono.GetType().Name} (Script)");
                AppendSerializedFields(mono, sb, indent + "      ");
            }
            else if (filterType == null)
            {
                sb.AppendLine($"{indent}    - {component.GetType().Name}");
            }
        }

        sb.AppendLine();

        for (int i = 0; i < transform.childCount; i++)
        {
            AppendObjectAndChildren(transform.GetChild(i), sb, level + 1, filterScript);
        }
    }

    static void AppendSerializedFields(MonoBehaviour mono, StringBuilder sb, string indent)
    {
        SerializedObject so = new SerializedObject(mono);
        SerializedProperty prop = so.GetIterator();

        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = true;

            if (prop.name == "m_Script")
                continue;

            sb.AppendLine($"{indent}{prop.displayName}: {PropertyToString(prop)}");
        }
    }

    static string PropertyToString(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                return prop.intValue.ToString();

            case SerializedPropertyType.Boolean:
                return prop.boolValue.ToString();

            case SerializedPropertyType.Float:
                return prop.floatValue.ToString();

            case SerializedPropertyType.String:
                return prop.stringValue;

            case SerializedPropertyType.Enum:
                return prop.enumDisplayNames[prop.enumValueIndex];

            case SerializedPropertyType.ObjectReference:
                return prop.objectReferenceValue ? prop.objectReferenceValue.name : "None";

            case SerializedPropertyType.Vector2:
                return prop.vector2Value.ToString();

            case SerializedPropertyType.Vector3:
                return prop.vector3Value.ToString();

            case SerializedPropertyType.Generic:
                if (prop.isArray)
                    return $"Array Size: {prop.arraySize}";
                return "(Struct/Class)";

            default:
                return "(Unsupported)";
        }
    }
}

public static class DevShortcuts
{
    [MenuItem("Dev/Full Reset (Save + Prefs) %#r")]
    public static void FullReset()
    {
        SaveSystem.DeleteSave();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        var gm = UnityEngine.Object.FindObjectOfType<GameManager>();
        gm?.FullRestart();
        Debug.Log("[DEV] Hot reset triggered.");
    }

    [MenuItem("Dev/Reset Tutorials Only %#t")]
    public static void ResetTutorials()
    {
        TutorialManager.Instance?.ResetAll();
        Debug.Log("[DEV] Tutorial flags cleared. Tutorials will replay.");
    }
}