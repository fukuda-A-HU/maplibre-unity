# Changelog

All notable changes to this package will be documented in this file.

## [0.4.0] - 2026-07-08

### Added

- Android (arm64-v8a) support in the C# layer via a new EGL shared context
  (`EglSharedContext`). Native binary `libmaplibre-native-c.so` is **not**
  bundled and must be built from maplibre-native-ffi and placed under
  `Runtime/Plugins/Android/arm64-v8a/` - see that folder's README.md.
- `MapLibreMapHandle.AttachTexture` now selects WGL (Windows) or EGL (Android)
  at compile time via preprocessor directives.

### Changed

- `MapLibreMapView` no longer disables itself on Android.
- `WglSharedContext` compilation gated to Windows only so Android/other builds
  don't attempt to link against opengl32.dll / user32.dll / gdi32.dll.

## [0.2.0] - 2026-07-07

### Added

- "Basic Map" sample scene, importable via Package Manager > this package > Samples tab. Contains a single
  scene (`BasicMap.unity`) wired up with the same full-screen `RawImage` + mouse-drag pan / scroll-wheel zoom
  demo previously only available as a loose script in the repository's `Assets/MapLibreDemo` folder.

## [0.1.0] - 2026-07-07

### Added

- Initial experimental release (MVP).
- Windows x64 only support via WGL offscreen shared-context rendering and CPU readback.
- `MapLibreMapView` MonoBehaviour rendering a MapLibre map into a `Texture2D`.
- `MapLibreMapHandle` core wrapper around the `mln_*` native C API (runtime/map lifecycle, camera control, render
  loop, pixel readback).
- Camera controls: jump-to, screen-space pan (`MoveBy`) and screen-space zoom (`ScaleBy`).
