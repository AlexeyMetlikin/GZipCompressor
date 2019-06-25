using System;
using System.Collections.Generic;
using System.Threading;

namespace Compressor.Extensions
{
    public static class DictionaryExtensions
    {
        public static void SafeAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value,
            object locker)
        {
            try
            {
                Monitor.Enter(locker);
                dictionary.Add(key, value);
            }
            finally
            {
                Monitor.Exit(locker);
            }
        }

        public static TValue SafeDelete<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey counter,
            object locker)
        {
            var resultValue = dictionary[counter];
            try
            {
                Monitor.Enter(locker);
                (dictionary[counter] as IDisposable)?.Dispose();
                dictionary.Remove(counter);
            }
            finally
            {
                Monitor.Exit(locker);
            }

            return resultValue;
        }
    }
}