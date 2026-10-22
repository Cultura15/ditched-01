using System;
using UnityEngine;

namespace MyGame.Environment
{
    /// <summary>All colors used by the cyber school generator, tuned for a violet/indigo evening sky.</summary>
    [Serializable]
    public class CyberSchoolPalette
    {
        [Header("Ground")]
        public Color Grass = new Color(0.34f, 0.55f, 0.52f);
        public Color GrassBlades = new Color(0.40f, 0.68f, 0.54f);
        public Color GrassBladesLight = new Color(0.52f, 0.76f, 0.58f);
        public Color Flowers = new Color(0.98f, 0.76f, 0.90f);
        public Color Plaza = new Color(0.80f, 0.78f, 0.90f);
        public Color Path = new Color(0.66f, 0.63f, 0.74f);

        [Header("Buildings")]
        public Color Wall = new Color(0.93f, 0.91f, 0.97f);
        public Color WallAlt = new Color(0.86f, 0.84f, 0.96f);
        public Color Accent = new Color(0.50f, 0.44f, 0.90f);
        public Color AccentWarm = new Color(0.95f, 0.58f, 0.72f);
        public Color Roof = new Color(0.36f, 0.33f, 0.58f);
        public Color Dome = new Color(0.64f, 0.58f, 0.96f);
        public Color Glass = new Color(0.20f, 0.25f, 0.45f);
        public Color WindowGlow = new Color(1.00f, 0.78f, 0.56f);
        public Color Trim = new Color(0.35f, 0.85f, 1.00f);

        [Header("Vegetation")]
        public Color Trunk = new Color(0.48f, 0.34f, 0.33f);
        public Color[] Foliage =
        {
            new Color(0.29f, 0.60f, 0.52f),
            new Color(0.40f, 0.70f, 0.56f),
            new Color(0.26f, 0.52f, 0.58f),
        };
        public Color[] Blossom =
        {
            new Color(0.90f, 0.64f, 0.88f),
            new Color(0.72f, 0.62f, 0.96f),
        };

        [Header("Props")]
        public Color BenchWood = new Color(0.80f, 0.58f, 0.44f);
        public Color Metal = new Color(0.30f, 0.30f, 0.42f);
        public Color LampPole = new Color(0.24f, 0.23f, 0.34f);
        public Color[] LampColors =
        {
            new Color(1.00f, 0.38f, 0.76f),
            new Color(0.28f, 0.86f, 1.00f),
            new Color(1.00f, 0.70f, 0.32f),
            new Color(0.66f, 0.42f, 1.00f),
            new Color(0.38f, 1.00f, 0.72f),
        };
        public Color Crystal = new Color(0.58f, 0.76f, 1.00f);
        public Color Motes = new Color(0.80f, 0.86f, 1.00f);
    }
}
