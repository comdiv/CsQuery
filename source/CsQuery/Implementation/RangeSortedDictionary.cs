﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using CsQuery.ExtensionMethods;
using CsQuery.HtmlParser;
using CsQuery.Implementation;

#if !ARRAYPATH

namespace CsQuery.Implementation
{
    /// <summary>
    /// A dictionary that is substring-lookup capable. This is the data structure used to index HTML
    /// documents for selectors. A SortedSet of keys is used for the index because it allows fast
    /// access by substring. A list of keys obtained from the SortedSet for a selector is used to
    /// obtain the target references from a regular dictionary.
    /// </summary>
    ///
    /// <typeparam name="TKey">
    /// Type of the key.
    /// </typeparam>
    /// <typeparam name="TValue">
    /// Type of the value
    /// </typeparam>

    public class RangeSortedDictionary<TKey,TValue> : IRangeSortedDictionary<TKey,TValue> where TKey: IConvertible, IComparable
    {
        #region constructor

        /// <summary>
        /// Default constructor.
        /// </summary>
        ///
        /// <param name="setComparer">
        /// The comparer.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer.
        /// </param>
        /// <param name="indexSeparator">
        /// The index separator.
        /// </param>

        public RangeSortedDictionary(IComparer<TKey[]> setComparer, IEqualityComparer<TKey[]> equalityComparer, TKey indexSeparator)
        {
            //Keys = new SortedSet<string>(TrueStringComparer.Comparer);
            //Index = new Dictionary<string, TValue>(TrueStringComparer.Comparer);
            Keys = new SortedSet<TKey[]>(setComparer);
            Index = new Dictionary<TKey[], TValue>(equalityComparer);
            IndexSeparator = indexSeparator;

            //Index = new MonoDictionary<string, TValue>(TrueStringComparer.Comparer);
            //Index = new Net46.Dictionary<string, TValue>(TrueStringComparer.Comparer);
        }
        
        #endregion

        #region private properties

        private TKey IndexSeparator;
        // the "threadsafe" flag causes certain objects to be compiled with thread safety. This causes
        // a performance hit so this is done mostly for debugging
        
#if threadsafe
        private object _locker = new object();
#endif

        /// <summary>
        /// An ordered set of all the keys in this dictionary.
        /// </summary>

        protected SortedSet<TKey[]> Keys;

        /// <summary>
        /// The inner index.
        /// </summary>

        protected IDictionary<TKey[], TValue> Index;

        #endregion

        #region public properties

        /// <summary>
        /// Returns the keys in human-readable format.
        /// </summary>

        public IEnumerable<string> KeysAudit
        { 
            get 
            {
                foreach (var item in Keys)
                {
#if DEBUG_PATH
                    yield return item;
#else
                   
                    string humanReadableKey = "";
                    int startIndex = 1;
                    if (!item[0].Equals( IndexSeparator))
                    {
                        ushort keyPart = (ushort)Convert.ChangeType(item[1], typeof(ushort));
                        humanReadableKey = Convert.ChangeType(item[0],typeof(char)) + HtmlData.TokenName(keyPart) + '/';
                        startIndex = 3;
                    }
                        
                    for (int i = startIndex;i<item.Length;i++)
                    {
                        ushort c = (ushort)Convert.ChangeType(item[i], typeof(ushort));
                        humanReadableKey += ((ushort)c).ToString().PadLeft(3,'0');
                        if (i<item.Length-1) {
                            humanReadableKey += '/';
                        }
                    }
                    yield return humanReadableKey;
#endif
                }
            } 
        }
        /// <summary>
        /// Retrieve all the keys that match the subkey provided; that is, all keys that start with the
        /// value of 'subkey'.
        /// </summary>
        ///
        /// <param name="subkey">
        /// The subkey to match
        /// </param>
        ///
        /// <returns>
        /// A sequence of keys found in the dictionary.
        /// </returns>

        //public IEnumerable<TKey[]> GetRangeKeys(TKey[] subkey)
        //{

        //    if (subkey==null || subkey.Length==0) {
        //        yield break;
        //    }
        //    string lastKey = subkey.Substring(0,subkey.Length - 1) + (char)(((int)subkey[subkey.Length - 1]) + 1);
            
        //    foreach (var key in Keys.GetViewBetween(subkey, lastKey))
        //    {
        //        if (key != lastKey)
        //        {
        //            yield return key;
        //        }
        //    }
        //}
        public IEnumerable<TKey[]> GetRangeKeys(TKey[] subkey)
        {

            if (subkey == null || subkey.Length == 0)
            {
                yield break;
            }

            int lastEl = (int)Convert.ChangeType(subkey[subkey.Length - 1], typeof(int));

            TKey[] lastKey = subkey.Take(subkey.Length - 1)
                .Concat((TKey)Convert.ChangeType(lastEl+ 1,typeof(TKey)))
                .ToArray();

            foreach (var key in Keys.GetViewBetween(subkey, lastKey))
            {
                if (key != lastKey)
                {
                    yield return key;
                }
            }
        }

        /// <summary>
        /// Return all matching keys at the specified depth relative to the subkey, e.g. 0 will return
        /// only the element that exactly matches the subkey.
        /// </summary>
        ///
        /// <param name="subKey">
        /// The subkey to match.
        /// </param>
        /// <param name="depth">
        /// The zero-based depth relative to the subkey's depth
        /// </param>
        /// <param name="descendants">
        /// When true, include elements that are at a greater depth too
        /// </param>
        ///
        /// <returns>
        /// A sequence of TValue elements.
        /// </returns>

