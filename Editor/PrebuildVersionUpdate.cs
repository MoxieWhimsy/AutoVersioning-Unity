#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Build.Editor
{
	public class PrebuildVersionUpdate : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;
		public void OnPreprocessBuild(BuildReport report)
		{
			if (!Versioning.GetOrCreateVersionData(out var versionData)) return;

			versionData = Versioning.UpdateVersionData();
			HandleMobileBuildNumber(report.summary.platform);

			PlayerSettings.bundleVersion = versionData ? versionData.Version : Versioning.ShortBundle;
		}

		private static void HandleMobileBuildNumber(BuildTarget target)
		{
			if (target != BuildTarget.Android && target != BuildTarget.iOS) return;
			ABuildTool.UpdateMobileBuildNumbers();
		}
	}
}
#endif
