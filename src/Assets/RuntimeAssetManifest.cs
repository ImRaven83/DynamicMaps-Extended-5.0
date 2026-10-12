using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DynamicMapsExtended
{
    // Runtime reads only installation paths. Full provenance remains in the packaged JSON.
    internal sealed class RuntimeAssetManifest
    {
        [JsonPropertyName("assets")] public List<RuntimeAssetEntry> Assets { get; set; }
    }

    internal sealed class RuntimeAssetEntry
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("pack")] public RuntimeAssetPath Pack { get; set; }
        [JsonPropertyName("preview")] public RuntimeAssetPath Preview { get; set; }
        [JsonPropertyName("warm")] public RuntimeAssetPath Warm { get; set; }
    }

    internal sealed class RuntimeAssetPath
    {
        [JsonPropertyName("outputPath")] public string OutputPath { get; set; }
    }
}
