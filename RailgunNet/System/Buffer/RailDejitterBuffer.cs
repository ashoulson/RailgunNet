/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
 *
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would be
 *     appreciated but is not required.
 *  2. Altered source versions must be plainly marked as such, and must not be
 *     misrepresented as being the original software.
 *  3. This notice may not be removed or altered from any source distribution.
*/

using System.Collections.Generic;

namespace Railgun
{
  /// <summary>
  /// Pre-allocated random access buffer for dejittering values. Preferable to
  /// DejitterList because of fast insertion and lookup, but harder to use.
  /// </summary>
  internal class RailDejitterBuffer<T>
    where T : class, IRailTimedValue, IRailPoolable<T>
  {
    // Used for converting a key to an index. For example, the server may only
    // send a snapshot every two ticks, so we would divide the tick number
    // key by 2 so as to avoid wasting space in the frame buffer
    private int divisor;

    private T[] data;
    private int latestIdx;
    private List<T> returnList; // A reusable list for returning results

    private readonly Comparer<Tick> tickComparer;

    /// <summary>
    /// The most recent value stored in this buffer.
    /// </summary>
    internal T Latest
    {
      get
      {
        if (this.latestIdx < 0)
          return null;
        return this.data[this.latestIdx];
      }
    }

    internal IEnumerable<T> Values
    {
      get
      {
        foreach (T value in this.data)
          if (value != null)
            yield return value;
      }
    }

    public RailDejitterBuffer(int capacity, int divisor = 1)
    {
      this.returnList = new List<T>();
      this.divisor = divisor;
      this.data = new T[capacity / divisor];
      this.latestIdx = -1;
      this.tickComparer = Tick.CreateComparer();
    }

    /// <summary>
    /// Clears the buffer, freeing all contents.
    /// </summary>
    public void Clear()
    {
      for (int i = 0; i < this.data.Length; i++)
        RailPool.SafeReplace(ref this.data[i], null);
      this.latestIdx = -1;
    }

    /// <summary>
    /// Stores a value. Will not replace a stored value with an older one.
    /// </summary>
    public bool Store(T value)
    {
      int index = this.TickToIndex(value.Tick);
      bool store = false;

      if (this.latestIdx < 0)
      {
        store = true;
      }
      else
      {
        T latest = this.data[this.latestIdx];
        if (value.Tick >= latest.Tick)
          store = true;
      }

      if (store)
      {
        RailPool.SafeReplace(ref this.data[index], value);
        this.latestIdx = index;
      }
      return store;
    }

    public T Get(Tick tick)
    {
      if (tick == Tick.INVALID)
        return null;

      T result = this.data[this.TickToIndex(tick)];
      if ((result != null) && (result.Tick == tick))
        return result;
      return null;
    }

    /// <summary>
    /// Given a tick, returns the the following values:
    /// - The value at or immediately before the tick (current)
    /// - The value immediately after that (next)
    /// 
    /// Runs in O(n).
    /// </summary>
    public void GetFirstAfter(
      Tick currentTick,
      out T current,
      out T next)
    {
      current = null;
      next = null;

      if (currentTick == Tick.INVALID)
        return;

      for (int i = 0; i < this.data.Length; i++)
      {
        T value = this.data[i];
        if (value != null)
        {
          if (value.Tick > currentTick)
          {
            if ((next == null) || (value.Tick < next.Tick))
              next = value;
          }
          else if ((current == null) || (value.Tick > current.Tick))
          {
            current = value;
          }
        }
      }
    }

    /// <summary>
    /// Finds the latest value at or before a given tick. O(n)
    /// </summary>
    public T GetLatestAt(Tick tick)
    {
      if (tick == Tick.INVALID)
        return null;

      T result = this.Get(tick);
      if (result != null)
        return result;

      for (int i = 0; i < this.data.Length; i++)
      {
        T value = this.data[i];
        if (value != null)
        {
          if (value.Tick == tick)
            return value;

          if (value.Tick < tick)
            if ((result == null) || (result.Tick < value.Tick))
              result = value;
        }
      }
      return result;
    }

    /// <summary>
    /// Finds all items at or later than the given tick, in order.
    /// </summary>
    public IList<T> GetRange(Tick start)
    {
      this.returnList.Clear();
      if (start == Tick.INVALID)
        return this.returnList;

      for (int i = 0; i < this.data.Length; i++)
      {
        T val = this.data[i];
        if ((val != null) && (val.Tick >= start))
          this.returnList.Add(val);
      }

      this.returnList.Sort(this.Compare);
      return this.returnList;
    }

    /// <summary>
    /// Finds all items with ticks in the inclusive range [start, end]
    /// and also returns the value immediately following (if one exists)
    /// </summary>
    public IList<T> GetRangeAndNext(Tick start, Tick end, out T next)
    {
      next = null;
      this.returnList.Clear();
      if (start == Tick.INVALID)
        return this.returnList;

      Tick lowest = Tick.INVALID;
      for (int i = 0; i < this.data.Length; i++)
      {
        T val = this.data[i];
        if (val != null)
        {
          if ((val.Tick >= start) && (val.Tick <= end))
          {
            this.returnList.Add(val);
          }

          if (val.Tick > end)
          {
            if ((lowest == Tick.INVALID || val.Tick < lowest))
            {
              next = val;
              lowest = val.Tick;
            }
          }
        }
      }

      this.returnList.Sort(this.Compare);
      return this.returnList;
    }

    public bool Contains(Tick tick)
    {
      if (tick == Tick.INVALID)
        return false;

      T result = this.data[this.TickToIndex(tick)];
      if ((result != null) && (result.Tick == tick))
        return true;
      return false;
    }

    public bool TryGet(Tick tick, out T value)
    {
      if (tick == Tick.INVALID)
      {
        value = null;
        return false;
      }

      value = this.Get(tick);
      return (value != null);
    }

    private int TickToIndex(Tick tick)
    {
      return (int)(tick.RawValue / this.divisor) % this.data.Length;
    }

    private int Compare(T x, T y)
    {
      return this.tickComparer.Compare(x.Tick, y.Tick);
    }
  }
}
