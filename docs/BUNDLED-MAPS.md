# Complete map packaging

The complete ZIP contains the addon, all 42 prepared raster packs, 42 previews, 10 warm atlases and 43 provenance/readiness records. DynamicMaps and the optional MapVariants remain separate mods.

Build the base package with `build.ps1`. The prepared Satellite cache comes from an existing 2.x installation folder, or is rebuilt from Tarkov.dev (below). Then use Python 3.11 or newer:

```text
python tools/bundle-maps.py --base-zip BASE.zip --plugin-root PREPARED_EXTENDED_FOLDER --output DynamicMaps-Extended-3.0.0-SPT5.0.zip
```

The bundler uses metadata from the base ZIP. It verifies the prepared cache's metadata hash, per-layer signature, NVTILES2 index/layout and image dimensions, then checks all ZIP CRCs and writes a SHA-256 sidecar. Source files are read only. ZIP64 is supported; staging files, logs and unrelated cache files are excluded.

## Rebuilding the Satellite cache

`tools/rebuild-satellite-cache` is the 2.0.0 built-in installer as a standalone .NET 10 console tool. It downloads only the exact Tarkov.dev source tiles listed in the v2.0.0 manifest (about 94,000 tiles, 1.75 GB), first checks that each map's official structure (tile path, transform, bounds, rotation, zoom, layers) still matches the locked calibration, and refuses any map that changed. It then writes the NVTILES2 packs, previews, warm atlases and readiness markers that `bundle-maps.py` verifies.

```text
Expand-Archive artifacts/release/DynamicMaps-Extended-3.0.0-base.zip -DestinationPath work
dotnet run -c Release --project tools/rebuild-satellite-cache -- --plugin-root work/BepInEx/plugins/DynamicMaps-Extended
python tools/bundle-maps.py --base-zip artifacts/release/DynamicMaps-Extended-3.0.0-base.zip --plugin-root work/BepInEx/plugins/DynamicMaps-Extended --output DynamicMaps-Extended-3.0.0-SPT5.0.zip
```

Ctrl+C stops safely; running the same command again skips finished layers and resumes partial downloads. Progress and any failures are logged to `AssetCache/State/asset-preparation.log`. Tarkov.dev updates artwork in place, so a rebuild contains the current revision of each layer at the same locked geometry; the log notes where pixels differ from the 2026-10-04 capture. Run the tool on the extracted base ZIP so the readiness markers match the release's metadata files exactly.

The DLL does not download or generate artwork and has no repair downloader. Missing artwork requires re-extracting the complete ZIP. A base package is a developer intermediate, not a complete installation.

Frozen v2.0.0 manifest filenames and readiness signatures remain unchanged to preserve the verified artwork set. High-resolution content and world alignment are not altered by packaging.

No GitHub upload is performed. Third-party artwork retains its attribution and applicable upstream terms.
