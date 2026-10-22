using UnityEngine;

namespace MyGame.Environment
{
    /// <summary>Builds the cyber school buildings. Every building's entrance faces its local -Z.</summary>
    public static class CyberSchoolBuildings
    {
        private const float Plinth = 0.4f;
        private const float Inset = 0.06f;
        private const int RotundaSegments = 16;
        private const int DomeRings = 4;

        public static void MainHall(CyberSchoolBuildContext ctx, Transform parent, Vector3 position, float yaw)
        {
            const float width = 24f, height = 10f, depth = 10f;
            const float front = -depth * 0.5f;
            Transform root = ctx.Group("Main Hall", position, yaw, parent);

            AddShell(ctx, root, width, height, depth, Wall(ctx));
            ctx.Box(root, "Roof", new Vector3(0f, Plinth + height + 0.25f, 0f), new Vector3(width + 1.2f, 0.5f, depth + 1.2f), Roof(ctx), false);
            ctx.Box(root, "Roof Trim", new Vector3(0f, Plinth + height + 0.02f, front - 0.62f), new Vector3(width + 1.2f, 0.1f, 0.06f), Trim(ctx), false);

            for (int floor = 0; floor < 2; floor++)
            {
                float y = Plinth + 5.2f + floor * 3f;
                ctx.Box(root, "Windows", new Vector3(0f, y, front - Inset), new Vector3(width - 3f, 1.6f, 0.14f), Glass(ctx), false);
                AddSideWindows(ctx, root, width, depth, y, 1.6f);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                ctx.Box(root, "Windows", new Vector3(side * 7f, Plinth + 2f, front - Inset), new Vector3(7.5f, 1.6f, 0.14f), Glass(ctx), false);
                ctx.Box(root, "Fin", new Vector3(side * 3f, Plinth + height * 0.5f, front - 0.2f), new Vector3(0.5f, height, 0.4f), Accent(ctx), false);
                ctx.Box(root, "Corner", new Vector3(side * (width * 0.5f - 0.3f), Plinth + height * 0.5f, front - 0.1f),
                    new Vector3(0.9f, height, 0.5f), AccentWarm(ctx), false);
            }

            AddEntrance(ctx, root, front, 4.4f, 3f, 7f);
        }

        public static void Library(CyberSchoolBuildContext ctx, Transform parent, Vector3 position, float yaw)
        {
            const float width = 14f, height = 7f, depth = 10f;
            const float front = -depth * 0.5f;
            Transform root = ctx.Group("Library", position, yaw, parent);
            CyberSchoolPalette p = ctx.Palette;

            AddShell(ctx, root, width, height, depth, ctx.Lit("WallAlt", p.WallAlt, 0.3f));

            Mesh roof = ctx.SharedMesh("LibraryRoof", b => b.AddGable(width + 1.2f, depth + 1.2f, 2.6f));
            ctx.MeshObject(root, "Roof", roof, AccentWarm(ctx), new Vector3(0f, Plinth + height, 0f), Quaternion.identity, Vector3.one);
            ctx.Box(root, "Eave Trim", new Vector3(0f, Plinth + height + 0.02f, front - 0.62f), new Vector3(width + 1.2f, 0.1f, 0.06f), Trim(ctx), false);

            ctx.Box(root, "Windows", new Vector3(0f, Plinth + 4.6f, front - Inset), new Vector3(width - 3f, 1.4f, 0.14f), Glass(ctx), false);
            AddSideWindows(ctx, root, width, depth, Plinth + 4.6f, 1.4f);
            for (int side = -1; side <= 1; side += 2)
            {
                ctx.Box(root, "Windows", new Vector3(side * 4.3f, Plinth + 1.8f, front - Inset), new Vector3(3.8f, 1.4f, 0.14f), Glass(ctx), false);
            }

            ctx.Box(root, "Door Frame", new Vector3(0f, Plinth + 2.95f, front - 0.15f), new Vector3(3.4f, 0.3f, 0.3f), Accent(ctx), false);
            AddEntrance(ctx, root, front, 2.8f, 2.8f, 0f);
        }

