#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Build.Editor
{
	public class PrebuildVersionUpdate : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;
		public void OnPreprocessBuild(BuildReport report)
		{
			Versioning.PrebuildVersionUpdate(report.summary.platform);
		}
	}
}
#endif
