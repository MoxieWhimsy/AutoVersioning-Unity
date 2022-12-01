#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Build.Editor
{
    internal static class VersioningSettingsRegister
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

                    properties.Add(GenPropertyField(VersioningSettings.VersionControlSystemProperty,
                        "Version Control System", settings));
                    properties.Add(GenPropertyField(VersioningSettings.MainBranchNameProperty,
                        nameof(VersioningSettings.MainBranchName), settings));
                    properties.Add(GenPropertyField(VersioningSettings.NumberOffsetProperty, 
                        "Build Number Offset", settings));
                    properties.Add(GenPropertyField(VersioningSettings.CommitCountingStyleProperty,
                        "Commit Counting Style", settings));
                    properties.Add(GenPropertyField(VersioningSettings.IncludeBranchCountProperty,
                        "Include Branch Count in version data", settings));
                    properties.Add(GenPropertyField(VersioningSettings.IncludeStatusChangesProperty,
                        "Include Number of Changes in version data", settings));
                    properties.Add(GenPropertyField(VersioningSettings.MinorTagsProperty, "Minor Tags", settings));
                    properties.Add(GenPropertyField(VersioningSettings.PatchTagsProperty, "Patch Tags", settings));

                    
                    AddVersionDataPreview(rootElement);
                }
            };

            return provider;
        }

        private static void AddVersionDataPreview(VisualElement rootElement)
        {
            var data = VersionData.GetFromResources();
                    
            if (!data) return;
            var title = new Label { text = "Version Data" };

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

        private static PropertyField GenPropertyField(string property, string label, SerializedObject bindObject)
        {
            var propertyField = new PropertyField(bindObject.FindProperty(property), label);
            propertyField.Bind(bindObject);
            return propertyField;
        }
    }
}
#endif