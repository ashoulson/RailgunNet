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
using System.Collections.Generic;

namespace Railgun
{
  /// <summary>
  /// Helper function for delaying events and other timed occurences. 
  /// Not used internally.
  /// </summary>
  public class RailDelayList<T>
    where T : class, IRailTimedValue, IRailListNode<T>
  {
    private RailList<T> list;

    public int Count { get { return this.list.Count; } }
    public T Newest { get { return this.list.Last; } }
    public T Oldest { get { return this.list.First; } }

    public void Clear(Action<T> cleanup)
    {
      if (cleanup != null)
        this.list.ForEach(cleanup);
      this.list.Clear();
    }

    public void ForEach(Action<T> action)
    {
      this.list.ForEach(action);
    }

    public RailDelayList()
      : base()
    {
      this.list = new RailList<T>();
    }

    /// <summary>
    /// Inserts a value in the buffer. Allows for duplicate ticks.
    /// </summary>
    public void Insert(T value)
    {
      T iter = this.list.First;
      if (iter == null)
      {
        this.list.Add(value);
      }
      else
      {
        while (iter != null)
        {
          if (iter.Tick >= value.Tick)
          {
            this.list.InsertBefore(iter, value);
            return;
          }
          iter = this.list.GetNext(iter);
        }
        this.list.Add(value);
      }
    }

    /// <summary>
    /// Removes all elements older than the given tick.
    /// </summary>
    public IEnumerable<T> Drain(Tick tick)
    {
      while (this.list.First != null)
      {
        if (this.list.First.Tick <= tick)
          yield return this.list.RemoveFirst();
        else
          break;
      }
    }
  }
}