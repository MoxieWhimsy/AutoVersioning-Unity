#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using TMPro;
using UnityEngine;

namespace Build.UX
{
	[ExecuteAlways]
	public class VersionDisplay : MonoBehaviour
	{
		[SerializeField] private bool includeLabels = false;
		[SerializeField] private bool includeProductName = true;
		[SerializeField] private bool includeCommitHash = false;
		[SerializeField] private bool includeBuildNumber = true;
		[SerializeField] private bool includeBonusCounts = true;
		[SerializeField] private bool includeDebug = true;

		[SerializeField] private VersionData versionData;

		private TMP_Text versionText;

		private void Awake()
		{
			TryGetComponent(out versionText);
		}

		private void Reset()
		{
			TryGetComponent(out versionText);
			GrabVersionDataFromResources();
			DisplayVersion();
		}

		private void OnEnable()
		{
			if (!versionData) GrabVersionDataFromResources();
			DisplayVersion();
		}

		#if ODIN_INSPECTOR
		[Button]
		#endif
		private void DisplayVersion()
		{
			if (!versionText) return;

			var version = includeProductName ? $"{Application.productName} " : string.Empty;

			if (!versionData)
			{
				versionText.text = version;
				return;
			}

			version += versionData.Version;
			
			if (versionData && includeBuildNumber)
				version += includeLabels ? $"\nBuild: {versionData.Number}" : $" {versionData.Number}";
			if (versionData && includeDebug && !string.IsNullOrWhiteSpace(versionData.Debug))
				version += includeLabels ? $"\nDebug: {versionData.Debug}" : $" {versionData.Debug}"; 
			if (versionData && includeCommitHash)
				version += includeLabels ? $"\nHash: {versionData.Hash}" : $" {versionData.Hash}";
			if (versionData && includeBonusCounts) version += $"&{versionData.Bonus}";
			versionText.text = version;
		}
		
		#if ODIN_INSPECTOR
		[Button]
		#endif		
		[ContextMenu(nameof(GrabVersionDataFromResources))]
		private void GrabVersionDataFromResources()
		{
			versionData = VersionData.GetFromResources();
		}
	}
}