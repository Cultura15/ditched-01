using System.Collections.Generic;
using UnityEngine;

namespace MyGame.Environment
{
    /// <summary>Builds the small props of the cyber school: trees, bushes, benches, lamps, grass and the plaza centerpiece.</summary>
    public static class CyberSchoolProps
    {
        private const int FoliageVariants = 3;
        private const float LampHeight = 3.25f;

        private static readonly Vector2[] TrunkProfile =
            { new Vector2(0f, 0f), new Vector2(0.26f, 0f), new Vector2(0.15f, 2.4f), new Vector2(0f, 2.4f) };

        private static readonly Vector2[] ConeProfile =
            { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f) };

        private static readonly Vector2[] LampPoleProfile =
        {
            new Vector2(0f, 0f), new Vector2(0.18f, 0f), new Vector2(0.18f, 0.14f), new Vector2(0.08f, 0.28f),
            new Vector2(0.06f, 3.0f), new Vector2(0f, 3.0f),
        };

        private static readonly Vector2[] LampCapProfile =
            { new Vector2(0f, 3.5f), new Vector2(0.4f, 3.53f), new Vector2(0.14f, 3.72f), new Vector2(0f, 3.76f) };

        private static readonly Vector2[] PedestalProfile =
        {
            new Vector2(0f, 0f), new Vector2(1.6f, 0f), new Vector2(1.6f, 0.25f), new Vector2(1.1f, 0.4f),
            new Vector2(0.85f, 0.9f), new Vector2(1.2f, 1.05f), new Vector2(0f, 1.05f),
        };

        public static void Tree(CyberSchoolBuildContext ctx, Transform parent, Vector3 position, bool blossom)
        {
            CyberSchoolPalette p = ctx.Palette;
            Transform tree = ctx.Group("Tree", position, ctx.Range(0f, 360f), parent);
            float size = ctx.Range(0.85f, 1.25f);
            tree.localScale = Vector3.one * size;

            Mesh trunk = ctx.SharedMesh("TreeTrunk", b => b.AddLathe(TrunkProfile, 6));
            ctx.MeshObject(tree, "Trunk", trunk, ctx.Lit("Trunk", p.Trunk, 0.1f), Vector3.zero, Quaternion.identity, Vector3.one);

            Color leafColor = blossom ? ctx.Pick(p.Blossom) : ctx.Pick(p.Foliage);
            Material leaves = ctx.Lit($"Foliage_{ColorKey(leafColor)}", leafColor, 0.15f);

            if (!blossom && ctx.Rng.NextDouble() < 0.3) AddConeCanopy(ctx, tree, leaves);
            else AddBlobCanopy(ctx, tree, leaves);

            var col = tree.gameObject.AddComponent<CapsuleCollider>();
            col.radius = 0.3f;
            col.height = 3f;
            col.center = new Vector3(0f, 1.5f, 0f);
        }

