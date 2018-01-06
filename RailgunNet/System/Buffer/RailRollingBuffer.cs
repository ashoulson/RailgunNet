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

using System;
using System.Collections.Generic;

namespace Railgun
{
  /// <summary>
  /// A rolling buffer that contains a sliding window of the most recent
  /// stored values.
  /// </summary>
  internal class RailRollingBuffer<T>
  {
    internal int Count { get { return this.count; } }

    private int count;
    private int start;

    private readonly T[] data;
    private readonly int capacity;

    public RailRollingBuffer(int capacity)
    {
      if (capacity <= 0)
        throw new ArgumentOutOfRangeException("capacity");

      this.capacity = capacity;

      this.data = new T[capacity];
      this.count = 0;
      this.start = 0;
    }

    public void Clear()
    {
      this.count = 0;
      this.start = 0;
    }

    /// <summary>
    /// Stores a value as latest.
    /// </summary>
    public void Store(T value)
    {
      if (this.count < this.capacity)
      {
        this.data[this.count++] = value;
        this.IncrementStart();
      }
      else
      {
        this.data[this.start] = value;
        this.IncrementStart();
      }
    }

    /// <summary>
    /// Returns all values, but not in order.
    /// </summary>
    public IEnumerable<T> GetValues()
    {
      for (int i = 0; i < this.count; i++)
        yield return this.data[i];
    }

    private void IncrementStart()
    {
      this.start = (this.start + 1) % this.capacity;
    }
  }
}
