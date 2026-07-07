using System;
using System.IO;
using System.Reflection;
using MapLibre.Unity.Plateau;
using MapLibre.Unity.Terrain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MapLibreDemo.Editor
{
    /// <summary>
    /// Headless play-mode verification for the "3D Terrain + PLATEAU" sample. Intended to be invoked via
    /// `-executeMethod MapLibreDemo.Editor.PlateauTerrain3DSceneTest.Run` (no `-quit`, since this test calls
    /// <see cref="EditorApplication.Exit"/> itself once it reaches a verdict).
    ///
    /// Enters Play Mode with domain reload disabled - if domain reload were left enabled, all static state used to
    /// track the running <see cref="EditorApplication.update"/> polling callback would be wiped out the moment Play
    /// Mode starts, and the callback would never fire again, hanging the test forever.
    ///
    /// Note: this deliberately does NOT reference MapLibre.Unity.Samples.PlateauTerrain3DBootstrap directly. That
    /// type lives under Samples~ (hidden from the Editor's asset database outside of a Package Manager sample
    /// import), so a normal checkout of this repository has no compiled assembly containing it. A direct reference
    /// would make this file fail to compile in a normal checkout. Instead, the bootstrap component and its
    /// TerrainLayer/BuildingsLayer fields are found via reflection by type/field name.
    ///
    /// Because the sample's scene and script are equally hidden under Samples~, verification is a TWO-STEP batch
    /// protocol: first invoke <see cref="PrepareSample"/> (with `-quit`) to copy the sample into Assets the same
    /// way the Package Manager's Samples "Import" button would, then invoke <see cref="Run"/> in a second Editor
    /// session. The steps cannot share one session because the copied script triggers a recompile/domain reload.
    /// </summary>
    public static class PlateauTerrain3DSceneTest
    {
        private const string SampleSourceDir = "Packages/com.fukuda-a-hu.maplibre-unity/Samples~/PlateauTerrain3D";
        private const string SampleImportDir = "Assets/PlateauTerrain3DSampleImport";
        private const double TimeoutSeconds = 300.0;
        private const float ExpectedMinTerrainReliefMeters = 3f;
        private const float MaxHorizontalDistanceMeters = 3000f;
        private const float MinBuildingHeightMeters = 50f;
        private const float MaxBuildingHeightMeters = 400f;

        private static double _deadlineRealtimeSinceStartup;
        private static bool _running;

        /// <summary>
        /// Copies the sample's files (including .meta files, preserving GUIDs so the scene's script reference stays
        /// valid) from the package's hidden Samples~ folder into <see cref="SampleImportDir"/>, making the scene and
        /// bootstrap script visible to the AssetDatabase - the same effect as the Package Manager's Samples
        /// "Import" button. Invoke via `-executeMethod ... PlateauTerrain3DSceneTest.PrepareSample -quit` BEFORE
        /// <see cref="Run"/>; see the class-level remarks for why this must be a separate Editor session.
        /// </summary>
        public static void PrepareSample()
        {
            try
            {
                string sourceDir = Path.GetFullPath(SampleSourceDir);
                if (!Directory.Exists(sourceDir))
                {
                    Debug.LogError($"MAPLIBRE_P3D_PREPARE_FAIL: sample source not found at {sourceDir}.");
                    EditorApplication.Exit(1);
                    return;
                }

                Directory.CreateDirectory(SampleImportDir);
                foreach (string sourceFile in Directory.GetFiles(sourceDir))
                {
                    string destFile = Path.Combine(SampleImportDir, Path.GetFileName(sourceFile));
                    File.Copy(sourceFile, destFile, overwrite: true);
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Debug.Log("MAPLIBRE_P3D_PREPARE_OK");
                // The `-quit` command-line flag shuts the Editor down once the refresh/recompile completes.
            }
            catch (Exception e)
            {
                Debug.LogError($"MAPLIBRE_P3D_PREPARE_FAIL: {e}");
                EditorApplication.Exit(1);
            }
        }

        public static void Run()
        {
            try
            {
                string scenePath = FindSceneAssetPath();
                if (scenePath == null)
                {
                    Fail("could not find a scene asset named 'PlateauTerrain3D' via AssetDatabase. Run PrepareSample (as a separate -executeMethod invocation with -quit) first to import the sample from the hidden Samples~ folder.");
                    return;
                }

                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                EditorSettings.enterPlayModeOptionsEnabled = true;
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

                _deadlineRealtimeSinceStartup = EditorApplication.timeSinceStartup + TimeoutSeconds;
                _running = true;

                EditorApplication.update += OnUpdate;
                EditorApplication.EnterPlaymode();
            }
            catch (Exception e)
            {
                Fail($"unhandled exception during setup: {e}");
            }
        }

        private static string FindSceneAssetPath()
        {
            // Prefer the copy made by PrepareSample; fall back to a global search (e.g. a manually imported
            // Package Manager sample) if it isn't present.
            string[] guids = Directory.Exists(SampleImportDir)
                ? AssetDatabase.FindAssets("PlateauTerrain3D t:Scene", new[] { SampleImportDir })
                : Array.Empty<string>();

            if (guids.Length == 0)
            {
                guids = AssetDatabase.FindAssets("PlateauTerrain3D t:Scene");
            }

            if (guids.Length == 0)
            {
                return null;
            }

            return AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        /// <summary>
        /// Finds the running scene's bootstrap component and its TerrainLayer/BuildingsLayer fields via reflection,
        /// by type/field name rather than a compile-time reference - see the class-level remarks for why.
        /// </summary>
        private static bool TryFindBootstrapLayers(out GsiTerrainLayer terrain, out PlateauTilesetLayer buildings, out string error)
        {
            terrain = null;
            buildings = null;
            error = null;

#pragma warning disable CS0618 // FindObjectsOfType is obsolete in newer Unity versions but is used here for Unity 2021.3 API compatibility.
            MonoBehaviour[] allBehaviours = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
#pragma warning restore CS0618

            MonoBehaviour bootstrap = null;
            foreach (MonoBehaviour behaviour in allBehaviours)
            {
                if (behaviour.GetType().FullName == "MapLibre.Unity.Samples.PlateauTerrain3DBootstrap")
                {
                    bootstrap = behaviour;
                    break;
                }
            }

            if (bootstrap == null)
            {
                error = "no PlateauTerrain3DBootstrap found in the running scene.";
                return false;
            }

            Type bootstrapType = bootstrap.GetType();
            terrain = bootstrapType.GetProperty("TerrainLayer", BindingFlags.Instance | BindingFlags.Public)?.GetValue(bootstrap) as GsiTerrainLayer;
            buildings = bootstrapType.GetProperty("BuildingsLayer", BindingFlags.Instance | BindingFlags.Public)?.GetValue(bootstrap) as PlateauTilesetLayer;

            if (terrain == null || buildings == null)
            {
                error = "PlateauTerrain3DBootstrap did not create its terrain/buildings layers.";
                return false;
            }

            return true;
        }

        private static void OnUpdate()
        {
            if (!_running)
            {
                return;
            }

            try
            {
                if (!EditorApplication.isPlaying)
                {
                    // Still transitioning into Play Mode - wait for the next update tick.
                    return;
                }

                if (EditorApplication.timeSinceStartup > _deadlineRealtimeSinceStartup)
                {
                    Fail("timed out waiting for terrain/building load and verification conditions.");
                    return;
                }

                if (!TryFindBootstrapLayers(out GsiTerrainLayer terrain, out PlateauTilesetLayer buildings, out string findError))
                {
                    Fail(findError);
                    return;
                }

                if (!terrain.IsReady || !buildings.IsReady)
                {
                    // Still loading - wait for the next update tick.
                    return;
                }

                float relief = terrain.SampleMaxY - terrain.SampleMinY;
                if (relief <= ExpectedMinTerrainReliefMeters)
                {
                    Fail($"terrain relief too small: SampleMinY={terrain.SampleMinY}, SampleMaxY={terrain.SampleMaxY}, relief={relief} (expected > {ExpectedMinTerrainReliefMeters}m). This likely means the DEM tiles failed to load or decode.");
                    return;
                }

                if (buildings.LoadedTileCount < 1)
                {
                    Fail($"no PLATEAU building tiles loaded (LoadedTileCount={buildings.LoadedTileCount}).");
                    return;
                }

                Bounds bounds = buildings.CombinedBounds;
                float horizontalDistance = new Vector2(bounds.center.x, bounds.center.z).magnitude;
                float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.z);
                float maxHorizontalReach = horizontalDistance + maxExtent;

                if (maxHorizontalReach > MaxHorizontalDistanceMeters)
                {
                    Fail($"PLATEAU buildings bounds too far from origin: center={bounds.center}, extents={bounds.extents}, maxHorizontalReach={maxHorizontalReach}m (expected <= {MaxHorizontalDistanceMeters}m). This likely means the tile rotation/translation math (RTC center -> ENU) has a sign error.");
                    return;
                }

                // Note: CombinedBounds.size.y is an axis-aligned bounding box over potentially many buildings
                // spread across a wide (up to ~1km) tile footprint, so it conflates horizontal spread with actual
                // building height once the tile is rotated to align with true "up" at its geodetic location. Use
                // MaxLocalBuildingHeightMeters instead, which buckets vertices into a horizontal grid to isolate a
                // single building's vertical extent from the tile's horizontal footprint.
                float buildingHeight = buildings.MaxLocalBuildingHeightMeters;
                if (buildingHeight < MinBuildingHeightMeters || buildingHeight > MaxBuildingHeightMeters)
                {
                    try
                    {
                        SaveScreenshot();
                    }
                    catch (Exception)
                    {
                        // Ignore - this is a best-effort debug aid only.
                    }

                    Fail($"PLATEAU buildings max local height out of expected range: MaxLocalBuildingHeightMeters={buildingHeight}m (expected {MinBuildingHeightMeters}-{MaxBuildingHeightMeters}m; CombinedBounds.size={bounds.size} for reference). This likely means the tile rotation math (Unity/glTF/ECEF axis conversion) has a sign error, collapsing or exaggerating vertical extent.");
                    return;
                }

                Succeed(terrain, buildings, bounds);
            }
            catch (Exception e)
            {
                Fail($"unhandled exception during verification: {e}");
            }
        }

        private static void Succeed(GsiTerrainLayer terrain, PlateauTilesetLayer buildings, Bounds bounds)
        {
            try
            {
                SaveScreenshot();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"PlateauTerrain3DSceneTest: failed to save screenshot: {e}");
            }

            Debug.Log(
                $"PlateauTerrain3DSceneTest: terrain relief={terrain.SampleMaxY - terrain.SampleMinY}m, " +
                $"buildings LoadedTileCount={buildings.LoadedTileCount}, CombinedBounds center={bounds.center}, size={bounds.size}, " +
                $"MaxLocalBuildingHeightMeters={buildings.MaxLocalBuildingHeightMeters}m");
            Debug.Log("MAPLIBRE_P3D_OK");

            Stop();
            EditorApplication.Exit(0);
        }

        private static void Fail(string reason)
        {
            Debug.LogError($"MAPLIBRE_P3D_FAIL: {reason}");
            Stop();
            EditorApplication.Exit(1);
        }

        private static void Stop()
        {
            _running = false;
            EditorApplication.update -= OnUpdate;
        }

        private static void SaveScreenshot()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("PlateauTerrain3DSceneTest: no Camera.main found - skipping screenshot.");
                return;
            }

            const int width = 1280;
            const int height = 720;

            var renderTexture = new RenderTexture(width, height, 24);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture.active = renderTexture;
                var screenshot = new Texture2D(width, height, TextureFormat.RGB24, mipChain: false, linear: false);
                screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenshot.Apply(updateMipmaps: false);

                byte[] png = ImageConversion.EncodeToPNG(screenshot);
                UnityEngine.Object.DestroyImmediate(screenshot);

                string logsDir = @"C:\Users\hakat\maplibre-unity\Logs";
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }

                string outputPath = Path.Combine(logsDir, "plateau3d.png");
                File.WriteAllBytes(outputPath, png);
                Debug.Log($"PlateauTerrain3DSceneTest: saved screenshot to {outputPath}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(renderTexture);
            }
        }
    }
}
