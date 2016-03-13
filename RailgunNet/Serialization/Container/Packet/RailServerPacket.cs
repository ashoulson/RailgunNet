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

using System;
using System.Collections;
using System.Collections.Generic;

using CommonTools;

namespace Railgun
{

  /// <summary>
  /// Packet sent from client to server
  /// </summary>
  internal class RailServerPacket : IRailPoolable, IRailRingValue
  {
    /// <summary>
    /// If we can't fit an entity into a packet, this determines how many of
    /// the following entities we will try before giving up.
    /// </summary>
    internal const int RETRY_ATTEMPTS = 3;

    /// <summary>
    /// Maximum size for a single entity. We skip entities larger than this.
    /// </summary>
    internal const int MAX_ENTITY_SIZE = 100;

    /// <summary>
    /// Number of bytes reserved in the packet when determining how full it is.
    /// </summary>
    internal const int RESERVED_BYTES = 10;
    internal const int MAX_PACKET_SIZE =
      RailConfig.MAX_MESSAGE_SIZE - RailServerPacket.RESERVED_BYTES;

    private static void WarnTooBig(EntityUpdate update, int size)
    {
      CommonDebug.LogWarning(
        "Entity too big: " + 
        update.Entity.Id + 
        "(" + 
        update.Entity.Type + 
        ") -- " +
        size + "B");
    }

    private struct EntityUpdate
    {
      internal RailEntity Entity { get { return this.entity; } }
      internal Tick BasisTick { get { return this.basisTick; } }

      private readonly RailEntity entity;
      private readonly Tick basisTick;

      internal EntityUpdate(RailEntity entity, Tick basisTick)
      {
        this.entity = entity;
        this.basisTick = basisTick;
      }
    }

    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }
    Tick IRailRingValue.Tick { get { return this.LatestTick; } }

    internal Tick LatestTick { get; private set; }
    internal Tick LastProcessedCommandTick { get; private set; }

    internal IEnumerable<RailEvent> Events { get { return this.events; } }

    private readonly List<RailEvent> events;

    // Server-only
    internal List<EntityId> SentEntities { get; private set; }
    private readonly List<EntityUpdate> pendingUpdates;

    // Client-only
    internal List<RailState> States { get; private set; }

    public RailServerPacket()
    {
      this.events = new List<RailEvent>();
      this.SentEntities = new List<EntityId>();
      this.pendingUpdates = new List<EntityUpdate>();
      this.States = new List<RailState>();

      this.Reset();
    }

    internal void Initialize(
      Tick latestTick,
      Tick lastProcessedCommandTick,
      IEnumerable<RailEvent> events)
    {
      this.LatestTick = latestTick;
      this.LastProcessedCommandTick = lastProcessedCommandTick;
      this.events.AddRange(events);
    }

    internal void AddEntity(RailEntity entity, Tick basisTick)
    {
      this.pendingUpdates.Add(new EntityUpdate(entity, basisTick));
    }

    protected void Reset()
    {
      this.LatestTick = Tick.INVALID;
      this.LastProcessedCommandTick = Tick.INVALID;

      this.events.Clear();
      this.pendingUpdates.Clear();
      this.States.Clear();
    }

    #region Encode/Decode
    internal void Encode(
      BitBuffer buffer,
      IRailController destination)
    {
      // Write: [Events]
      this.EncodeEvents(buffer);

      // Write: [Entities]
      this.EncodeEntities(buffer, destination);

      // Write: [LastProcessedCommandTick]
      buffer.Push(RailEncoders.Tick, this.LastProcessedCommandTick);

      // Write: [LatestTick]
      buffer.Push(RailEncoders.Tick, this.LatestTick);

      CommonDebug.Assert(buffer.ByteSize <= RailConfig.MAX_MESSAGE_SIZE);
    }

    internal static RailServerPacket Decode(
      BitBuffer buffer,
      IDictionary<EntityId, RailEntity> knownEntities)
    {
      RailServerPacket packet = RailResource.Instance.AllocateServerPacket();

      // Read: [LatestTick]
      packet.LatestTick = buffer.Pop(RailEncoders.Tick);

      // Read: [LastProcessedCommandTick]
      packet.LastProcessedCommandTick = buffer.Pop(RailEncoders.Tick);

      // Read: [Entities]
      packet.DecodeEntities(buffer, knownEntities, packet.LatestTick);

      // Read: [Events]
      packet.DecodeEvents(buffer);

      return packet;
    }

    private void EncodeEvents(BitBuffer buffer)
    {
      // Write: [Events]
      foreach (RailEvent evnt in this.events)
        evnt.Encode(buffer);

      // Write: [EventCount]
      buffer.Push(RailEncoders.EventCount, this.events.Count);
    }

    private void DecodeEvents(BitBuffer buffer)
    {
      // TODO: Cap the number of event sends

      // Read: [EventCount]
      int eventCount = buffer.Pop(RailEncoders.EventCount);

      // Read: [Events]
      for (int i = 0; i < eventCount; i++)
        this.events.Add(RailEvent.Decode(buffer));

      // We need to reverse the events to restore the send order
      this.events.Reverse();
    }

    private void EncodeEntities(
      BitBuffer buffer,
      IRailController destination)
    {
      // Write: [Entity States]
      foreach (EntityUpdate pair in this.pendingUpdates)
      {
        buffer.SetRollback();
        int beforeSize = buffer.ByteSize;

        pair.Entity.EncodeState(
          buffer,
          this.LatestTick,
          pair.BasisTick,
          destination);

        int byteCost = buffer.ByteSize - beforeSize;
        if (byteCost > MAX_ENTITY_SIZE)
        {
          buffer.Rollback();
          RailServerPacket.WarnTooBig(pair, byteCost);
        }
        else if (buffer.ByteSize > RailServerPacket.MAX_PACKET_SIZE)
        {
          buffer.Rollback();
          break;
        }
        else
        {
          this.SentEntities.Add(pair.Entity.Id);
        }
      }

      // Write: [Entity Count]
      buffer.Push(RailEncoders.EntityCount, this.SentEntities.Count);
    }

    private void DecodeEntities(
      BitBuffer buffer,
      IDictionary<EntityId, RailEntity> knownEntities,
      Tick latestTick)
    {
      // Read: [Entity Count]
      int count = buffer.Pop(RailEncoders.EntityCount);

      // Read: [Entity States]
      RailState state = null;
      for (int i = 0; i < count; i++)
      {
        try
        {
          state = 
            RailEntity.DecodeState(
              buffer,
              latestTick, 
              knownEntities);
          if (state != null)
            this.States.Add(state);
        }
        catch (BasisNotFoundException bnfe)
        {
          CommonDebug.LogWarning(bnfe);
          break;
        }
      }
    }
    #endregion
  }
}
