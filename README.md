# DynamicMaps Extended 3.0.0

Artwork and map-variant addon for SPT 5.0.x (BepInEx 6 IL2CPP). Requires **DynamicMaps 2.x** (the SPT 5.0 build). **MapVariants** is optional: without it, Manimal Interchange and other shared-ID map variants use the native DynamicMaps map, while all artwork styles still work. Install the appropriate map mods separately to use their corresponding variants.

## Installation

1. Exit the game.
2. Extract `DynamicMaps-Extended-3.0.0-SPT5.0.zip` into your SPT root, allowing overwrite.
3. Keep the included `Maps`, `Assets` and `AssetCache` folders together with the DLL under `BepInEx/plugins/DynamicMaps-Extended/`.

The complete ZIP includes all 42 prepared raster layers. No runtime downloads, map generation, download progress UI or F12 asset-download options remain. Re-extract the complete ZIP if artwork files are missing. DynamicMaps (and MapVariants, if used) are separate mods and are not included.

SPT 4.1.x users should stay on DynamicMaps Extended 2.1.0; 3.0.0 only loads on SPT 5.0.

## Artwork settings

Use F12 → DynamicMaps Extended to select the global artwork mode or a per-map override. Recommended prefers available Satellite artwork; DynamicMaps Vanilla uses the original artwork. Abstract is offered where distinct verified artwork is available. See [the artwork matrix](docs/ARTWORK-MATRIX.md).

DynamicMaps 2.0 for SPT 5.0 no longer renders SVG maps, so Woods, Factory and Streets Abstract ship as PNGs pre-rendered from the bundled SVGs (4096 px on the long side; Factory 2048 px) and load asynchronously like other Extended artwork. Manimal Interchange Abstract uses prepared raster previews and high-resolution tiles. Satellite retains its prepared high-resolution tile content, with visible tiles decoded asynchronously and bounded caches.

Switching artwork updates the current view in place, preserving live markers, tracking and the viewed coordinate. DynamicMaps continues to own marker providers, quests, player trails, floors and minimap behavior. Extended does not patch Fika networking; client/UI functionality is disabled on Fika Headless.

## Diagnostics and limits

Enable `Debug / EnableDebugLogging` in F12 when diagnosing a problem. Frame timing and committed viewport state are written to `BepInEx/LogOutput.log`. Disable it for normal play. First use of uncached artwork can still cause a brief loading or style-switch hitch; steady frame times do not guarantee every texture upload is stutter-free.

## Building

Install the .NET 10 SDK and start SPT 5.0 once so BepInEx generates `BepInEx/interop`, then run `build.ps1 -SptRoot "C:/SPT" -NoPause`. The build creates a **base package**, which omits the large prepared raster cache. Produce the complete installation ZIP using `tools/bundle-maps.py` as described in [bundling instructions](docs/BUNDLED-MAPS.md). Neither step publishes to GitHub or changes the game installation.

Project code is MIT licensed. Third-party artwork retains its original terms and attribution; see [THIRD-PARTY.md](THIRD-PARTY.md), [LICENSING-STATUS.md](LICENSING-STATUS.md) and the included artwork license.
