#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Build.Editor
{
    public class VersioningSettings : ScriptableObject
    {
        [SerializeField] private string mainBranchName = "main";
        public string MainBranchName => mainBranchName;

        [SerializeField] private int numberOffset = 0;
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

        [SerializeField] private Versioning.NumberType commitCountingStyle = Versioning.NumberType.BothMinorAndPatch;
        public Versioning.NumberType CommitCountingStyle => commitCountingStyle;

        public static string MainBranchNameProperty => nameof(mainBranchName);
        public static string NumberOffsetProperty => nameof(numberOffset);
        public static string IncludeBranchCountProperty => nameof(includeBranchCount);
        public static string IncludeStatusChangesProperty => nameof(includeStatusChanges);
        public static string MinorTagsProperty => nameof(minorTags);
        public static string PatchTagsProperty => nameof(patchTags);
        public static string CommitCountingStyleProperty => nameof(commitCountingStyle);
        
        private const string DefaultSettingsFolder = "Assets/Editor/Settings";
        private static readonly string DefaultSettingsPath = $"{DefaultSettingsFolder}/{nameof(VersioningSettings)}.asset";

        public void GetMinorAndThenPatch(string[] lines, out int minor, out int patch)
        {
            minor = lines.Count(line => MinorRegex.IsMatch(line));
            lines = lines.TakeWhile(line => !MinorRegex.IsMatch(line)).ToArray();
            patch = lines.Count(line => PatchRegex.IsMatch(line));
        }
        
        internal static VersioningSettings GetOrCreate()
        {
            var foundPaths = AssetDatabase.FindAssets($"t:{nameof(VersioningSettings)}")
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if (!foundPaths.Any())
            {
                var newSettings = CreateInstance<VersioningSettings>();
                Versioning.MakeFolderValid(DefaultSettingsFolder);
                AssetDatabase.CreateAsset(newSettings, DefaultSettingsPath);
                AssetDatabase.SaveAssets();
            }

            var settings = AssetDatabase.LoadAssetAtPath<VersioningSettings>(foundPaths.First()); 
            return settings;
        }

        private static string[] GetSettingsPaths() => AssetDatabase.FindAssets($"t:{nameof(VersioningSettings)}")
        
        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreate());
        }
    }

    internal static class BuildSettingsUIElementsRegister
    {
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/Build package", SettingsScope.Project)
            {
                label = "Build Tool & Versioning",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = VersioningSettings.GetSerializedSettings();
                    
                    // rootElement is a VisualElement. If you add any children to it, the OnGUI function
                    // isn't called because the SettingsProvider uses the UIElements drawing framework.
                    const string styleSheetDirectory = "Assets/Editor";
                    var styleSheetPath = $"{styleSheetDirectory}/settings_ui.uss";

                    if (!Directory.Exists(styleSheetDirectory))
                    {
                        Directory.CreateDirectory(styleSheetDirectory);
                    }
                    if (!File.Exists(styleSheetPath))
                    {
                        var stream = File.CreateText(styleSheetPath);
                        stream.Write("VisualElement {}");
                        stream.Close();
                        AssetDatabase.Refresh();
                    }
                    
                    var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
                    if (styleSheet) rootElement.styleSheets.Add(styleSheet);
                    var title = new Label { text = "Git" };
                    title.AddToClassList("title");
                    rootElement.Add(title);
                    
                    var properties = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                    properties.AddToClassList("property-list");
                    rootElement.Add(properties);
                    
                    properties.Add(GenPropertyField(VersioningSettings.MainBranchNameProperty,
                        nameof(VersioningSettings.MainBranchName), settings));
                    properties.Add(GenPropertyField(VersioningSettings.NumberOffsetProperty, 
                        "Build Number Offset", settings));
                    properties.Add(GenPropertyField(VersioningSettings.IncludeBranchCountProperty,
                        "Include Branch Count in version data", settings));
                    properties.Add(GenPropertyField(VersioningSettings.IncludeStatusChangesProperty,
                        "Include Number of Changes in version data", settings));
                    properties.Add(GenPropertyField(VersioningSettings.MinorTagsProperty, "Minor Tags", settings));
                    properties.Add(GenPropertyField(VersioningSettings.PatchTagsProperty, "Patch Tags", settings));
                    properties.Add(GenPropertyField(VersioningSettings.CommitCountingStyleProperty,
                        "Commit Counting Style", settings));

                    var data = VersionData.GetFromResources();
                    title = new Label { text = "Version Data" };
                    title.AddToClassList("title");
                    var preview = new VisualElement { style = { flexDirection = FlexDirection.Column } };
                    preview.AddToClassList("property-list");
                    preview.Add(new Label(data.Version));
                    preview.Add(new Label(data.Number.ToString()));
                    preview.Add(new Label(data.Hash));
                    var box = new Box();
                    box.Add(title);
                    box.Add(preview);
                    rootElement.Add(box);
                }
            };

            return provider;
        }

        private static PropertyField GenPropertyField(string property, string label, SerializedObject bindObject)
        {
            var propertyField = new PropertyField(bindObject.FindProperty(property), label);
            propertyField.Bind(bindObject);
            return propertyField;
        }
    }
}
#endif
