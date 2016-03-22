using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  /// <summary>
  /// A dictionary that keeps values up to a given age. Can be cleaned
  /// to remove all entries older than a given tick. Does not accept
  /// entries older than its current age.
  /// </summary>
  internal class RailExpiringDictionary<TKey, TValue>
    where TValue : IRailTimedValue, IRailKeyedValue<TKey>
  {
    private class TimedComparer : Comparer<TValue>
    {
      public override int Compare(TValue x, TValue y)
      {
        return Tick.Comparer.Compare(x.Tick, y.Tick);
      }
    }

    private static TimedComparer Comparer = new TimedComparer();

    private MinHeap<TValue> values;
    private Dictionary<TKey, TValue> contents;

    private Tick minTick;

    public RailExpiringDictionary(IEqualityComparer<TKey> comparer)
    {
      this.values = new MinHeap<TValue>(RailExpiringDictionary<TKey, TValue>.Comparer);
      this.contents = new Dictionary<TKey, TValue>(comparer);
      this.minTick = Tick.INVALID;
    }

    /// <summary>
    /// Removes all entries older than the given tick.
    /// </summary>
    public IEnumerable<TValue> Expire(Tick latest)
    {
      if (this.minTick.IsValid && (this.minTick > latest))
        yield break;

      while (this.contents.Count > 0)
      {
        TValue first = this.values.PeekFirst();
        if (first.Tick > latest)
          break;

        this.contents.Remove(first.Key);
        this.values.PopFirst();

        yield return first;
      }

      this.minTick = latest;
    }

    /// <summary>
    /// Adds an entry to the dictionary, unless that entry is too old or
    /// is already contained in the dictionary.
    /// </summary>
    public bool Store(TValue value)
    {
      if (this.minTick.IsValid && (this.minTick > value.Tick))
        return false;
      if (this.contents.ContainsKey(value.Key))
        return false;

      this.contents.Add(value.Key, value);
      this.values.Add(value);
      return true;
    }
  }
}
