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
  /// A rolling queue that maintains entries in order. Designed to access
  /// the entry at a given tick, or the most recent entry before it.
  /// </summary>
  internal class RailQueueBuffer<T>
    where T : class, IRailTimedValue, IRailPoolable<T>
  {
    private static IEnumerable<T> Remainder(
      T latest, 
      Queue<T>.Enumerator enumerator)
    {
      yield return latest;
      while (enumerator.MoveNext())
        yield return enumerator.Current;
    }

    internal T Latest { get; private set; }

    private readonly Queue<T> data;
    private readonly int capacity;

    public RailQueueBuffer(int capacity)
    {
      this.Latest = null;
      this.capacity = capacity;
      this.data = new Queue<T>();
    }

    public void Store(T val)
    {
      if (this.data.Count >= this.capacity)
        RailPool.Free(this.data.Dequeue());
      this.data.Enqueue(val);
      this.Latest = val;
    }

    /// <summary>
    /// Returns the first value with a tick less than or equal to the given
    /// tick, followed by all subsequent values stored. If no value has a tick
    /// less than or equal to the given one, this function returns null.
    /// </summary>
    public IEnumerable<T> LatestFrom(Tick tick)
    {
      if (tick.IsValid == false)
        return null;

      var head = this.data.GetEnumerator();
      var tail = this.data.GetEnumerator();

      // Find the value at the given tick. TODO: Binary search?
      T latest = null;

      while (head.MoveNext())
      {
        if (head.Current.Tick <= tick)
          latest = head.Current;
        else
          break;
        tail.MoveNext();
      }

      if (latest == null)
        return null;
      return RailQueueBuffer<T>.Remainder(latest, tail);
    }

    /// <summary>
    /// Clears the buffer, freeing all contents.
    /// </summary>
    public void Clear()
    {
      foreach (T val in this.data)
        RailPool.Free(val);
      this.data.Clear();
      this.Latest = null;
    }
  }
}
