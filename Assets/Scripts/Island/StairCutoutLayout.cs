// Runtime layout for the carved-stair cutout overlay.
// Loaded from StreamingAssets/stair_cutout_layout.json (hot-reload friendly).
using System;
using System.IO;
using UnityEngine;

namespace SkyHarvest.Island
{
    [Serializable]
    public class StairCutoutLayoutData
    {
        public float offsetX    = 0f;
        public float offsetY    = -0.5f;
        public float heightWorld = 1.44f;
        public float scaleX     = 1f;
        public float scaleY     = 1f;
    }

    public static class StairCutoutLayout
    {
        private const string FileName = "stair_cutout_layout.json";

        public static StairCutoutLayoutData Current { get; private set; } = new();

        public static string FilePath =>
            Path.Combine(Application.streamingAssetsPath, FileName);

        public static void Load()
        {
            Current = new StairCutoutLayoutData();
            string path = FilePath;
            if (!File.Exists(path)) return;

            try
            {
                var json = File.ReadAllText(path);
                var loaded = JsonUtility.FromJson<StairCutoutLayoutData>(json);
                if (loaded != null) Current = loaded;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StairCutoutLayout] Load failed: {e.Message}");
            }
        }

        public static void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Current, true);
                string path = FilePath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, json);

                if (TryFindProjectStreamingAssetsDir() is string projectDir)
                    File.WriteAllText(Path.Combine(projectDir, FileName), json);

                Debug.Log($"[StairCutoutLayout] Saved → {path}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[StairCutoutLayout] Save failed: {e.Message}");
            }
        }

        /// <summary>Walk up from cwd to find Assets/StreamingAssets (built player → project).</summary>
        private static string? TryFindProjectStreamingAssetsDir()
        {
            var dir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 6 && dir != null; i++)
            {
                var candidate = Path.Combine(dir, "Assets", "StreamingAssets");
                if (Directory.Exists(candidate)) return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }

        public static void ApplyTransform(Transform cutout)
        {
            var d = Current;
            cutout.localPosition = new Vector3(d.offsetX, d.offsetY, 0f);
            cutout.localScale    = new Vector3(d.scaleX, d.scaleY, 1f);
        }

        public static void ReadFromTransform(Transform cutout)
        {
            var p = cutout.localPosition;
            var s = cutout.localScale;
            Current.offsetX = p.x;
            Current.offsetY = p.y;
            Current.scaleX  = s.x;
            Current.scaleY  = s.y;
        }
    }
}
