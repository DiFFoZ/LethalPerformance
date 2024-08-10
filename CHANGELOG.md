# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
