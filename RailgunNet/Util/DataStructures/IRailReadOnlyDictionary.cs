using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public class RailReadOnlyDictionary<TKey, TValue>
  {
    IDictionary<TKey, TValue> backingDict;

    public RailReadOnlyDictionary(IDictionary<TKey, TValue> backingDict)
    {
      this.backingDict = backingDict;
    }

    public bool ContainsKey(TKey key)
    {
      return this.backingDict.ContainsKey(key);
    }

    public ICollection<TKey> Keys
    {
      get { return this.backingDict.Keys; }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
      return this.backingDict.TryGetValue(key, out value);
    }

    public ICollection<TValue> Values
    {
      get { return this.backingDict.Values; }
    }

    public TValue this[TKey key]
    {
      get { return this.backingDict[key]; }
    }

    public bool Contains(KeyValuePair<TKey, TValue> item)
    {
      return this.backingDict.Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
      this.backingDict.CopyTo(array, arrayIndex);
    }

    public int Count
    {
      get { return this.backingDict.Count; }
    }
  }
}
