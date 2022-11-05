using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Build.Editor
{
    public class XcodebuildProcess
    {
        private Process myProcess;
        private TaskCompletionSource<bool> eventHandled;
        private readonly string arguments;
        private readonly string workingDirectory;

        public XcodebuildProcess(string arguments, string directory)
        {
            this.arguments = arguments;
            this.workingDirectory = directory;
        }
        
        public delegate void ProcessOutput(Report report);

        public event ProcessOutput Completed;
        
        // Print a file with any known extension.
        public async Task Start()
        {
            eventHandled = new TaskCompletionSource<bool>();

            using (myProcess = new Process())
            {
                try
                {
                    myProcess.StartInfo = new ProcessStartInfo
                    {
                        CreateNoWindow = false,
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true,
                        FileName = @"xcodebuild",
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory
                    };
                    
                    // Use the following event to read both output and errors output.
                    var outputBuilder = new StringBuilder();
                    var errorsBuilder = new StringBuilder();
                    myProcess.OutputDataReceived += (_, args) => outputBuilder.AppendLine(args.Data);
                    myProcess.ErrorDataReceived += (_, args) => errorsBuilder.AppendLine(args.Data);
                    
                    myProcess.EnableRaisingEvents = true;
                    myProcess.Exited += myProcess_Exited;
                    myProcess.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred running an Xcodebuild process with arguments \"{arguments}\":\n{ex.Message}");
                    myProcess.Dispose();
                    return;
                }
            }
        }

        // Handle Exited event and display process information.
        private void myProcess_Exited(object sender, System.EventArgs e)
        {
            var report = new Report();
            report.arguments = arguments;
            report.errors = myProcess.StandardError.ReadToEnd();
            report.output = myProcess.StandardOutput.ReadToEnd();
            report.exitCode = myProcess.ExitCode;
            Completed?.Invoke(new Report());
            Console.WriteLine(
                $"Exit time    : {myProcess.ExitTime}\n" +
                $"Exit code    : {myProcess.ExitCode}\n" +
                $"Elapsed time : {Math.Round((myProcess.ExitTime - myProcess.StartTime).TotalMilliseconds)}");
            eventHandled.TrySetResult(true);
            myProcess.Dispose();
        }
        
        [Serializable]
        public struct Report
        {
            public string arguments;
            public string output;
            public string errors;
            public int exitCode;
            public bool Success => exitCode != 0;
        }
    }
}