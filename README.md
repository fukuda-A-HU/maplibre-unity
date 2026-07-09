# MapLibre for Unity

Experimental Unity bindings for [MapLibre Native](https://github.com/maplibre/maplibre-native), built on top of the
[maplibre-native-ffi](https://github.com/maplibre/maplibre-native-ffi) C API.

**Status: Experimental (v0.5.0).** APIs are unstable and may change without notice.

## Supported Platforms

| Platform       | Status                                                          |
|----------------|-------------------------------------------------------------------|
| Windows x64    | ✅ Supported                                                      |
| Android arm64  | 🧪 Experimental (native binary required, not bundled)             |
| iOS arm64      | 🧪 Experimental (Metal; native binary required, not bundled)      |
| macOS          | planned                                                          |
| Linux          | planned                                                          |

Windows x64 (Editor and Standalone Player) is fully supported out of the box. Android arm64 support was added in
the C# layer in v0.4.0 and iOS arm64 (Metal) support in v0.5.0, but both require you to build and supply the
native binary yourself - see [Android](#android) / [iOS](#ios) below. Other platforms are on the roadmap but not
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

## Android

New in v0.4.0: the C# layer (`MapLibreMapHandle`, `MapLibreMapView`) now supports Android arm64 via an EGL shared
context (`EglSharedContext`), selected at compile time alongside the existing Windows/WGL path
(`WglSharedContext`).

**The native binary (`libmaplibre-native-c.so`) is not bundled** - maplibre-native-ffi does not currently publish
prebuilt Android artifacts, so you must build it yourself from
[maplibre-native-ffi](https://github.com/maplibre/maplibre-native-ffi) and place it under
`Packages/com.fukuda-a-hu.maplibre-unity/Runtime/Plugins/Android/arm64-v8a/`. See that folder's
[README.md](Packages/com.fukuda-a-hu.maplibre-unity/Runtime/Plugins/Android/arm64-v8a/README.md) for build steps
and the required Unity PluginImporter settings. Until that binary is supplied, `MapLibreMapView` will fail to
initialize on Android at runtime.

## iOS

New in v0.5.0: the C# layer (`MapLibreMapHandle`, `MapLibreMapView`) now supports iOS arm64 via Metal
(`MetalDeviceContext`), the only render backend iOS's maplibre-native-ffi build supports (there is no
OpenGL/EGL path on iOS). On iOS, P/Invoke targets `__Internal` since plugins are statically linked into the
player binary rather than loaded as a separate dynamic library.

**The native static library (`libmaplibre-native-c.a`) is not bundled** - maplibre-native-ffi does not currently
publish prebuilt iOS artifacts, so you must build it yourself on macOS from
[maplibre-native-ffi](https://github.com/maplibre/maplibre-native-ffi) (the `ios-arm64-metal` variant) and place
it under `Packages/com.fukuda-a-hu.maplibre-unity/Runtime/Plugins/iOS/`. See that folder's
[README.md](Packages/com.fukuda-a-hu.maplibre-unity/Runtime/Plugins/iOS/README.md) for build steps and the
required Unity PluginImporter settings. A small bundled Objective-C bridge
(`MapLibreUnityMetalBridge.mm`, compiled into the Xcode project automatically) obtains the system default
`MTLDevice`, since Unity does not expose one to C#. Until the static library is supplied, `MapLibreMapView` will
fail to initialize on iOS at runtime.

## License

This repository's own source code is licensed under the [MIT License](LICENSE).

The bundled native binary (`maplibre-native-c.dll`) is distributed under the BSD 2-Clause License. See
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for the full text and provenance.

## 日本語サマリ

MapLibre Native を Unity から使うための実験的なプラグインです(Windows x64 対応、Android arm64 は実験的対応)。
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

v0.4.0 では、C# 側に Android(arm64)対応を追加しました。Windows の WGL 共有コンテキスト
(`WglSharedContext`)と対になる EGL 共有コンテキスト(`EglSharedContext`)を新設し、`MapLibreMapHandle` が
ビルドターゲットに応じてコンパイル時に WGL/EGL を切り替えます。ただし **ネイティブバイナリ
(`libmaplibre-native-c.so`)は同梱していません**。maplibre-native-ffi は現時点で Android 向けのビルド済み
バイナリを配布していないため、利用者自身が maplibre-native-ffi をビルドし、
`Packages/com.fukuda-a-hu.maplibre-unity/Runtime/Plugins/Android/arm64-v8a/` に配置する必要があります
(手順は同ディレクトリの README.md を参照)。バイナリを配置するまでは、Android 上で `MapLibreMapView` は
実行時に初期化に失敗します。Windows 側の挙動・API に変更はありません。

v0.5.0 では、C# 側に iOS(Metal)対応を追加しました。iOS の maplibre-native-ffi ビルドは Metal のみ対応
(OpenGL/EGL パスなし)のため、`MetalDeviceContext` が `MTLCreateSystemDefaultDevice` を呼び出す小さな
Objective-C ブリッジ(`MapLibreUnityMetalBridge.mm`)を同梱し、`MapLibreMapHandle` はビルドターゲットが
iOS の場合に Metal セッション所有テクスチャ(`mln_metal_owned_texture_attach`)へアタッチします。iOS では
プラグインが静的リンクされるため、P/Invoke の対象も `__Internal` に切り替わります。こちらも **ネイティブ
静的ライブラリ(`libmaplibre-native-c.a`)は同梱していません**。maplibre-native-ffi の ios-arm64-metal
variant を利用者自身が macOS 上でビルドし、`Packages/com.fukuda-a-hu.maplibre-unity/Runtime/Plugins/iOS/`
に配置する必要があります(手順は同ディレクトリの README.md を参照)。既存の Windows / Android の挙動・API
に変更はありません。
