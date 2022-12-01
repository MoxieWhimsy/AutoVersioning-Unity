#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using RedBlueGames;
using UnityEngine;

namespace Build.Editor
{
    public partial class VersioningSettings
    {
        private const string VersionTagRegex = @"""*v[0-9]*""";

        private int CountBothMinorAndPatch(IEnumerable<string> lines)
            => lines.Count(line => UnionRegex.IsMatch(line));

        /// <summary>
        /// Returns the number of commits on main up to the commit where this branch connects to main.
        /// </summary>
        private int CountCommitsOnMainToBranch()
        {
            var commitId = GetWhichCommitBranchesFromMain();
            return int.TryParse(Git.Run($@"rev-list --count {commitId}"), out var number) ? number : 0;
        }

        /// <summary>
        /// Return the number of commits since the current branch forks from the main branch. 
        /// </summary>
        private int CountCommitsSinceMain()
            => int.Parse(Git.Run($@"rev-list --count {MainBranchName}..HEAD"));

        private int CountMinorThenPatch(string[] lines)
        {
            var patch = GetMinorAndThenPatch(lines, out var minor);
            return MaxPatchesPerMinor * minor + patch;
        }

        private int CountMainAndBranch()
        {
            var mainToBranchPt = CountCommitsOnMainToBranch();
            var headFromBranchPt = CountCommitsSinceMain();
            return BranchCommitLimit * mainToBranchPt + headFromBranchPt;
        }

        internal int GetBuildNumber(string[] lines)
        {
            return commitCountingStyle switch
            {
                Versioning.NumberType.BothMinorAndPatch => CountBothMinorAndPatch(lines),
                Versioning.NumberType.MinorThenPatch => CountMinorThenPatch(lines),
                Versioning.NumberType.MainAndBranch => CountMainAndBranch(),
                _ => 0
            } + numberOffset;
        }
        
        internal string[] GetCommitLogLines() => (VersionControlSystem switch
        {
            VersionControl.Git => Git.CommitLog,
            VersionControl.PlasticScm => PlasticProcess.CommitLog,
            _ => string.Empty,
        }).Split('\n').Select(line => line.Trim()).ToArray();
        
        
        /// <summary>
        /// Retrieves the most recent version tag on current branch
        /// </summary>
        public bool GetDescription(out string description)
        {
            try
            {
                description = VersionControlSystem switch
                {
                    VersionControl.Git => Git.Run($@"describe --tags --long --match {VersionTagRegex}"),
                    _ => string.Empty,
                };
                return true;
            }
            catch (GitException exception)
            {
                Debug.LogError($"{nameof(Versioning)}: exit code = {exception.ExitCode}\n{exception.Message}");
                description = string.Empty;
                return false;
            }
        }

        internal int GetMinorAndThenPatch(string[] lines, out int minor)
        {
            minor = lines.Count(line => MinorRegex.IsMatch(line));
            lines = lines.TakeWhile(line => !MinorRegex.IsMatch(line)).ToArray();
            return lines.Count(line => PatchRegex.IsMatch(line));
        }

        private string GetWhichCommitBranchesFromMain()
            => Git.Run($@"merge-base {MainBranchName} HEAD");
    }
}
#endif