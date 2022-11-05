#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using RedBlueGames;
using UnityEditor;
using UnityEngine;

namespace Build.Editor
{
    public static class Versioning
    {
        private static string MainBranchName => VersioningSettings.GetOrCreate().MainBranchName;
        private const string VersionTagRegex = @"""*v[0-9]*""";

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
            => GetBuildVersion(out var version, out _) ? version : "unknown";
        
        public static string BranchCount => $"{CommitsOnMainToBranch}.{CommitsSinceMain}";


        public static string CommitBranchingFromMain
            => Git.Run($@"merge-base {MainBranchName} HEAD");

        /// <summary>
        /// Returns the number of commits on the main branch
        /// </summary>
        public static int CommitsOnMain 
            => int.TryParse(Git.Run($@"rev-list --count {MainBranchName}"), out var commitsOnMain)
                ? commitsOnMain : 0;


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
        
        public enum NumberType
        {
            BothMinorAndPatch,
            MinorThenPatch,
            MainAndBranch,
        }


        private static int CountCommits(string[] lines)
        {
            return Settings.CommitCountingStyle switch
            {
                NumberType.BothMinorAndPatch => CountBothMinorAndPatch(lines),
                NumberType.MinorThenPatch => CountMinorThenPatch(lines),
                NumberType.MainAndBranch => CountMainAndBranch(),
                _ => 0
            };
        }

        private static int CountBothMinorAndPatch(IEnumerable<string> lines) 
            => lines.Count(line => Settings.UnionRegex.IsMatch(line));

        private static int CountMinorThenPatch(string[] lines)
        {
            var minor = lines.Count(line => Settings.MinorRegex.IsMatch(line));
            lines = lines.TakeWhile(line => !Settings.MinorRegex.IsMatch(line)).ToArray();
            var patch = lines.Count(line => Settings.PatchRegex.IsMatch(line));
            return Settings.MaxPatchesPerMinor * minor + patch;
        }

        private static int CountMainAndBranch()
        {
            var mainToBranchPt = CommitsOnMainToBranch;
            var headFromBranchPt = CommitsSinceMain;
            return Settings.BranchCommitLimit * mainToBranchPt + headFromBranchPt;
        }

        
        /// <summary>
        /// Generates a build number suitable for platforms that require an incrementing integer build number.
        /// Such as Android.
        /// </summary>
        public static bool GetBuildNumber(out int number)
        {
            var lines = GetCommitLogLines();

            number = CountCommits(lines);
            number += Settings.NumberOffset;
            return lines.Any();
        }

        /// <summary>
        /// Retrieves the build version from git based on the most recent matching tag and
        /// commit history. This returns the version as: {major.minor.build} where 'build'
        /// represents the nth commit after the tagged commit.
        /// Note: The initial 'v' and the commit hash code are removed.
        /// </summary>
        public static bool GetBuildVersion(out string version, out string hash)
        {
            if (!GetDescription(out var description))
            {
                version = "unknown";
                hash = string.Empty;
                return false;
            }

            var hashDash = description.LastIndexOf('-');
            hash = description[(hashDash + 1)..];
            description = description[..hashDash];
            Debug.Log($"d:{description} h:{hash}");
            var commitsDash = description.LastIndexOf('-');
            var commits = int.Parse(description[(commitsDash + 1)..]);
            description = description[..commitsDash];
            Debug.Log($"d:{description} p:{commits} h:{hash}");
            var tag = description;

            var afterV = description.LastIndexOf('v') + 1;
            var majorAndMinor = description[afterV..];
            var minorDot = majorAndMinor.LastIndexOf('.');
            var major = minorDot > 0 ? majorAndMinor[..minorDot] : majorAndMinor;

            var lines = GetCommitLogLinesSinceTag(tag);

            int minor, patch;
            if (minorDot > 0)
            {
                minor = int.Parse(majorAndMinor[(minorDot + 1)..]);
                patch = CountBothMinorAndPatch(lines);
            }
            else
                Settings.GetMinorAndThenPatch(lines, out minor, out patch);

            version = $"{major}.{minor}.{patch}";
            return true;
        }
        
        
        /// <summary>
        /// Retrieves the most recent version tag on current branch
        /// </summary>
        public static bool GetDescription(out string description)
        {
            try
            {
                description = Git.Run($@"describe --tags --long --match {VersionTagRegex}");
                return true;
            }
            catch (GitException exception)
            {
                Debug.LogError($"{nameof(Versioning)}: exit code = {exception.ExitCode}\n{exception.Message}");
                description = string.Empty;
                return false;
            }
        }


        [MenuItem("Build/Version Data/Clear Most (not debug)")]
        public static void ClearMostVersionData()
        {
            if (!VersionData.TryGetFromResources(out var versionData)) return;
            versionData.ClearAlmostAll();
            EditorUtility.SetDirty(versionData);
            AssetDatabase.SaveAssets();
        }
        
        [MenuItem("Build/Version Data/Update Version Data (fills most fields)")]
        public static void UpdateVersionDataFromMenu()
        {
            UpdateVersionData();
        }
        
        public static VersionData UpdateVersionData()
        {
            var data = VersionData.GetFromResources();
            if (GetBuildVersion(out var memo, out var hash))
            {
                data.Version = memo;
                data.Hash = hash;
                data.BranchCount = BranchCount;
            }

            if (GetBuildNumber(out var number))
                data.Number = number;

            data.Changes = GetGitChanges();
            
            EditorUtility.SetDirty(data);
			AssetDatabase.SaveAssets();
			Debug.Log( $"Info, Debug: Filled version data: {data.Version} Build: {data.BranchCount}");
            return data;
        }

        private static int GetGitChanges()
        {
            var status = Git.Status;
            return status.Trim().Length > 0 ? status.Split('\n').Length : 0;
        }

        private static string[] GetCommitLogLinesSinceTag(string tag)
            => GetCommitLogSinceTag(tag).Split('\n').Select(line => line.Trim()).ToArray();

        private static string[] GetCommitLogLines()
            => Git.CommitLog.Split('\n').Select(line => line.Trim()).ToArray();
        
        private static string GetCommitLogSinceTag(string tag) => Git.Run($@"log {tag}..head");

        public static string GetVersionString(bool includeHash = false, bool includeBuildNumber = false, bool commitStatus = false)
        {
            GetBuildVersion(out var version, out var hash);
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
    }
}
#endif