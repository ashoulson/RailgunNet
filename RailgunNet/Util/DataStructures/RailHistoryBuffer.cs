using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  /// <summary>
  /// A rolling queue that maintains entries in order. Supports fast access
  /// to the entry (or most recent entry) at a given tick.
  /// </summary>
  internal class RailHistoryBuffer<T>
    where T : class, IRailTimedValue
  {
    // TODO: Replace me with something that supports binary search!
    private readonly Queue<T> data;
    private readonly int capacity;

    public RailHistoryBuffer(int capacity)
    {
      this.capacity = capacity;
      this.data = new Queue<T>();
    }

    public T Store(T val)
    {
      T retVal = null;
      if (this.data.Count >= this.capacity)
        retVal = this.data.Dequeue();
      this.data.Enqueue(val);
      return retVal;
    }

    public T Latest(Tick tick)
    {
      // TODO: Binary Search
      T retVal = null;
      foreach (T val in this.data)
        if (val.Tick <= tick)
          retVal = val;
      return retVal;
    }
  }
}
