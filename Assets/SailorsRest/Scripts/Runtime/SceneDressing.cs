using UnityEngine;

namespace SailorsRest
{
    /// <summary>
    /// Shore Pond scenery from <see cref="WorldArt"/>: sky and underwater backdrops with parallax, the waterline,
    /// lake-bed weeds, the dock, the fisherman and his rod. Gameplay does not depend on any of it.
    /// Everything idles: SpriteCook flipbooks where the art has one, free code effects everywhere else
    /// (sky motes, light rays, bubbles, water sparkles, the waterline shimmer and the bobber's swing).
    /// </summary>
    public class SceneDressing : MonoBehaviour
    {
        const int WeedSeed = 7;   // fixed so the weeds sit in the same places every visit

        public GameBalance balance;
        public WorldArt art;
        public SpriteRenderer hookRenderer;

        void Awake()
        {
            var cam = Camera.main.transform;
            float camX = cam.position.x;
            var dockEdge = new Vector2(balance.dockEdgeX, GameBalance.WaterlineY);

            var sky = Backdrop("Sky", art.sky, new Vector2(camX, art.skyBottomY), SortOrder.Sky, art.skyParallax, cam);
            var underwater = Backdrop("Underwater", art.underwater, new Vector2(camX, art.underwaterTopY), SortOrder.Underwater, art.underwaterParallax, cam);
            Waterline();
            Weeds();

            Animate(Place("Dock", art.dock, dockEdge + art.dockOffset, SortOrder.Dock), art.dockIdle);
            Animate(Place("Fisherman", art.fisherman, dockEdge + art.fishermanOffset, SortOrder.Fisherman), art.fishermanIdle);
            Pole("Rod", dockEdge + art.handsOffset, dockEdge + art.rodTipOffset, art.rodThickness, Palette.WoodDark, SortOrder.Rod);

            hookRenderer.sprite = art.bobber;
            hookRenderer.sortingOrder = SortOrder.Bobber;
            // Rotation only: the fishing controller owns the hook's position.
            IdleMotion.Sway(hookRenderer.gameObject, art.bobberSwayDegrees, 2.2f);

            SkyMotes(sky.transform);
            LightRays(underwater.transform);
            Bubbles();
            SurfaceSparkles();
        }

        void Animate(SpriteRenderer sr, Sprite[] frames) => SpriteFlipbook.Play(sr, frames, art.idleFps);

        // ----- free code idles -----

        /// <summary>Warm dust motes and fireflies drifting over the treeline, moving with the sky's parallax.</summary>
        void SkyMotes(Transform sky)
        {
            if (art.skyMotes <= 0 || art.sky == null) return;
            var size = art.sky.bounds.size;
            float waterline = -art.skyBottomY;   // the sky's pivot is bottom-centre, so this is the waterline in its space
            var motes = Particles("Sky motes", sky, IdleSprites.Dot, art.skyMotes, SortOrder.SkyMotes,
                new Rect(-size.x / 2f, waterline + 0.5f, size.x, size.y * 0.45f));
            motes.minVelocity = new Vector2(-0.15f, 0.02f);
            motes.maxVelocity = new Vector2(0.15f, 0.2f);
            motes.wobble = 0.25f;
            motes.wobbleHz = 0.25f;
            motes.lifeSeconds = new Vector2(4f, 9f);
            motes.color = new Color(1f, 0.86f, 0.55f);
            motes.maxAlpha = 0.85f;
            motes.blinkHz = 0.35f;
        }

        void LightRays(Transform underwater)
        {
            if (art.lightRays <= 0) return;
            var go = new GameObject("Light rays");
            go.transform.SetParent(underwater, false);
            var rays = go.AddComponent<LightRays>();
            rays.rays = art.lightRays;
            rays.width = art.underwater != null ? art.underwater.bounds.size.x : 40f;
        }

