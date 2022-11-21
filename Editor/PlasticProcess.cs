using System.Threading.Tasks;
using RedBlueGames;
using UnityEngine;

namespace Build.Editor
{
    public class PlasticProcess
    {
        private TaskCompletionSource<bool> _eventHandled;
        private readonly string _arguments;
        private readonly string _workingDirectory;

        public PlasticProcess(string arguments, string directory)
        {
            _arguments = arguments;
            _workingDirectory = directory;
        }

        /// <summary>
        /// The full commit log.
        /// </summary>
        public static string CommitLog
            => new PlasticProcess(
                @"log --csformat='Changeset {changesetid} created on {date};{tab}Comments: {newline}{comment}'",
                Application.dataPath).Run().Output;

        public static string GetDescription()
        {
            var log = new PlasticProcess(@"log --csformat='{comment}'", Application.dataPath).Run().Output;
            if (log.Length <= 0) return string.Empty;
            var lines = log.Split('\n');
            throw new System.NotImplementedException();
        }

        public Report Run()
        {
            var report = new Report();
            using var process = new System.Diagnostics.Process();
            try
            {
                process.Run(@"cm", _arguments, _workingDirectory, out var output, out var errors);
                report.Output = output;
                report.Errors = errors;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{e}");
            }
            finally
            {
                process.Dispose();
            }
            return report;
        }
        
        
        [System.Serializable]
        public struct Report
        {
            public string Output { get; internal set; }
            public string Errors { get; internal set; }
            public int ExitCode { get; internal set; }
            public bool Success => ExitCode != 0;
        }
    }
}
