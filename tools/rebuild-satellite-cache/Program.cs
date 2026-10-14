using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace DynamicMapsExtended
{
    // Usage:
    //   rebuild-satellite-cache --plugin-root <folder> [--mirror <base-url>]
    // <folder> is a DynamicMaps-Extended plugin folder (for example the extracted base ZIP's
    // BepInEx/plugins/DynamicMaps-Extended). It must contain Assets/ with the v2.0.0 manifest and
    // structure lock. The cache is written into <folder>/AssetCache and the previews/warm atlases
    // into <folder>/Maps. Re-running resumes: finished layers are skipped, partial downloads kept.
    internal static class Program
    {
        private const string ManifestName = "DMEXT-ASSET-MANIFEST-v2.0.0.json";
        private const string StructureName = "DMEXT-UPSTREAM-STRUCTURE-v2.0.0.json";

        private static int Main(string[] args)
        {
            string pluginRoot = null, mirror = null;
            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "--plugin-root" && i + 1 < args.Length) pluginRoot = args[++i];
                else if (args[i] == "--mirror" && i + 1 < args.Length) mirror = args[++i];
                else { Usage(); return 2; }
            }
            if (string.IsNullOrWhiteSpace(pluginRoot)) { Usage(); return 2; }

            pluginRoot = Path.GetFullPath(pluginRoot);
            var manifest = Path.Combine(pluginRoot, "Assets", ManifestName);
            var structure = Path.Combine(pluginRoot, "Assets", StructureName);
            if (!File.Exists(manifest) || !File.Exists(structure))
            {
                Console.Error.WriteLine("Assets/" + ManifestName + " and Assets/" + StructureName + " must exist under " + pluginRoot);
                return 2;
            }

            if (!string.IsNullOrWhiteSpace(mirror))
            {
                var baseUrl = mirror.TrimEnd('/');
                LockedAssetInstaller.UrlRewrite = url => url
                    .Replace("https://assets.tarkov.dev", baseUrl + "/assets.tarkov.dev")
                    .Replace("https://raw.githubusercontent.com", baseUrl + "/raw.githubusercontent.com");
            }

            PersistentAssetCache.PluginRoot = pluginRoot;
            using var cancel = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); Console.WriteLine("Stopping after the current file; progress is kept."); };

            var installer = new LockedAssetInstaller(pluginRoot, manifest, Sha256Files(manifest, structure), cancel.Token, Print, () => false);
            Console.WriteLine("DynamicMaps Extended Satellite cache rebuild");
            Console.WriteLine("Plugin folder: " + pluginRoot);
            Console.WriteLine("Layers: " + installer.Manifest.Assets.Count.ToString(CultureInfo.InvariantCulture) + ". Source: Tarkov.dev (about 94k tiles, 1.75 GB). Ctrl+C stops; re-run to resume.");
            installer.Run();

            Console.WriteLine();
            Console.WriteLine("Log: " + installer.LogPath);
            if (installer.GlobalReadyMarkerMatches())
            {
                Console.WriteLine("ALL 42 LAYERS READY. Bundle with tools/bundle-maps.py using this folder as --plugin-root.");
                return 0;
            }
            Console.WriteLine("Not every layer is ready yet. Re-run to resume; see the log for failures.");
            return 1;
        }

        private static string _lastLine = string.Empty;

        private static void Print(LockedAssetInstaller.ProgressSnapshot p)
        {
            var line = $"[{p.Percent,3}%] {p.Completed}/{p.Total} {p.Stage} {p.Asset} - {p.Detail}";
            if (p.Failed > 0) line += $" (failed: {p.Failed})";
            if (line == _lastLine) return;
            _lastLine = line;
            Console.WriteLine(line);
        }

        // Identical to the 2.0.0 AssetPreparationManager global readiness hash.
        private static string Sha256Files(params string[] paths)
        {
            using var sha = SHA256.Create();
            using var stream = new MemoryStream();
            foreach (var path in paths)
            {
                var name = Encoding.UTF8.GetBytes(Path.GetFileName(path));
                stream.Write(name, 0, name.Length);
                stream.WriteByte(0);
                var bytes = File.ReadAllBytes(path);
                stream.Write(bytes, 0, bytes.Length);
                stream.WriteByte(0);
            }
            stream.Position = 0;
            var hash = sha.ComputeHash(stream);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        private static void Usage()
            => Console.Error.WriteLine("Usage: rebuild-satellite-cache --plugin-root <DynamicMaps-Extended folder> [--mirror <base-url>]");
    }
}
