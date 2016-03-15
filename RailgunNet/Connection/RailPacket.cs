using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public abstract class RailPacket
  {
    /// <summary>
    /// Minimum number of reliable events to send.
    /// </summary>
    internal const int MIN_EVENT_SEND = 3;

    internal IEnumerable<RailEvent> GlobalEvents 
    {
      get { return this.globalEvents; } 
    }

    internal Tick SenderTick { get; private set; }
    protected Tick AckedTick { get; private set; }
    protected EventId AckedEventId { get; private set; }

    private readonly List<RailEvent> pendingReliableEvents;
    private readonly List<RailEvent> globalEvents;

    public RailPacket()
    {
      this.pendingReliableEvents = new List<RailEvent>();
      this.globalEvents = new List<RailEvent>();

      this.SenderTick = Tick.INVALID;
      this.AckedTick = Tick.INVALID;
      this.AckedEventId = EventId.INVALID;
    }

    internal void Initialize(
      Tick senderTick,
      Tick lastReceivedTick,
      EventId lastReceivedEventId,
      IEnumerable<RailEvent> reliableEvents)
    {
      this.SenderTick = senderTick;
      this.AckedTick = lastReceivedTick;
      this.AckedEventId = lastReceivedEventId;
      this.pendingReliableEvents.AddRange(reliableEvents);
    }

    protected virtual void Reset()
    {
      this.SenderTick = Tick.INVALID;
      this.AckedTick = Tick.INVALID;
      this.AckedEventId = EventId.INVALID;

      this.pendingReliableEvents.Clear();
      this.globalEvents.Clear();
    }

    #region Encoding/Decoding

    #region Header
    protected void EncodeHeader(BitBuffer buffer)
    {
      // Write: [LocalTick]
      buffer.Write(RailEncoders.Tick, this.SenderTick);

      // Write: [AckedTick]
      buffer.Write(RailEncoders.Tick, this.AckedTick);
    }

    internal void DecodeHeader(BitBuffer buffer)
    {
      // Read: [LocalTick]
      this.SenderTick = buffer.Read(RailEncoders.Tick);

      // Read: [AckedTick]
      this.AckedTick = buffer.Read(RailEncoders.Tick);
    }
    #endregion

    #region Events
    protected void EncodeEvents(BitBuffer buffer)
    {
      // Write: [EventCount]
      buffer.Write(RailEncoders.EventCount, this.pendingReliableEvents.Count);

      // Write: [Events]
      foreach (RailEvent evnt in this.pendingReliableEvents)
        evnt.Encode(buffer);
    }

    protected void DecodeEvents(BitBuffer buffer)
    {
      // TODO: Cap the number of event sends

      // Read: [EventCount]
      int eventCount = buffer.Read(RailEncoders.EventCount);

      // Read: [Events]
      for (int i = 0; i < eventCount; i++)
        this.globalEvents.Add(RailEvent.Decode(buffer));
    }
    #endregion

    #endregion
  }
}