        public IEnumerable<TValue> GetRange(TKey[] subKey, int depth, bool descendants)
        {
            if (depth == 0 && !descendants)
            {
                //if (Index.ContainsKey(subKey))
                if (Index.ContainsKey(subKey))
                {
                    yield return Index[subKey];
                }
                else
                {
                    yield break;
                }
            }
            else
            {
                int len = subKey.Length;
                int curDepth=0;
                foreach (var key in GetRangeKeys(subKey))
                {
                    if (key.Length > len)
                    {
                        {
#if DEBUG_PATH
                            curDepth = (key.Length - len) / HtmlData.pathIdLength;
#else
                            curDepth = key.Length - len;
#endif
                        }
                    }
                    if (curDepth == depth || (descendants && curDepth >= depth))
                    {
                        yield return Index[key];
                    }
                }
            }
        }

        /// <summary>
        /// Return a sequence of values for each key that starts with the value of 'subkey'.
        /// </summary>
        ///
        /// <param name="subKey">
        /// The subkey to match.
        /// </param>
        ///
        /// <returns>
        /// A sequence of values from the dictionary.
        /// </returns>

        public IEnumerable<TValue> GetRange(TKey[] subKey)
        {
            foreach (var key in GetRangeKeys(subKey))
            {
                yield return Index[key];
            }
        }

        #endregion

        #region IDictionary<string,TValue> Members

        /// <summary>
        /// Adds a key/value pair to the dictionary
        /// </summary>
        ///
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="value">
        /// [out] The value.
        /// </param>

        public void Add(TKey[] key, TValue value)
        {
#if threadsafe
            lock (_locker)
            {
#endif
                if (Keys.Add(key))
                {
                    Index.Add(key, value);
                }
#if threadsafe
            }
#endif
        }

        /// <summary>
        /// Test whether the dictionary contains a value for 'key'
        /// </summary>
        ///
        /// <param name="key">
        /// The key.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool ContainsKey(TKey[] key)
        {
            return Keys.Contains(key);
        }

        ICollection<TKey[]> IDictionary<TKey[], TValue>.Keys
        {
            get { return Keys; }
        }

        /// <summary>
        /// Removes the given key
        /// </summary>
        ///
        /// <param name="key">
        /// The key.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Remove(TKey[] key)
        {
#if threadsafe
            lock (_locker)
            {
#endif
                if (Keys.Remove(key))
                {
                    Index.Remove(key);
                    return true;
                }
                else
                {
                    return false;
                }
#if threadsafe
            }
#endif
        }

        /// <summary>
        /// Try to get a value by name
        /// </summary>
        ///
        /// <param name="key">
        /// The key.
        /// </param>
        /// <param name="value">
        /// [out] The value.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool TryGetValue(TKey[] key, out TValue value)
        {
            return Index.TryGetValue(key, out value);
      
        }

        /// <summary>
        /// Gets the values.
        /// </summary>

        public ICollection<TValue> Values
        {
            get {
                return Values;
            }
        }

        /// <summary>
        /// Return the value for 'key'
        /// </summary>
        ///
        /// <param name="key">
        /// The key.
        /// </param>
        ///
        /// <returns>
        /// The indexed item.
        /// </returns>

        public TValue this[TKey[] key]
        {
            get
            {
                return Index[key];
            }
            set
            {
#if threadsafe
                lock (_locker)
                {
#endif
                if (ContainsKey(key))
                {
                    Index[key] = value;
                }
                else
                {
                    Add(key, value);
                }
#if threadsafe
                }
#endif
            }
        }

        #endregion

        #region ICollection<KeyValuePair<string,TValue>> Members

        /// <summary>
        /// Adds a key/value pair to the dictionary.
        /// </summary>
        ///
        /// <param name="item">
        /// The item to test for.
        /// </param>

        public void Add(KeyValuePair<TKey[], TValue> item)
        {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Clears this object to its blank/initial state.
        /// </summary>

        public void Clear()
        {
#if threadsafe
            lock (_locker)
            {
#endif
                Keys.Clear();
                Index.Clear();
#if threadsafe
            }
#endif
        }

        /// <summary>
        /// Test whether the KeyValuePair object exists in this dictionary.
        /// </summary>
        ///
        /// <param name="item">
        /// The item to test for.
        /// </param>
        ///
        /// <returns>
        /// true if the object is in this collection, false if not.
        /// </returns>

        public bool Contains(KeyValuePair<TKey[], TValue> item)
        {
            return Index.Contains(item);
        }

        /// <summary>
        /// Copies the contents of the dictionary to an array of KeyValuePair objects.
        /// </summary>
        ///
        /// <param name="array">
        /// The array.
        /// </param>
        /// <param name="arrayIndex">
        /// Zero-based index of the array at which to start copying.
        /// </param>

        public void CopyTo(KeyValuePair<TKey[], TValue>[] array, int arrayIndex)
        {
            foreach (var kvp in this)
            {
                array[arrayIndex++] = kvp;
            }
        }

        /// <summary>
        /// Gets the number of items in this dictionary.
        /// </summary>

        public int Count
        {
            get { return Index.Count; }
        }

        /// <summary>
        /// Gets a value indicating whether this object is read only. This is always false.
        /// </summary>

        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Removes the given KeyValuePair from the dictionary if it exists
        /// </summary>
        ///
        /// <param name="item">
        /// The item to remove.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Remove(KeyValuePair<TKey[], TValue> item)
        {
            return Remove(item.Key);
        }

        #endregion

        #region IEnumerable<KeyValuePair<string,TValue>> Members

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        public IEnumerator<KeyValuePair<TKey[], TValue>> GetEnumerator()
        {
            // Don't use dictionary enumerator - return values in sorted order when enumerating over entire object
            foreach (var key in Keys)
            {
                yield return new KeyValuePair<TKey[], TValue>(key, Index[key]);
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
 
    }
}
#endif