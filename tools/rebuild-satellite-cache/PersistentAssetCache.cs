using System.IO;

namespace DynamicMapsExtended
{
    // Same layout as the plugin's AssetCache folder; the root is the plugin folder passed on the
    // command line instead of the loaded plugin's location.
    internal static class PersistentAssetCache
    {
        internal static string PluginRoot { get; set; }
        internal static string Root => Path.GetFullPath(Path.Combine(PluginRoot, "AssetCache"));
        internal static string PacksRoot => Path.Combine(Root, "Packs");
        internal static string StateRoot => Path.Combine(Root, "State");
        internal static string StagingRoot => Path.Combine(Root, "Staging");
        internal static string LegacySiblingRoot => Path.Combine(Path.GetDirectoryName(Path.GetFullPath(PluginRoot)) ?? PluginRoot, "DynamicMaps-Extended-AssetCache");

        internal static string GetPackPath(string logicalOutputPath)
        {
            var relative = logicalOutputPath.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(PacksRoot, relative));
        }

        internal static void EnsureDirectories()
        {
            Directory.CreateDirectory(PacksRoot);
            Directory.CreateDirectory(StateRoot);
            Directory.CreateDirectory(StagingRoot);
        }
    }
}
