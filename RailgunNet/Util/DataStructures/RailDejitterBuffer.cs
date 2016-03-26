/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
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

using System;
using System.Collections;
using System.Collections.Generic;

namespace Railgun
{
  internal class RailDejitterBuffer<T>
    where T : class, IRailTimedValue
  {
    // Used for converting a key to an index. For example, the server may only
    // send a snapshot every two ticks, so we would divide the tick number
    // key by 2 so as to avoid wasting space in the frame buffer
    private int divisor;

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

    private T[] data;
    private int latestIdx;

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
      this.divisor = divisor;
      this.data = new T[capacity / divisor];
      this.latestIdx = -1;
    }

    /// <summary>
    /// Stores a value. If this displaces an existing value in the process,
    /// this function will return it. Note that this function does not enforce
    /// time constraints. It is possible to replace a value with an older one.
    /// </summary>
    public T Store(T value)
    {
      int index = this.TickToIndex(value.Tick);
      T current = this.data[index];

      bool updateLatest = false;
      if (this.latestIdx < 0)
      {
        this.latestIdx = index;
      }
      else
      {
        T latest = this.data[this.latestIdx];
        if (value.Tick >= latest.Tick)
        {
          this.latestIdx = index;
        }
        else if (index == this.latestIdx)
        {
          // We're replacing the latest element with an older one, will
          // need to find a new one after we do the insertion
          updateLatest = true;
        }
      }

      this.data[index] = value;
      if (updateLatest)
        this.UpdateLatest();
      return current;
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
    public void GetRangeAt(
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

    /// <summary>
    /// Finds the absolute latest value in the buffer. O(n)
    /// </summary>
    private void UpdateLatest()
    {
      Tick retTick = Tick.INVALID;
      int newIndex = -1;

      for (int i = 0; i < this.data.Length; i++)
      {
        T value = this.data[i];
        if (value != null)
        {
          if ((newIndex < 0) || (value.Tick > retTick))
          {
            newIndex = i;
            retTick = value.Tick;
          }
        }
      }

      this.latestIdx = newIndex;
    }

    private int TickToIndex(Tick tick)
    {
      return (tick.RawValue / this.divisor) % this.data.Length;
    }
  }
}
