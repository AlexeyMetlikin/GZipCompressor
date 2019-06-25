using System;

namespace Compressor.Extensions
{
    public static class BitConverterExtensions
    {
        public static byte[] ToByteArray(this int length)
        {
            return BitConverter.GetBytes(length);
        }

        public static int ToInt32(this byte[] intToParse)
        {
            return BitConverter.ToInt32(intToParse, 0);
        }
    }
}