        public static void Rotunda(CyberSchoolBuildContext ctx, Transform parent, Vector3 position, float yaw)
        {
            const float radius = 6f, drumTop = 5.4f, corniceTop = 5.8f, domeHeight = 4.2f;
            Transform root = ctx.Group("Rotunda", position, yaw, parent);
            CyberSchoolPalette p = ctx.Palette;

            AddLathe(ctx, root, "Rotunda Plinth", Path(ctx), true,
                new Vector2(0f, 0f), new Vector2(radius + 1f, 0f), new Vector2(radius + 1f, Plinth), new Vector2(0f, Plinth));
            AddLathe(ctx, root, "Rotunda Drum", Wall(ctx), true,
                new Vector2(0f, Plinth), new Vector2(radius, Plinth), new Vector2(radius, drumTop), new Vector2(0f, drumTop));
            AddLathe(ctx, root, "Rotunda Windows", Glass(ctx), false,
                new Vector2(radius + Inset, 2f), new Vector2(radius + Inset, 3.6f));
            AddLathe(ctx, root, "Rotunda Cornice", Accent(ctx), false,
                new Vector2(0f, drumTop), new Vector2(radius + 0.5f, drumTop), new Vector2(radius + 0.5f, corniceTop), new Vector2(0f, corniceTop));
            AddLathe(ctx, root, "Rotunda Trim", Trim(ctx), false,
                new Vector2(radius + 0.52f, drumTop + 0.02f), new Vector2(radius + 0.52f, drumTop + 0.1f));

            var dome = new Vector2[DomeRings + 1];
            for (int i = 0; i <= DomeRings; i++)
            {
                float angle = i / (float)DomeRings * Mathf.PI * 0.5f;
                dome[i] = new Vector2((radius + 0.1f) * Mathf.Cos(angle), corniceTop + domeHeight * Mathf.Sin(angle));
            }
            AddLathe(ctx, root, "Rotunda Dome", ctx.Lit("Dome", p.Dome, 0.45f), false, dome);

            AddBeacon(ctx, root, new Vector3(0f, corniceTop + domeHeight + 0.35f, 0f), 0.3f);
            AddEntrance(ctx, root, -radius, 2.6f, 3f, 3.6f);
        }

        public static void Gate(CyberSchoolBuildContext ctx, Transform parent, Vector3 position, float pathWidth)
        {
            Transform root = ctx.Group("School Gate", position, 0f, parent);
            float pillarX = pathWidth * 0.5f + 0.8f;

            for (int side = -1; side <= 1; side += 2)
            {
                ctx.Box(root, "Pillar", new Vector3(side * pillarX, 2.1f, 0f), new Vector3(0.9f, 4.2f, 0.9f), Wall(ctx), true);
                ctx.Box(root, "Pillar Cap", new Vector3(side * pillarX, 4.35f, 0f), new Vector3(1.2f, 0.3f, 1.2f), AccentWarm(ctx), false);
                ctx.Box(root, "Beam Trim", new Vector3(0f, 3.58f, side * 0.36f), new Vector3(pillarX * 2f - 0.9f, 0.08f, 0.08f), Trim(ctx), false);
            }

            ctx.Box(root, "Beam", new Vector3(0f, 3.9f, 0f), new Vector3(pillarX * 2f + 1.2f, 0.55f, 0.7f), Accent(ctx), false);
            AddBeacon(ctx, root, new Vector3(0f, 4.6f, 0f), 0.25f);
        }

        private static void AddShell(CyberSchoolBuildContext ctx, Transform root, float width, float height, float depth, Material wall)
        {
            ctx.Box(root, "Plinth", new Vector3(0f, Plinth * 0.5f, 0f), new Vector3(width + 2f, Plinth, depth + 2f), Path(ctx), true);
            ctx.Box(root, "Body", new Vector3(0f, Plinth + height * 0.5f, 0f), new Vector3(width, height, depth), wall, true);
        }

