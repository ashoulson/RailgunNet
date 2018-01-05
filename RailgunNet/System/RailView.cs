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
  internal struct RailViewEntry
  {
    internal static readonly RailViewEntry INVALID = 
      new RailViewEntry(Tick.INVALID, Tick.INVALID, true);

    internal bool IsValid { get { return this.lastReceivedTick.IsValid; } }
    internal Tick LastReceivedTick { get { return this.lastReceivedTick; } }
    internal Tick LocalUpdateTick { get { return this.localUpdateTick; } }
    internal bool IsFrozen { get { return this.isFrozen; } }

    private readonly Tick lastReceivedTick;
    private readonly Tick localUpdateTick;
    private readonly bool isFrozen;

    public RailViewEntry(
      Tick lastReceivedTick, 
      Tick localUpdateTick,
      bool isFrozen)
    {
      this.lastReceivedTick = lastReceivedTick;
      this.localUpdateTick = localUpdateTick;
      this.isFrozen = isFrozen;
    }
  }

  internal class RailView
  {
    private class ViewComparer :
      Comparer<KeyValuePair<EntityId, RailViewEntry>>
    {
      private readonly Comparer<Tick> comparer;

      public ViewComparer()
      {
        this.comparer = Tick.CreateComparer();
      }

      public override int Compare(
        KeyValuePair<EntityId, RailViewEntry> x, 
        KeyValuePair<EntityId, RailViewEntry> y)
      {
        return this.comparer.Compare(
          x.Value.LastReceivedTick, 
          y.Value.LastReceivedTick);
      }
    }

    private readonly ViewComparer viewComparer;
    private readonly Dictionary<EntityId, RailViewEntry> latestUpdates;
    private readonly List<KeyValuePair<EntityId, RailViewEntry>> sortList;

    public RailView()
    {
      this.viewComparer = new ViewComparer();
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
    internal void RecordUpdate(
      EntityId entityId, 
      Tick receivedTick, 
      Tick localTick, 
      bool isFrozen)
    {
      this.RecordUpdate(
        entityId, 
        new RailViewEntry(receivedTick, localTick, isFrozen));
    }

    /// <summary>
    /// Records an acked status from the peer for a given entity ID.
    /// </summary>
    internal void RecordUpdate(
      EntityId entityId,
      RailViewEntry entry)
    {
      RailViewEntry currentEntry;
      if (this.latestUpdates.TryGetValue(entityId, out currentEntry))
        if (currentEntry.LastReceivedTick > entry.LastReceivedTick)
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
    public IEnumerable<KeyValuePair<EntityId, RailViewEntry>> GetOrdered(
      Tick localTick)
    {
      this.sortList.Clear();
      foreach (var pair in this.latestUpdates)
        // If we haven't received an update on an entity for too long, don't
        // bother sending a view for it (the server will update us eventually)
        if (localTick - pair.Value.LocalUpdateTick < RailConfig.VIEW_TICKS)
          this.sortList.Add(pair);

      this.sortList.Sort(this.viewComparer);
      this.sortList.Reverse();
      return this.sortList;
    }
  }
}
