using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class RailHeapBuffer<TKey, TValue>
    where TValue : IRailTimedValue, IRailKeyedValue<TKey>, IRailPoolable<TValue>
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

    public RailHeapBuffer(IEqualityComparer<TKey> comparer)
    {
      this.values = new MinHeap<TValue>(RailHeapBuffer<TKey, TValue>.Comparer);
      this.contents = new Dictionary<TKey, TValue>(comparer);
      this.minTick = Tick.INVALID;
    }

    public IEnumerable<TValue> Advance(Tick latest)
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
        RailPool.Free(first);
      }

      this.minTick = latest;
    }

    public bool Record(TValue value)
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
