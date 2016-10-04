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

    public T LatestAt(Tick tick)
    {
      // TODO: Binary Search
      T retVal = null;
      foreach (T val in this.data)
        if (val.Tick <= tick)
          retVal = val;
      return retVal;
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
