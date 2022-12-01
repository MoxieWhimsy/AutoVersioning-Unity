#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RedBlueGames;
using UnityEditor;
using UnityEngine;

namespace Build.Editor
{
    public class VersioningSettings : ScriptableObject
    {
        [SerializeField] private Versioning.VersionControl _versionControlSystem = Versioning.VersionControl.Git;
        public Versioning.VersionControl VersionControlSystem => _versionControlSystem;
        
        [SerializeField] private Versioning.NumberType commitCountingStyle = Versioning.NumberType.BothMinorAndPatch;
        public Versioning.NumberType CommitCountingStyle => commitCountingStyle;

        
        [SerializeField] private string mainBranchName = "main";
        public string MainBranchName => mainBranchName;

        [SerializeField] private int numberOffset;
        public int NumberOffset => numberOffset;
        [SerializeField] private bool includeBranchCount;
        [SerializeField] private bool includeStatusChanges;
        public bool IncludeBranchCount => includeBranchCount;
        public bool IncludeChanges => includeStatusChanges;
        
        [SerializeField] private int branchCommitLimit = 100;
        [SerializeField] private int maxPatchesPerMinor = 20;
        public int BranchCommitLimit => branchCommitLimit;
        public int MaxPatchesPerMinor => maxPatchesPerMinor;

        [SerializeField] private string[] minorTags = { "feat" };
        [SerializeField] private string[] patchTags = { "fix", "asset", "adjust" };

        public Regex MinorRegex => new(string.Join('|', minorTags.Select(tag => @$"^{tag}")));
        public Regex PatchRegex => new(string.Join('|', patchTags.Select(tag => @$"^{tag}")));
        public Regex UnionRegex => new(string.Join('|', minorTags.Union(patchTags).Select(tag => @$"^{tag}")));

        public static string CommitCountingStyleProperty => nameof(commitCountingStyle);
        public static string IncludeBranchCountProperty => nameof(includeBranchCount);
        public static string IncludeStatusChangesProperty => nameof(includeStatusChanges);
        public static string MainBranchNameProperty => nameof(mainBranchName);
        public static string MinorTagsProperty => nameof(minorTags);
        public static string NumberOffsetProperty => nameof(numberOffset);
        public static string PatchTagsProperty => nameof(patchTags);
        public static string VersionControlSystemProperty => nameof(_versionControlSystem);
        
        private const string DefaultSettingsFolder = "Assets/Editor/Settings";
        private static readonly string DefaultSettingsPath = $"{DefaultSettingsFolder}/{nameof(VersioningSettings)}.asset";

        
        private int CountBothMinorAndPatch(IEnumerable<string> lines) 
            => lines.Count(line => UnionRegex.IsMatch(line));
        
        /// <summary>
        /// Returns the number of commits on main up to the commit where this branch connects to main.
        /// </summary>
        public int CountCommitsOnMainToBranch()
        {
            var commitId = GetWhichCommitBranchesFromMain();
            return int.TryParse(Git.Run($@"rev-list --count {commitId}"), out var number) ? number : 0;
        }

        /// <summary>
        /// Return the number of commits since the current branch forks from the main branch. 
        /// </summary>
        private int CountCommitsSinceMain()
            => int.Parse(Git.Run($@"rev-list --count {MainBranchName}..HEAD"));
        
        private int CountMinorThenPatch(string[] lines)
        {
            var patch = GetMinorAndThenPatch(lines, out var minor);
            return MaxPatchesPerMinor * minor + patch;
        }
        
        private int CountMainAndBranch()
        {
            var mainToBranchPt = CountCommitsOnMainToBranch();
            var headFromBranchPt = CountCommitsSinceMain();
            return BranchCommitLimit * mainToBranchPt + headFromBranchPt;
        }
        
        internal int GetBuildNumber(string[] lines)
        {
            return commitCountingStyle switch
            {
                Versioning.NumberType.BothMinorAndPatch => CountBothMinorAndPatch(lines),
                Versioning.NumberType.MinorThenPatch => CountMinorThenPatch(lines),
                Versioning.NumberType.MainAndBranch => CountMainAndBranch(),
                _ => 0
            } + numberOffset;
        }
        public int GetMinorAndThenPatch(string[] lines, out int minor)
        {
            minor = lines.Count(line => MinorRegex.IsMatch(line));
            lines = lines.TakeWhile(line => !MinorRegex.IsMatch(line)).ToArray();
            return lines.Count(line => PatchRegex.IsMatch(line));
        }
        
        public string GetWhichCommitBranchesFromMain()
            => Git.Run($@"merge-base {MainBranchName} HEAD");
        
        internal static VersioningSettings GetOrCreate()
        {
            var foundPaths = GetSettingsPaths();
            if (!foundPaths.Any())
            {
                var newSettings = CreateInstance<VersioningSettings>();
                Versioning.MakeFolderValid(DefaultSettingsFolder);
                AssetDatabase.CreateAsset(newSettings, DefaultSettingsPath);
                AssetDatabase.SaveAssets();
            }

            foundPaths = GetSettingsPaths();

            var settings = AssetDatabase.LoadAssetAtPath<VersioningSettings>(foundPaths.First()); 
            return settings;
        }

        private static string[] GetSettingsPaths() => AssetDatabase.FindAssets($"t:{nameof(VersioningSettings)}")
            .Select(AssetDatabase.GUIDToAssetPath).ToArray();

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreate());
        }
    }
}
#endif
