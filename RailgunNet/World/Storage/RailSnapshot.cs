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

using CommonTools;

namespace Railgun
{
  /// <summary>
  /// A snapshot is a collection of images representing a complete state
  /// of the world at a given frame.
  /// </summary>
  public class RailSnapshot 
    : RailRecordCollection<RailImage>, IPoolable, IRingValue
  {
    Pool IPoolable.Pool { get; set; }
    void IPoolable.Reset() { this.Reset(); }
    int IRingValue.Key { get { return this.Tick; } }

    public int Tick { get; internal protected set; }

    public RailSnapshot()
    {
      this.Tick = RailClock.INVALID_TICK;
    }

    /// <summary>
    /// Deep-copies this Snapshot, allocating from the pool in the process.
    /// </summary>
    internal RailSnapshot Clone()
    {
      RailSnapshot clone = RailResource.Instance.AllocateSnapshot();
      clone.Tick = this.Tick;
      foreach (RailImage image in this.Entries.Values)
        clone.Add(image.Clone());
      return clone;
    }

    protected virtual void Reset()
    {
      foreach (RailImage image in this.Entries.Values)
        Pool.Free(image);
      this.Entries.Clear();
    }
  }
}
