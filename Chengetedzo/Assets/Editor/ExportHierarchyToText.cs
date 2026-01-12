using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public class ExportHierarchyToText : EditorWindow
{
    // Add a menu item to the Window menu
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
        // Get all root game objects in the currently active scene
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        StringBuilder sb = new StringBuilder();

        foreach (GameObject go in rootObjects)
        {
            AppendObjectAndChildren(go.transform, sb, 0);
        }

        // Save the string to a file
        string path = EditorUtility.SaveFilePanel("Save Hierarchy Text", "", "Hierarchy.txt", "txt");
        if (!string.IsNullOrEmpty(path))
        {
            File.WriteAllText(path, sb.ToString());
            Debug.Log("Hierarchy exported to: " + path);
        }
    }

    // Recursive function to go through all children
    static void AppendObjectAndChildren(Transform transform, StringBuilder sb, int level)
    {
        // Add indentation based on the hierarchy level
        sb.AppendLine(new string('-', level * 2) + transform.name);

        // Recursively call for all children
        for (int i = 0; i < transform.childCount; i++)
        {
            AppendObjectAndChildren(transform.GetChild(i), sb, level + 1);
        }
    }
}
