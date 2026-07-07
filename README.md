# MapLibre for Unity

Experimental Unity bindings for [MapLibre Native](https://github.com/maplibre/maplibre-native), built on top of the
[maplibre-native-ffi](https://github.com/maplibre/maplibre-native-ffi) C API.

**Status: Experimental (v0.3.0).** APIs are unstable and may change without notice.

## Supported Platforms

| Platform       | Status                |
|----------------|------------------------|
| Windows x64    | ✅ Supported            |
| macOS          | planned                |
| Linux          | planned                |
| Android        | planned                |
| iOS            | planned                |

This MVP targets the Windows x64 Editor and Standalone Player only. Other platforms are on the roadmap but not
implemented yet.

## Installation

Add the package via the Unity Package Manager using a git URL:

```
https://github.com/fukuda-A-HU/maplibre-unity.git?path=Packages/com.fukuda-a-hu.maplibre-unity
```

(Window > Package Manager > "+" > "Add package from git URL...")

## Quick Start

The fastest way to see the plugin working is to import the bundled sample:

1. Open **Window > Package Manager**, select **MapLibre for Unity** in the package list.
2. Open the **Samples** tab and click **Import** next to **Basic Map**.
3. Open the imported `BasicMap.unity` scene (under `Assets/Samples/MapLibre for Unity/.../Basic Map/`) and press
   **Play**. You should see a full-screen map with mouse-drag panning and scroll-wheel zooming.

To add the map to your own scene instead:

1. Create an empty `GameObject` in your scene.
2. Add the `MapLibreMapView` component (`MapLibre.Unity` namespace) to it.
3. Configure `Style Url`, `Width`/`Height`, and the initial `Latitude`/`Longitude`/`Zoom` in the Inspector.
4. Read the `Texture` property at runtime (e.g. from a script or `RawImage`) once the map starts rendering.

**Repository developers:** the sample's actual source lives at
`Packages/com.fukuda-a-hu.maplibre-unity/Samples~/BasicMap` (the trailing `~` hides it from the Unity Editor
outside of the Package Manager import flow, per Unity's samples convention).

## Architecture

```
FFI (mln_* C API, maplibre-native-c.dll)
   -> WGL offscreen context, shared with a MapLibre-owned GL context
   -> MapLibre renders into that shared context's texture
   -> CPU readback via mln_texture_read_premultiplied_rgba8
   -> Bytes copied into a Unity Texture2D (RGBA32) every frame
```

In short: this plugin does **not** integrate with Unity's own graphics device or command buffers. It drives an
independent, hidden-window WGL context that shares a context group with MapLibre Native's renderer, reads the
rendered frame back to the CPU as premultiplied RGBA8, and uploads it into a `Texture2D` each frame. This keeps the
integration simple at the cost of a CPU readback copy every frame - acceptable for the v0.1 MVP, but a likely target
for a future GPU-side (D3D11/interop) fast path.

## 3D Terrain + PLATEAU (v0.3.0)

On top of the existing 2D `MapLibreMapView`, the package now also includes an experimental 3D mode built from two
Japan-specific open data sources:

- **3D terrain** (`GsiTerrainLayer`) from GSI (Geospatial Information Authority of Japan) elevation (`dem_png`) and
  aerial photo (`seamlessphoto`) tiles.
- **3D buildings** (`PlateauTilesetLayer`) from [Project PLATEAU](https://www.mlit.go.jp/plateau/)'s 3D city models,
  streamed as 3D Tiles 1.0 / b3dm and loaded via glTFast.

See the [package README](Packages/com.fukuda-a-hu.maplibre-unity/README.md#3d-terrain--plateau-v030) for usage,
required dependencies (`com.unity.cloud.gltfast`, `com.unity.cloud.draco`, `com.unity.nuget.newtonsoft-json` -
resolved automatically via UPM), and data source attribution requirements. Import the **3D Terrain + PLATEAU
(Japan)** sample from the Package Manager's Samples tab to try it.

## License

This repository's own source code is licensed under the [MIT License](LICENSE).

The bundled native binary (`maplibre-native-c.dll`) is distributed under the BSD 2-Clause License. See
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for the full text and provenance.

## 日本語サマリ

MapLibre Native を Unity から使うための実験的なプラグインです(v0.1 MVP、現時点では Windows x64 のみ対応)。
内部では WGL の共有コンテキスト上で MapLibre Native にレンダリングさせ、その結果を CPU 経由で読み戻して
Unity の `Texture2D` に転送する方式を採用しています。`MapLibreMapView` コンポーネントを GameObject に
アタッチするだけで最小構成の地図表示が可能です。同梱のネイティブ DLL は BSD-2-Clause ライセンスであり、
本体コードとはライセンスが異なる点に注意してください(詳細は `THIRD_PARTY_NOTICES.md` を参照)。

v0.3.0 では、国土地理院(GSI)の標高タイル・写真タイルによる3D地形表示(`GsiTerrainLayer`)と、
Project PLATEAU(国土交通省)の3D都市モデルを 3D Tiles(b3dm)形式でストリーミング表示する建物レイヤー
(`PlateauTilesetLayer`)を追加しました。既存の2D地図機能には変更を加えていません。glTFast / Draco /
Newtonsoft.Json はパッケージの依存関係として宣言されており、UPM が自動的に解決します。国土地理院タイルは
利用時に出典表記が必要、PLATEAU データは CC BY 4.0 相当のオープンデータですが同様に出典表記が必要です。
また両データの配信サービスはいずれも試験運用中で無保証である点にご注意ください。
