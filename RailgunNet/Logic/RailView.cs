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
  internal class RailView
  {
    private class ViewComparer :
      Comparer<KeyValuePair<EntityId, Tick>>
    {
      private static readonly Comparer<Tick> Comparer = Tick.Comparer;

      public override int Compare(
        KeyValuePair<EntityId, Tick> x, 
        KeyValuePair<EntityId, Tick> y)
      {
        return ViewComparer.Comparer.Compare(x.Value, y.Value);
      }
    }

    private static readonly ViewComparer Comparer = new ViewComparer();

    private Dictionary<EntityId, Tick> latestUpdates;

    public RailView()
    {
      this.latestUpdates = new Dictionary<EntityId, Tick>();
    }

    /// <summary>
    /// Returns the latest tick the peer has acked for this entity ID.
    /// </summary>
    public Tick GetLatest(EntityId id)
    {
      Tick result;
      if (this.latestUpdates.TryGetValue(id, out result))
        return result;
      return Tick.INVALID;
    }

    public void Clear()
    {
      this.latestUpdates.Clear();
    }

    /// <summary>
    /// Records an acked tick from the peer for a given entity ID.
    /// </summary>
    public void RecordUpdate(EntityId id, Tick tick)
    {
      Tick currentTick;
      if (this.latestUpdates.TryGetValue(id, out currentTick))
        if (currentTick > tick)
          return;
      this.latestUpdates[id] = tick;
    }

    public void Integrate(RailView other)
    {
      foreach (KeyValuePair<EntityId, Tick> pair in other.latestUpdates)
        this.RecordUpdate(pair.Key, pair.Value);
    }

    /// <summary>
    /// Views sort in descending tick order. When sending a view to the server
    /// we send the most recent updated entities since they're the most likely
    /// to actually matter to the server/client scope.
    /// </summary>
    public IEnumerable<KeyValuePair<EntityId, Tick>> GetOrdered()
    {
      List<KeyValuePair<EntityId, Tick>> values =
        new List<KeyValuePair<EntityId, Tick>>(this.latestUpdates);
      values.Sort(RailView.Comparer);
      values.Reverse();
      return values;
    }
  }
}
