
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NatureGenerator))]
public class NatureGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NatureGenerator generator =
            (NatureGenerator)target;

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.45f, 0.8f, 0.45f);

        if (GUILayout.Button("Generate Environment",
            GUILayout.Height(35)))
        {
            Undo.RegisterFullObjectHierarchyUndo(
                generator.gameObject, "Generate Environment");

            generator.GenerateEnvironment();
        }

        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Clear Generated Objects",
            GUILayout.Height(28)))
        {
            Undo.RegisterFullObjectHierarchyUndo(
                generator.gameObject, "Clear Environment");

            generator.ClearEnvironment();
        }
    }
}
#endif
