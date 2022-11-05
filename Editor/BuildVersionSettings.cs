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
    public class BuildVersionSettings : ScriptableObject
    {
        [SerializeField] private string mainBranchName = "main";
        public string MainBranchName => mainBranchName;

        [SerializeField] private int numberOffset = 0;
        public int NumberOffset => numberOffset;
        [SerializeField] private int branchCommitLimit = 100;
        [SerializeField] private int maxPatchesPerMinor = 20;
        public int BranchCommitLimit => branchCommitLimit;
        public int MaxPatchesPerMinor => maxPatchesPerMinor;
        
        [SerializeField] private string[] minorTags = {"feat"};
        [SerializeField] private string[] patchTags = { "fix", "asset", "adjust" };

        public Regex MinorRegex => new(string.Join('|', minorTags.Select(tag => @$"^{tag}")));
        public Regex PatchRegex => new(string.Join('|', patchTags.Select(tag => @$"^{tag}")));
        public Regex UnionRegex => new(string.Join('|', minorTags.Union(patchTags).Select(tag => @$"^{tag}")));

        [SerializeField] private BuildVersion.NumberType commitCountingStyle = BuildVersion.NumberType.BothMinorAndPatch;
        public BuildVersion.NumberType CommitCountingStyle => commitCountingStyle;

        public static string MainBranchNameProperty => nameof(mainBranchName);
        public static string NumberOffsetProperty => nameof(numberOffset);
        public static string MinorTagsProperty => nameof(minorTags);
        public static string PatchTagsProperty => nameof(patchTags);
        public static string CommitCountingStyleProperty => nameof(commitCountingStyle);


        public void GetMinorAndThenPatch(string[] lines, out int minor, out int patch)
        {
            minor = lines.Count(line => MinorRegex.IsMatch(line));
            lines = lines.TakeWhile(line => !MinorRegex.IsMatch(line)).ToArray();
            patch = lines.Count(line => PatchRegex.IsMatch(line));
        }
        
        internal static BuildVersionSettings GetOrCreate()
        {
            var foundPaths = AssetDatabase.FindAssets($"t:{nameof(BuildVersionSettings)}")
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            if (!foundPaths.Any())
            {
                var newSettings = CreateInstance<BuildVersionSettings>();
                AssetDatabase.CreateAsset(newSettings, DefaultSettingsPath);
                AssetDatabase.SaveAssets();
            }

            var settings = AssetDatabase.LoadAssetAtPath<BuildVersionSettings>(foundPaths.First()); 
            return settings;
        }

        private static readonly string DefaultSettingsPath = $"Assets/{nameof(BuildVersionSettings)}.asset";
        
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
                label = "Build & Version Code",
                // activateHandler is called when the user clicks on the Settings item in the Settings window.
                activateHandler = (searchContext, rootElement) =>
                {
                    var settings = BuildVersionSettings.GetSerializedSettings();
                    
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
                    
                    properties.Add(GenPropertyField(BuildVersionSettings.MainBranchNameProperty,
                        nameof(BuildVersionSettings.MainBranchName), settings));
                    properties.Add(GenPropertyField(BuildVersionSettings.NumberOffsetProperty, 
                        "Build Number Offset", settings));
                    properties.Add(GenPropertyField(BuildVersionSettings.MinorTagsProperty, "Minor Tags", settings));
                    properties.Add(GenPropertyField(BuildVersionSettings.PatchTagsProperty, "Patch Tags", settings));
                    properties.Add(GenPropertyField(BuildVersionSettings.CommitCountingStyleProperty,
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
