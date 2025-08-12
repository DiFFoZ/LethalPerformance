# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] 2025-08-12
### Changed
- Forced EntranceTeleport caching.
- For faster game start up disabled transpiler patch that replaces Object.FindObjectOfType.
- Disabled culling for custom pass to reuse main culling instead (that should also resolve crashing on moons that use tree/detail terrain rendering).
### Fixed
- Steam valve doesn't pop (disabled caching of SteamValveHazard).

## [1.1.6] 2025-06-02
### Fixed
- Harmony patch failure warning.

## [1.1.5] 2025-04-20
### Fixed
- CruiserCourier mod causing moon objects to not be cached.

## [1.1.4] 2025-04-19
### Fixed
- Snowfall VFX from LethalElements doesn't work.

## [1.1.3] 2025-04-14
### Fixed
- Landing on Gratar moon causing camera to render only some parts of the sky [Thanks ThecheeseXD].

## [1.1.2] 2025-04-12
### Fixed
- Item duplication on lobby save [Thanks Kestrel16C].

## [1.1.1] 2025-04-12
### Added
- Check for internal flag from HarmonyX for better mod compatibility.

## [1.1.0] 2025-04-12
Note: some mods are causing some entrance teleports to be not found. Tracking issue: https://github.com/DiFFoZ/LethalPerformance/issues/15

### Added
- Caching of many networked objects (EntranceTeleport, VehicleController, Landmine, etc).
- Caching of moon objects (ItemDropship, RuntimeDungeon).
- Enable all optimization of Mono JIT, expect of aggressive inlining.
- Warning tip if entrance teleport are broken by some mod.
- Warning log if Terbium mod is installed.

## [1.0.0] 2025-03-10
### Added
- Caching of VehicleController, PlayerVoiceIngameSettings objects.
- Check for optimization to prevent vehicle to search dropship after delivery.
### Changed
- Make rendering sampler to be included in release build, so dev can use it.
### Fixed
- UnityExplorer may break caching system.
- PerProfileSaves mod incompatibility.

## Pre-release [0.0.111] 2025-03-07
### Added
- Check for optimization to prevent vehicle to search dropship after delivery.
### Fixed
- After client disconnect the voice component cause index out of range in caching.

## Pre-release [0.0.110] 2025-03-06
### Added
- Caching of VehicleController, PlayerVoiceIngameSettings objects.
### Changed
- Make rendering sampler to be included in release build, so dev can use it.
### Fixed
- UnityExplorer may break caching system.

## [0.6.0] 2025-03-04
ES3 caching is here! yay

### Added
- Caching of AudioListener, DepositItemsDesk, BeltBagInventoryUI.
- Caching of ES3 files.
### Changed
- Updated readme to include Lethal Sponge mod.

## Pre-release [0.0.106] 2025-03-04
### Changed
- Updated readme to include Lethal Sponge mod.
### Fixed
- Lag spike during saving due to deleting save keys.

## Pre-release [0.0.105] 2025-03-03
### Fixed
- LCBetterSave and SmartItemSaving throwing exception, because of cached saving feature.

## Pre-release [0.0.104] 2025-03-02
### Fixed
- Crash when deleting save file.

## Pre-release [0.0.103] 2025-03-02
### Fixed
- Saving value by forcing it as object, causing writing garbage data.

## Pre-release [0.0.102] 2025-03-02
### Changed
- Force encryption for all saves. Prevents losing save when loading to the game without LethalPerformance.

## Pre-release [0.0.101] 2025-03-02
### Fixed
- Diversity intro cutscene plays every session.

## Pre-release [0.0.100] 2025-03-02
ES3 caching is here! yay<br>

**Precaution:**<br>
**If you load the game without `LethalPerformance` after playing with it, you will not able to get your saves to get loaded!**

### Added
- Caching of AudioListener, DepositItemsDesk, BeltBagInventoryUI.
- Caching of ES3 files.
- Saving ES3 files as unencrypted. Note that `LCGeneralSaveData` is still will be encrypted.

## [0.5.1] 2025-01-27
### Fixed
- Compatibility with BrutalCompanyMinusExtra(Reborn).

## [0.5.0] 2024-12-03
### Added
- Experimental option to utilize Steam Voice API instead of the Unity Microphone API. It's disabled by default, enabling it may help with less lag spike.
- Patched FoliageDetailDistance to remove allocation of material.
- Cached normals array in player normals calculation to remove allocation.

## [0.4.3] 2024-11-29
### Fixed
- Dummy from MoreCompany doesn't render when in game.

## [0.4.2] 2024-11-05
### Fixed
- Stack overflow when using Immersive Visor mod.

## [0.4.1] 2024-10-03
### Added
- Dynamic method compilation of the volume component for searching options to remove reflection usage.
- Enabled persistent history flag for ship cameras.
- Audio spatialization warning disabler if LethalLevelLoader or LobbyControl is not installed.
### Changed
- Modified the mod to not wait until Loadstone and LCVR are loaded.
- Lowered priority of patches to minimize breaking other mods.

