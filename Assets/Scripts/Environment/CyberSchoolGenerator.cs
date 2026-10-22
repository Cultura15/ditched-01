using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Environment
{
    /// <summary>Generates a lightweight low-poly cyber school: plaza, paths, buildings, lamps, benches and vegetation.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("MyGame/Environment/Cyber School Generator")]
    public class CyberSchoolGenerator : MonoBehaviour
    {
        public const string ContainerName = "[Generated]";
        public const string SpawnPointName = "Player Spawn";

        private const string LitShaderName = "Universal Render Pipeline/Lit";
        private const string ParticleShaderName = "Universal Render Pipeline/Particles/Unlit";
        private const float PathTop = 0.1f;
        private const float PlazaTop = 0.12f;
        private const int PlacementAttempts = 40;
        private const float MainHallDistance = 28f;
        private const float SideBuildingDistance = 25f;
        private const float SideBuildingHalfDepth = 5f;
        private const float RotundaRadius = 6f;
        private const int TuftsPerClump = 5;
        private const float ClumpRadius = 0.9f;

        [Header("Output")]
        [SerializeField, Tooltip("Prefix for saved assets under Assets/Generated/Environment.")]
        private string _outputName = "CyberSchool";

        [Header("Shaders (auto-filled when empty)")]
        [SerializeField] private Shader _litShader;
        [SerializeField] private Shader _particleShader;

        [Header("Ground")]
        [SerializeField, Min(40f)] private float _groundRadius = 130f;
        [SerializeField, Min(1f)] private float _groundCellSize = 6f;
        [SerializeField, Min(20f), Tooltip("Ground is perfectly flat inside this radius; hills rise beyond it.")]
        private float _flatRadius = 52f;
        [SerializeField, Min(0f)] private float _hillHeight = 9f;

        [Header("Layout")]
        [SerializeField, Min(5f)] private float _plazaRadius = 9f;
        [SerializeField, Min(2f)] private float _pathWidth = 4f;
        [SerializeField, Min(12f)] private float _southPathLength = 32f;
        [SerializeField, Min(3f)] private float _lampSpacing = 8f;

        [Header("Vegetation")]
        [SerializeField, Range(0, 80)] private int _treeCount = 30;
        [SerializeField, Range(0f, 1f)] private float _blossomChance = 0.25f;
        [SerializeField, Range(0, 40)] private int _bushCount = 18;
        [SerializeField, Range(0, 2000)] private int _grassTuftCount = 700;
        [SerializeField, Range(0, 300)] private int _flowerCount = 70;
        [SerializeField, Min(15f)] private float _vegetationRadius = 48f;
        [SerializeField, Min(1f)] private float _minTreeSpacing = 4.5f;

        [Header("Lamps")]
        [SerializeField, Min(0f)] private float _lampGlowIntensity = 4f;
        [SerializeField, Min(0f)] private float _lampLightIntensity = 3f;
        [SerializeField, Min(1f)] private float _lampLightRange = 8f;

        [Header("Ambience")]
        [SerializeField, Range(0, 60), Tooltip("Tiny glowing motes drifting over the plaza. Keep it low.")]
        private int _moteCount = 16;
        [SerializeField] private AudioClip _ambientMusic;
        [SerializeField, Range(0f, 1f)] private float _musicVolume = 0.2f;

        [Header("Palette")]
        [SerializeField] private CyberSchoolPalette _palette = new CyberSchoolPalette();

        [Header("Randomness")]
        [SerializeField] private int _seed = 2026;
        [SerializeField] private bool _randomizeSeed;

        private readonly List<Rect> _blockedRects = new List<Rect>();
        private readonly List<Vector3> _treePositions = new List<Vector3>();
        private int _lampColorIndex;

        public bool CanGenerate => ResolveShaders() && HasValidPalette();
        public int Seed { get => _seed; set => _seed = value; }
        public string OutputName => string.IsNullOrWhiteSpace(_outputName) ? nameof(CyberSchoolGenerator) : _outputName;
        public AudioClip AmbientMusic { get => _ambientMusic; set => _ambientMusic = value; }

        /// <summary>Clears and rebuilds the whole environment under the "[Generated]" child.</summary>
        public void Generate()
        {
            if (!ResolveShaders())
            {
                Debug.LogWarning($"[{nameof(CyberSchoolGenerator)}] URP Lit / Particles shaders not found. Is URP installed?", this);
                return;
            }

            if (!HasValidPalette())
            {
                Debug.LogWarning($"[{nameof(CyberSchoolGenerator)}] Palette needs at least one Foliage, Blossom and Lamp color.", this);
                return;
            }

            Clear();
            if (_randomizeSeed) _seed = System.Environment.TickCount;

            var container = new GameObject(ContainerName).transform;
            container.SetParent(transform, false);
            var ctx = new CyberSchoolBuildContext(container, new System.Random(_seed), _palette, _litShader, _particleShader);

            _blockedRects.Clear();
            _treePositions.Clear();
            _lampColorIndex = 0;

            BuildGround(ctx);
            BuildPlaza(ctx);
            BuildPathsAndLamps(ctx);
            BuildBuildings(ctx);
            BuildBenches(ctx);
            ScatterVegetation(ctx);
            BuildMotes(ctx);
            BuildMusic(ctx);
            BuildSpawnPoint(ctx);
        }

        /// <summary>Removes the generated container only. Hand-placed objects are never touched.</summary>
        public void Clear()
        {
            Transform container = transform.Find(ContainerName);
            if (container == null) return;

            if (Application.isPlaying) Destroy(container.gameObject);
            else DestroyImmediate(container.gameObject);
        }

        private bool ResolveShaders()
        {
            if (_litShader == null) _litShader = Shader.Find(LitShaderName);
            if (_particleShader == null) _particleShader = Shader.Find(ParticleShaderName);
            return _litShader != null && _particleShader != null;
        }

        private bool HasValidPalette()
        {
            return _palette != null
                && _palette.Foliage != null && _palette.Foliage.Length > 0
                && _palette.Blossom != null && _palette.Blossom.Length > 0
                && _palette.LampColors != null && _palette.LampColors.Length > 0;
        }

        private void BuildGround(CyberSchoolBuildContext ctx)
        {
            float offsetX = ctx.Range(0f, 1000f), offsetZ = ctx.Range(0f, 1000f);
            Mesh terrain = new LowPolyMeshBuilder()
                .AddTerrain(_groundRadius, _groundCellSize, 0.3f, (x, z) => GroundHeight(x, z, offsetX, offsetZ), ctx.Rng)
                .ToMesh("Terrain");

            Material grass = ctx.Lit("Ground", _palette.Grass, 0.1f);
            GameObject ground = ctx.MeshObject(ctx.Root, "Ground", terrain, grass, Vector3.zero, Quaternion.identity, Vector3.one);
            ground.AddComponent<MeshCollider>().sharedMesh = terrain;
        }

        private float GroundHeight(float x, float z, float offsetX, float offsetZ)
        {
            float distance = new Vector2(x, z).magnitude;
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_flatRadius, _flatRadius + 35f, distance));
            if (rise <= 0f) return 0f;

            float noise = Mathf.PerlinNoise(x * 0.025f + offsetX, z * 0.025f + offsetZ);
            return rise * _hillHeight * (0.45f + 0.75f * noise);
        }

        private void BuildPlaza(CyberSchoolBuildContext ctx)
        {
            Transform plaza = ctx.Group("Plaza");
            Vector2[] disc = { new Vector2(0f, 0f), new Vector2(_plazaRadius, 0f), new Vector2(_plazaRadius, PlazaTop), new Vector2(0f, PlazaTop) };
            Mesh discMesh = ctx.SharedMesh("PlazaDisc", b => b.AddLathe(disc, 32));
            GameObject floor = ctx.MeshObject(plaza, "Plaza Floor", discMesh, ctx.Lit("Plaza", _palette.Plaza, 0.35f),
                Vector3.zero, Quaternion.identity, Vector3.one);
            floor.AddComponent<MeshCollider>().sharedMesh = discMesh;

            float inlay = _plazaRadius * 0.7f;
            Vector2[] ring = { new Vector2(inlay + 0.25f, PlazaTop + 0.01f), new Vector2(inlay, PlazaTop + 0.01f) };
            Mesh ringMesh = ctx.SharedMesh("PlazaInlay", b => b.AddLathe(ring, 48));
            ctx.MeshObject(plaza, "Plaza Inlay", ringMesh, ctx.Emissive("PlazaInlay", _palette.Trim, _palette.Trim, 0.8f),
                Vector3.zero, Quaternion.identity, Vector3.one, false);

            CyberSchoolProps.Centerpiece(ctx, plaza, new Vector3(0f, PlazaTop, 0f));
            _blockedRects.Add(Rect.MinMaxRect(-_plazaRadius, -_plazaRadius, _plazaRadius, _plazaRadius));

            Transform lamps = ctx.Group("Plaza Lamps", plaza);
            for (int i = 0; i < 4; i++)
            {
                float angle = (45f + i * 90f) * Mathf.Deg2Rad;
                var position = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * (_plazaRadius - 0.8f) + Vector3.up * PlazaTop;
                AddLamp(ctx, lamps, position);
            }
        }

        private void BuildPathsAndLamps(CyberSchoolBuildContext ctx)
        {
            Transform paths = ctx.Group("Paths");
            float start = _plazaRadius - 0.5f;
            float southEnd = _plazaRadius + _southPathLength;

            BuildPath(ctx, paths, "North Path", Vector3.forward, start, MainHallDistance - SideBuildingHalfDepth - 1.5f);
            BuildPath(ctx, paths, "South Path", Vector3.back, start, southEnd);
            BuildPath(ctx, paths, "West Path", Vector3.left, start, SideBuildingDistance - SideBuildingHalfDepth - 1.5f);
            BuildPath(ctx, paths, "East Path", Vector3.right, start, SideBuildingDistance - RotundaRadius - 1.5f);

            CyberSchoolBuildings.Gate(ctx, ctx.Root, new Vector3(0f, PathTop, -(southEnd - 5f)), _pathWidth);
        }

        private void BuildPath(CyberSchoolBuildContext ctx, Transform parent, string name, Vector3 direction, float from, float to)
        {
            Transform path = ctx.Group(name, parent);
            float length = to - from;
            Vector3 center = direction * (from + length * 0.5f) + Vector3.up * (PathTop * 0.5f);
            bool alongZ = Mathf.Abs(direction.z) > 0.5f;
            var size = alongZ ? new Vector3(_pathWidth, PathTop, length) : new Vector3(length, PathTop, _pathWidth);
            ctx.Box(path, "Path Surface", center, size, ctx.Lit("Path", _palette.Path, 0.2f), true);

            var rect = new Rect(center.x - size.x * 0.5f, center.z - size.z * 0.5f, size.x, size.z);
            _blockedRects.Add(Inflate(rect, 1.5f));

            Vector3 side = Vector3.Cross(Vector3.up, direction);
            int index = 0;
            for (float t = from + 4f; t < to - 1f; t += _lampSpacing, index++)
            {
                float sideSign = index % 2 == 0 ? 1f : -1f;
                AddLamp(ctx, path, direction * t + side * sideSign * (_pathWidth * 0.5f + 0.9f));
            }
        }

        private void BuildBuildings(CyberSchoolBuildContext ctx)
        {
            Transform buildings = ctx.Group("Buildings");
            CyberSchoolBuildings.MainHall(ctx, buildings, new Vector3(0f, 0f, MainHallDistance), 0f);
            CyberSchoolBuildings.Library(ctx, buildings, new Vector3(-SideBuildingDistance, 0f, 0f), -90f);
            CyberSchoolBuildings.Rotunda(ctx, buildings, new Vector3(SideBuildingDistance, 0f, 0f), 90f);

            _blockedRects.Add(Rect.MinMaxRect(-14f, MainHallDistance - 9f, 14f, MainHallDistance + 7f));
            _blockedRects.Add(Rect.MinMaxRect(-SideBuildingDistance - 8f, -10f, -SideBuildingDistance + 9f, 10f));
            _blockedRects.Add(Rect.MinMaxRect(SideBuildingDistance - 10f, -9f, SideBuildingDistance + 9f, 9f));
            _blockedRects.Add(Rect.MinMaxRect(-6f, -(_plazaRadius + _southPathLength) - 2f, 6f, -(_plazaRadius + _southPathLength) + 8f));
        }

        private void BuildBenches(CyberSchoolBuildContext ctx)
        {
            Transform benches = ctx.Group("Benches");
            float radius = _plazaRadius - 1.1f;
            for (int i = 0; i < 4; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float angle = (45f + i * 90f + side * 18f) * Mathf.Deg2Rad;
                    var position = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
                    CyberSchoolProps.Bench(ctx, benches, position + Vector3.up * PlazaTop, Quaternion.LookRotation(-position));
                }
            }

            float benchX = _pathWidth * 0.5f + 1.2f;
            float midSouth = -(_plazaRadius + _southPathLength * 0.5f);
            CyberSchoolProps.Bench(ctx, benches, new Vector3(benchX, 0f, midSouth), Quaternion.LookRotation(Vector3.left));
            CyberSchoolProps.Bench(ctx, benches, new Vector3(-benchX, 0f, midSouth - 4f), Quaternion.LookRotation(Vector3.right));
        }

        private void ScatterVegetation(CyberSchoolBuildContext ctx)
        {
            Transform trees = ctx.Group("Trees");
            for (int i = 0; i < _treeCount; i++)
            {
                if (!TryFindSpot(ctx, 2.5f, _minTreeSpacing, out Vector3 spot)) continue;
                _treePositions.Add(spot);
                CyberSchoolProps.Tree(ctx, trees, spot, ctx.Rng.NextDouble() < _blossomChance);
            }

            Transform bushes = ctx.Group("Bushes");
            for (int i = 0; i < _bushCount; i++)
            {
                if (TryFindSpot(ctx, 0.8f, 2f, out Vector3 spot)) CyberSchoolProps.Bush(ctx, bushes, spot);
            }

            ScatterGroundCover(ctx);
        }

        private void ScatterGroundCover(CyberSchoolBuildContext ctx)
        {
            Transform cover = ctx.Group("Ground Cover");
            var grassA = new List<Vector3>();
            var grassB = new List<Vector3>();
            for (int clump = 0; clump * TuftsPerClump < _grassTuftCount; clump++)
            {
                if (!TryFindSpot(ctx, 0.5f, 0f, out Vector3 center)) continue;
                List<Vector3> target = clump % 2 == 0 ? grassA : grassB;
                for (int i = 0; i < TuftsPerClump; i++)
                {
                    var tuft = center + new Vector3(ctx.Range(-1f, 1f), 0f, ctx.Range(-1f, 1f)) * ClumpRadius;
                    if (!IsBlocked(tuft, 0.2f)) target.Add(tuft);
                }
            }

            var flowers = new List<Vector3>();
            for (int i = 0; i < _flowerCount; i++)
            {
                if (TryFindSpot(ctx, 0.5f, 0f, out Vector3 spot)) flowers.Add(spot);
            }

            AddCover(ctx, cover, "Grass_A", CyberSchoolProps.GrassField("Grass_A", grassA, ctx.Rng), ctx.Lit("GrassBlades", _palette.GrassBlades, 0.2f));
            AddCover(ctx, cover, "Grass_B", CyberSchoolProps.GrassField("Grass_B", grassB, ctx.Rng), ctx.Lit("GrassBladesLight", _palette.GrassBladesLight, 0.2f));
            AddCover(ctx, cover, "Flowers", CyberSchoolProps.FlowerField("Flowers", flowers, ctx.Rng), ctx.Lit("Flowers", _palette.Flowers, 0.3f));
        }

        private static void AddCover(CyberSchoolBuildContext ctx, Transform parent, string name, Mesh mesh, Material material)
        {
            if (mesh.vertexCount == 0) return;
            ctx.MeshObject(parent, name, mesh, material, Vector3.zero, Quaternion.identity, Vector3.one, false);
        }

        private void BuildMotes(CyberSchoolBuildContext ctx)
        {
            if (_moteCount <= 0) return;

            var go = new GameObject("Floating Motes");
            go.transform.SetParent(ctx.Root, false);
            go.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var particles = go.AddComponent<ParticleSystem>();

            const float lifetime = 9f;
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = lifetime;
            main.startSpeed = 0.12f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
            main.startColor = new ParticleSystem.MinMaxGradient(_palette.Motes, Color.Lerp(_palette.Motes, _palette.LampColors[0], 0.5f));
            main.maxParticles = _moteCount;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = _moteCount / lifetime;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(_plazaRadius * 2.6f, 1.5f, _plazaRadius * 2.6f);

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.15f;
            noise.frequency = 0.25f;

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0f), new Keyframe(0.2f, 1f), new Keyframe(0.8f, 1f), new Keyframe(1f, 0f)));

            var particleRenderer = go.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Mesh;
            particleRenderer.mesh = ctx.SharedMesh("Mote", b => b.AddIcosphere(Vector3.zero, Vector3.one * 0.5f, 0, 0f, null));
            particleRenderer.sharedMaterial = ctx.ParticleUnlit("Motes", Color.white, 2.2f);
            particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void BuildMusic(CyberSchoolBuildContext ctx)
        {
            if (_ambientMusic == null)
            {
                Debug.LogWarning($"[{nameof(CyberSchoolGenerator)}] No ambient music assigned; skipping background music.", this);
                return;
            }

            var go = new GameObject("Ambient Music");
            go.transform.SetParent(ctx.Root, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = _ambientMusic;
            source.loop = true;
            source.playOnAwake = true;
            source.volume = _musicVolume;
            source.spatialBlend = 0f;
            source.priority = 0;
        }

        private void BuildSpawnPoint(CyberSchoolBuildContext ctx)
        {
            var spawn = new GameObject(SpawnPointName).transform;
            spawn.SetParent(ctx.Root, false);
            spawn.localPosition = new Vector3(0f, PathTop, -(_plazaRadius + _southPathLength - 2f));
            spawn.localRotation = Quaternion.identity;
        }

        private void AddLamp(CyberSchoolBuildContext ctx, Transform parent, Vector3 position)
        {
            Color color = _palette.LampColors[_lampColorIndex++ % _palette.LampColors.Length];
            CyberSchoolProps.StreetLamp(ctx, parent, position, color, _lampGlowIntensity, _lampLightIntensity, _lampLightRange);
        }

        private bool TryFindSpot(CyberSchoolBuildContext ctx, float margin, float spacing, out Vector3 spot)
        {
            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                float angle = ctx.Range(0f, Mathf.PI * 2f);
                float distance = Mathf.Sqrt(ctx.Range(0.04f, 1f)) * _vegetationRadius;
                var candidate = new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
                if (IsBlocked(candidate, margin) || IsTooCloseToTrees(candidate, spacing)) continue;

                spot = candidate;
                return true;
            }

            spot = Vector3.zero;
            return false;
        }

        private bool IsBlocked(Vector3 point, float margin)
        {
            var p = new Vector2(point.x, point.z);
            foreach (Rect rect in _blockedRects)
            {
                if (Inflate(rect, margin).Contains(p)) return true;
            }
            return false;
        }

        private bool IsTooCloseToTrees(Vector3 point, float spacing)
        {
            if (spacing <= 0f) return false;
            foreach (Vector3 tree in _treePositions)
            {
                if ((tree - point).sqrMagnitude < spacing * spacing) return true;
            }
            return false;
        }

        private static Rect Inflate(Rect rect, float amount)
        {
            return Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);
        }
    }
}
