using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace SailorsRest.EditorTools
{
    /// <summary>
    /// One-click setup: creates the data assets and the Map, Shore Pond and Market scenes, wires every component,
    /// and applies Steam/PC player settings. Safe to run again; it rebuilds the scenes and keeps tuned values.
    /// </summary>
    public static class SailorsRestSetup
    {
        const string Menu = PixelArtImport.Menu;
        const string Root = "Assets/SailorsRest";
        const string DataDir = Root + "/Data";
        const string SceneDir = Root + "/Scenes";
        const string PondPath = SceneDir + "/" + GameSession.PondScene + ".unity";
        const string MapPath = SceneDir + "/" + GameSession.MapScene + ".unity";
        const string MarketPath = SceneDir + "/" + GameSession.MarketScene + ".unity";
        const string UiKitDir = Root + "/Art/SpriteCook/Landscape/Elements";
        const string LinePath = DataDir + "/FishingLine.mat";
        /// <summary>A still's idle loop is named after it: fish_carp → fish_carp_idle.</summary>
        const string IdleSuffix = "_idle";

        // The pond renders at 480x270 and scales by whole numbers: x4 at 1080p, x5 (letterboxed) at 1440p.
        const int PixelWidth = 480, PixelHeight = 270;
        const float CameraZ = -10f;

        const string ProductName = "Sailor's Rest";
        const int WindowWidth = 1920, WindowHeight = 1080;

        const string LineShader = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
        const string FallbackLineShader = "Sprites/Default";
        const float LineAlpha = 0.9f;

        /// <summary>
        /// The Shore Pond's species. The numbers here are only the starting values for a new asset;
        /// once Species_*.asset exists, its tuned values are kept and only the id, name and sprite are rewired.
        /// </summary>
        static readonly SpeciesSpec[] Species =
        {
            new SpeciesSpec("Perch", "perch", "fish_perch", 0.8f, 4.5f, 1.2f, 34f, new Vector2(0.15f, 0.4f), new Vector2(0.5f, 0.9f), new Vector2(1.2f, 2f)),
            new SpeciesSpec("Carp", "carp", "fish_carp", 2.5f, 8f, 1f, 28f, new Vector2(0.4f, 0.9f), new Vector2(1.1f, 2f), new Vector2(2.6f, 4.4f)),
            new SpeciesSpec("Catfish", "catfish", "fish_catfish", 5.5f, 12f, 0.7f, 26f, new Vector2(0.8f, 1.6f), new Vector2(2f, 3.4f), new Vector2(4.6f, 7.5f)),
        };

        sealed class SpeciesSpec
        {
            public readonly string Name, Id, Sprite;
            public readonly float MinDepth, MaxDepth, SpawnWeight, PricePerKg;
            public readonly Vector2[] WeightRangeKg;

            public SpeciesSpec(string name, string id, string sprite, float minDepth, float maxDepth, float spawnWeight, float pricePerKg,
                params Vector2[] weightRangeKg)
            {
                Name = name; Id = id; Sprite = sprite;
                MinDepth = minDepth; MaxDepth = maxDepth; SpawnWeight = spawnWeight; PricePerKg = pricePerKg;
                WeightRangeKg = weightRangeKg;
            }
        }

        sealed class Data
        {
            public GameBalance balance;
            public UiSkin skin;
            public WorldArt art;
        }

        // ----- menu -----

        [MenuItem(Menu + "Build All Scenes", priority = 0)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            WorldArtDownloader.ReapplyImport();
            var data = LoadData();
            BuildPond(data);
            BuildMarket(data);
            BuildMap(data);   // last, so the Map is open and Play starts there
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MapPath, true),
                new EditorBuildSettingsScene(PondPath, true),
                new EditorBuildSettingsScene(MarketPath, true),
            };
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("Sailor's Rest: Map, Shore Pond and Market built. Press Play (starts on the Map).");
        }

        [MenuItem(Menu + "Build Shore Pond Scene", priority = 1)]
        public static void BuildPondMenu() => BuildOne(BuildPond);

        [MenuItem(Menu + "Build Map Scene", priority = 2)]
        public static void BuildMapMenu() => BuildOne(BuildMap);

        [MenuItem(Menu + "Build Market Scene", priority = 3)]
        public static void BuildMarketMenu() => BuildOne(BuildMarket);

        /// <summary>Builds one scene, after offering to save edits in the scene that is about to be replaced.</summary>
        static void BuildOne(Action<Data> build)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            build(LoadData());
        }

        [MenuItem(Menu + "Apply Steam Player Settings", priority = 20)]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = ProductName;
            PlayerSettings.defaultScreenWidth = WindowWidth;
            PlayerSettings.defaultScreenHeight = WindowHeight;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.allowFullscreenSwitch = true;
        }

        [MenuItem(Menu + "Refresh Sprites From SpriteCook", priority = 41)]
        public static void RefreshSprites()
        {
            LoadData();
            AssetDatabase.SaveAssets();
            Debug.Log("Sailor's Rest: UI skin, world art, idle animations and species sprites refreshed.");
        }

        [MenuItem(Menu + "Reset Save", priority = 60)]
        public static void ResetSave()
        {
            GameSession.ResetSave();
            Debug.Log("Sailor's Rest: save cleared (" + GameSession.SavePath + ").");
        }

        // ----- data -----

        static Data LoadData()
        {
            EnsureFolder(DataDir);
            EnsureFolder(SceneDir);
            var data = new Data
            {
                balance = Asset<GameBalance>("GameBalance"),
                skin = FillSprites(Asset<UiSkin>("UiSkin"), UiKitDir),
                art = FillSprites(Asset<WorldArt>("WorldArt"), WorldArtDownloader.WorldDir),
            };
            data.balance.species = Array.ConvertAll(Species, BuildSpecies);
            EditorUtility.SetDirty(data.balance);
            AssetDatabase.SaveAssets();
            return data;
        }

        static FishSpecies BuildSpecies(SpeciesSpec spec)
        {
            var s = Asset<FishSpecies>("Species_" + spec.Name, out bool created);
            s.id = spec.Id;
            s.displayName = spec.Name;
            s.sprite = LoadSprite(WorldArtDownloader.WorldDir, spec.Sprite);
            s.swimFrames = AnimArtDownloader.LoadFrames(spec.Sprite + IdleSuffix);
            EditorUtility.SetDirty(s);
            if (!created) return s;   // keep the designer's tuning
            s.minDepth = spec.MinDepth;
            s.maxDepth = spec.MaxDepth;
            s.spawnWeight = spec.SpawnWeight;
            s.weightRangeKg = spec.WeightRangeKg;
            s.pricePerKg = spec.PricePerKg;
            return s;
        }

        /// <summary>
        /// Sets every field tagged with <see cref="SpriteFileAttribute"/> from the PNG it names: a Sprite from
        /// <paramref name="dir"/>, a Sprite[] (an idle loop) from the SpriteCook Anim folder. A missing loop is left
        /// empty and the still sprite plays instead.
        /// </summary>
        static T FillSprites<T>(T target, string dir) where T : Object
        {
            foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var file = field.GetCustomAttribute<SpriteFileAttribute>();
                if (file == null) continue;
                if (field.FieldType == typeof(Sprite[])) field.SetValue(target, AnimArtDownloader.LoadFrames(file.FileName));
                else field.SetValue(target, LoadSprite(dir, file.FileName));
            }
            EditorUtility.SetDirty(target);
            return target;
        }

        static Sprite LoadSprite(string dir, string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{name}.png");
            if (sprite != null) return sprite;
            string fix = dir == WorldArtDownloader.WorldDir ? $"Run {Menu}Download World Art." : "Unzip the SpriteCook UI kit into Art/SpriteCook (see its README).";
            Debug.LogWarning($"Sailor's Rest: sprite '{name}' not found in {dir}. {fix}");
            return null;
        }

        // ----- scenes -----

        static void BuildMap(Data data) => BuildUiScene<MapView>(MapPath, view =>
        {
            view.balance = data.balance;
            view.skin = data.skin;
            view.art = data.art;
        });

        static void BuildMarket(Data data) => BuildUiScene<MarketView>(MarketPath, view =>
        {
            view.balance = data.balance;
            view.skin = data.skin;
            view.art = data.art;
        });

        /// <summary>A scene that is only a screen-space canvas: a plain camera and the view that builds the UI.</summary>
        static void BuildUiScene<T>(string path, Action<T> wire) where T : Component
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = NewCamera();
            cam.transform.position = new Vector3(0f, 0f, CameraZ);
            wire(new GameObject(typeof(T).Name).AddComponent<T>());
            EditorSceneManager.SaveScene(scene, path);
        }

        static void BuildPond(Data data)
        {
            var balance = data.balance;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = NewCamera();
            cam.orthographicSize = PixelHeight / (2f * PixelSprites.PPU);
            var ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = PixelSprites.PPU;
            ppc.refResolutionX = PixelWidth;
            ppc.refResolutionY = PixelHeight;
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            cam.transform.position = new Vector3(follow.restPosition.x, follow.restPosition.y, CameraZ);

            new GameObject("Global Light 2D").AddComponent<Light2D>().lightType = Light2D.LightType.Global;

            var systems = new GameObject("Systems");
            var input = systems.AddComponent<FishingInput>();

            var spawner = new GameObject("Fish").AddComponent<FishSpawner>();
            spawner.balance = balance;

            var rodTip = new GameObject("Rod Tip").transform;
            rodTip.position = new Vector2(balance.dockEdgeX, GameBalance.WaterlineY) + data.art.rodTipOffset;

            var hook = new GameObject("Hook").AddComponent<SpriteRenderer>();
            hook.transform.position = rodTip.position;

            var line = new GameObject("Fishing Line").AddComponent<LineRenderer>();
            line.widthMultiplier = 1f / PixelSprites.PPU;   // one pixel wide
            line.numCapVertices = 0;
            line.sortingOrder = SortOrder.FishingLine;
            line.sharedMaterial = LineMaterial();
            var lineColor = Palette.OnWood;
            lineColor.a = LineAlpha;
            line.startColor = line.endColor = lineColor;

            var dressing = new GameObject("Scene Dressing").AddComponent<SceneDressing>();
            dressing.balance = balance;
            dressing.art = data.art;
            dressing.hookRenderer = hook;

            var game = systems.AddComponent<FishingController>();
            game.balance = balance;
            game.spawner = spawner;
            game.input = input;
            game.qteView = new GameObject("QTE Ring").AddComponent<QteRingView>();
            game.cameraFollow = follow;
            game.rodTip = rodTip;
            game.hook = hook.transform;
            game.line = line;

            var hud = new GameObject("HUD").AddComponent<HudView>();
            hud.game = game;
            hud.input = input;
            hud.skin = data.skin;

            EditorSceneManager.SaveScene(scene, PondPath);
        }

        static Camera NewCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Edge;
            return cam;
        }

        // ----- assets -----

        /// <summary>Loads Data/{name}.asset, creating it the first time.</summary>
        static T Asset<T>(string name) where T : ScriptableObject => Asset<T>(name, out _);

        static T Asset<T>(string name, out bool created) where T : ScriptableObject
        {
            string path = $"{DataDir}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            created = existing == null;
            if (!created) return existing;
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Material LineMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(LinePath);
            if (existing != null) return existing;
            var mat = new Material(Shader.Find(LineShader) ?? Shader.Find(FallbackLineShader));
            AssetDatabase.CreateAsset(mat, LinePath);
            return mat;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