## [0.4.0] 2024-09-09
### Added
- Caching of `AudioReverbPresets` that will help to prevent lag spikes when entering/exiting ship.
- Auto force disabling volumetric fog if the hardware system doesn't support it. 

## [0.3.5] 2024-09-01
### Changed
- Increased the reflection probe texture atlas from 1024x1024 to 2048x1024.
### Removed
- The option to compress suit textures, as it is now enabled by default.
### Fixed
- Config files for new mods are not saved if they contain only booleans [thanks @sweetbale].

## [0.3.4] 2024-08-19
### Fixed
- Pressing join by LAN crashing the server [thanks @Zaggy1024].

## [0.3.3] 2024-08-10
### Changed
- Do not modify UI camera settings if LCVR mod is loaded.

## [0.3.2] 2024-08-07
### Fixed
- String value with backslash are parsed incorrectly.

## [0.3.1] 2024-08-07
### Fixed
- Patcher assembly file included twice.

## [0.3.0] 2024-08-07
### Added
- Optimization to reduce memory allocation of reloading/saving config file.
- Async saving config file.

## [0.2.1] 2024-07-31
### Fixed
- LCVR mod fails to load because of missing OpenXR burst code.

## [0.2.0] 2024-07-30
### Added
- Keybind to open the Unity logs folder with `Ctrl + Shift + L`.
- Patching of BepInEx configuration to reduce memory allocation.
- `CookieAtlasResolution` and `ReflectionProbeCacheResolution` configuration options.
- Patching camera initialization to remove debugging window registration.
### Fixed
- Caching was not working before Awake was called on the component.

## [0.1.2] 2024-07-25
### Changed
- Temporarily increased reflection probe atlas resolution to 2048x2048 to fix log spam.

## [0.1.1] 2024-07-24
### Added
- Config option `Experimental.Mods::Compress custom suits textures`, by default is disabled.
### Changed
- Make caching to run before of all mods.
### Fixed
- Terminal accessible objects code names are not rendered to the map camera.

## [0.1.0] 2024-07-21
### Added
- Config option `Unsafe.Rendering` to disable some rendering code, by default is disabled.
- Patching of stormy/rainy weathers to prevent logging: `Sub-emitters may not use stop actions. The Stop action will not be executed`.
- Added disabler of FileSystemWatcher. It's very unoptimized on Windows Mono.
- Added remover of some objects on scene loading.
### Changed
- The system of unsafe caching instances.

## [0.0.11] 2024-07-18
### Changed
- Updated README.
### Fixed
- Fixed Mirage unable to parse .wav file correctly.

## [0.0.10] 2024-07-16
### Added
- Implemented patches to reduce memory allocation:
	- `AudioReverbTrigger` no longer allocates every frame by avoiding the retrieval of a collider tag.
	- `HangarShipDoor` no longer allocates every frame by replacing `string.Format` with `TMP_Text.SetText(string, float)`.
	- `WaveFileWriter` no longer allocates by rewriting Mono `BinaryWriter.Write(float)` to avoid array allocation with every write.
	- Resolved an issue where the local username billboard was toggling between enabled and disabled every frame, leading to unnecessary memory allocation.
### Fixed
- Resolved an exception thrown by another mod attempting to access the object instance while in the main menu.

## [0.0.9] 2024-07-08
### Added
- Further optimized the process of finding a singleton object by not sorting by instance id.
- Added `HarmonyXTranspilerFix` dependency for patching edge cases that caused methods to break.
### Removed
- Patch of ItemDropship that fixes NullReferenceException on custom moons. Recommended alternative [CompanyCruiserFix](https://thunderstore.io/c/lethal-company/p/DiFFoZ/CompanyCruiserFix/).

## [0.0.8] 2024-07-08
### Added
- Optimization of finding a singleton object. This should help reduce lag spikes.

## [0.0.7] 2024-07-04
### Fixed
- Invalid patch of `StartOfRound.SetPlayerSafeInShip`.

## [0.0.6] 2024-07-04
### Added
- Optimization of `HDCamera.UpdateShaderVariablesGlobalCB`.
- Optimization of `StartOfRound.SetPlayerSafeInShip`.
### Fixed
- Harmony patching exception with Loadstone mod.

## [0.0.5] 2024-06-29
### Fixed
- Temp bandaid fix for custom moons that item drop ship throwing NullReferenceException (important, it still doesn't fix spawning of vehicle on custom moons).
### Known issues
- Harmony patch exception with Loadstone mod. You can ignore it safely.

## [0.0.4] 2024-06-24
### Fixed
- Burst API cannot find the burst method because of different assembly version.

## [0.0.3] 2024-06-24
### Added
- Optimization of `HDCamera.UpdateShaderVariablesXRCB`.

## [0.0.2] 2024-06-22
### Fixed
- Burst library cannot be found.

## [0.0.1] 2024-06-21
### Added
- Initial commit.
