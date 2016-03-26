﻿/*
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

using System;
using System.Collections.Generic;

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

    private readonly List<RailEvent> pendingEvents;
    private readonly List<RailEvent> events;

    private int eventsWritten;

    public RailPacket()
    {
      this.senderTick = Tick.INVALID;
      this.ackTick = Tick.INVALID;
      this.ackEventId = EventId.INVALID;

      this.pendingEvents = new List<RailEvent>();
      this.events = new List<RailEvent>();

      this.eventsWritten = 0;
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
      this.pendingEvents.AddRange(events);
    }

    protected virtual void Reset()
    {
      this.senderTick = Tick.INVALID;
      this.ackTick = Tick.INVALID;
      this.ackEventId = EventId.INVALID;

      this.pendingEvents.Clear();
      this.events.Clear();

      this.eventsWritten = 0;
    }

    #region Encoding/Decoding
    /// <summary>
    /// After writing the header we write the packet data in three passes.
    /// The first pass is a fill of events up to a percentage of the packet.
    /// The second pass is the payload value, which will try to fill the
    /// remaining packet space. If more space is available, we will try
    /// to fill it with any remaining events, up to the maximum packet size.
    /// </summary>
    public void Encode(BitBuffer buffer)
    {
      // Write: [Header]
      this.EncodeHeader(buffer);

      // Write: [Payload]
      this.EncodePayload(buffer);
    }

    internal void Decode(BitBuffer buffer)
    {
      // Write: [Header]
      this.DecodeHeader(buffer);

      // Write: [Payload]
      this.DecodePayload(buffer);
    }

    protected abstract void EncodePayload(BitBuffer buffer);
    protected abstract void DecodePayload(BitBuffer buffer);

    #region Header
    private void EncodeHeader(BitBuffer buffer)
    {
      // Write: [LocalTick]
      buffer.WriteTick(this.senderTick);

      // Write: [AckTick]
      buffer.WriteTick(this.ackTick);

      // Write: [AckEventId]
      buffer.WriteEventId(this.ackEventId);
    }

    private void DecodeHeader(BitBuffer buffer)
    {
      // Read: [LocalTick]
      this.senderTick = buffer.ReadTick();

      // Read: [AckTick]
      this.ackTick = buffer.ReadTick();

      // Read: [AckEventId]
      this.ackEventId = buffer.ReadEventId();
    }

    #endregion

    #region Events
    ///// <summary>
    ///// Writes as many events as possible up to maxSize and returns the number
    ///// of events written in the batch. Also increments the total counter.
    ///// </summary>
    //private void PartialEncodeEvents(
    //  BitBuffer buffer, 
    //  int keyReserve, 
    //  int maxSize)
    //{
    //  // Count slot is already reserved prior to calling
      
    //  IEnumerable<RailEvent> packed =
    //    buffer.PackToSize<RailEvent>(
    //      keyReserve,
    //      RailPacket.KEY_ROLLBACK,
    //      RailEncoders.EventCount,
    //      this.GetWritableEvents(),
    //      maxSize,
    //      this.WriteEvent);

    //  foreach (RailEvent evnt in packed)
    //    this.eventsWritten++;
    //}

    //private void PartialDecodeEvents(
    //  BitBuffer buffer, 
    //  int count,
    //  IRailLookup<EntityId, RailEntity> entityLookup)
    //{
    //  // Read: [Events]
    //  for (int i = 0; i < count; i++)
    //  {
    //    RailEvent read = 
    //      RailEvent.Decode(buffer, this.senderTick, entityLookup);
    //    if (read != null)
    //      this.events.Add(read);
    //  }
    //}

    //private IEnumerable<RailEvent> GetWritableEvents()
    //{
    //  while (this.eventsWritten < this.pendingEvents.Count)
    //    yield return this.pendingEvents[this.eventsWritten];
    //}

    //private void WriteEvent(BitBuffer buffer, RailEvent evnt)
    //{
    //  // Write: [Event]
    //  evnt.Encode(buffer, this.senderTick);
    //}
    #endregion
    #endregion
  }
}
