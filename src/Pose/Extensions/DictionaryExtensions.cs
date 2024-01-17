// Putting the extension method under this namespace makes it synonymous
// with its official counterpart when using the library on platforms
// which do not have the official version.
#if !(NETCOREAPP2_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)
// ReSharper disable once CheckNamespace
namespace System.Collections.Generic
{
    internal static class DictionaryExtensions
    {
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            try
            {
                dictionary.Add(key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
#endif