# MapLibre for Unity (Package)

Experimental MapLibre Native bindings for Unity. Windows x64 only in this 0.1.0 MVP release.

See the [repository README](https://github.com/fukuda-A-HU/maplibre-unity) for installation instructions, a quick
start guide, and architecture notes.

## Contents

- `Runtime/MapLibreMapView.cs` - `MonoBehaviour` that renders a MapLibre map into a `Texture2D`.
- `Runtime/MapLibreMapHandle.cs` - Unity-independent core wrapping the native `mln_*` C API lifecycle.
- `Runtime/Native/` - P/Invoke declarations, native structs/enums, and the WGL shared-context helper.
- `Runtime/Plugins/Windows/x86_64/maplibre-native-c.dll` - bundled native binary (see `THIRD_PARTY_NOTICES.md` at the
  repository root for license information).
