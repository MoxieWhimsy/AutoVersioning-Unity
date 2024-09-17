using System.Threading.Tasks;

namespace Build.Editor
{
    public static class XcodeBuild
    {
        /// <summary>
        /// Runs xcodebuild with the specified arguments and returns the output.
        /// </summary>
        /// <returns>whether the command completed with exit code 0.</returns>
        public static async Task RunAsync(string arguments, string directory, XcodebuildProcess.ProcessOutput callback = null)
        {
            var xcodebuildProcess = new XcodebuildProcess(arguments, directory);
            if (callback != null) xcodebuildProcess.Completed += callback;
            await xcodebuildProcess.Start();
        }
        
    }
}
