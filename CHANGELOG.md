# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.9] 2024-07-08
### Added
- Further optimized the process of finding a singleton object by not sorting by instance id.
- Added `HarmonyXTranspilerFix` dependency for patching edge cases that caused methods to break.

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
