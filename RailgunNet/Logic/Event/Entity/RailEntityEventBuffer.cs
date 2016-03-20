using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class RailEntityEventBuffer
  {
    private class RailEventComparer : Comparer<RailEvent>
    {
      public override int Compare(RailEvent x, RailEvent y)
      {
        return Tick.Comparer.Compare(x.Tick, y.Tick);
      }
    }

    private static RailEventComparer Comparer = new RailEventComparer();

    private MinHeap<RailEvent> events;
    private HashSet<EventId> containedIds;

    private Tick latest;

    public RailEntityEventBuffer()
    {
      this.events = new MinHeap<RailEvent>(RailEntityEventBuffer.Comparer);
      this.containedIds = new HashSet<EventId>(EventId.Comparer);
      this.latest = Tick.INVALID;
    }

    public IEnumerable<RailEvent> Advance(Tick latest, int maxAge)
    {
      if (this.latest.IsValid && (this.latest > latest))
        yield break;

      while (this.events.Count > 0)
      {
        RailEvent evnt = this.events.PeekFirst();
        if (evnt.Tick > latest)
          break;

        this.containedIds.Remove(evnt.EventId);
        this.events.PopFirst();

        if ((latest - evnt.Tick) <= maxAge)
          yield return evnt;
        RailPool.Free(evnt);
      }

      this.latest = latest;
    }

    public void AddEvent(RailEvent evnt)
    {
      if (this.containedIds.Contains(evnt.EventId))
        return;

      this.containedIds.Add(evnt.EventId);
      this.events.Add(evnt);
    }
  }
}
