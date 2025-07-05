using TMPro;
using UnityEngine;

namespace Build.UX
{
    public class VersionBadge : MonoBehaviour
    {
        [SerializeField] private TMP_Text _version;
        [SerializeField] private TMP_Text _build;
        [SerializeField] private TMP_Text _hash;
        [SerializeField] private TMP_Text _bonus;
        
        [SerializeField] private VersionData _versionData;
        
        private void Reset()
        {
            GrabVersionDataFromResources();
            DisplayVersion();
        }

        private void OnEnable()
        {
            if (!_versionData) GrabVersionDataFromResources();
            DisplayVersion();
        }
        private void DisplayVersion()
        {
            if (!_versionData) return;
            if (_version) _version.SetText($"Version: {_versionData.Version}");
            if (_build) _build.SetText($"Build: {_versionData.Number}");
            if (_hash) _hash.SetText($"Hash: {_versionData.Hash}");
            if (_bonus) _bonus.SetText(_versionData.TryGetBonusStrings(out var bonus) ? bonus : "No notes!");
        }
        
        [ContextMenu(nameof(GrabVersionDataFromResources))]
        private void GrabVersionDataFromResources()
        {
            _versionData = VersionData.GetFromResources();
        }
    }
}