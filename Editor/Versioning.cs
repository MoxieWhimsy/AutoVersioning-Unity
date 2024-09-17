#if UNITY_EDITOR
using System.Linq;
using RedBlueGames;
using UnityEditor;
using UnityEngine;

namespace Build.Editor
{
    public static class Versioning
    {
        private static string MainBranchName => VersioningSettings.GetOrCreate().MainBranchName;

        private static VersioningSettings s_cache;

        private static VersioningSettings Settings
        {
            get
            {
                if (!s_cache) s_cache = VersioningSettings.GetOrCreate();
                return s_cache;

            }
        }
        
        public static string ShortBundle
            => Settings.GetBuildVersion(out var version, out _) ? version : "unknown";
        
        public static string BranchCount => $"{CommitsOnMainToBranch}.{CommitsSinceMain}";


        public static string CommitBranchingFromMain
            => Git.Run($@"merge-base {MainBranchName} HEAD");


        /// <summary>
        /// Returns the number of commits on main up to the commit where this branch connects to main.
        /// </summary>
        public static int CommitsOnMainToBranch
            => int.TryParse(Git.Run($@"rev-list --count {CommitBranchingFromMain}"), out var number) ? number : 0;

        /// <summary>
        /// Return the number of commits since the current branch forks from the main branch. 
        /// </summary>
        public static int CommitsSinceMain
            => int.TryParse(Git.Run($@"rev-list --count {MainBranchName}..HEAD"), out var commitsSinceMain)
                ? commitsSinceMain : 0;
        
        /// <summary>
        /// True if we've checked out the main branch
        /// </summary>
        public static bool IsOnMain => Git.Branch == MainBranchName;

        /// <returns>true as long as output VersionData is usable, not null</returns>
        internal static bool GetOrCreateVersionData(out VersionData versionData)
        {
            if (VersionData.TryGetFromResources(out versionData)) return true;
            if (!ConfirmCreateNewVersionDataPopup()) return false;
            SaveNewVersionDataFilePopup(out var path);
            path = TrimPathToAssetsFolder(path);
            if (path.Length == 0)
                return false;

            CreateNewVersionData(out versionData, path);
            versionData = VersionData.GetFromResources();

            return versionData;
        }
		
        private static bool ConfirmCreateNewVersionDataPopup()
            => EditorUtility.DisplayDialog("Version Data not found",
                "Create new version data asset?", "Create New", "Cancel");

        private static void SaveNewVersionDataFilePopup(out string path)
        {
            const string defaultVersionDataDirectory = "Assets/Resources/Config";
            MakeFolderValid(defaultVersionDataDirectory);
            path = EditorUtility.SaveFilePanel("Save Version Data Asset", defaultVersionDataDirectory, "Version", "asset");
        }

        private static void CreateNewVersionData(out VersionData versionData, string path)
        {
            versionData = ScriptableObject.CreateInstance<VersionData>();

            SplitPath(path, out path, out var filename);

            AssetDatabase.CreateAsset(versionData, $"{path}/{filename}");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        internal static void MakeFolderValid(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            SplitPath(path, out var parent, out var folder);

            MakeFolderValid(parent);
			
            AssetDatabase.CreateFolder(parent, folder);
            AssetDatabase.Refresh();
        }

        private static void SplitPath(string path, out string folder, out string tail)
        {
            var last = path.LastIndexOf('/');
            folder = path.Substring(0, last);
            tail = path.Substring(last + 1);
        }

        private static string TrimPathToAssetsFolder(string path)
        {
            var index = path.IndexOf("Assets", System.StringComparison.Ordinal);
            return index < 0 ? string.Empty : path.Substring(index);
        }


        /// <summary>
        /// Generates a build number suitable for platforms that require an incrementing integer build number.
        /// Such as Android.
        /// </summary>
        public static bool GetBuildNumber(out int number)
        {
            var lines = Settings.GetCommitLogLines();

            number = Settings.GetBuildNumber(lines);
            return lines.Any();
        }


        [MenuItem("Build/Version Data/Clear Most (not debug)")]
        public static void ClearMostVersionData()
        {
            if (!VersionData.TryGetFromResources(out var versionData)) return;
            versionData.ClearAlmostAll();
            EditorUtility.SetDirty(versionData);
            AssetDatabase.SaveAssets();
        }
        
        [MenuItem("Build/Version Data/Update Version Data (fills most fields)", priority = 0)]
        public static void UpdateVersionDataFromMenu()
        {
            UpdateVersionData();
        }
        
        public static VersionData UpdateVersionData()
        {
            var data = VersionData.GetFromResources();
            if (Settings.GetBuildVersion(out var memo, out var hash))
            {
                data.Version = memo;
                data.Hash = hash;
            }

            if (GetBuildNumber(out var number))
                data.Number = number;

            if (Settings.IncludeBranchCount && Settings.IncludeChanges)
                data.Bonus = $"Branch: {BranchCount}+{GetGitChanges()}";
            else if (Settings.IncludeChanges)
                data.Bonus = $"Changes: {GetGitChanges()}";
            else if (Settings.IncludeBranchCount)
                data.Bonus = $"Branch: {BranchCount}";
            else
                data.Bonus = string.Empty;

            EditorUtility.SetDirty(data);
			AssetDatabase.SaveAssets();
			Debug.Log( $"Info, Debug: Filled version data: {data.Version} Build: {data.Number}");
            return data;
        }

        private static int GetGitChanges()
        {
            var status = Git.Status;
            return status.Trim().Length > 0 ? status.Split('\n').Length : 0;
        }

        public static string GetVersionString(bool includeHash = false, bool includeBuildNumber = false, bool commitStatus = false)
        {
            Settings.GetBuildVersion(out var version, out var hash);
            if (includeHash) version += $" {hash}";
            if (includeBuildNumber && GetBuildNumber(out var number)) version += $" ({number})";
            if (commitStatus && !includeBuildNumber) version += $"+{CommitsSinceMain}";
            if (commitStatus)
            {
                var gitStatus = Git.Status;
                version += gitStatus.Length > 0 ? $"&{gitStatus.Split('\n').Length}" : "&0";
            }

            return version;
        }
        
        private static void HandleMobileBuildNumber(BuildTarget target)
        {
            if (target != BuildTarget.Android && target != BuildTarget.iOS) return;
            ABuildTool.UpdateMobileBuildNumbers(target);
        }

        public static void PrebuildVersionUpdate(BuildTarget target)
        {
            if (!GetOrCreateVersionData(out var versionData)) return;

            versionData = UpdateVersionData();
            HandleMobileBuildNumber(target);

            PlayerSettings.bundleVersion = versionData ? versionData.Version : ShortBundle;
        }
    }
}
#endif
