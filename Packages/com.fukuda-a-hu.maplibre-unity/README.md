# MapLibre for Unity (Package)

Experimental MapLibre Native bindings for Unity. Windows x64 only in this MVP release.

See the [repository README](https://github.com/fukuda-A-HU/maplibre-unity) for installation instructions, a quick
start guide, and architecture notes.

## Quick Start

1. In **Window > Package Manager**, select this package, open the **Samples** tab, and click **Import** next to
   **Basic Map**.
2. Open the imported `BasicMap.unity` scene and press **Play**.

Or add `MapLibreMapView` (`MapLibre.Unity` namespace) to any `GameObject` directly and read its `Texture` property
once the map starts rendering.

## Contents

- `Runtime/MapLibreMapView.cs` - `MonoBehaviour` that renders a MapLibre map into a `Texture2D`.
- `Runtime/MapLibreMapHandle.cs` - Unity-independent core wrapping the native `mln_*` C API lifecycle.
- `Runtime/Native/` - P/Invoke declarations, native structs/enums, and the WGL shared-context helper.
- `Runtime/Plugins/Windows/x86_64/maplibre-native-c.dll` - bundled native binary (see `THIRD_PARTY_NOTICES.md` at the
  repository root for license information).
- `Samples~/BasicMap` - the "Basic Map" sample (see the Samples tab in Package Manager), a full-screen `RawImage`
  demo with mouse-drag panning and scroll-wheel zooming.
