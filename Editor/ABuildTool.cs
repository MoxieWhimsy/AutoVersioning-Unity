#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Build.Editor
{
    public abstract class ABuildTool : MonoBehaviour
    {
        protected static string[] Scenes => EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        protected static string HomePath => System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
        
        protected delegate void PreBuildDelegate(BuildPlayerOptions buildPlayerOptions);

        protected delegate void PostBuildDelegate(BuildSummary buildSummary);

        protected static event PreBuildDelegate OnBeforeBuild;
        protected static event PostBuildDelegate OnBuildSucceeded;

        protected static BuildSummary MostRecentSummary { get; private set; }

        [System.Obsolete("Must specify build targets as parameters")]
        public static void UpdateMobileBuildNumbers()
        {
            Debug.LogError(
                $"missing build targets. {nameof(UpdateMobileBuildNumbers)}() calls without params is Obsolete. Please fix this. ");
        }
        public static void UpdateMobileBuildNumbers(params BuildTarget[] buildTargets)
        {
            Versioning.GetBuildNumber(out var number);
            foreach (var target in buildTargets)
            {
                switch (target)
                {
                    case BuildTarget.Android:
                        PlayerSettings.Android.bundleVersionCode = number;
                        break;
                    case BuildTarget.iOS:
                        PlayerSettings.iOS.buildNumber = $"{number}";
                        break;
                    case BuildTarget.tvOS:
                        PlayerSettings.tvOS.buildNumber = $"{number}";
                        break;
                }
            }
        }

        protected static async Task ArchiveIosBuild(string buildPath, XcodebuildProcess.ProcessOutput callback)
            => await XcodeBuild.RunAsync("archive -scheme Unity-iPhone -sdk iphoneos -allowProvisioningUpdates",
                buildPath, callback);

        protected static void AddAppUsesExemptEncryption(string buildPath)
        {
            var path = @$"{buildPath}/Info.plist";
            var stream = new FileStream(path, FileMode.Open);
            var reader = new StreamReader(stream);
            var lines = new Queue<string>(reader.ReadToEnd().Split('\n'));
            reader.Close();
            stream.Close();
            var result = new Queue<string>();
            if (lines.Count <= 0)
            {
                Debug.LogError($"failed to load {path}");
                return;
            }

            string line;
            do
            {
                line = lines.Dequeue();
                result.Enqueue(line);
            } while (!line.Trim().StartsWith("<dict>"));

            result.Enqueue(@"<key>ITSAppUsesNonExemptEncryption</key>");
            result.Enqueue(@"<false/>");
            while (lines.Count > 0)
            {
                result.Enqueue(lines.Dequeue());
            }

            stream = new FileStream(path, FileMode.Truncate);
            var writer = new StreamWriter(stream);
            while (result.Count > 0)
            {
                writer.WriteLine(result.Dequeue());
            }

            writer.Close();
            stream.Close();
        }

        protected static string Version()
        {
            var versionData = VersionData.GetFromResources();
            return versionData ? versionData.Version : Versioning.ShortBundle;
        }

        protected static async Task BuildWithOptions(BuildPlayerOptions buildPlayerOptions)
        {
            PlayerSettings.bundleVersion = Version();
            await BeforeBuild(buildPlayerOptions);
            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;
            Debug.Log($"Build Platform: {summary.platform}");
            MostRecentSummary = summary;

            switch (summary.result)
            {
                case BuildResult.Succeeded:
                    Debug.Log($"Build succeeded: {summary.totalSize} bytes");
					
                    OnBuildSucceeded?.Invoke(summary);

                    break;
                case BuildResult.Failed:
                    Debug.Log($"Build failed on {summary.platform} " +
                              $"errors: {summary.totalErrors} warnings: {summary.totalWarnings}");
                    break;
                case BuildResult.Cancelled:
                    Debug.Log($"Build cancelled. Total time was {summary.totalTime}");
                    break;
                default:
                    Debug.Log($"result: {summary.result} on {summary.platform}");
                    break;
            }
        }

        private static Task BeforeBuild(BuildPlayerOptions buildPlayerOptions)
        {
            OnBeforeBuild?.Invoke(buildPlayerOptions);
            return Task.CompletedTask;
        }

        protected static BuildPlayerOptions GetBuildPlayerOptions(string buildPath, string filename,
            BuildTarget buildTarget, bool addExtension = true)
        {
            var extension = addExtension ? GetExtensionByBuildTarget(buildTarget) : string.Empty;
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = $"{buildPath}/{filename}{extension}",
                target = buildTarget,
                options = BuildOptions.ShowBuiltPlayer
            };
            return buildPlayerOptions;
        }

        private static string GetExtensionByBuildTarget(BuildTarget target) =>
            target switch
            {
                BuildTarget.Android => EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk",
                BuildTarget.StandaloneWindows => ".exe",
                BuildTarget.StandaloneWindows64 => ".exe",
                _ => ""
            };

        [MenuItem("Build/Version Data/Set Debug: true")]
        public static void SetDebugVersionDataTrue()
        {
            GetDebugVersionData(out string memo);
            if (string.IsNullOrWhiteSpace(memo))
                SetDebugVersionData("true");
        }

        protected static void SetDebugVersionData(string memo)
        {
            if (!VersionData.TryGetFromResources(out var versionData)) return;
            versionData.SetDebug(memo);
            EditorUtility.SetDirty(versionData);
            AssetDatabase.SaveAssets();
        }

        protected static void GetDebugVersionData(out string memo)
            => memo = VersionData.TryGetFromResources(out var versionData) ? versionData.Debug : null;

        [MenuItem("Build/Version Data/Clear Debug")]
        public static void ClearDebugVersionData()
        {
            if (!VersionData.TryGetFromResources(out var versionData)) return;
            versionData.ClearDebug();
            EditorUtility.SetDirty(versionData);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif