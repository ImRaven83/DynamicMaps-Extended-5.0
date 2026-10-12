using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace DynamicMapsExtended
{
    [HarmonyPatch]
    internal static class RasterSpritePatch
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        // DynamicMaps-owned PNG artwork chosen by an F12 style (for example Manimal Interchange's
        // "DynamicMaps Vanilla" choice). Loaded the way DynamicMaps loads its own layers.
        private static readonly Dictionary<string, Sprite> NativeOverrideCache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

        private static PropertyInfo _imagePathProperty;
        private static MethodInfo _getOrLoadCachedSpriteMethod;
        private static FieldInfo _mapLayerDefField;
        private static PropertyInfo _mapLayerImageProperty;
        private static PropertyInfo _layerDefImagePathProperty;
        private static PropertyInfo _imageSpriteProperty;

        private static Sprite _placeholder;
        private static long _estimatedGpuBytes;

        [ThreadStatic] private static bool _inMapLayerCreate;

        internal static bool InMapLayerCreate
        {
            get => _inMapLayerCreate;
            set => _inMapLayerCreate = value;
        }

        private static MethodBase TargetMethod()
        {
            // DynamicMaps 2.0 (SPT 5.0) loads every layer through MapLayerImageUtils.
            var type = AccessTools.TypeByName("DynamicMaps.Utils.MapLayerImageUtils")
                ?? throw new MissingMemberException("DynamicMaps.Utils.MapLayerImageUtils not found");
            var layerDef = AccessTools.TypeByName("DynamicMaps.Data.MapLayerDef")
                ?? throw new MissingMemberException("DynamicMaps.Data.MapLayerDef not found");
            return _getOrLoadCachedSpriteMethod = AccessTools.Method(type, "GetOrLoadCachedSprite", new[] { layerDef })
                ?? throw new MissingMethodException(type.FullName, "GetOrLoadCachedSprite");
        }

        private static bool Prefix(object __0, ref Sprite __result, out long __state)
        {
            __state = 0;
            if (__0 == null) return true;

            _imagePathProperty ??= AccessTools.Property(__0.GetType(), "ImagePath");
            var imagePath = _imagePathProperty?.GetValue(__0) as string;
            if (string.IsNullOrEmpty(imagePath))
                return true;

            var style = MapStyleManager.Resolve(imagePath);
            var resolvedPath = style.ResolvedPath;

            // A DynamicMaps-owned image chosen by a style override (e.g. Manimal's "DynamicMaps
            // Vanilla" choice pointing back at DynamicMaps' Interchange artwork) differs from the
            // MapLayerDef source, so DynamicMaps' own loader would pick the wrong file.
            if (ShouldLoadNativeOverride(imagePath, resolvedPath))
            {
                if (NativeOverrideCache.TryGetValue(resolvedPath, out var overrideCached) && overrideCached != null)
                {
                    __result = overrideCached;
                    return false;
                }

                var sprite = LoadNativeOverride(resolvedPath);
                if (sprite != null)
                {
                    NativeOverrideCache[resolvedPath] = sprite;
                    __result = sprite;
                    return false;
                }

                Plugin.Log?.LogWarning($"Could not load map artwork override: {resolvedPath}; falling back to the MapDef source artwork.");
                __state = Stopwatch.GetTimestamp();
                return true;
            }

            if (!IsRasterPath(resolvedPath))
            {
                __state = Stopwatch.GetTimestamp();
                return true;
            }

            // Native DynamicMaps raster layers keep using DynamicMaps' own cache/lifecycle.
            // DMExt only owns raster files stored inside the extension folder.
            if (!IsExtensionOwnedRaster(resolvedPath))
            {
                __state = Stopwatch.GetTimestamp();
                return true;
            }

            if (TryGetCached(resolvedPath, out var cached))
            {
                __result = cached;
                return false;
            }

            // MapLayer.Create is called for every floor. Never queue from that construction pass.
            // The visible/default floor asks for its resolved raster later; hidden floors stay on demand.
            __result = GetPlaceholder();
            if (!InMapLayerCreate)
            {
                RasterWarmupManager.QueuePath(
                    resolvedPath,
                    "DynamicMaps raster request");
            }
            return false;
        }

        // Mirrors DynamicMaps 2.0 MapLayerImageUtils: synchronous decode, trilinear, no mips kept
        // readable. Only used for DynamicMaps-owned files that a style explicitly selects.
        private static Sprite LoadNativeOverride(string absolutePath)
        {
            try
            {
                if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                    return null;

                var sw = Stopwatch.StartNew();
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (!texture.LoadImage(File.ReadAllBytes(absolutePath), false))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                texture.name = "DMExtNativeOverride:" + Path.GetFileName(absolutePath);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Trilinear;
                texture.anisoLevel = 4;
                texture.Apply(false, true);

                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                sprite.name = texture.name;
                sw.Stop();

                if (sw.ElapsedMilliseconds >= 5)
                    Plugin.Debug($"Artwork override loaded in {sw.ElapsedMilliseconds} ms: {Path.GetFileName(absolutePath)} ({texture.width}x{texture.height})");
                return sprite;
            }
            catch (Exception e)
            {
                Plugin.Debug("Artwork override load failed: " + e);
                return null;
            }
        }

        private static bool ShouldLoadNativeOverride(string originalPath, string resolvedPath)
        {
            if (!IsRasterPath(resolvedPath) || !Path.IsPathRooted(resolvedPath) || IsExtensionOwnedRaster(resolvedPath))
                return false;

            try
            {
                // DynamicMaps itself would load the rendered PNG of the MapDef source.
                var originalAbsolute = RenderedImage.For(ResolveAbsolutePath(originalPath));
                return string.IsNullOrEmpty(originalAbsolute) ||
                       !string.Equals(Path.GetFullPath(originalAbsolute), Path.GetFullPath(resolvedPath), StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return true;
            }
        }

        private static void Postfix(object __0, Sprite __result, long __state)
        {
            if (__state == 0 || !Plugin.DebugEnabled)
                return;

            var elapsedMs = (Stopwatch.GetTimestamp() - __state) * 1000.0 / Stopwatch.Frequency;
            if (elapsedMs < 5.0)
                return;

            try
            {
                _imagePathProperty ??= AccessTools.Property(__0.GetType(), "ImagePath");
                var imagePath = _imagePathProperty?.GetValue(__0) as string ?? "<unknown>";
                Plugin.Debug($"Native map sprite load/lookup took {elapsedMs:F1} ms: {imagePath}");
            }
            catch
            {
                Plugin.Debug($"Native map sprite load/lookup took {elapsedMs:F1} ms.");
            }
        }

        internal static bool IsCached(string imagePath)
            => TryGetCached(imagePath, out _);

        internal static bool TryGetCached(string imagePath, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(imagePath))
                return false;

            return Cache.TryGetValue(imagePath, out sprite) && sprite != null;
        }

        internal static Sprite StoreAsyncTexture(string imagePath, Texture2D texture, long fileBytes, long elapsedMs, string reason)
        {
            if (string.IsNullOrEmpty(imagePath) || texture == null)
                return null;

            if (TryGetCached(imagePath, out var existing))
            {
                UnityEngine.Object.Destroy(texture);
                return existing;
            }

            try
            {
                var sprite = Sprite.Create(
                    texture,
                    new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = texture.name;

                Cache[imagePath] = sprite;
                _estimatedGpuBytes += (long)texture.width * texture.height * 4L;

                Plugin.Debug(
                    $"Raster async-ready: {imagePath}, reason={reason}, file={fileBytes / 1024.0:F1} KiB, " +
                    $"decoded={texture.width}x{texture.height}, estimatedRGBA={texture.width * (long)texture.height * 4L / (1024.0 * 1024.0):F1} MiB, " +
                    $"elapsed={elapsedMs} ms, readable={texture.isReadable}");
                return sprite;
            }
            catch (Exception e)
            {
                UnityEngine.Object.Destroy(texture);
                Plugin.Log?.LogError($"Could not create raster sprite for {imagePath}: {e}");
                return null;
            }
        }

        internal static Sprite StoreOrUpgradeAsyncTexture(string imagePath, Texture2D texture, long fileBytes, long elapsedMs, string reason)
        {
            if (string.IsNullOrEmpty(imagePath) || texture == null)
                return null;

            if (TryGetCached(imagePath, out var existing) && existing != null)
            {
                try
                {
                    var oldTexture = existing.texture;
                    var oldPixels = oldTexture == null ? 0L : (long)oldTexture.width * oldTexture.height;
                    var newPixels = (long)texture.width * texture.height;

                    // Keep an equal-or-better cached image. This also prevents a late low-resolution
                    // preview request from replacing the raid-start whole-floor warm atlas.
                    if (oldPixels >= newPixels)
                    {
                        UnityEngine.Object.Destroy(texture);
                        return existing;
                    }

                    Cache.Remove(imagePath);
                    if (oldTexture != null)
                        _estimatedGpuBytes = Math.Max(0, _estimatedGpuBytes - (long)oldTexture.width * oldTexture.height * 4L);
                    UnityEngine.Object.Destroy(existing);
                    if (oldTexture != null)
                        UnityEngine.Object.Destroy(oldTexture);
                }
                catch (Exception e)
                {
                    Plugin.Debug("Could not replace lower-resolution raster cache entry cleanly: " + e.Message);
                }
            }

            var sprite = StoreAsyncTexture(imagePath, texture, fileBytes, elapsedMs, reason);
            if (sprite != null)
                Plugin.Debug($"Raster cache upgraded for raid warmup: {imagePath} -> {DescribeSprite(sprite)}");
            return sprite;
        }

        internal static bool NeedsNativeRefresh(object mapLayer)
        {
            var current = GetLayerSprite(mapLayer);
            if (current == null)
                return true;

            var source = GetLayerImagePath(mapLayer);
            var resolved = MapStyleManager.Resolve(source).ResolvedPath;
            if (ShouldLoadNativeOverride(source, resolved))
            {
                return !NativeOverrideCache.TryGetValue(resolved, out var expected) ||
                       !ReferenceEquals(current, expected);
            }

            var name = current.name ?? string.Empty;
            return name.StartsWith("DMExt:", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("DMExtWarm:", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("DMExtNativeOverride:", StringComparison.OrdinalIgnoreCase);
        }

        internal static bool EnsureLayerSprite(object mapLayer, string reason, bool background = false)
        {
            if (!TryGetLayerInfo(mapLayer, out var imagePath, out var image, out var spriteProperty))
                return false;

            var style = MapStyleManager.Resolve(imagePath);
            var resolvedPath = style.ResolvedPath;
            if (!IsRasterPath(resolvedPath) || !IsExtensionOwnedRaster(resolvedPath))
                return false;

            if (TryGetCached(resolvedPath, out var sprite))
            {
                var current = spriteProperty.GetValue(image) as Sprite;
                if (!ReferenceEquals(current, sprite))
                    spriteProperty.SetValue(image, sprite);
                return true;
            }

            RasterWarmupManager.QueuePath(resolvedPath, reason, background);
            return false;
        }

        internal static bool TryApplyCachedRaster(object mapLayer, string resolvedPath)
        {
            if (mapLayer == null || string.IsNullOrEmpty(resolvedPath))
                return false;

            if (!TryGetCached(resolvedPath, out var sprite))
                return false;

            if (!TryGetLayerInfo(mapLayer, out _, out var image, out var spriteProperty))
                return false;

            var current = spriteProperty.GetValue(image) as Sprite;
            if (!ReferenceEquals(current, sprite))
                spriteProperty.SetValue(image, sprite);

            return true;
        }

        internal static bool RefreshLayerSprite(object mapLayer, string reason)
        {
            if (!TryGetLayerInfo(mapLayer, out var imagePath, out var image, out var spriteProperty))
                return false;

            var style = MapStyleManager.Resolve(imagePath);

            if (IsRasterPath(style.ResolvedPath) && IsExtensionOwnedRaster(style.ResolvedPath))
            {
                if (!TryGetCached(style.ResolvedPath, out var rasterSprite))
                {
                    RasterWarmupManager.QueuePath(style.ResolvedPath, reason);
                    return false;
                }

                var current = spriteProperty.GetValue(image) as Sprite;
                if (!ReferenceEquals(current, rasterSprite))
                    spriteProperty.SetValue(image, rasterSprite);
                return true;
            }

            var nativeSprite = GetOrLoadNativeSprite(GetLayerDef(mapLayer), reason);
            if (nativeSprite == null)
                return false;

            var before = spriteProperty.GetValue(image) as Sprite;
            if (!ReferenceEquals(before, nativeSprite))
                spriteProperty.SetValue(image, nativeSprite);

            return true;
        }

        private static Sprite GetOrLoadNativeSprite(object mapLayerDef, string reason)
        {
            if (mapLayerDef == null) return null;

            FrameDiagnostics.RecordNativeRefresh();
            var method = _getOrLoadCachedSpriteMethod;
            if (method == null)
                return null;

            var sw = Stopwatch.StartNew();
            try
            {
                var sprite = method.Invoke(null, new[] { mapLayerDef }) as Sprite;
                sw.Stop();

                if (sw.ElapsedMilliseconds >= 5)
                    Plugin.Debug($"Native artwork refresh took {sw.ElapsedMilliseconds} ms, reason={reason}");

                return sprite;
            }
            catch (Exception e)
            {
                sw.Stop();
                Plugin.Debug("GetOrLoadNativeSprite failed: " + e);
                return null;
            }
        }

        internal static string DescribeSprite(Sprite sprite)
        {
            if (sprite == null) return "<null>";

            try
            {
                var tex = sprite.texture;
                if (tex == null)
                    return $"{sprite.name ?? "<unnamed>"} sprite={sprite.rect.width:F0}x{sprite.rect.height:F0}, texture=<null>";

                var mib = tex.width * (long)tex.height * 4L / (1024.0 * 1024.0);
                return $"{sprite.name ?? "<unnamed>"} sprite={sprite.rect.width:F0}x{sprite.rect.height:F0}, texture={tex.width}x{tex.height}, approxRGBA={mib:F1} MiB";
            }
            catch (Exception e)
            {
                return $"<describe failed: {e.Message}>";
            }
        }

        internal static string DescribeLayerSprite(object mapLayer)
            => DescribeSprite(GetLayerSprite(mapLayer));

        internal static Sprite GetLayerSprite(object mapLayer)
        {
            if (!TryGetLayerInfo(mapLayer, out _, out var image, out var spriteProperty))
                return null;

            try { return spriteProperty.GetValue(image) as Sprite; }
            catch { return null; }
        }

        internal static string GetLayerImagePath(object mapLayer)
        {
            var def = GetLayerDef(mapLayer);
            if (def == null) return null;

            _layerDefImagePathProperty ??= AccessTools.Property(def.GetType(), "ImagePath");
            return _layerDefImagePathProperty?.GetValue(def) as string;
        }

        internal static bool IsRasterPath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath)) return false;
            var ext = Path.GetExtension(imagePath);
            return ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        internal static string ResolveAbsolutePath(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
                return null;

            if (Path.IsPathRooted(imagePath))
                return Path.GetFullPath(imagePath);

            return Path.GetFullPath(Path.Combine(
                DynamicMapsBridge.PluginPath,
                imagePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        internal static void ReleasePaths(IEnumerable<string> imagePaths, string reason)
        {
            if (imagePaths == null) return;

            var released = 0;
            foreach (var imagePath in imagePaths)
            {
                if (string.IsNullOrEmpty(imagePath) || !Cache.TryGetValue(imagePath, out var sprite))
                    continue;

                Cache.Remove(imagePath);
                if (sprite == null) continue;

                var texture = sprite.texture;
                if (texture != null)
                    _estimatedGpuBytes = Math.Max(0, _estimatedGpuBytes - (long)texture.width * texture.height * 4L);

                UnityEngine.Object.Destroy(sprite);
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
                released++;
            }

            if (released > 0)
                Plugin.Debug($"Released {released} raster layer(s), reason={reason}. {DescribeCache()}");
        }

        internal static void ReleaseAll(string reason)
        {
            if (Cache.Count > 0)
                ReleasePaths(new List<string>(Cache.Keys), reason);

            if (NativeOverrideCache.Count > 0)
            {
                var released = 0;
                foreach (var sprite in NativeOverrideCache.Values)
                {
                    if (sprite == null) continue;
                    var texture = sprite.texture;
                    UnityEngine.Object.Destroy(sprite);
                    if (texture != null)
                        UnityEngine.Object.Destroy(texture);
                    released++;
                }
                NativeOverrideCache.Clear();

                if (released > 0)
                    Plugin.Debug($"Released {released} artwork override sprite(s), reason={reason}.");
            }
        }

        internal static void ReleaseAllExcept(IEnumerable<string> keepPaths, string reason)
        {
            if (Cache.Count == 0)
                return;

            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (keepPaths != null)
            {
                foreach (var path in keepPaths)
                {
                    if (!string.IsNullOrEmpty(path))
                        keep.Add(path);
                }
            }

            var release = new List<string>();
            foreach (var path in Cache.Keys)
            {
                if (!keep.Contains(path))
                    release.Add(path);
            }

            if (release.Count > 0)
                ReleasePaths(release, reason);
        }

        internal static string DescribeCache()
            => $"rasterEntries={Cache.Count}, nativeOverrideEntries={NativeOverrideCache.Count}, estimatedRasterRGBA={_estimatedGpuBytes / (1024.0 * 1024.0):F1} MiB";

        internal static bool IsExtensionOwnedRaster(string imagePath)
            => IsRasterPath(imagePath) && IsExtensionOwnedPath(imagePath);

        private static bool IsExtensionOwnedPath(string imagePath)
        {
            try
            {
                var absolute = ResolveAbsolutePath(imagePath);
                var root = Path.GetFullPath(Plugin.ExtensionRoot)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                return absolute.StartsWith(root, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static object GetLayerDef(object mapLayer)
        {
            if (mapLayer == null) return null;
            _mapLayerDefField ??= AccessTools.Field(mapLayer.GetType(), "_def");
            return _mapLayerDefField?.GetValue(mapLayer);
        }

        private static bool TryGetLayerInfo(object mapLayer, out string imagePath, out object image, out PropertyInfo spriteProperty)
        {
            imagePath = null;
            image = null;
            spriteProperty = null;
            if (mapLayer == null) return false;

            try
            {
                var def = GetLayerDef(mapLayer);
                if (def == null) return false;

                _layerDefImagePathProperty ??= AccessTools.Property(def.GetType(), "ImagePath");
                imagePath = _layerDefImagePathProperty?.GetValue(def) as string;
                if (string.IsNullOrEmpty(imagePath)) return false;

                _mapLayerImageProperty ??= AccessTools.Property(mapLayer.GetType(), "Image");
                image = _mapLayerImageProperty?.GetValue(mapLayer);
                if (image == null) return false;

                _imageSpriteProperty ??= AccessTools.Property(image.GetType(), "sprite");
                spriteProperty = _imageSpriteProperty;
                return spriteProperty != null;
            }
            catch
            {
                return false;
            }
        }

        private static Sprite GetPlaceholder()
        {
            if (_placeholder != null) return _placeholder;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = "DMExt:deferred-raster-placeholder",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            texture.SetPixels32(new[]
            {
                new Color32(0,0,0,0), new Color32(0,0,0,0),
                new Color32(0,0,0,0), new Color32(0,0,0,0)
            });
            texture.Apply(false, true);

            _placeholder = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            _placeholder.name = texture.name;
            return _placeholder;
        }
    }
}
