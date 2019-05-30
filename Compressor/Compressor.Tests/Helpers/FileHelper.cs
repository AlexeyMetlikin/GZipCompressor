using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Compressor.Tests.Helpers
{
    public static class FileHelper
    {
        private const string TestResourcesFolderName = "TestResources";

        public static string GetFullPathByFileName(string fileName)
        {
            var baseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!Directory.Exists(baseDirectory))
                return null;

            var matches = new Regex(@"\\").Matches(baseDirectory);
            return $"{baseDirectory.Remove(matches[matches.Count - 2].Index)}\\{TestResourcesFolderName}\\{fileName}";
        }
    }
}