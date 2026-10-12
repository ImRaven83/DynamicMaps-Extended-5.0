using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using UnityEngine;

namespace DynamicMapsExtended
{
    [BepInPlugin("com.blackhawk.dynamicmapsextended", "DynamicMaps Extended", "3.0.0")]
    [BepInDependency("com.mpstark.dynamicmaps", BepInDependency.DependencyFlags.HardDependency)]
    // Optional on SPT 5.0: without MapVariants, shared-ID maps keep their native DynamicMaps
    // definition and only artwork-style choices apply.
    [BepInDependency("com.lennoxp90.mapvariants", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.lennoxp90.factoryclassic", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.manimal.interchange", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.manimal.lighthouse", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.manimal.labsboiler", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.manimal.icebreaker", BepInDependency.DependencyFlags.SoftDependency)]
    // Soft dependency is intentional: when Fika Headless is installed, BepInEx loads it
    // before this plugin so the guard at the very start of Load() can disable all
    // DynamicMaps Extended client/UI work before patches or artwork workers begin.
    [BepInDependency("com.fika.headless", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BasePlugin
    {
        public const string Guid = "com.blackhawk.dynamicmapsextended";
        public const string Name = "DynamicMaps Extended";
        public const string Version = "3.0.0";
        public const string BuildLabel = "RELEASE";

        public const string DynamicMapsGuid = "com.mpstark.dynamicmaps";
        public const string MapVariantsGuid = "com.lennoxp90.mapvariants";
        public const string FactoryClassicGuid = "com.lennoxp90.factoryclassic";
        public const string ManimalInterchangeGuid = "com.manimal.interchange";
        public const string ManimalLighthouseGuid = "com.manimal.lighthouse";
        public const string ManimalLabsGuid = "com.manimal.labsboiler";
        public const string ManimalIcebreakerGuid = "com.manimal.icebreaker";
        public const string FikaCoreGuid = "com.fika.core";
        public const string FikaHeadlessGuid = "com.fika.headless";

        internal static Plugin Instance { get; private set; }
        internal static new ManualLogSource Log { get; private set; }

        // SPT 5.0 runs EFT as IL2CPP: a BepInEx 6 plugin is not a MonoBehaviour, so Update,
        // OnDestroy and every coroutine run on this injected component instead.
        internal static PluginRunner Runner { get; private set; }
        internal static ConfigEntry<bool> DebugLogging { get; private set; }
        internal static bool DebugEnabled => DebugLogging != null && DebugLogging.Value;

        internal static string ExtensionRoot { get; private set; }
        internal static string MapsRoot => System.IO.Path.Combine(ExtensionRoot, "Maps");

        private Harmony _harmony;
        private bool _disabledForHeadless;

        public override void Load()
        {
            Instance = this;
            Log = base.Log;
            ExtensionRoot = System.IO.Path.GetDirectoryName(typeof(Plugin).Assembly.Location);

            // Fika Headless is a non-visual raid host. DynamicMaps Extended is a client/UI
            // extension and must not patch UI, start raster workers, or load map
            // artwork there. The soft dependency above guarantees Headless is already in
            // Chainloader.PluginInfos when both plugins are installed.
            if (PluginDetection.Loaded(FikaHeadlessGuid))
            {
                _disabledForHeadless = true;
                Log.LogInfo($"{Name} {Version}: Fika Headless detected ({FikaHeadlessGuid}) - client/UI functionality disabled. No Harmony patches or artwork workers were started.");
                return;
            }

            MapStyleManager.Bind(Config);

            DebugLogging = Config.Bind(
                "Debug",
                "EnableDebugLogging",
                false,
                "Detailed DynamicMaps Extended diagnostics. Enable this in F12 before reproducing a problem, then send LogOutput.log. This does not enable any gameplay/network hooks.");

            DebugLogging.SettingChanged += (_, __) =>
            {
                if (DebugEnabled)
                {
                    Log.LogInfo("[DMExt:DBG] Debug logging enabled from F12.");
                    LogDiagnosticSnapshot("F12 enabled");
                }
                else
                {
                    Log.LogInfo("[DMExt] Debug logging disabled.");
                }
            };

            Runner = AddComponent<PluginRunner>();

            _harmony = new Harmony(Guid);
            PatchAllIsolated();

            TilePackManager.Initialize();
            BundledAssetAvailability.Initialize();

            Log.LogInfo($"{Name} {Version} {BuildLabel} loaded");
            Log.LogInfo($"Extension root: {ExtensionRoot}");
            Log.LogInfo("Artwork is resolved per floor. Recommended prefers locally prepared Satellite artwork; missing Satellite floors stay on their original DynamicMaps artwork. Raster loading is on-demand and asynchronous.");
            Log.LogInfo("DynamicMaps keeps ownership of player tracking, quest/extract markers, minimap, floor selection and all marker providers.");
            Log.LogInfo("Fika is not patched; this addon creates no Fika packets and contains no multiplayer synchronization code.");

            if (DebugEnabled)
                LogDiagnosticSnapshot("startup");
        }

        // Patch class by class: a DynamicMaps update that removes one hook must not stop every
        // other DMExt patch from applying (Harmony's PatchAll aborts on the first failure).
        private void PatchAllIsolated()
        {
            var failed = 0;
            foreach (var type in AccessTools.GetTypesFromAssembly(typeof(Plugin).Assembly))
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0)
                    continue;

                try
                {
                    _harmony.CreateClassProcessor(type).Patch();
                }
                catch (Exception e)
                {
                    failed++;
                    Log.LogWarning($"Patch {type.Name} was not applied; that feature stays inactive: {(e.InnerException ?? e).Message}");
                }
            }

            if (failed > 0)
                Log.LogWarning($"{failed} DynamicMaps Extended patch(es) did not apply. Check that DynamicMaps 2.x for SPT 5.0 is installed.");
        }

        public override bool Unload()
        {
            Shutdown();
            return true;
        }

        internal static Coroutine StartWorker(IEnumerator routine)
            => Runner == null ? null : Runner.StartCoroutine(routine);

        internal static void StopWorker(Coroutine coroutine)
        {
            if (Runner != null && coroutine != null)
                Runner.StopCoroutine(coroutine);
        }

        internal void Shutdown()
        {
            if (Instance == null)
                return;

            if (_disabledForHeadless)
            {
                Instance = null;
                return;
            }

            RaidWarmCacheManager.Shutdown();
            TilePackManager.Shutdown();
            RasterWarmupManager.Shutdown();
            RasterSpritePatch.ReleaseAll("plugin shutdown");
            _harmony?.UnpatchSelf();
            Instance = null;
            Runner = null;
        }

        internal static void Debug(string message)
        {
            if (DebugEnabled)
                Log?.LogInfo("[DMExt:DBG] " + message);
        }

        internal static void LogDiagnosticSnapshot(string reason)
        {
            if (!DebugEnabled) return;

            Debug($"Diagnostic snapshot ({reason})");
            Debug($"Location: {DynamicMapsBridge.DescribeCurrentLocation()}");
            Debug($"Dependencies: DynamicMaps={PluginDetection.Loaded(DynamicMapsGuid)}, MapVariants={PluginDetection.Loaded(MapVariantsGuid)}, FactoryClassic={PluginDetection.Loaded(FactoryClassicGuid)}, Interchange={PluginDetection.Loaded(ManimalInterchangeGuid)}, Lighthouse={PluginDetection.Loaded(ManimalLighthouseGuid)}, Labs={PluginDetection.Loaded(ManimalLabsGuid)}, Icebreaker={PluginDetection.Loaded(ManimalIcebreakerGuid)}");
            Debug($"Fika presence only (never patched): core={PluginDetection.Loaded(FikaCoreGuid)}, headless={PluginDetection.Loaded(FikaHeadlessGuid)}");
            Debug($"MapVariants API: {MapVariantsBridge.DescribeApi()}");
            Debug($"FactoryClassic API: {FactoryClassicBridge.DescribeApi()}");
            Debug($"Raster cache: {RasterSpritePatch.DescribeCache()}");
            Debug($"Raster queue: {RasterWarmupManager.DescribeQueue()}");
            Debug($"High-res tiles: {TilePackManager.Describe()}");
            Debug($"Raid warm cache: {RaidWarmCacheManager.Describe()}");
            Debug($"Map style: {MapStyleManager.DescribeSettings()}");
            Debug($"Map style overrides: {MapStyleManager.DescribeOverrides()}");
            Debug($"Tracked DynamicMaps layers: {StyleRefreshManager.TrackedLayerCount}");
            Debug($"Factory Classic packaged map: {FactoryClassicAssets.Describe()}");
            Debug($"Extension asset readiness: {ExtensionAssetAvailability.Describe()}");
            Debug($"Bundled artwork: {BundledAssetAvailability.Describe()}");
        }
    }

    internal sealed class PluginRunner : MonoBehaviour
    {
        public PluginRunner(IntPtr pointer) : base(pointer) { }

        private void Update()
        {
            FrameDiagnostics.Update();
        }

        private void OnDestroy()
        {
            Plugin.Instance?.Shutdown();
        }
    }
}