        /// <summary>Bubbles rising from the lake bed to just under the surface, fixed in the world like the fish.</summary>
        void Bubbles()
        {
            if (art.bubbles <= 0) return;
            float left = balance.dockEdgeX + art.firstWeedGap / 2f;
            float bottom = balance.lakeBottomY;
            var bubbles = Particles("Bubbles", transform, IdleSprites.Bubble, art.bubbles, SortOrder.Bubbles,
                new Rect(left, bottom, balance.lakeEndX - left, 2f));
            bubbles.minVelocity = new Vector2(0f, 0.9f);
            bubbles.maxVelocity = new Vector2(0f, 1.8f);
            bubbles.wobble = 0.15f;
            bubbles.wobbleHz = 0.8f;
            // Long enough to reach the surface from the bottom at the slowest speed, a little short at the fastest.
            float depth = GameBalance.WaterlineY - art.surfaceBandHeight - bottom;
            bubbles.lifeSeconds = new Vector2(depth / 1.8f, depth / 0.9f);
            bubbles.color = new Color(0.85f, 0.97f, 1f);
            bubbles.maxAlpha = 0.55f;
        }

        /// <summary>Glints that wink on and off along the waterline, plus a slow shimmer of the band itself.</summary>
        void SurfaceSparkles()
        {
            if (art.surfaceSparkles <= 0) return;
            float left = balance.dockEdgeX - art.surfaceBandOverhang;
            float right = balance.lakeEndX + art.surfaceBandOverhang;
            var sparkles = Particles("Water sparkles", transform, IdleSprites.Sparkle, art.surfaceSparkles, SortOrder.SurfaceSparkles,
                new Rect(left, GameBalance.WaterlineY - art.surfaceBandHeight, right - left, art.surfaceBandHeight));
            sparkles.minVelocity = new Vector2(0.05f, 0f);
            sparkles.maxVelocity = new Vector2(0.25f, 0f);
            sparkles.wobble = 0f;
            sparkles.lifeSeconds = new Vector2(0.8f, 2f);
            sparkles.color = Palette.OnWood;
            sparkles.maxAlpha = 0.9f;
            sparkles.blinkHz = 1.2f;
        }

        PixelParticles Particles(string name, Transform parent, Sprite sprite, int count, int order, Rect area)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var p = go.AddComponent<PixelParticles>();
            p.sprite = sprite;
            p.count = count;
            p.sortingOrder = order;
            p.area = area;
            return p;
        }

        void Waterline()
        {
            float left = balance.dockEdgeX - art.surfaceBandOverhang;
            float right = balance.lakeEndX + art.surfaceBandOverhang;
            var color = Palette.WaterTop;
            color.a = art.surfaceBandAlpha;
            var band = Block("Water surface", new Vector2((left + right) / 2f, GameBalance.WaterlineY - art.surfaceBandHeight / 2f),
                new Vector2(right - left, art.surfaceBandHeight), color, SortOrder.WaterSurface);
            if (art.surfaceShimmer > 0f)
                AlphaPulse.Add(band.gameObject, art.surfaceBandAlpha - art.surfaceShimmer, art.surfaceBandAlpha, 3.5f);
        }

        void Weeds()
        {
            var rng = new System.Random(WeedSeed);
            for (int i = 0; i < art.weedClumps; i++)
            {
                float jitter = ((float)rng.NextDouble() - 0.5f) * art.weedJitter;
                float x = balance.dockEdgeX + art.firstWeedGap + i * art.weedSpacing + jitter;
                var clump = Place("Weeds", art.weeds, new Vector2(x, balance.lakeBottomY - art.weedSink), SortOrder.Weeds);
                clump.flipX = rng.Next(2) == 0;
                Animate(clump, art.weedsIdle);
            }
        }

        SpriteRenderer Backdrop(string name, Sprite sprite, Vector2 pos, int order, float parallax, Transform cam)
        {
            var sr = Place(name, sprite, pos, order);
            var layer = sr.gameObject.AddComponent<Parallax>();
            layer.cam = cam;
            layer.factor = parallax;
            return sr;
        }

        /// <summary>A straight pole of the given thickness from one point to another.</summary>
        void Pole(string name, Vector2 from, Vector2 to, float thickness, Color color, int order)
        {
            var pole = Block(name, (from + to) / 2f, new Vector2(thickness, Vector2.Distance(from, to)), color, order);
            pole.transform.rotation = Quaternion.FromToRotation(Vector3.up, to - from);
        }

        SpriteRenderer Place(string name, Sprite sprite, Vector2 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>A flat-coloured rectangle, size in world units.</summary>
        SpriteRenderer Block(string name, Vector2 centre, Vector2 size, Color color, int order)
        {
            var sr = Place(name, PixelSprites.White, centre, order);
            sr.transform.localScale = new Vector3(size.x * PixelSprites.PPU, size.y * PixelSprites.PPU, 1f);
            sr.color = color;
            return sr;
        }
    }
}
