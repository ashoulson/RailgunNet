using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class RailEventWriter
  {
    /// <summary>
    /// A rolling queue for outgoing events, in order.
    /// </summary>
    private readonly Queue<RailEvent> outgoingEvents;

    /// <summary>
    /// Used for uniquely identifying and ordering events.
    /// </summary>
    private EventId lastEventId;

    public RailEventWriter()
    {
      this.outgoingEvents = new Queue<RailEvent>();

      // We pretend that one event has already been transmitted
      this.lastEventId = EventId.START.Next;
    }

    /// <summary>
    /// Queues an event for sending.
    /// </summary>
    public void QueueEvent(
      RailEvent evnt, 
      int numRetries = RailEvent.UNLIMITED)
    {
      RailEvent clone = evnt.Clone();
      clone.NumRetries = numRetries;
      clone.EventId = this.lastEventId;
      this.outgoingEvents.Enqueue(clone);
      this.lastEventId = this.lastEventId.Next;
    }

    /// <summary>
    /// Registers that an event has been sent, lowering any retry counters.
    /// </summary>
    public void RegisterSent(EventId highestSentId)
    {
      foreach (RailEvent evnt in this.outgoingEvents)
        if (evnt.EventId <= highestSentId)
          evnt.NumRetries -= 1;
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
    /// Gets all outgoing events with remaining retries that are newer than
    /// the given oldest tick.
    /// </summary>
    public IEnumerable<RailEvent> GetOutgoing(Tick oldestTick)
    {
      foreach (RailEvent evnt in this.outgoingEvents)
        if ((evnt.NumRetries == RailEvent.UNLIMITED) || (evnt.NumRetries > 0))
          if ((oldestTick.IsValid == false) || (evnt.Tick > oldestTick))
            yield return evnt;
    }

    /// <summary>
    /// Gets all outgoing events with remaining retries.
    /// </summary>
    public IEnumerable<RailEvent> GetOutgoing()
    {
      return GetOutgoing(Tick.INVALID);
    }
  }
}
