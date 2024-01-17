using System.Collections.Generic;

namespace Pose.Extensions
{
    internal static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
#if NETSTANDARD2_0 || NET48
            try
            {
                dictionary.Add(key, value);
                return true;
            }
            catch
            {
                return false;
            }
#else
            return dictionary.TryAdd(key, value);
#endif
        }
    }
}