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
    // Used for converting a key to an index. For example, the host may only
    // send a snapshot every two ticks, so we would divide the tick number
    // key by 2 so as to avoid wasting space in the frame buffer
    private int divisor;

    private T[] data;

    public RailRingBuffer(int capacity, int divisor = 1)
    {
      this.divisor = divisor;
      this.data = new T[capacity / divisor];
      for (int i = 0; i < this.data.Length; i++)
        this.data[i] = null;
    }

    public void Store(T value)
    {
      int index = this.TickToIndex(value.Tick);
      if (this.data[index] != null)
        RailPool.Free(this.data[index]);
      this.data[index] = value;
    }

    public T Get(int tick)
    {
      if (tick == RailClock.INVALID_TICK)
        return null;

      T result = this.data[this.TickToIndex(tick)];
      if ((result != null) && (result.Tick == tick))
        return result;
      return null;
    }

    public void PopulateDelta(RailRingDelta<T> delta, int currentTick)
    {
      if (currentTick == RailClock.INVALID_TICK)
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

    public T GetLatest(int tick)
    {
      if (tick == RailClock.INVALID_TICK)
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

    public bool Contains(int tick)
    {
      if (tick == RailClock.INVALID_TICK)
        return false;

      T result = this.data[this.TickToIndex(tick)];
      if ((result != null) && (result.Tick == tick))
        return true;
      return false;
    }

    public bool TryGet(int tick, out T value)
    {
      if (tick == RailClock.INVALID_TICK)
      {
        value = null;
        return false;
      }

      value = this.Get(tick);
      return (value != null);
    }

    private int TickToIndex(int tick)
    {
      return (tick / this.divisor) % this.data.Length;
    }
  }
}
