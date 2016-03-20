using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal class RailEventReaderReliable
  {
    public EventId LastReadEventId { get { return this.lastReadEventId; } }

    private EventId lastReadEventId;

    public RailEventReaderReliable()
    {
      this.lastReadEventId = EventId.START;
    }

    /// <summary>
    /// Gets all events that we haven't processed yet, in order with no gaps.
    /// </summary>
    public IEnumerable<RailEvent> Filter(IEnumerable<RailEvent> events)
    {
      foreach (RailEvent evnt in events)
      {
        bool isExpected =
          (this.lastReadEventId.IsValid == false) ||
          (this.lastReadEventId.Next == evnt.EventId);

        if (isExpected)
        {
          this.lastReadEventId = this.lastReadEventId.Next;
          yield return evnt;
        }
      }
    }
  }
}
