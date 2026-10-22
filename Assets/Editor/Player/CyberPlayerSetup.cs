using MyGame.Core.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MyGame.Player.Editor
{
    /// <summary>One-click setup that turns the existing PlayerMovement object into the stylized cyber school avatar.</summary>
    public static class CyberPlayerSetup
    {
        private const string MenuPath = "Tools/MyGame/Player/Set Up Cyber Player";
        private const string AvatarName = "Avatar";
        private const string OldVisualSuffix = " (Old Visual)";
        private const string SpawnPointName = "Player Spawn";
        private const string Feature = "Player";
        private const string MaterialPrefix = "CyberAvatar";
        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const float ControllerRadius = 0.4f;

        private static readonly Color BodyColor = new Color(0.45f, 0.42f, 0.88f);
        private static readonly Color AccentColor = new Color(0.96f, 0.58f, 0.74f);
        private static readonly Color HeadColor = new Color(0.96f, 0.94f, 1.00f);
        private static readonly Color VisorColor = new Color(0.08f, 0.10f, 0.22f);
        private static readonly Color GlowColor = new Color(0.35f, 0.90f, 1.00f);
        private static readonly Color LegColor = new Color(0.26f, 0.24f, 0.46f);

        [MenuItem(MenuPath)]
        public static void SetUp()
        {
            Scene scene = SceneManager.GetActiveScene();
            PlayerMovement movement = FindPlayer(scene);
            if (movement == null)
            {
                Debug.LogWarning($"[{nameof(CyberPlayerSetup)}] No enabled PlayerMovement found in the active scene.");
                return;
            }

            if (Shader.Find(LitShaderName) == null)
            {
                Debug.LogWarning($"[{nameof(CyberPlayerSetup)}] URP Lit shader not found.");
                return;
            }

            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Set Up Cyber Player");

            GameObject root = movement.gameObject;
            Camera cam = movement.playerCamera != null ? movement.playerCamera : root.GetComponentInChildren<Camera>(true);

            HideOldVisuals(root, cam);
            ConfigureController(root, movement.defaultHeight);
            BuildAvatar(root);
            ConfigureCamera(movement, cam);
            MoveToSpawn(scene, root);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = root;
        }

        private static PlayerMovement FindPlayer(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (PlayerMovement movement in root.GetComponentsInChildren<PlayerMovement>())
                {
                    if (movement.enabled) return movement;
                }
            }
            return null;
        }

        // Deactivates (never deletes) old visual children such as the original capsule, which also carried its own
        // Rigidbody and CharacterController that fight the real controller.
        private static void HideOldVisuals(GameObject root, Camera cam)
        {
            foreach (Transform child in root.transform)
            {
                bool holdsCamera = cam != null && cam.transform.IsChildOf(child);
                if (holdsCamera || child.name == AvatarName || !child.gameObject.activeSelf) continue;
                if (child.GetComponentInChildren<Renderer>(true) == null) continue;

                Undo.RecordObject(child.gameObject, "Hide Old Player Visual");
                child.gameObject.SetActive(false);
                if (!child.name.EndsWith(OldVisualSuffix)) child.name += OldVisualSuffix;
            }
        }

        private static void ConfigureController(GameObject root, float height)
        {
            var controller = root.GetComponent<CharacterController>();
            Undo.RecordObject(controller, "Configure Character Controller");
            controller.height = height;
            controller.radius = ControllerRadius;
            controller.center = Vector3.up * (height * 0.5f);
            controller.stepOffset = 0.35f;
            controller.slopeLimit = 50f;
            controller.skinWidth = 0.05f;
        }

        private static void BuildAvatar(GameObject root)
        {
            Transform existing = root.transform.Find(AvatarName);
            if (existing != null) Undo.DestroyObjectImmediate(existing.gameObject);

            var avatar = new GameObject(AvatarName);
            Undo.RegisterCreatedObjectUndo(avatar, "Create Avatar");
            avatar.transform.SetParent(root.transform, false);
            avatar.AddComponent<PlayerAvatar>();
            Transform t = avatar.transform;

            Material body = SaveMaterial("Body", BodyColor, 0.35f);
            Material accent = SaveMaterial("Accent", AccentColor, 0.35f);
            Material head = SaveMaterial("Head", HeadColor, 0.5f);
            Material visor = SaveMaterial("Visor", VisorColor, 0.9f);
            Material glow = SaveMaterial("Glow", Color.Lerp(GlowColor, Color.white, 0.4f), 0.6f, GlowColor * 2.2f);
            Material legs = SaveMaterial("Legs", LegColor, 0.3f);

            for (int side = -1; side <= 1; side += 2)
            {
                Part(t, PrimitiveType.Capsule, "Leg", new Vector3(side * 0.14f, 0.3f, 0f), new Vector3(0.22f, 0.3f, 0.22f), Vector3.zero, legs);
                Part(t, PrimitiveType.Sphere, "Hand", new Vector3(side * 0.5f, 0.92f, 0.06f), Vector3.one * 0.2f, Vector3.zero, head);
                Part(t, PrimitiveType.Sphere, "Eye", new Vector3(side * 0.09f, 1.83f, 0.29f), new Vector3(0.07f, 0.09f, 0.04f), Vector3.zero, glow);
            }

            Part(t, PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.98f, 0f), new Vector3(0.72f, 0.5f, 0.58f), Vector3.zero, body);
            Part(t, PrimitiveType.Capsule, "Backpack", new Vector3(0f, 1.05f, -0.33f), new Vector3(0.42f, 0.25f, 0.22f), Vector3.zero, accent);
            Part(t, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.8f, 0f), Vector3.one * 0.62f, Vector3.zero, head);
            Part(t, PrimitiveType.Capsule, "Visor", new Vector3(0f, 1.82f, 0.2f), new Vector3(0.26f, 0.24f, 0.2f), new Vector3(0f, 0f, 90f), visor);
            Part(t, PrimitiveType.Cylinder, "Antenna", new Vector3(0f, 2.17f, 0f), new Vector3(0.03f, 0.08f, 0.03f), Vector3.zero, legs);
            Part(t, PrimitiveType.Sphere, "Antenna Tip", new Vector3(0f, 2.28f, 0f), Vector3.one * 0.08f, Vector3.zero, glow);
        }

        private static void Part(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Vector3 euler, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localRotation = Quaternion.Euler(euler);
            part.transform.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material SaveMaterial(string part, Color color, float smoothness, Color? emission = null)
        {
            var material = new Material(Shader.Find(LitShaderName)) { name = $"{MaterialPrefix}_{part}" };
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            string path = $"{GeneratedAssetSaver.GetFolder(Feature, "Materials")}/{material.name}.mat";
            Material saved = GeneratedAssetSaver.SaveOrUpdate(material, path);
            if (saved != material) Object.DestroyImmediate(material);
            return saved;
        }

        private static void ConfigureCamera(PlayerMovement movement, Camera cam)
        {
            if (cam == null)
            {
                Debug.LogWarning($"[{nameof(CyberPlayerSetup)}] Player has no camera; skipping camera setup.");
                return;
            }

            if (movement.playerCamera == null)
            {
                Undo.RecordObject(movement, "Assign Player Camera");
                movement.playerCamera = cam;
            }

            Undo.RecordObject(cam, "Configure Player Camera");
            Undo.RecordObject(cam.transform, "Configure Player Camera");
            cam.gameObject.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
            cam.transform.localPosition = new Vector3(0.35f, 1.6f, -4.5f);
            cam.transform.localRotation = Quaternion.identity;

            var data = cam.GetUniversalAdditionalCameraData();
            Undo.RecordObject(data, "Configure Player Camera");
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        private static void MoveToSpawn(Scene scene, GameObject player)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>())
                {
                    if (t.name != SpawnPointName) continue;

                    Undo.RecordObject(player.transform, "Move Player To Spawn");
                    player.transform.SetPositionAndRotation(t.position, t.rotation);
                    return;
                }
            }

            Debug.Log($"[{nameof(CyberPlayerSetup)}] No '{SpawnPointName}' found; generate the Cyber School first to auto-place the player.");
        }
    }
}
