using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoLib
{
    class CleverDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        Dictionary<TKey, TValue> innerDictoinary;
        TValue defaultValue;

        public CleverDictionary()
        {
            innerDictoinary = new Dictionary<TKey, TValue>();
            this.defaultValue = default(TValue);
        }

        public CleverDictionary(TValue defaultValue)
        {
            innerDictoinary = new Dictionary<TKey, TValue>();
            this.defaultValue = defaultValue;
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue temp;
                if (innerDictoinary.TryGetValue(key, out temp))
                {
                    return temp;
                }
                return defaultValue;
            }

            set
            {
                innerDictoinary[key] = value;
            }
        }

        public int Count
        {
            get
            {
                return innerDictoinary.Count;
            }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>)innerDictoinary).IsReadOnly;
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return innerDictoinary.Keys;
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                return innerDictoinary.Values;
            }
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)innerDictoinary).Add(item);
        }

        public void Add(TKey key, TValue value)
        {
            innerDictoinary.Add(key, value);
        }

        public void Clear()
        {
            innerDictoinary.Clear();
        }

        // should not use this func??
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();

            /*if (innerDictoinary.Contains(item))
            {
                return true;
            }
            
            return item.Value.Equals(defaultValue);  // ボックス化されるけど大丈夫？
            */
        }

        // should not use this func??
        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            //return innerDictoinary.ContainsKey(key);
            return true;
        }

        /*public bool ContainsValue(TValue val)
        {
            return innerDictoinary.ContainsValue(val);
        }*/

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)innerDictoinary).CopyTo(array, arrayIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerDictoinary.GetEnumerator();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return innerDictoinary.GetEnumerator();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((ICollection<KeyValuePair<TKey, TValue>>)innerDictoinary).Remove(item);
        }

        // should not use this func??
        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            return innerDictoinary.Remove(key);
        }
        
        public bool TryGetValue(TKey key, out TValue value)
        {
            if (innerDictoinary.TryGetValue(key, out value) == false)
            {
                value = defaultValue;
            }

            return true;
        }
    }
}
