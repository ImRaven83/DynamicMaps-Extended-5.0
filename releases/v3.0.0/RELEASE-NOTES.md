# DynamicMaps Extended 3.0.0

- Updated for SPT 5.0. Escape from Tarkov now runs on IL2CPP with BepInEx 6, so this version does not work on SPT 4.1.x (keep using 2.1.0 there).
- Works with DynamicMaps 2.0 for SPT 5.0.
- Abstract maps on Woods, Factory, Streets and Manimal Interchange are now included as ready-made images, because DynamicMaps 2.0 no longer draws SVG maps. They look the same but can soften slightly at very high zoom.
- MapVariants is now optional. Without it, Manimal Interchange and other shared-ID variants show the normal DynamicMaps map; all artwork styles still work.
- Fixed an error in DynamicMaps 2.0 on SPT 5.0 that could stop the floor slider and live markers from updating when a map opened.
- If a future DynamicMaps update removes something Extended hooks into, only that feature turns off and a warning is written to the log, instead of the whole mod stopping.
- Satellite maps, calibration and map positions are unchanged from 2.1.0.

**You need:** SPT 5.0.x and DynamicMaps 2.x. MapVariants and custom map mods are optional and installed separately.

**Installation:** Close the game, remove `BepInEx/plugins/DynamicMaps-Extended` if you are coming from SPT 4.1, then extract the whole `DynamicMaps-Extended-3.0.0-SPT5.0.zip` into your SPT folder.

**Please note:** This is the first SPT 5.0 build and has not yet been tested in-game. If something looks wrong, enable `Debug / EnableDebugLogging` in F12, reproduce it, and share `BepInEx/LogOutput.log`. Loading a map style for the first time may still cause a brief hitch.
