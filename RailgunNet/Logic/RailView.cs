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
  internal struct RailViewEntry
  {
    internal static readonly RailViewEntry INVALID = 
      new RailViewEntry(Tick.INVALID, true);

    internal bool IsValid { get { return this.tick.IsValid; } }
    internal Tick Tick { get { return this.tick; } }
    internal bool IsFrozen { get { return this.isFrozen; } }

    private readonly Tick tick;
    private readonly bool isFrozen;

    public RailViewEntry(Tick tick, bool isFrozen)
    {
      this.tick = tick;
      this.isFrozen = isFrozen;
    }
  }

  internal class RailView
  {
    private class ViewComparer :
      Comparer<KeyValuePair<EntityId, RailViewEntry>>
    {
      private static readonly Comparer<Tick> Comparer = Tick.Comparer;

      public override int Compare(
        KeyValuePair<EntityId, RailViewEntry> x, 
        KeyValuePair<EntityId, RailViewEntry> y)
      {
        return ViewComparer.Comparer.Compare(x.Value.Tick, y.Value.Tick);
      }
    }

    private static readonly ViewComparer Comparer = new ViewComparer();

    private readonly Dictionary<EntityId, RailViewEntry> latestUpdates;
    private readonly List<KeyValuePair<EntityId, RailViewEntry>> sortList;

    public RailView()
    {
      this.latestUpdates = new Dictionary<EntityId, RailViewEntry>();
      this.sortList = new List<KeyValuePair<EntityId, RailViewEntry>>();
    }

    /// <summary>
    /// Returns the latest tick the peer has acked for this entity ID.
    /// </summary>
    public RailViewEntry GetLatest(EntityId id)
    {
      RailViewEntry result;
      if (this.latestUpdates.TryGetValue(id, out result))
        return result;
      return RailViewEntry.INVALID;
    }

    public void Clear()
    {
      this.latestUpdates.Clear();
    }

    /// <summary>
    /// Records an acked status from the peer for a given entity ID.
    /// </summary>
    internal void RecordUpdate(EntityId entityId, Tick tick, bool isFrozen)
    {
      this.RecordUpdate(entityId, new RailViewEntry(tick, isFrozen));
    }

    /// <summary>
    /// Records an acked status from the peer for a given entity ID.
    /// </summary>
    internal void RecordUpdate(EntityId entityId, RailViewEntry entry)
    {
      RailViewEntry currentEntry;
      if (this.latestUpdates.TryGetValue(entityId, out currentEntry))
        if (currentEntry.Tick > entry.Tick)
          return;

      this.latestUpdates[entityId] = entry;
    }

    public void Integrate(RailView other)
    {
      foreach (KeyValuePair<EntityId, RailViewEntry> pair in other.latestUpdates)
        this.RecordUpdate(pair.Key, pair.Value);
    }

    /// <summary>
    /// Views sort in descending tick order. When sending a view to the server
    /// we send the most recent updated entities since they're the most likely
    /// to actually matter to the server/client scope.
    /// </summary>
    public IEnumerable<KeyValuePair<EntityId, RailViewEntry>> GetOrdered()
    {
      // TODO: If we have an entity frozen, we probably shouldn't constantly
      //       send view acks for it unless we're getting requests to freeze.
      this.sortList.Clear();
      this.sortList.AddRange(this.latestUpdates);
      this.sortList.Sort(RailView.Comparer);
      this.sortList.Reverse();
      return this.sortList;
    }
  }
}
