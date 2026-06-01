using System.Linq;
using UnityEditor;
using UnityEngine;

// Bulk-applies aggressive WebGL texture import settings to the current Project
// window selection (folders are expanded to every Texture2D inside them).
// Purpose: shrink WebGL build size + runtime VRAM for iOS Safari, which kills
// tabs that exceed its memory ceiling. Run via the Tools menu after selecting
// the heavy asset-pack folders.
public static class WebGLTextureOptimizer
{
    const string WebGLPlatform = "WebGL";

    // Heavy asset-pack folders targeted by the headless batch run.
    static readonly string[] BatchFolders =
    {
        "Assets/BK/PureNature_Jungle",
        "Assets/URP_WasteOvergrowth_SA",
    };

    [MenuItem("Tools/WebGL/Optimize Selected Textures (Aggressive)")]
    static void Optimize()
    {
        // Resolve selection (folders -> contained textures, single assets -> self).
        var roots = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath);
        Run(roots);
    }

    // Batch-mode entry point: `Unity -batchmode -quit -executeMethod
    // WebGLTextureOptimizer.OptimizeAll`. Selection APIs don't work headless,
    // so this operates on the hardcoded BatchFolders above.
    static void OptimizeAll() => Run(BatchFolders);

    static void Run(System.Collections.Generic.IEnumerable<string> roots)
    {
        var paths = roots
            .SelectMany(p => AssetDatabase.IsValidFolder(p)
                ? AssetDatabase.FindAssets("t:Texture2D", new[] { p })
                    .Select(AssetDatabase.GUIDToAssetPath)
                : new[] { p })
            .Distinct()
            .ToArray();

        int count = 0;
        try
        {
            for (int i = 0; i < paths.Length; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar(
                        "WebGL Texture Optimizer",
                        $"{i + 1}/{paths.Length}  {paths[i]}",
                        (float)i / Mathf.Max(1, paths.Length)))
                    break;

                if (AssetImporter.GetAtPath(paths[i]) is not TextureImporter ti) continue;

                // Normal maps and *_n suffixed sets tolerate heavier downscale.
                bool isNormal = ti.textureType == TextureImporterType.NormalMap
                                || paths[i].EndsWith("_n.png");
                int maxSize = isNormal ? 512 : 1024;

                // Engine-wide flags: stream mips instead of resident-loading all,
                // and crunch the base import for a smaller on-disk/download size.
                ti.streamingMipmaps = true;
                ti.crunchedCompression = true;
                ti.compressionQuality = 50;

                // Per-platform WebGL override. ASTC is the mobile-friendly format;
                // switch to Automatic if a specific texture rejects ASTC.
                var s = ti.GetPlatformTextureSettings(WebGLPlatform);
                s.overridden = true;
                s.maxTextureSize = maxSize;
                s.format = TextureImporterFormat.ASTC_6x6;
                s.textureCompression = TextureImporterCompression.Compressed;
                s.crunchedCompression = true;
                ti.SetPlatformTextureSettings(s);

                ti.SaveAndReimport();
                count++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"[WebGLTextureOptimizer] Optimized {count}/{paths.Length} selected textures.");
    }
}
