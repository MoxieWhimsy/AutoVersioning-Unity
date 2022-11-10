using UnityEngine;

namespace Build.Editor
{
    public class FillVersionDataOnce : MonoBehaviour
    {
		[SerializeField] private VersionData versionData;

        private void Reset()
        {
            versionData = VersionData.GetFromResources();
        }

        private void Start()
        {
            #if UNITY_EDITOR
            Versioning.UpdateVersionData();
            #endif
            Destroy(this);
        }
    }
}
