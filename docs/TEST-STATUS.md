# 3.0.0 validation (SPT 5.0)

3.0.0 compiles against the SPT 5.0 BepInEx 6 IL2CPP core and interop assemblies. Every DynamicMaps type, member and Harmony parameter name it uses was checked against DynamicMaps 2.0.0 (`com.mpstark.dynamicmaps`, IL2CPP build), and all extension map definitions satisfy DynamicMaps 2.0's required JSON fields. MapVariants is an optional dependency in 3.0.0. MapVariants and FactoryClassic SPT 5.0 builds have not been checked; their integration is reflection-based and degrades to native maps if names changed. Not yet run in-game.

# 2.1.0 validation

The release retains the renderer and atomic viewport behavior from the successfully tested local Fix 7. Release cleanup removes unreachable downloader hooks, unused manifest fields, obsolete raster experiments and stale documentation; it does not change rendering algorithms or calibration.

Regression coverage: 294 clipped/sparse/capped tile-selection cases, 14 native/warm/SVG restoration cases, and 8 main/minimap viewport transactions covering interrupted zoom, clamping, pending tweens and screen state (316 cases total). These are extracted production-method checks with stand-ins for Unity objects, not an in-game renderer test.

The final Fix 7 gameplay log reported 42/42 layers installed, no Extended warnings/errors, matching logical/displayed zoom after style changes, zero repeated native refresh requests and steady Woods frame windows around 16.7 ms. The user reported that the run appeared fine. One Woods style switch took 201 ms. Results apply to that run and do not certify every map/mod combination or zero stutter.

The 2.1.0 release build, archive contents, bundled cache and checksums are verified during preparation. The version-bumped DLL has not been separately tested in-game.
