using System;
using System.IO;
using System.Threading;
using MapLibre.Unity;
using UnityEditor;
using UnityEngine;

namespace MapLibreDemo.Editor
{
    /// <summary>
    /// Headless smoke test intended to be invoked via `-executeMethod MapLibreDemo.Editor.MapLibreSmokeTest.Run`.
    /// Creates a MapLibreMapHandle directly (no MonoBehaviour/scene needed), steps the render loop until a frame
    /// containing non-uniform map content is produced (or a timeout elapses), saves a PNG for manual inspection,
    /// and exits the Editor with a status code reflecting success/failure.
    /// </summary>
    public static class MapLibreSmokeTest
    {
        private const int Width = 512;
        private const int Height = 512;
        private const string StyleUrl = "https://demotiles.maplibre.org/style.json";
        private const double TimeoutSeconds = 120.0;
        private const int StepSleepMilliseconds = 16;

        // A frame counts as "real map content" only when it is not a uniform fill: the style's background color
        // renders long before any tile data arrives, so a non-zero-pixel check alone passes on background-only
        // frames. Requiring a minimum ratio of pixels that differ from the frame's first pixel guarantees that
        // some vector data (coastlines/land polygons) has actually been rendered.
        private const double DistinctPixelRatioThreshold = 0.02;

        public static void Run()
        {
            MapLibreMapHandle handle = null;
            try
            {
                handle = MapLibreMapHandle.Create(Width, Height, 1.0, StyleUrl);
                // World-region view centered near Japan: at this zoom the demotiles style always shows both ocean
                // and land/coastline, so a successful render is guaranteed to contain more than one color.
                handle.SetCamera(35.681236, 139.767125, 2.5, 0, 0);

                byte[] buffer = null;
                DateTime deadline = DateTime.UtcNow.AddSeconds(TimeoutSeconds);

                while (DateTime.UtcNow < deadline)
                {
                    handle.Step();

                    if (handle.TryReadPixels(ref buffer, out int width, out int height, out int stride))
                    {
                        int distinctCount = CountPixelsDifferingFromFirst(buffer, width, height, stride);
                        long totalPixels = (long)width * height;
                        double ratio = totalPixels > 0 ? (double)distinctCount / totalPixels : 0.0;

                        if (ratio >= DistinctPixelRatioThreshold)
                        {
                            SavePng(buffer, width, height, stride);
                            Debug.Log("MAPLIBRE_SMOKE_OK");
                            EditorApplication.Exit(0);
                            return;
                        }
                    }

                    Thread.Sleep(StepSleepMilliseconds);
                }

                Debug.LogError("MAPLIBRE_SMOKE_FAIL: timed out waiting for a rendered frame containing non-uniform map content.");
                EditorApplication.Exit(1);
            }
            catch (Exception e)
            {
                Debug.LogError($"MAPLIBRE_SMOKE_FAIL: unhandled exception: {e}");
                EditorApplication.Exit(1);
            }
            finally
            {
                handle?.Dispose();
            }
        }

        private static int CountPixelsDifferingFromFirst(byte[] buffer, int width, int height, int stride)
        {
            byte r0 = buffer[0];
            byte g0 = buffer[1];
            byte b0 = buffer[2];

            int count = 0;
            for (int y = 0; y < height; y++)
            {
                int rowStart = y * stride;
                for (int x = 0; x < width; x++)
                {
                    int pixelStart = rowStart + x * 4;
                    if (buffer[pixelStart] != r0 || buffer[pixelStart + 1] != g0 || buffer[pixelStart + 2] != b0)
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static void SavePng(byte[] buffer, int width, int height, int stride)
        {
            int rowBytes = width * 4;
            byte[] packed = buffer;
            if (stride != rowBytes)
            {
                packed = new byte[rowBytes * height];
                for (int row = 0; row < height; row++)
                {
                    Buffer.BlockCopy(buffer, row * stride, packed, row * rowBytes, rowBytes);
                }
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: false);
            // Texture2D has no (byte[], offset, length) overload of LoadRawTextureData - only the
            // whole-array overload, an IntPtr overload, and a generic NativeArray<T> overload. This
            // runs once per smoke-test invocation, so an exact-length array is fine here.
            texture.LoadRawTextureData(packed);
            texture.Apply(updateMipmaps: false);

            byte[] png = ImageConversion.EncodeToPNG(texture);
            UnityEngine.Object.DestroyImmediate(texture);

            string logsDir = @"C:\Users\hakat\maplibre-unity\Logs";
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            string outputPath = Path.Combine(logsDir, "smoke.png");
            File.WriteAllBytes(outputPath, png);
            Debug.Log($"MapLibreSmokeTest: saved output to {outputPath}");
        }
    }
}
