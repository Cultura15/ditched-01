
using System.Collections.Generic;
using UnityEngine;

public class NatureGenerator : MonoBehaviour
{
    [Header("World Settings")]
    [Min(10)] public int worldSize = 50;
    [Min(0)] public int grassCount = 300;
    [Min(0)] public int treeCount = 20;
    [Min(0)] public int plantCount = 50;
    [Min(0)] public int stoneCount = 15;
    public int seed = 12345;

    private const string RootName = "Generated Environment";

    private Transform generatedRoot;
    private readonly Dictionary<Color, Material> materials =
        new Dictionary<Color, Material>();

    [ContextMenu("Generate Environment")]
    public void GenerateEnvironment()
    {
        ClearEnvironment();

        Random.InitState(seed);

        GameObject rootObject = new GameObject(RootName);
        generatedRoot = rootObject.transform;
        generatedRoot.SetParent(transform, false);

        Transform groundGroup = CreateGroup("Ground");
        Transform grassGroup = CreateGroup("Grass");
        Transform treeGroup = CreateGroup("Trees");
        Transform plantGroup = CreateGroup("Plants");
        Transform stoneGroup = CreateGroup("Stone Plates");

        // Ground
        GameObject ground =
            GameObject.CreatePrimitive(PrimitiveType.Plane);

        ground.name = "Ground";
        ground.transform.localScale =
            Vector3.one * (worldSize / 10f);
        ground.GetComponent<Renderer>().sharedMaterial =
            GetMaterial(new Color(0.25f, 0.4f, 0.18f));
        ground.transform.SetParent(groundGroup, true);

        // Grass
        for (int i = 0; i < grassCount; i++)
        {
            GameObject grass =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            grass.name = $"Grass_{i + 1:000}";
            grass.transform.position =
                RandomPosition() + Vector3.up * 0.2f;
            grass.transform.localScale = new Vector3(
                0.07f, Random.Range(0.2f, 0.5f), 0.07f);
            grass.transform.rotation =
                Quaternion.Euler(0, Random.Range(0, 360), 0);

            grass.GetComponent<Renderer>().sharedMaterial =
                GetMaterial(new Color(
                    0.15f, Random.Range(0.4f, 0.75f), 0.1f));

            RemoveCollider(grass);
            grass.transform.SetParent(grassGroup, true);
        }

        // Trees
        for (int i = 0; i < treeCount; i++)
        {
            GameObject tree = new GameObject($"Tree_{i + 1:000}");
            tree.transform.position = RandomPosition();
            tree.transform.SetParent(treeGroup, true);

            GameObject trunk =
                GameObject.CreatePrimitive(PrimitiveType.Cylinder);

            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = Vector3.up * 1.5f;
            trunk.transform.localScale =
                new Vector3(0.35f, 1.5f, 0.35f);
            trunk.GetComponent<Renderer>().sharedMaterial =
                GetMaterial(new Color(0.35f, 0.18f, 0.08f));

            GameObject leaves =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            leaves.name = "Leaves";
            leaves.transform.SetParent(tree.transform, false);
            leaves.transform.localPosition = Vector3.up * 3.5f;
            leaves.transform.localScale = Vector3.one * 2.5f;
            leaves.GetComponent<Renderer>().sharedMaterial =
                GetMaterial(new Color(0.08f, 0.35f, 0.08f));
        }

        // Small plants
        for (int i = 0; i < plantCount; i++)
        {
            GameObject plant =
                GameObject.CreatePrimitive(PrimitiveType.Sphere);

            plant.name = $"Plant_{i + 1:000}";
            plant.transform.position =
                RandomPosition() + Vector3.up * 0.2f;
            plant.transform.localScale =
                new Vector3(0.3f, 0.4f, 0.3f);
            plant.GetComponent<Renderer>().sharedMaterial =
                GetMaterial(Color.green);

            RemoveCollider(plant);
            plant.transform.SetParent(plantGroup, true);
        }

        // Stone plates
        for (int i = 0; i < stoneCount; i++)
        {
            GameObject stone =
                GameObject.CreatePrimitive(PrimitiveType.Cube);

            stone.name = $"StonePlate_{i + 1:000}";
            stone.transform.position =
                RandomPosition() + Vector3.up * 0.1f;
            stone.transform.localScale = new Vector3(
                Random.Range(1f, 2f), 0.2f,
                Random.Range(1f, 2f));
            stone.transform.rotation =
                Quaternion.Euler(0, Random.Range(0, 360), 0);
            stone.GetComponent<Renderer>().sharedMaterial =
                GetMaterial(Color.gray);

            stone.transform.SetParent(stoneGroup, true);
        }

        Debug.Log(
            $"NatureGenerator: Generated environment. " +
            $"Grass: {grassCount}, Trees: {treeCount}, " +
            $"Plants: {plantCount}, Stones: {stoneCount}.",
            this);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(gameObject);
            UnityEditor.SceneManagement.EditorSceneManager
                .MarkSceneDirty(gameObject.scene);
        }
#endif
    }

    private Transform CreateGroup(string groupName)
    {
        GameObject group = new GameObject(groupName);
        group.transform.SetParent(generatedRoot, false);
        return group.transform;
    }

    private Vector3 RandomPosition()
    {
        float half = worldSize / 2f;

        return new Vector3(
            Random.Range(-half, half),
            0f,
            Random.Range(-half, half));
    }

    private Material GetMaterial(Color color)
    {
        if (materials.TryGetValue(color, out Material existing)
            && existing != null)
        {
            return existing;
        }

        Shader shader =
            Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
            shader = Shader.Find("HDRP/Lit");

        if (shader == null)
            shader = Shader.Find("Standard");

        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            Debug.LogError(
                "NatureGenerator: No compatible shader was found.",
                this);
            return null;
        }

        Material material = new Material(shader);
        material.color = color;
        material.name = $"NatureMaterial_{color}";
        materials[color] = material;

        return material;
    }

    private static void RemoveCollider(GameObject obj)
    {
        Collider col = obj.GetComponent<Collider>();

        if (col == null)
            return;

        if (Application.isPlaying)
            Destroy(col);
        else
            DestroyImmediate(col);
    }

    public void ClearEnvironment()
    {
        Transform existing = transform.Find(RootName);

        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }

        generatedRoot = null;

        foreach (Material material in materials.Values)
        {
            if (material == null)
                continue;

            if (Application.isPlaying)
                Destroy(material);
            else
                DestroyImmediate(material);
        }

        materials.Clear();
    }
}
