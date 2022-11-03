using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Build
{
	[CreateAssetMenu(menuName = "Create VersionData", fileName = "Version", order = 0)]
	public class VersionData : ScriptableObject
	{
		[SerializeField] private string version;

		public string Version
		{
			get => version;
			#if UNITY_EDITOR
			set => version = value;
			#endif
		}

		[FormerlySerializedAs("build")] [SerializeField] private string branchCount;
		public string BranchCount
		{
			get => branchCount;
			#if UNITY_EDITOR
			set => branchCount = value;
			#endif
		}

		[SerializeField] private int number;

		public int Number
		{
			get => number;
			#if UNITY_EDITOR
			set => number = value;
			#endif
		}

		[SerializeField] private int changes;

		public int Changes
		{
			get => changes;
			#if UNITY_EDITOR
			set => changes = value;
			#endif
		}
		
		[SerializeField] private string debug;

		public string Debug
		{
			get => debug;
			#if UNITY_EDITOR
			set => debug = value;
			#endif
		}

		[SerializeField] private string hash;

		public string Hash
		{
			get => hash;
			#if UNITY_EDITOR
			set => hash = value;
			#endif
		}

		#if UNITY_EDITOR
		public void ClearAlmostAll()
		{
			version = string.Empty;
			branchCount = string.Empty;
			number = 0;
			changes = 0;
			hash = string.Empty;
		}
		
		public void ClearDebug()
		{
			debug = string.Empty;
		}

		public void SetDebug(string memo = null)
		{
			debug = string.IsNullOrWhiteSpace(debug) && string.IsNullOrWhiteSpace(memo) ? "true" : memo;
		}
		#endif
		
		public static VersionData GetFromResources()
		{
			var allVersionData = Resources.LoadAll<VersionData>("");
			if (allVersionData.Length < 1)
				UnityEngine.Debug.LogError("Could not find VersionData asset. Please create one.");
			if (allVersionData.Length <= 1) 
				return allVersionData.Length == 1 ? allVersionData[0] : null;
			var memo = string.Join(", ", allVersionData.Select(data => data.name));
			UnityEngine.Debug.LogWarning($"Debug: Found multiple VersionData assets: {memo}. Selected the first.");
			return allVersionData[0];
		}

		public static bool TryGetFromResources(out VersionData versionData)
		{
			versionData = GetFromResources();
			return versionData;
		}
	}
}
