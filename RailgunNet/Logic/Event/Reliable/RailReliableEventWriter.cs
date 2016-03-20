using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class RailReliableEventWriter
  {
    /// <summary>
    /// A rolling queue for outgoing events, in order.
    /// </summary>
    private readonly Queue<RailEvent> outgoingEvents;

    /// <summary>
    /// Used for uniquely identifying and ordering events.
    /// </summary>
    private EventId lastEventId;

    public RailReliableEventWriter()
    {
      this.outgoingEvents = new Queue<RailEvent>();

      // We pretend that one event has already been transmitted
      this.lastEventId = EventId.START.Next;
    }

    /// <summary>
    /// Queues an event for sending.
    /// </summary>
    public void QueueEvent(RailEvent evnt)
    {
      RailEvent clone = evnt.Clone();
      clone.EventId = this.lastEventId;
      this.outgoingEvents.Enqueue(clone);
      this.lastEventId = this.lastEventId.Next;
    }

    /// <summary>
    /// Cleans the outgoing queue for all events that have been acked.
    /// </summary>
    public void CleanOutgoing(EventId highestAckedId)
    {
      if (highestAckedId.IsValid == false)
        return;

      while (this.outgoingEvents.Count > 0)
      {
        RailEvent top = this.outgoingEvents.Peek();
        if (top.EventId > highestAckedId)
          break;
        RailPool.Free(this.outgoingEvents.Dequeue());
      }
    }

    /// <summary>
    /// Gets all outgoing events.
    /// </summary>
    public IEnumerable<RailEvent> GetOutgoing()
    {
      foreach (RailEvent evnt in this.outgoingEvents)
        yield return evnt;
    }
  }
}
