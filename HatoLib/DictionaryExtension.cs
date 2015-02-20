using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoLib
{
    public static class DictionaryExtension
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue val;

            if (dictionary.TryGetValue(key, out val))
            {
                return val;
            }
            else
            {
                return default(TValue);
            }
        }
    }
}
