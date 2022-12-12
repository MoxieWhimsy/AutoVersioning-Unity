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

        private int CountMainAndBranch(string[] lines)
        {
            switch (_versionControlSystem)
            {
                case VersionControl.Git:
                    return BranchCommitLimit * CountCommitsOnMainToBranch() + CountCommitsSinceMain();
                case VersionControl.PlasticScm:
                {
                    var branchLines = lines.Where(line => line.StartsWith("Branch:")).ToArray();
                    var numberOnMain = branchLines.Count(line => line.EndsWith(mainBranchName));
                    return BranchCommitLimit * numberOnMain + branchLines.Length - numberOnMain;
                }
                default:
                    return 0;
            }
        }

        internal int GetBuildNumber(string[] lines) => _commitCountingStyle switch
        {
            NumberType.BothMinorAndPatch => CountBothMinorAndPatch(lines),
            NumberType.MinorThenPatch => CountMinorThenPatch(lines),
            NumberType.MainAndBranch => CountMainAndBranch(lines),
            _ => 0
        } + numberOffset;

        /// <summary>
        /// Retrieves the build version from the chosen version control system based on
        /// a combination of commit history and the most recent matching tag
        /// This returns the version as: {major.minor.build} where 'build'
        /// represents the nth commit after a minor or tagged commit.
        /// Note: The initial 'v' and the commit hash code are removed.
        /// </summary>
        public bool GetBuildVersion(out string version, out string hash)
        {
            var majorAndMinor = GetMajorAndMinor(out hash, out var minorDot, out var lines);
            var major = int.Parse(minorDot > 0 ? majorAndMinor[..minorDot] : majorAndMinor);
            var minor = minorDot > 0 ? int.Parse(majorAndMinor[(minorDot + 1)..]) : 0;


            var minorFromLines = 0;
            var patch = _bundleVersionStyle switch
            {
                NumberType.BothMinorAndPatch => CountBothMinorAndPatch(lines),
                NumberType.MinorThenPatch => GetMinorAndThenPatch(lines, out minorFromLines),
                NumberType.MainAndBranch => CountMainAndBranch(lines),
                _ => 0
            };

            minor += minorFromLines;

            version = $"{major}.{minor}.{patch}";
            return true;
        }

        
        internal string[] GetCommitLogLines() => (VersionControlSystem switch
        {
            VersionControl.Git => Git.CommitLog,
            VersionControl.PlasticScm => PlasticProcess.CommitLog,
            _ => string.Empty,
        }).Split('\n').Select(line => line.Trim()).ToArray();

        
        private static string[] CountCommitLogLinesSinceGitTag(string tag)
            => Git.Run($@"log {tag}..head").Split('\n').Select(line => line.Trim()).ToArray();


        /// <summary>
        /// Retrieves the most recent version tag on current branch
        /// </summary>
        private static string GetGitDescription()
        {
            try
            {
                return Git.Run($@"describe --tags --long --match {VersionTagRegex}");
            }
            catch (GitException exception)
            {
                Debug.LogError($"{nameof(Versioning)}: exit code = {exception.ExitCode}\n{exception.Message}");
                return string.Empty;
            }
        }

        private string GetMajorAndMinor(out string hash, out int minorDot, out string[] lines)
            => _versionControlSystem switch
            {
                VersionControl.Git => GetMajorAndMinorFromGit(out hash, out minorDot, out lines),
                VersionControl.PlasticScm => GetMajorAndMinorFromPlastic(out hash, out minorDot, out lines),
                _ => GetMajorAndMinorUnhandled(_versionControlSystem, out hash, out minorDot, out lines),
            };
        
        private static string GetMajorAndMinorFromGit(out string hash, out int minorDot, out string[] lines)
        {
            var description = GetGitDescription();
            var hashDash = description.LastIndexOf('-');
            hash = description[(hashDash + 1)..];
            description = description[..hashDash];
            Debug.Log($"d:{description} h:{hash}");
            var commitsDash = description.LastIndexOf('-');
            var commits = int.Parse(description[(commitsDash + 1)..]);
            description = description[..commitsDash];
            Debug.Log($"d:{description} p:{commits} h:{hash}");
            var tag = description;

            var afterV = description.LastIndexOf('v') + 1;
            var majorAndMinor = description[afterV..];
            minorDot = majorAndMinor.LastIndexOf('.');
            lines = CountCommitLogLinesSinceGitTag(tag);
            return majorAndMinor;
        }

        private string GetMajorAndMinorFromPlastic(out string hash, out int minorDot, out string[] lines)
        {
            lines = PlasticProcess.CommitLog.Split('\n');
            var statusHeader = new PlasticProcess(@"status --header", Application.dataPath).Run().Output;
            var hashColon = statusHeader.LastIndexOf("cs:", System.StringComparison.Ordinal) + 2;
            var headDash = statusHeader.LastIndexOf('-');
            hash = statusHeader[hashColon..(headDash - 1)];

            var versionTags = lines.Where(line => PlasticVersionTagRegex.IsMatch(line)).ToArray();
            if (versionTags.Length <= 0)
            {
                minorDot = 0;
                return "0";
            }

            var description = versionTags.First();
            lines = lines.TakeWhile(line => !PlasticVersionTagRegex.IsMatch(line)).ToArray();
            
            var afterColon = description.LastIndexOf(':') + 1;
            var majorAndMinor = description[afterColon..];
            minorDot = majorAndMinor.LastIndexOf('.');
            return majorAndMinor;
        }

        private static string GetMajorAndMinorUnhandled(VersionControl versionControl, out string hash, out int zero,
            out string[] lines)
        {
            lines = new string[] { };
            zero = 0;
            hash = $"unhandled version control system: {versionControl}";
            Debug.LogError(hash);
            return hash;
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