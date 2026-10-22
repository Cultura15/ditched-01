using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MyGame.Core.Editor
{
    /// <summary>Saves generator output (meshes, materials, prefabs) as real assets under Assets/Generated.</summary>
    public static class GeneratedAssetSaver
    {
        public const string Root = "Assets/Generated";

        /// <summary>Returns Assets/Generated/{feature}/{kind}, creating it if needed. Kind: Meshes, Materials, Textures, Prefabs.</summary>
        public static string GetFolder(string feature, string kind)
        {
            string path = $"{Root}/{feature}/{kind}";
            EnsureFolder(path);
            return path;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Creates the asset, or overwrites it in place so the GUID stays stable.</summary>
        public static T SaveOrUpdate<T>(T source, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(source, path);
                return source;
            }

            EditorUtility.CopySerialized(source, existing);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        /// <summary>Saves every in-memory mesh/material under <paramref name="root"/> as assets, re-points the scene
        /// objects (renderers, mesh colliders, particle meshes) to them, then saves a prefab named <paramref name="baseName"/>.</summary>
        public static GameObject BakeToPrefab(GameObject root, string feature, string baseName)
        {
            var leftovers = new List<Object>();
            var usedNames = new HashSet<string>();
            var meshMap = new Dictionary<Mesh, Mesh>();
            var matMap = new Dictionary<Material, Material>();

            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                mf.sharedMesh = SaveMesh(mf.sharedMesh, feature, baseName, meshMap, usedNames, leftovers);

            foreach (var mc in root.GetComponentsInChildren<MeshCollider>(true))
                mc.sharedMesh = SaveMesh(mc.sharedMesh, feature, baseName, meshMap, usedNames, leftovers);

            foreach (var pr in root.GetComponentsInChildren<ParticleSystemRenderer>(true))
                pr.mesh = SaveMesh(pr.mesh, feature, baseName, meshMap, usedNames, leftovers);

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = SaveMaterial(mats[i], feature, baseName, matMap, usedNames, leftovers);
                r.sharedMaterials = mats;
            }

            // Save a clean copy so the prefab root is named after the asset, not "[Generated]".
            var copy = Object.Instantiate(root);
            copy.transform.SetParent(null);
            copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            copy.transform.localScale = Vector3.one;
            copy.name = baseName;

            string prefabPath = $"{GetFolder(feature, "Prefabs")}/{baseName}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(copy, prefabPath);
            Object.DestroyImmediate(copy);

            foreach (var o in leftovers) if (o != null) Object.DestroyImmediate(o);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return prefab;
        }

        private static Mesh SaveMesh(Mesh mesh, string feature, string baseName, Dictionary<Mesh, Mesh> map,
            HashSet<string> usedNames, List<Object> leftovers)
        {
            if (mesh == null || EditorUtility.IsPersistent(mesh)) return mesh;
            if (map.TryGetValue(mesh, out Mesh saved)) return saved;

            string name = UniqueName(mesh.name, "Mesh", usedNames, map.Count);
            saved = SaveOrUpdate(mesh, $"{GetFolder(feature, "Meshes")}/{baseName}_Mesh_{name}.asset");
            if (saved != mesh) leftovers.Add(mesh);
            map[mesh] = saved;
            return saved;
        }

        private static Material SaveMaterial(Material material, string feature, string baseName, Dictionary<Material, Material> map,
            HashSet<string> usedNames, List<Object> leftovers)
        {
            if (material == null || EditorUtility.IsPersistent(material)) return material;
            if (map.TryGetValue(material, out Material saved)) return saved;

            string name = UniqueName(material.name, "Mat", usedNames, map.Count);
            saved = SaveOrUpdate(material, $"{GetFolder(feature, "Materials")}/{baseName}_Mat_{name}.mat");
            if (saved != material) leftovers.Add(material);
            map[material] = saved;
            return saved;
        }

        // Deterministic file-safe name; falls back to a zero-padded index when the object has no name.
        private static string UniqueName(string rawName, string kind, HashSet<string> usedNames, int index)
        {
            string name = string.IsNullOrWhiteSpace(rawName) ? index.ToString("000") : rawName;
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Replace(' ', '_');

            string candidate = name;
            for (int i = 1; !usedNames.Add($"{kind}/{candidate}"); i++) candidate = $"{name}_{i}";
            return candidate;
        }
    }
}