        public static void Bush(CyberSchoolBuildContext ctx, Transform parent, Vector3 position)
        {
            Color color = ctx.Pick(ctx.Palette.Foliage);
            Material material = ctx.Lit($"Foliage_{ColorKey(color)}", color, 0.15f);
            float size = ctx.Range(0.55f, 0.95f);
            ctx.MeshObject(parent, "Bush", FoliageMesh(ctx), material, position + Vector3.up * size * 0.45f,
                Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f), new Vector3(1f, 0.7f, 1f) * size);
        }

        /// <summary>Bench whose seat faces local +Z.</summary>
        public static void Bench(CyberSchoolBuildContext ctx, Transform parent, Vector3 position, Quaternion rotation)
        {
            CyberSchoolPalette p = ctx.Palette;
            Material wood = ctx.Lit("BenchWood", p.BenchWood, 0.35f);
            Material metal = ctx.Lit("Metal", p.Metal, 0.55f, 0.6f);

            Transform bench = ctx.Group("Bench", parent);
            bench.localPosition = position;
            bench.localRotation = rotation;

            ctx.Box(bench, "Seat_Front", new Vector3(0f, 0.48f, 0.12f), new Vector3(1.9f, 0.08f, 0.22f), wood, false);
            ctx.Box(bench, "Seat_Back", new Vector3(0f, 0.48f, -0.13f), new Vector3(1.9f, 0.08f, 0.22f), wood, false);
            ctx.Box(bench, "Back_Low", new Vector3(0f, 0.72f, -0.3f), new Vector3(1.9f, 0.14f, 0.06f), wood, false, -12f);
            ctx.Box(bench, "Back_High", new Vector3(0f, 0.95f, -0.34f), new Vector3(1.9f, 0.14f, 0.06f), wood, false, -12f);

            for (int side = -1; side <= 1; side += 2)
            {
                ctx.Box(bench, "Leg", new Vector3(side * 0.82f, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.46f), metal, false);
                ctx.Box(bench, "Post", new Vector3(side * 0.82f, 0.75f, -0.3f), new Vector3(0.07f, 0.55f, 0.07f), metal, false, -12f);
            }

            var col = bench.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, -0.05f);
            col.size = new Vector3(1.9f, 1f, 0.6f);
        }

        public static void StreetLamp(CyberSchoolBuildContext ctx, Transform parent, Vector3 position, Color glow,
            float glowIntensity, float lightIntensity, float lightRange)
        {
            Transform lamp = ctx.Group("Street Lamp", position, 0f, parent);
            Material pole = ctx.Lit("LampPole", ctx.Palette.LampPole, 0.5f, 0.4f);

            ctx.MeshObject(lamp, "Pole", ctx.SharedMesh("LampPole", b => b.AddLathe(LampPoleProfile, 8)),
                pole, Vector3.zero, Quaternion.identity, Vector3.one);
            ctx.MeshObject(lamp, "Cap", ctx.SharedMesh("LampCap", b => b.AddLathe(LampCapProfile, 8)),
                pole, Vector3.zero, Quaternion.identity, Vector3.one);

            Mesh orb = ctx.SharedMesh("LampOrb", b => b.AddIcosphere(Vector3.zero, Vector3.one * 0.3f, 1, 0f, null));
            Material orbMaterial = ctx.Emissive($"LampGlow_{ColorKey(glow)}", Color.Lerp(glow, Color.white, 0.5f), glow, glowIntensity);
            ctx.MeshObject(lamp, "Glow Orb", orb, orbMaterial, Vector3.up * LampHeight, Quaternion.identity, Vector3.one, false);

            ctx.PointLight(lamp, "Lamp Light", Vector3.up * (LampHeight - 0.15f), glow, lightIntensity, lightRange);

            var col = lamp.gameObject.AddComponent<CapsuleCollider>();
            col.radius = 0.2f;
            col.height = 3.6f;
            col.center = new Vector3(0f, 1.8f, 0f);
        }

        public static void Centerpiece(CyberSchoolBuildContext ctx, Transform parent, Vector3 position)
        {
            CyberSchoolPalette p = ctx.Palette;
            Transform root = ctx.Group("Centerpiece", position, 0f, parent);

            Mesh pedestal = ctx.SharedMesh("Pedestal", b => b.AddLathe(PedestalProfile, 10));
            GameObject pedestalObject = ctx.MeshObject(root, "Pedestal", pedestal, ctx.Lit("Wall", p.Wall, 0.3f),
                Vector3.zero, Quaternion.identity, Vector3.one);
            pedestalObject.AddComponent<MeshCollider>().sharedMesh = pedestal;

            Mesh crystal = ctx.SharedMesh("Crystal", b => b.AddIcosphere(Vector3.zero, new Vector3(0.55f, 1f, 0.55f), 0, 0f, null));
            ctx.MeshObject(root, "Crystal", crystal, ctx.Emissive("Crystal", p.Crystal, p.Crystal, 1.6f, 0.8f),
                Vector3.up * 2.4f, Quaternion.Euler(0f, 18f, 0f), Vector3.one, false);

            ctx.PointLight(root, "Crystal Light", Vector3.up * 2.2f, p.Crystal, 2f, 9f);
        }

        /// <summary>Builds one merged mesh of grass tufts at the given positions.</summary>
        public static Mesh GrassField(string name, IReadOnlyList<Vector3> positions, System.Random rng)
        {
            var builder = new LowPolyMeshBuilder();
            foreach (Vector3 position in positions)
            {
                int blades = 3 + rng.Next(3);
                float tuftHeight = 0.22f + (float)rng.NextDouble() * 0.25f;
                for (int i = 0; i < blades; i++) AddBlade(builder, position, tuftHeight, rng);
            }
            return builder.ToMesh(name);
        }

        /// <summary>Builds one merged mesh of tiny flower heads at the given positions.</summary>
        public static Mesh FlowerField(string name, IReadOnlyList<Vector3> positions, System.Random rng)
        {
            var builder = new LowPolyMeshBuilder();
            foreach (Vector3 position in positions)
            {
                float height = 0.14f + (float)rng.NextDouble() * 0.12f;
                builder.AddIcosphere(position + Vector3.up * height, Vector3.one * 0.07f, 0, 0.1f, rng);
            }
            return builder.ToMesh(name);
        }

        public static Mesh FoliageMesh(CyberSchoolBuildContext ctx)
        {
            int variant = ctx.Rng.Next(FoliageVariants);
            return ctx.SharedMesh($"Foliage_{variant}", b => b.AddIcosphere(Vector3.zero, Vector3.one, 1, 0.12f, ctx.Rng));
        }

        private static void AddBlobCanopy(CyberSchoolBuildContext ctx, Transform tree, Material leaves)
        {
            ctx.MeshObject(tree, "Canopy", FoliageMesh(ctx), leaves, new Vector3(0f, 2.9f, 0f),
                Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f), new Vector3(1.6f, 1.35f, 1.6f));
            ctx.MeshObject(tree, "Canopy_Top", FoliageMesh(ctx), leaves, new Vector3(ctx.Range(-0.4f, 0.4f), 3.9f, ctx.Range(-0.4f, 0.4f)),
                Quaternion.Euler(0f, ctx.Range(0f, 360f), 0f), Vector3.one * 0.95f);
            if (ctx.Rng.NextDouble() < 0.5)
            {
                ctx.MeshObject(tree, "Canopy_Side", FoliageMesh(ctx), leaves, new Vector3(ctx.Range(0.8f, 1.1f), 2.6f, ctx.Range(-0.3f, 0.3f)),
                    Quaternion.identity, Vector3.one * 0.8f);
            }
        }

        private static void AddConeCanopy(CyberSchoolBuildContext ctx, Transform tree, Material leaves)
        {
            Mesh cone = ctx.SharedMesh("FoliageCone", b => b.AddLathe(ConeProfile, 7));
            ctx.MeshObject(tree, "Cone_Low", cone, leaves, new Vector3(0f, 1.5f, 0f), Quaternion.identity, new Vector3(1.6f, 2.4f, 1.6f));
            ctx.MeshObject(tree, "Cone_High", cone, leaves, new Vector3(0f, 2.9f, 0f), Quaternion.Euler(0f, 25f, 0f), new Vector3(1.15f, 2.1f, 1.15f));
        }

        private static void AddBlade(LowPolyMeshBuilder builder, Vector3 root, float tuftHeight, System.Random rng)
        {
            float angle = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            var facing = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            var side = new Vector3(-facing.z, 0f, facing.x);
            Vector3 basePoint = root + facing * (float)rng.NextDouble() * 0.12f;
            float width = 0.035f + (float)rng.NextDouble() * 0.03f;
            float height = tuftHeight * (0.7f + (float)rng.NextDouble() * 0.3f);
            Vector3 tip = basePoint + Vector3.up * height + facing * height * 0.35f;
            builder.AddBlade(basePoint - side * width, basePoint + side * width, tip);
        }

        private static string ColorKey(Color c)
        {
            return ColorUtility.ToHtmlStringRGB(c);
        }
    }
}
