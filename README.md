# LethalPerformance
Optimizes CPU time and minimizes GC allocation. It should help with lag spikes, so your frametime will be smoother.

## Features
- Adds key bind to Unity logs folder: `Ctrl + Shift + L`

## Help

### My screen is black \[Failed to find "lib_burst_generated.data"\]
Disable Kaspersky or other types of antivirus. After that, uninstall the mod, click to clean up unused mods, and then reinstall the mod. It should download correctly.

### My logs are spammed with "No more space in Reflection Probe Atlas. To solve this issue, increase the size of the Reflection Probe Atlas in the HDRP settings."
Increase config value for `Reflection probe atlas texture resolution` in the Lethal Performance configuration.

## Recommended mods
Recommended to use with these mods:
- [LethalFixes](https://thunderstore.io/c/lethal-company/p/Dev1A3/LethalFixes/) by Dev1A3 - fixes lag spikes caused by Dissonance and RPC logging and more.
- [AsyncLogger](https://thunderstore.io/c/lethal-company/p/mattymatty/AsyncLoggers/) by Matty_Matty - moves logging to another thread, resulting in smoother frametime.
- [BepInEx Faster Load AssetBundles Patcher](https://thunderstore.io/c/lethal-company/p/DiFFoZ/BepInEx_Faster_Load_AssetBundles_Patcher/) by DiFFoZ - reduces RAM usage and speeds up asset loading, leading to smoother frametime.
- [PathfindingLagFix](https://thunderstore.io/c/lethal-company/p/Zaggy1024/PathfindingLagFix/) by Zaggy1024 - makes the calculation of AI path to use time-slicing, resulting in smoother frametime.
- [CullFactory](https://thunderstore.io/c/lethal-company/p/fumiko/CullFactory/) by fumiko & Zaggy1024 - stops rendering interior rooms that aren't visible.

## Credits
- Icon by [Lorc](https://lorcblog.blogspot.com/) via [game-icons.net](https://game-icons.net/)
