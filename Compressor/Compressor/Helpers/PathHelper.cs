using System.IO;
using System.Reflection;

namespace Compressor.Helpers
{
    public static class PathHelper
    {
        private const string MaxPathPropertyName = "MaxPath";

        public static int GetMaxPathLength()
        {
            var maxPathField = typeof(Path).GetField(MaxPathPropertyName,
                BindingFlags.Static |
                BindingFlags.GetField |
                BindingFlags.NonPublic);

            return (int)(maxPathField?.GetValue(null) ?? 0);
        }
    }
}