using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class RailEntityEventWriter
  {
    private static bool IsValid(RailEvent evnt, Tick latest)
    {
      int numRetries = evnt.NumRetries;
      if ((numRetries == RailEvent.UNLIMITED) || (numRetries > 0))
        return true;

      int maxAge = evnt.MaximumAge;
      if ((maxAge == RailEvent.UNLIMITED) || (latest - evnt.Tick) <= maxAge)
        return true;

      return false;
    }

    /// <summary>
    /// A rolling queue for outgoing events, in order.
    /// </summary>
    private readonly Queue<RailEvent> outgoingEvents;

    /// <summary>
    /// Used for uniquely identifying and ordering events.
    /// </summary>
    private EventId lastEventId;

    public RailEntityEventWriter()
    {
      this.outgoingEvents = new Queue<RailEvent>();

      // We pretend that one event has already been transmitted
      this.lastEventId = EventId.START.Next;
    }

    /// <summary>
    /// Queues an event for sending.
    /// </summary>
    public void QueueEvent(RailEvent evnt, int numRetries, int maxAge)
    {
      RailEvent clone = evnt.Clone();
      clone.NumRetries = numRetries;
      clone.MaximumAge = maxAge;
      clone.EventId = this.lastEventId;
      this.outgoingEvents.Enqueue(clone);
      this.lastEventId = this.lastEventId.Next;
    }

    /// <summary>
    /// Cleans the outgoing queue for all events that have been acked.
    /// </summary>
    public void CleanOutgoing(Tick latest)
    {
      while (this.outgoingEvents.Count > 0)
      {
        RailEvent top = this.outgoingEvents.Peek();
        if (RailEntityEventWriter.IsValid(top, latest))
          break;
        RailPool.Free(this.outgoingEvents.Dequeue());
      }
    }

    /// <summary>
    /// Gets all outgoing events.
    /// </summary>
    public IEnumerable<RailEvent> GetOutgoing(Tick latest)
    {
      foreach (RailEvent evnt in this.outgoingEvents)
        if (RailEntityEventWriter.IsValid(evnt, latest))
          yield return evnt;
    }
  }
}
