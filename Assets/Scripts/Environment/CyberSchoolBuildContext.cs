using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MyGame.Environment
{
    /// <summary>Shared state for one generation pass: shared materials, shared meshes, and object helpers.</summary>
    public class CyberSchoolBuildContext
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        private static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Shader _litShader;
        private readonly Shader _particleShader;
        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        private readonly Dictionary<string, Mesh> _meshes = new Dictionary<string, Mesh>();
        private Mesh _cubeMesh;

        public CyberSchoolBuildContext(Transform root, System.Random rng, CyberSchoolPalette palette, Shader litShader, Shader particleShader)
        {
            Root = root;
            Rng = rng;
            Palette = palette;
            _litShader = litShader;
            _particleShader = particleShader;
        }

        public Transform Root { get; }
        public System.Random Rng { get; }
        public CyberSchoolPalette Palette { get; }

        public float Range(float min, float max) => min + (float)Rng.NextDouble() * (max - min);

        public T Pick<T>(IReadOnlyList<T> items) => items[Rng.Next(items.Count)];

        /// <summary>Returns a shared opaque URP Lit material.</summary>
        public Material Lit(string key, Color color, float smoothness = 0.25f, float metallic = 0f)
        {
            if (_materials.TryGetValue(key, out Material existing)) return existing;

            var material = new Material(_litShader) { name = key };
            material.SetColor(BaseColorId, color);
            material.SetFloat(SmoothnessId, smoothness);
            material.SetFloat(MetallicId, metallic);
            material.enableInstancing = true;
            _materials[key] = material;
            return material;
        }

        /// <summary>Returns a shared Lit material that glows (HDR emission picked up by bloom).</summary>
        public Material Emissive(string key, Color baseColor, Color emission, float intensity, float smoothness = 0.5f)
        {
            if (_materials.TryGetValue(key, out Material existing)) return existing;

            Material material = Lit(key, baseColor, smoothness);
            material.EnableKeyword("_EMISSION");
            material.SetColor(EmissionColorId, emission * intensity);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            return material;
        }

        /// <summary>Returns a shared opaque particle material with an HDR tint.</summary>
        public Material ParticleUnlit(string key, Color color, float intensity)
        {
            if (_materials.TryGetValue(key, out Material existing)) return existing;

            var material = new Material(_particleShader) { name = key };
            material.SetColor(BaseColorId, color * intensity);
            _materials[key] = material;
            return material;
        }

        /// <summary>Returns a cached mesh, building it on first use.</summary>
        public Mesh SharedMesh(string key, System.Func<LowPolyMeshBuilder, LowPolyMeshBuilder> build)
        {
            if (_meshes.TryGetValue(key, out Mesh existing)) return existing;

            Mesh mesh = build(new LowPolyMeshBuilder()).ToMesh(key);
            _meshes[key] = mesh;
            return mesh;
        }

        public Transform Group(string name, Transform parent = null)
        {
            return Group(name, Vector3.zero, 0f, parent);
        }

        public Transform Group(string name, Vector3 position, float yaw, Transform parent = null)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent != null ? parent : Root, false);
            group.localPosition = position;
            group.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return group;
        }

        /// <summary>Creates a box using Unity's built-in cube mesh, optionally with a BoxCollider.</summary>
        public GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material,
            bool collider, float pitch = 0f)
        {
            GameObject box = MeshObject(parent, name, CubeMesh(), material, position, Quaternion.Euler(pitch, 0f, 0f), size);
            if (collider) box.AddComponent<BoxCollider>();
            return box;
        }

        public GameObject MeshObject(Transform parent, string name, Mesh mesh, Material material,
            Vector3 position, Quaternion rotation, Vector3 scale, bool castShadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation;
            go.transform.localScale = scale;
            go.isStatic = true;

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        public Light PointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.None;
            return light;
        }

        private Mesh CubeMesh()
        {
            if (_cubeMesh != null) return _cubeMesh;

            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(temp);
            else Object.DestroyImmediate(temp);
            return _cubeMesh;
        }
    }
}