        private static void AddSideWindows(CyberSchoolBuildContext ctx, Transform root, float width, float depth, float y, float height)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                ctx.Box(root, "Side Windows", new Vector3(side * (width * 0.5f + Inset), y, 0f), new Vector3(0.14f, height, depth - 3f), Glass(ctx), false);
            }
        }

        /// <summary>Door, optional canopy with neon trim, and a step up onto the plinth.</summary>
        private static void AddEntrance(CyberSchoolBuildContext ctx, Transform root, float front, float doorWidth, float doorHeight, float canopyWidth)
        {
            Material door = ctx.Emissive("DoorGlass", ctx.Palette.Glass, ctx.Palette.WindowGlow, 1.3f, 0.85f);
            ctx.Box(root, "Entrance", new Vector3(0f, Plinth + doorHeight * 0.5f, front - 0.08f), new Vector3(doorWidth, doorHeight, 0.16f), door, false);

            if (canopyWidth > 0f)
            {
                float canopyY = Plinth + doorHeight + 0.4f;
                ctx.Box(root, "Canopy", new Vector3(0f, canopyY, front - 1.4f), new Vector3(canopyWidth, 0.25f, 2.8f), Roof(ctx), false);
                ctx.Box(root, "Canopy Trim", new Vector3(0f, canopyY - 0.15f, front - 2.8f), new Vector3(canopyWidth, 0.08f, 0.08f), Trim(ctx), false);
            }

            ctx.Box(root, "Step", new Vector3(0f, 0.1f, front - 1.6f), new Vector3(Mathf.Max(doorWidth + 2f, 4f), 0.2f, 1.2f), Path(ctx), true);
        }

        private static void AddBeacon(CyberSchoolBuildContext ctx, Transform root, Vector3 position, float size)
        {
            Mesh gem = ctx.SharedMesh("Beacon", b => b.AddIcosphere(Vector3.zero, new Vector3(0.7f, 1f, 0.7f), 0, 0f, null));
            ctx.MeshObject(root, "Beacon", gem, Trim(ctx), position, Quaternion.identity, Vector3.one * size, false);
        }

        private static void AddLathe(CyberSchoolBuildContext ctx, Transform root, string name, Material material, bool collider, params Vector2[] profile)
        {
            Mesh mesh = ctx.SharedMesh(name, b => b.AddLathe(profile, RotundaSegments));
            GameObject go = ctx.MeshObject(root, name, mesh, material, Vector3.zero, Quaternion.identity, Vector3.one);
            if (collider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private static Material Wall(CyberSchoolBuildContext ctx) => ctx.Lit("Wall", ctx.Palette.Wall, 0.3f);
        private static Material Path(CyberSchoolBuildContext ctx) => ctx.Lit("Path", ctx.Palette.Path, 0.2f);
        private static Material Roof(CyberSchoolBuildContext ctx) => ctx.Lit("Roof", ctx.Palette.Roof, 0.35f);
        private static Material Accent(CyberSchoolBuildContext ctx) => ctx.Lit("Accent", ctx.Palette.Accent, 0.4f);
        private static Material AccentWarm(CyberSchoolBuildContext ctx) => ctx.Lit("AccentWarm", ctx.Palette.AccentWarm, 0.35f);
        private static Material Glass(CyberSchoolBuildContext ctx) => ctx.Emissive("WindowGlass", ctx.Palette.Glass, ctx.Palette.WindowGlow, 0.7f, 0.85f);
        private static Material Trim(CyberSchoolBuildContext ctx) => ctx.Emissive("NeonTrim", ctx.Palette.Trim, ctx.Palette.Trim, 1.8f, 0.6f);
    }
}
