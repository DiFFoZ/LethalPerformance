# LethalPerformance
LethalPerformance reduces CPU work, allocations, and common frame-time spikes in Lethal Company. It is designed to work alongside gameplay and content mods without changing gameplay.

For support or discussion, use the [Discord channel](https://discord.gg/XeyYqRdRGC) at [Discord thread](https://canary.discord.com/channels/1168655651455639582/1253705079605956640).

## What it improves
- Caches frequently searched game, network, moon, dungeon objects to avoid repeated `FindObjectOfType`-style work.
- Reduces allocations and main-thread work in common paths.
- Reduces save-related hitching by caching Easy Save 3 file data and scheduling saves.
- Tunes HDRP defaults to reduce unnecessary memory use, including the reflection-probe texture cache.
- Includes compatibility optimizations for supported mods, including MoreSuits.
- Keeps Unity profiling scopes available in development builds for diagnosing rendering performance.

## Useful shortcut
Press <kbd>Ctrl</kbd> + <kbd>Shift</kbd> + <kbd>L</kbd> to open the Unity log folder. This is useful when reporting a problem.

## Troubleshooting

### My screen is black \[Failed to find "lib_burst_generated.data"\]
Disable Kaspersky or other types of antivirus. After that, uninstall the mod, click to clean up unused mods, and then reinstall the mod. It should download correctly.

### The log says that the Reflection Probe Atlas or 2D Cookie Texture Atlas is full
Increase the matching LethalPerformance rendering setting.

### I see an "Entrance teleport mismatch" tip
Another mod likely prevented an entrance teleport from spawning correctly. Expect the affected entrance to report that it is blocked; update or temporarily remove recently added moon/interior mods to identify the conflict.

### The log warns that Terbium is installed
Remove Terbium for better compatibility with LethalPerformance.

## Recommended mods
For a preconfigured set of performance and bug fixes mods that can be added to any modpack, use [Starter Pack](https://thunderstore.io/c/lethal-company/p/ThecheeseXD/Starter_Pack/) by ThecheeseXD.

LethalPerformance also works well with:
- [LethalFixes](https://thunderstore.io/c/lethal-company/p/Dev1A3/LethalFixes/) by Dev1A3 - fixes lag spikes caused by Dissonance and RPC logging and more.
- [AsyncLogger](https://thunderstore.io/c/lethal-company/p/mattymatty/AsyncLoggers/) by Matty_Matty - moves logging to another thread, resulting in smoother frametime.
- [BepInEx Faster Load AssetBundles Patcher](https://thunderstore.io/c/lethal-company/p/DiFFoZ/BepInEx_Faster_Load_AssetBundles_Patcher/) by DiFFoZ - reduces RAM usage and speeds up asset loading, leading to smoother frametime.
- [PathfindingLagFix](https://thunderstore.io/c/lethal-company/p/Zaggy1024/PathfindingLagFix/) by Zaggy1024 - makes the calculation of AI path to use time-slicing, resulting in smoother frametime.
- [CullFactory](https://thunderstore.io/c/lethal-company/p/fumiko/CullFactory/) by fumiko & Zaggy1024 - stops rendering interior rooms that aren't visible.
- [LethalSponge](https://thunderstore.io/c/lethal-company/p/Scoops/LethalSponge/) by Scoops - frame limiting cameras in the ship, creating LOD for items and more.
- [ReXuvination](https://thunderstore.io/c/lethal-company/p/XuXiaolan/ReXuvination/) by XuXiaolan - optimizes colliders that unnecessarily calling OnTriggerStay message.

## Credits
- Icon by [Lorc](https://lorcblog.blogspot.com/) via [game-icons.net](https://game-icons.net/)
