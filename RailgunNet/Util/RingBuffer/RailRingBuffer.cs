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
  internal class RailRingBuffer<T>
    where T : class, IRailRingValue, IRailPoolable
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
      get { return this.latest; }
    }

    /// <summary>
    /// The earliest ever value stored in this buffer.
    /// May no longer be in the buffer itself.
    /// </summary>
    internal T Earliest
    {
      get { return this.earliest; }
    }

    private T[] data;
    private T latest;
    private T earliest;

    // Whether or not the earliest is still in the buffer (used for freeing)
    private bool earliestInBuffer;
    private bool latestInBuffer;

    internal IEnumerable<T> Values
    {
      get
      {
        foreach (T value in this.data)
          if (value != null)
            yield return value;
      }
    }

    public RailRingBuffer(int capacity, int divisor = 1)
    {
      this.divisor = divisor;
      this.data = new T[capacity / divisor];
    }

    public void Store(T value)
    {
      int index = this.TickToIndex(value.Tick);

      // Replace the current value in that slot and free it unless the
      // current is stored as the earliest or latest values
      T current = this.data[index];
      if (this.data[index] != null)
      {
        bool canFree = true;

        if (current == this.earliest)
        {
          this.earliestInBuffer = false;
          canFree = false;
        }

        if (current == this.latest)
        {
          this.latestInBuffer = false;
          canFree = false;
        }

        if (canFree)
        {
          RailPool.Free(current);
        }
      }

      this.data[index] = value;

      // Replace the earliest if applicable
      if ((this.earliest == null) || (this.earliest.Tick > value.Tick))
      {
        if ((this.earliest != null) && (this.earliestInBuffer == false))
          RailPool.Free(this.earliest);

        this.earliest = value;
        this.earliestInBuffer = true;
      }

      // Replace the latest if applicable
      if ((this.latest == null) || (this.latest.Tick < value.Tick))
      {
        if ((this.latest != null) && (this.latestInBuffer == false))
          RailPool.Free(this.latest);

        this.latest = value;
        this.latestInBuffer = true;
      }
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

    public void PopulateDelta(RailRingDelta<T> delta, Tick currentTick)
    {
      if (currentTick == Tick.INVALID)
      {
        delta.Set(null, null, null);
        return;
      }

      T prior = null;
      T latest = null;
      T next = null;

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
          else if ((prior == null) || (value.Tick > prior.Tick))
          {
            if ((latest == null) || (value.Tick > latest.Tick))
            {
              prior = latest;
              latest = value;
            }
            else
            {
              prior = value;
            }
          }
        }
      }

      delta.Set(prior, latest, next);
    }

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

    private int TickToIndex(Tick tick)
    {
      return (tick.RawValue / this.divisor) % this.data.Length;
    }
  }
}
