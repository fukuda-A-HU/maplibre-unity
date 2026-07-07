# Changelog

All notable changes to this package will be documented in this file.

## [0.1.0] - 2026-07-07

### Added

- Initial experimental release (MVP).
- Windows x64 only support via WGL offscreen shared-context rendering and CPU readback.
- `MapLibreMapView` MonoBehaviour rendering a MapLibre map into a `Texture2D`.
- `MapLibreMapHandle` core wrapper around the `mln_*` native C API (runtime/map lifecycle, camera control, render
  loop, pixel readback).
- Camera controls: jump-to, screen-space pan (`MoveBy`) and screen-space zoom (`ScaleBy`).
