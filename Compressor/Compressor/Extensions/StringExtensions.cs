namespace Compressor.Extensions
{
    public static class StringExtensions
    {
        public static string WithFirstLetterUpper(this string str)
        {
            return str?.Length > 0
                ? str.Substring(0, 1).ToUpper() + str.Substring(1)
                : str;
        }
    }
}