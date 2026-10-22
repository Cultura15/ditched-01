using MyGame.Core.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyGame.Environment.Editor
{
    [CustomEditor(typeof(CyberSchoolGenerator))]
    public class CyberSchoolGeneratorEditor : UnityEditor.Editor
    {
        private const string Feature = "Environment";
        private const string DefaultMusicPath = "Assets/Audio/Music/CyberSchoolAmbient.wav";
        private const string OldEnvironmentName = "Generated Environment";

        private GameObject _oldEnvironment;

        private void OnEnable()
        {
            _oldEnvironment = FindByName(((CyberSchoolGenerator)target).gameObject.scene, OldEnvironmentName);
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var generator = (CyberSchoolGenerator)target;

            EditorGUILayout.Space();
            DrawOldEnvironmentNotice();

            using (new EditorGUI.DisabledScope(!generator.CanGenerate))
            {
                if (GUILayout.Button("Generate & Save Assets", GUILayout.Height(32)))
                    Run(generator, "Generate & Save Cyber School", () => GenerateAndSave(generator));

                if (GUILayout.Button("Generate (Preview Only)"))
                    Run(generator, "Generate Cyber School", generator.Generate);

                if (GUILayout.Button("Randomize Seed & Generate"))
                    Run(generator, "Generate Cyber School (Random Seed)", () =>
                    {
                        generator.Seed = System.Environment.TickCount;
                        generator.Generate();
                    });
            }

            if (GUILayout.Button("Clear") && ConfirmClear(generator))
                Run(generator, "Clear Cyber School", generator.Clear);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Atmosphere", EditorStyles.boldLabel);
            if (GUILayout.Button("Apply Sky, Fog, Lighting & Post-Processing", GUILayout.Height(28)))
            {
                CyberSchoolAtmosphere.Apply(generator.gameObject.scene);
                EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
            }

            if (GUILayout.Button("Ping Output Folder"))
                EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>($"{GeneratedAssetSaver.Root}/{Feature}"));
        }

        private void DrawOldEnvironmentNotice()
        {
            if (_oldEnvironment == null || !_oldEnvironment.activeSelf) return;

            EditorGUILayout.HelpBox($"The old '{OldEnvironmentName}' from NatureGenerator is still active and will overlap the new school.", MessageType.Info);
            if (GUILayout.Button("Hide Old Nature Environment"))
            {
                Undo.RecordObject(_oldEnvironment, "Hide Old Nature Environment");
                _oldEnvironment.SetActive(false);
                EditorSceneManager.MarkSceneDirty(_oldEnvironment.scene);
            }
            EditorGUILayout.Space();
        }

        private static void GenerateAndSave(CyberSchoolGenerator generator)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Cyber School", "Generating environment...", 0.3f);
                generator.Generate();

                Transform container = generator.transform.Find(CyberSchoolGenerator.ContainerName);
                if (container == null) return;

                EditorUtility.DisplayProgressBar("Cyber School", "Saving meshes, materials and prefab...", 0.7f);
                GeneratedAssetSaver.BakeToPrefab(container.gameObject, Feature, generator.OutputName);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void Run(CyberSchoolGenerator generator, string undoName, System.Action action)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, undoName);

            if (generator.AmbientMusic == null)
                generator.AmbientMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultMusicPath);

            action();

            EditorUtility.SetDirty(generator);
            EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
        }

        private static bool ConfirmClear(CyberSchoolGenerator generator)
        {
            if (generator.transform.Find(CyberSchoolGenerator.ContainerName) == null) return false;
            return EditorUtility.DisplayDialog("Clear Cyber School",
                "Remove the generated school from the scene? Saved assets in Assets/Generated are kept.", "Clear", "Cancel");
        }

        private static GameObject FindByName(UnityEngine.SceneManagement.Scene scene, string objectName)
        {
            if (!scene.IsValid()) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == objectName) return root;
                foreach (Transform child in root.transform)
                {
                    if (child.name == objectName) return child.gameObject;
                }
            }
            return null;
        }
    }
}
