using MyGame.Core.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MyGame.Environment.Editor
{
    /// <summary>Applies the dreamy late-afternoon look: skybox, fog, ambient light, sun and post-processing.
    /// Sky material and volume profile are created once; later runs keep your manual tweaks.</summary>
    public static class CyberSchoolAtmosphere
    {
        public const string SkyboxMaterialPath = "Assets/Materials/Environment/CyberSchoolSky.mat";
        public const string VolumeProfilePath = "Assets/Settings/CyberSchoolVolumeProfile.asset";

        private const string SkyboxShaderName = "MyGame/Environment/DreamySkybox";
        private const string VolumeObjectName = "Cyber School Atmosphere";
        private const float VolumePriority = 10f;

        private static readonly Vector3 SunDirection = new Vector3(-0.82f, 0.30f, -0.18f).normalized;
        private static readonly Color SunColor = new Color(1.00f, 0.77f, 0.64f);
        private const float SunIntensity = 1.15f;

        private static readonly Color AmbientSky = new Color(0.50f, 0.46f, 0.80f);
        private static readonly Color AmbientEquator = new Color(0.54f, 0.45f, 0.64f);
        private static readonly Color AmbientGround = new Color(0.20f, 0.19f, 0.31f);

        private static readonly Color FogColor = new Color(0.58f, 0.52f, 0.82f);
        private const float FogStart = 30f;
        private const float FogEnd = 190f;

        public static void Apply(Scene scene)
        {
            Light sun = FindOrCreateSun(scene);
            Material sky = GetOrCreateSkybox();
            if (sky != null)
            {
                Undo.RecordObject(sky, "Apply Cyber School Atmosphere");
                sky.SetVector("_SunDirection", SunDirection);
                EditorUtility.SetDirty(sky);
            }

            ApplyRenderSettings(sky, sun);
            ApplyVolume(scene, GetOrCreateVolumeProfile());
            EnablePostProcessingOnCameras();
            DynamicGI.UpdateEnvironment();
            AssetDatabase.SaveAssets();
        }

        private static Light FindOrCreateSun(Scene scene)
        {
            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Light light in root.GetComponentsInChildren<Light>(true))
                    {
                        if (light.type == LightType.Directional) { sun = light; break; }
                    }
                    if (sun != null) break;
                }
            }

            if (sun == null)
            {
                var go = new GameObject("Directional Light");
                Undo.RegisterCreatedObjectUndo(go, "Create Sun");
                sun = go.AddComponent<Light>();
                sun.type = LightType.Directional;
            }

            Undo.RecordObject(sun, "Apply Cyber School Sun");
            Undo.RecordObject(sun.transform, "Apply Cyber School Sun");
            sun.transform.rotation = Quaternion.LookRotation(-SunDirection);
            sun.color = SunColor;
            sun.intensity = SunIntensity;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            return sun;
        }

        private static Material GetOrCreateSkybox()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(SkyboxMaterialPath);
            if (existing != null) return existing;

            Shader shader = Shader.Find(SkyboxShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[{nameof(CyberSchoolAtmosphere)}] Shader '{SkyboxShaderName}' not found. Check the Console for shader errors.");
                return null;
            }

            GeneratedAssetSaver.EnsureFolder(System.IO.Path.GetDirectoryName(SkyboxMaterialPath).Replace('\\', '/'));
            var material = new Material(shader) { name = "CyberSchoolSky" };
            AssetDatabase.CreateAsset(material, SkyboxMaterialPath);
            return material;
        }

        private static void ApplyRenderSettings(Material sky, Light sun)
        {
            if (sky != null) RenderSettings.skybox = sky;
            RenderSettings.sun = sun;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = AmbientSky;
            RenderSettings.ambientEquatorColor = AmbientEquator;
            RenderSettings.ambientGroundColor = AmbientGround;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 0.6f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = FogStart;
            RenderSettings.fogEndDistance = FogEnd;
        }

        private static VolumeProfile GetOrCreateVolumeProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile != null) return profile;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);

            Bloom bloom = AddComponent<Bloom>(profile);
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.7f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.93f, 1f));

            Tonemapping tonemapping = AddComponent<Tonemapping>(profile);
            tonemapping.mode.Override(TonemappingMode.Neutral);

            ColorAdjustments color = AddComponent<ColorAdjustments>(profile);
            color.postExposure.Override(0.15f);
            color.contrast.Override(-4f);
            color.saturation.Override(6f);

            Vignette vignette = AddComponent<Vignette>(profile);
            vignette.color.Override(new Color(0.12f, 0.08f, 0.25f));
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.45f);

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static T AddComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            T component = profile.Add<T>();
            component.name = typeof(T).Name;
            AssetDatabase.AddObjectToAsset(component, profile);
            return component;
        }

        private static void ApplyVolume(Scene scene, VolumeProfile profile)
        {
            Volume volume = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == VolumeObjectName && root.TryGetComponent(out volume)) break;
            }

            if (volume == null)
            {
                var go = new GameObject(VolumeObjectName);
                SceneManager.MoveGameObjectToScene(go, scene);
                Undo.RegisterCreatedObjectUndo(go, "Create Atmosphere Volume");
                volume = go.AddComponent<Volume>();
            }

            Undo.RecordObject(volume, "Apply Atmosphere Volume");
            volume.isGlobal = true;
            volume.priority = VolumePriority;
            volume.sharedProfile = profile;
        }

        private static void EnablePostProcessingOnCameras()
        {
            foreach (Camera cam in Camera.allCameras)
            {
                var data = cam.GetUniversalAdditionalCameraData();
                Undo.RecordObject(data, "Enable Post Processing");
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                EditorUtility.SetDirty(data);
            }
        }
    }
}
