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
  public class RailServerPacket : RailPacket, IRailPoolable
  {
    /// <summary>
    /// Maximum size for a single entity. We skip entities larger than this.
    /// </summary>
    internal const int MAX_ENTITY_SIZE = 100;

    /// <summary>
    /// Maximum size for this packet when sending. Used for scoping.
    /// </summary>
    internal const int MAX_PACKET_SIZE = RailConfig.MAX_MESSAGE_SIZE;

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

    internal Tick LastProcessedCommandTick { get; private set; }

    // Server-only
    internal List<EntityId> SentEntities { get; private set; }
    private readonly List<EntityUpdate> pendingUpdates;

    // Client-only
    internal List<RailState> States { get; private set; }

    public RailServerPacket() : base()
    {
      this.SentEntities = new List<EntityId>();
      this.pendingUpdates = new List<EntityUpdate>();
      this.States = new List<RailState>();

      this.Reset();
    }

    internal void InitializeServer(
      Tick lastProcessedCommandTick)
    {
      this.LastProcessedCommandTick = lastProcessedCommandTick;
    }

    internal void AddEntity(RailEntity entity, Tick basisTick)
    {
      this.pendingUpdates.Add(new EntityUpdate(entity, basisTick));
    }

    protected override void Reset()
    {
      base.Reset();

      this.pendingUpdates.Clear();
      this.States.Clear();
    }

    #region Encode/Decode
    internal void Encode(
      BitBuffer buffer,
      IRailController destination)
    {
      // Write: [Header]
      this.EncodeHeader(buffer);

      // Write: [LastProcessedCommandTick]
      buffer.Write(RailEncoders.Tick, this.LastProcessedCommandTick);

      // Write: [Events]
      this.EncodeEvents(buffer);

      // Write: [Entities]
      this.EncodeEntities(buffer, destination);

      CommonDebug.Assert(buffer.ByteSize <= RailConfig.MAX_MESSAGE_SIZE);
    }

    internal static RailServerPacket Decode(
      BitBuffer buffer,
      IDictionary<EntityId, RailEntity> knownEntities)
    {
      RailServerPacket packet = RailResource.Instance.AllocateServerPacket();

      // Read: [Header]
      packet.DecodeHeader(buffer);

      // Read: [LastProcessedCommandTick]
      packet.LastProcessedCommandTick = buffer.Read(RailEncoders.Tick);

      // Read: [Events]
      packet.DecodeEvents(buffer);

      // Read: [Entities]
      packet.DecodeEntities(buffer, knownEntities, packet.SenderTick);

      CommonDebug.Assert(buffer.IsFinished);
      return packet;
    }

    #region Entities
    private void EncodeEntities(
      BitBuffer buffer,
      IRailController destination)
    {
      // Reserve: [Entity Count]
      buffer.Reserve(RailEncoders.EntityCount);

      // Write: [Entity States]
      foreach (EntityUpdate pair in this.pendingUpdates)
      {
        buffer.SetRollback();
        int beforeSize = buffer.ByteSize;

        pair.Entity.EncodeState(
          buffer,
          this.SenderTick,
          pair.BasisTick,
          destination);

        int byteCost = buffer.ByteSize - beforeSize;
        if (byteCost > MAX_ENTITY_SIZE)
        {
          buffer.Rollback();
          CommonDebug.LogWarning(
            "Entity too big: " +
            pair.Entity.Id +
            "(" +
            pair.Entity.Type +
            ") -- " +
            byteCost + "B");
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

      // Reserved Write: [Entity Count]
      buffer.WriteReserved(RailEncoders.EntityCount, this.SentEntities.Count);
    }

    private void DecodeEntities(
      BitBuffer buffer,
      IDictionary<EntityId, RailEntity> knownEntities,
      Tick latestTick)
    {
      // Read: [Entity Count]
      int count = buffer.Read(RailEncoders.EntityCount);

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

    #endregion
  }
}
