using System;
using System.IO;

namespace DynamicMapsExtended
{
    // DynamicMaps 2.0 (SPT 5.0) has no runtime SVG renderer: a layer that references X.svg is
    // drawn from the pre-rendered X.png beside it. Extended follows the same convention for its
    // own Abstract artwork and for DynamicMaps artwork it points at.
    internal static class RenderedImage
    {
        internal static string For(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath) ||
                !imagePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
                return imagePath;

            return Path.ChangeExtension(imagePath, ".png");
        }

        internal static bool Exists(string absolutePath)
        {
            try
            {
                var rendered = For(absolutePath);
                return !string.IsNullOrEmpty(rendered) && File.Exists(rendered) && new FileInfo(rendered).Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
