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
  interface IRailPacket
  {
    void Encode(RailBitBuffer buffer);
  }

  internal abstract class RailPacket 
    : IRailPoolable<RailPacket>
    , IRailPacket
  {
    #region Pooling
    IRailPool<RailPacket> IRailPoolable<RailPacket>.Pool { get; set; }
    void IRailPoolable<RailPacket>.Reset() { this.Reset(); }
    #endregion

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
    internal SequenceId AckEventId { get { return this.ackEventId; } }

    /// <summary>
    /// Global reliable events from the sender, in order.
    /// </summary>
    internal IEnumerable<RailEvent> Events { get { return this.events; } }

    private Tick senderTick;
    private Tick ackTick;
    private SequenceId ackEventId;

    private readonly List<RailEvent> pendingEvents;
    private readonly List<RailEvent> events;

    private int eventsWritten;

    public RailPacket()
    {
      this.senderTick = Tick.INVALID;
      this.ackTick = Tick.INVALID;
      this.ackEventId = SequenceId.INVALID;

      this.pendingEvents = new List<RailEvent>();
      this.events = new List<RailEvent>();
      this.eventsWritten = 0;
    }

    internal void Initialize(
      Tick senderTick,
      Tick ackTick,
      SequenceId ackEventId,
      IEnumerable<RailEvent> events)
    {
      this.senderTick = senderTick;
      this.ackTick = ackTick;
      this.ackEventId = ackEventId;
      this.pendingEvents.AddRange(events);
      this.eventsWritten = 0;
    }

    internal virtual void Reset()
    {
      this.senderTick = Tick.INVALID;
      this.ackTick = Tick.INVALID;
      this.ackEventId = SequenceId.INVALID;

      this.pendingEvents.Clear();
      this.events.Clear();
      this.eventsWritten = 0;
    }

    #region Encoding/Decoding
    protected abstract void EncodePayload(RailBitBuffer buffer, int reservedBytes);
    protected abstract void DecodePayload(RailBitBuffer buffer);

    /// <summary>
    /// After writing the header we write the packet data in three passes.
    /// The first pass is a fill of events up to a percentage of the packet.
    /// The second pass is the payload value, which will try to fill the
    /// remaining packet space. If more space is available, we will try
    /// to fill it with any remaining events, up to the maximum packet size.
    /// </summary>
    public void Encode(RailBitBuffer buffer)
    {
      // Write: [Header]
      this.EncodeHeader(buffer);

      // Write: [Events] (Early Pack)
      this.EncodeEvents(buffer, RailConfig.PACKCAP_EARLY_EVENTS);

      // Write: [Payload]
      this.EncodePayload(buffer, 1); // Leave one byte for the event count

      // Write: [Events] (Fill Pack)
      this.EncodeEvents(buffer, RailConfig.PACKCAP_MESSAGE_TOTAL);
    }

    internal void Decode(RailBitBuffer buffer)
    {
      // Read: [Header]
      this.DecodeHeader(buffer);

      // Read: [Events] (Early Pack)
      this.DecodeEvents(buffer);

      // Read: [Payload]
      this.DecodePayload(buffer);

      // Read: [Events] (Fill Pack)
      this.DecodeEvents(buffer);
    }

    #region Header
    private void EncodeHeader(RailBitBuffer buffer)
    {
      // Write: [LocalTick]
      buffer.WriteTick(this.senderTick);

      // Write: [AckTick]
      buffer.WriteTick(this.ackTick);

      // Write: [AckReliableEventId]
      buffer.WriteSequenceId(this.ackEventId);
    }

    private void DecodeHeader(RailBitBuffer buffer)
    {
      // Read: [LocalTick]
      this.senderTick = buffer.ReadTick();

      // Read: [AckTick]
      this.ackTick = buffer.ReadTick();

      // Read: [AckReliableEventId]
      this.ackEventId = buffer.ReadSequenceId();
    }

    #endregion

    #region Events
    /// <summary>
    /// Writes as many events as possible up to maxSize and returns the number
    /// of events written in the batch. Also increments the total counter.
    /// </summary>
    private void EncodeEvents(
      RailBitBuffer buffer,
      int maxSize)
    {
      this.eventsWritten +=
        buffer.PackToSize(
          maxSize,
          RailConfig.MAXSIZE_EVENT,
          this.GetNextEvents(),
          (evnt) => evnt.Encode(buffer, this.senderTick),
          (evnt) => evnt.RegisterSent());
    }

    private void DecodeEvents(
      RailBitBuffer buffer)
    {
      IEnumerable<RailEvent> decoded =
        buffer.UnpackAll(
          () => RailEvent.Decode(buffer, this.SenderTick));
      foreach (RailEvent evnt in decoded)
        this.events.Add(evnt);
    }

    private IEnumerable<RailEvent> GetNextEvents()
    {
      for (int i = this.eventsWritten; i < this.pendingEvents.Count; i++)
        yield return this.pendingEvents[i];
    }
    #endregion
    #endregion
  }
}
