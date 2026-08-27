using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace HatilloVerde.Editor
{
    public static class HatilloXrSetup
    {
        const string OpenXrLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";

        [MenuItem("Hatillo Verde/Setup Quest XR")]
        public static void Run()
        {
            try
            {
                ConfigureAndroidPlayer();
                EnableOpenXr(BuildTargetGroup.Android);
                EnableOpenXr(BuildTargetGroup.Standalone);
                EnableOpenXrFeatures(BuildTargetGroup.Android);
                EnableOpenXrFeatures(BuildTargetGroup.Standalone);
                AssetDatabase.SaveAssets();
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
                Debug.Log("[HatilloXR] OpenXR + Android Quest settings applied. Switch Platform → Android and Build.");
            }
            catch (Exception e)
            {
                Debug.LogError("[HatilloXR] setup failed: " + e);
                throw;
            }
        }

        [MenuItem("Hatillo Verde/Build Quest APK")]
        public static void BuildApk()
        {
            ConfigureAndroidPlayer();
            EnableOpenXr(BuildTargetGroup.Android);
            EnableOpenXrFeatures(BuildTargetGroup.Android);
            AssetDatabase.SaveAssets();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes in Build Settings.");

            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds"));
            Directory.CreateDirectory(dir);
            var apk = Path.Combine(dir, "HatilloVerde-quest.apk");

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = apk,
                target = BuildTarget.Android,
                options = BuildOptions.CompressWithLz4HC
            };

            Debug.Log("[HatilloXR] building " + apk + " scenes=" + string.Join(",", scenes));
            var report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;
            Debug.Log("[HatilloXR] build " + summary.result + " size=" + summary.totalSize + " path=" + apk);
            if (summary.result != BuildResult.Succeeded)
                throw new Exception("Android build failed: " + summary.result);
        }

        static void ConfigureAndroidPlayer()
        {
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.spatiallab.hatilloverde");
            PlayerSettings.companyName = "SpatialLab";
            PlayerSettings.productName = "Hatillo Verde";
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.renderOutsideSafeArea = true;
            PlayerSettings.SetMobileMTRendering(BuildTargetGroup.Android, true);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[]
            {
                GraphicsDeviceType.Vulkan,
                GraphicsDeviceType.OpenGLES3
            });
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Low);
            PlayerSettings.Android.forceInternetPermission = true;
        }

        static void EnableOpenXr(BuildTargetGroup group)
        {
            var perTarget = GetOrCreatePerTarget();
            var general = perTarget.SettingsForBuildTarget(group);
            if (general == null)
            {
                general = ScriptableObject.CreateInstance<XRGeneralSettings>();
                general.name = group + " Settings";
                perTarget.SetSettingsForBuildTarget(group, general);
                AssetDatabase.AddObjectToAsset(general, perTarget);
            }

            var manager = general.AssignedSettings;
            if (manager == null)
            {
                manager = ScriptableObject.CreateInstance<XRManagerSettings>();
                manager.name = group + " Loaders";
                general.AssignedSettings = manager;
                AssetDatabase.AddObjectToAsset(manager, perTarget);
            }

            general.InitManagerOnStart = true;
            XRPackageMetadataStore.AssignLoader(manager, OpenXrLoader, group);
            EditorUtility.SetDirty(perTarget);
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(manager);
        }

        static XRGeneralSettingsPerBuildTarget GetOrCreatePerTarget()
        {
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget perTarget);
            if (perTarget != null)
                return perTarget;

            EnsureFolder("Assets/XR");
            EnsureFolder("Assets/XR/Settings");
            perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget, "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            return perTarget;
        }

        static void EnableOpenXrFeatures(BuildTargetGroup group)
        {
            UnityEditor.XR.OpenXR.Features.FeatureHelpers.RefreshFeatures(group);

            var enableIds = new[]
            {
                "com.unity.openxr.feature.metaquest",
                "com.unity.openxr.feature.compositionlayers",
                "com.unity.openxr.feature.input.oculustouch",
                "com.unity.openxr.feature.input.metaquestpro",
                "com.unity.openxr.feature.input.metaquestplus",
            };
            var disableIds = new[]
            {
                "com.unity.openxr.feature.oculusquest",
            };

            foreach (var id in enableIds)
            {
                var feature = UnityEditor.XR.OpenXR.Features.FeatureHelpers.GetFeatureWithIdForBuildTarget(group, id);
                if (feature == null)
                    continue;
                feature.enabled = true;
                Debug.Log("[HatilloXR] enabled " + id + " for " + group);
            }

            foreach (var id in disableIds)
            {
                var feature = UnityEditor.XR.OpenXR.Features.FeatureHelpers.GetFeatureWithIdForBuildTarget(group, id);
                if (feature == null)
                    continue;
                feature.enabled = false;
                Debug.Log("[HatilloXR] disabled " + id + " for " + group);
            }

            UnityEditor.XR.OpenXR.Features.OpenXRFeatureSetManager.SetFeaturesFromEnabledFeatureSets(group);
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (settings != null)
                EditorUtility.SetDirty(settings);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            var leaf = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
