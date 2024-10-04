#if UNITY_EDITOR
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Build.Editor
{
    public partial class VersioningSettings : ScriptableObject
    {
        [SerializeField] private VersionControl _versionControlSystem = VersionControl.Git;
        public VersionControl VersionControlSystem => _versionControlSystem;
        
        [SerializeField] private NumberType _commitCountingStyle = NumberType.BothMinorAndPatch;
        [SerializeField] private NumberType _bundleVersionStyle = NumberType.MinorThenPatch;


        [SerializeField] private string mainBranchName = "main";
        public string MainBranchName => mainBranchName;

        [SerializeField] private int numberOffset;
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
        [SerializeField] private string[] buildTags = { "" };
        [SerializeField] private string _plasticVersionTag = @"^version-tag:";

        public Regex MinorRegex => new(string.Join('|', minorTags.Select(tag => $"^{tag}")));
        public Regex PatchRegex => new(string.Join('|', patchTags.Select(tag => $"^{tag}")));

        public Regex MinorPatchRegex => new(string.Join('|', minorTags.Union(patchTags).Select(tag => $"^{tag}")));

        public Regex MinorPatchBuildRegex
            => new(string.Join('|', minorTags.Union(patchTags).Union(buildTags).Select(tag => $"^{tag}")));
        public Regex PatchBuildRegex
            => new(string.Join('|', patchTags.Union(buildTags).Select(tag => $"^{tag}")));

        public Regex PlasticVersionTagRegex => new(_plasticVersionTag);

        
        public enum NumberType
        {
            BothMinorAndPatch = 0,
            MinorThenPatch = 1,
            MainAndBranch = 2,
            PatchAndBuild = 3,
            MinorThenPatchAndBuild = 4,
            MinorAndPatchAndBuild = 5,
        }

        public enum VersionControl
        {
            Git,
            PlasticScm,
        }

        public static string BundleVersionStyleProperty => nameof(_bundleVersionStyle);
        public static string CommitCountingStyleProperty => nameof(_commitCountingStyle);
        public static string IncludeBranchCountProperty => nameof(includeBranchCount);
        public static string IncludeStatusChangesProperty => nameof(includeStatusChanges);
        public static string MainBranchNameProperty => nameof(mainBranchName);
        public static string MinorTagsProperty => nameof(minorTags);
        public static string NumberOffsetProperty => nameof(numberOffset);
        public static string PatchTagsProperty => nameof(patchTags);
        public static string BuildTagsProperty => nameof(buildTags);
        public static string VersionControlSystemProperty => nameof(_versionControlSystem);
        
        private const string DefaultSettingsFolder = "Assets/Editor/Settings";
        private static readonly string DefaultSettingsPath = $"{DefaultSettingsFolder}/{nameof(VersioningSettings)}.asset";


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
