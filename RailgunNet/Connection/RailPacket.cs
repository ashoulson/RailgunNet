using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using CommonTools;

namespace Railgun
{
  interface IRailPacket
  {
    void Encode(BitBuffer buffer);
  }

  internal abstract class RailPacket : IRailPacket
  {
    /// <summary>
    /// Minimum number of reliable events to send.
    /// </summary>
    internal const int MIN_EVENT_SEND = 3;

    /// <summary>
    /// The latest tick from the sender.
    /// </summary>
    internal Tick SenderTick { get { return this.senderTick; } }

    /// <summary>
    /// The last tick the sender received.
    /// </summary>
    internal Tick AckTick { get { return this.ackTick; } }

    /// <summary>
    /// The last global reliable event id the sender received.
    /// </summary>
    internal EventId AckEventId { get { return this.ackEventId; } }

    /// <summary>
    /// Global reliable events from the sender, in order.
    /// </summary>
    internal IEnumerable<RailEvent> Events { get { return this.events; } }

    private Tick senderTick;
    private Tick ackTick;
    private EventId ackEventId;

    private readonly List<RailEvent> pendingReliableEvents;
    private readonly List<RailEvent> events;

    public RailPacket()
    {
      this.senderTick = Tick.INVALID;
      this.ackTick = Tick.INVALID;
      this.ackEventId = EventId.INVALID;

      this.pendingReliableEvents = new List<RailEvent>();
      this.events = new List<RailEvent>();
    }

    internal void Initialize(
      Tick senderTick,
      Tick ackTick,
      EventId ackEventId,
      IEnumerable<RailEvent> events)
    {
      this.senderTick = senderTick;
      this.ackTick = ackTick;
      this.ackEventId = ackEventId;
      this.pendingReliableEvents.AddRange(events);
    }

    protected virtual void Reset()
    {
      this.senderTick = Tick.INVALID;
      this.ackTick = Tick.INVALID;
      this.ackEventId = EventId.INVALID;

      this.pendingReliableEvents.Clear();
      this.events.Clear();
    }

    #region Encoding/Decoding
    public void Encode(BitBuffer buffer)
    {
      // Write: [Header]
      this.EncodeHeader(buffer);

      // Write: [Events]
      this.EncodeEvents(buffer);

      // Write: [Payload]
      this.EncodePayload(buffer);

      // TODO: Second pass to pack remaining space with events
    }

    internal void Decode(BitBuffer buffer)
    {
      // Write: [Header]
      this.DecodeHeader(buffer);

      // Write: [Events]
      this.DecodeEvents(buffer);

      // Write: [Payload]
      this.DecodePayload(buffer);

      // TODO: Second pass to get extra packed events
    }

    protected abstract void EncodePayload(BitBuffer buffer);
    protected abstract void DecodePayload(BitBuffer buffer);

    #region Header
    protected void EncodeHeader(BitBuffer buffer)
    {
      // Write: [LocalTick]
      buffer.Write(RailEncoders.Tick, this.senderTick);

      // Write: [AckTick]
      buffer.Write(RailEncoders.Tick, this.ackTick);

      // Write: [AckEventId]
      buffer.Write(RailEncoders.EventId, this.ackEventId);
    }

    internal void DecodeHeader(BitBuffer buffer)
    {
      // Read: [LocalTick]
      this.senderTick = buffer.Read(RailEncoders.Tick);

      // Read: [AckedTick]
      this.ackTick = buffer.Read(RailEncoders.Tick);

      // Read: [AckEventId]
      this.ackEventId = buffer.Read(RailEncoders.EventId);
    }

    #endregion

    #region Events
    protected void EncodeEvents(BitBuffer buffer)
    {
      // TODO: Packing and maximum count

      // Write: [EventCount]
      buffer.Write(RailEncoders.EventCount, this.pendingReliableEvents.Count);

      // Write: [Events]
      foreach (RailEvent evnt in this.pendingReliableEvents)
        evnt.Encode(buffer);
    }

    protected void DecodeEvents(BitBuffer buffer)
    {
      // Read: [EventCount]
      int eventCount = buffer.Read(RailEncoders.EventCount);

      // Read: [Events]
      for (int i = 0; i < eventCount; i++)
        this.events.Add(RailEvent.Decode(buffer));
    }
    #endregion
    #endregion
  }
}
