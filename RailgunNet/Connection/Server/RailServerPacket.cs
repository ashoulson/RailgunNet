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
  interface IRailServerPacket : IRailPacket
  {
    IEnumerable<RailState> States { get; }
  }

  /// <summary>
  /// Packet sent from server to client.
  /// </summary>
  internal class RailServerPacket : 
    RailPacket, IRailServerPacket, IRailPoolable<RailServerPacket>
  {
    IRailPool<RailServerPacket> IRailPoolable<RailServerPacket>.Pool { get; set; }
    void IRailPoolable<RailServerPacket>.Reset() { this.Reset(); }

    /// <summary>
    /// Maximum size for a single entity. We skip entities larger than this.
    /// </summary>
    internal const int MAX_ENTITY_SIZE = 100;

    public IEnumerable<RailState> States 
    { 
      get { return this.states; }
    }

    private readonly List<RailState> states;

    // Values controlled by the peer
    internal Tick CommandTick { get; set; }
    internal IRailController Destination { private get; set; }
    internal IEnumerable<EntityId> SentIds { get { return this.sentIds; } }
    internal IDictionary<EntityId, RailEntity> Reference { private get; set; }

    // Input/output information for entity scoping
    private readonly List<KeyValuePair<RailEntity, Tick>> pendingEntities;
    private readonly List<EntityId> sentIds;

    public RailServerPacket() : base()
    {
      this.states = new List<RailState>();

      this.CommandTick = Tick.INVALID;
      this.Destination = null;

      this.pendingEntities = new List<KeyValuePair<RailEntity, Tick>>();
      this.sentIds = new List<EntityId>();

      this.Reference = null;
    }

    protected override void Reset()
    {
      base.Reset();

      this.states.Clear();

      this.CommandTick = Tick.INVALID;
      this.Destination = null;

      this.pendingEntities.Clear();
      this.sentIds.Clear();

      this.Reference = null;
    }

    internal void QueueEntity(RailEntity entity, Tick lastAcked)
    {
      this.pendingEntities.Add(
        new KeyValuePair<RailEntity, Tick>(
          entity, 
          lastAcked));
    }

    #region Encode/Decode
    protected override void EncodePayload(BitBuffer buffer)
    {
      // Write: [Header]
      this.EncodeHeader(buffer);

      // Write: [CommandTick]
      buffer.Write(RailEncoders.Tick, this.CommandTick);

      // Write: [Events]
      this.EncodeEvents(buffer);

      // Write: [States]
      this.EncodeStates(buffer);

      CommonDebug.Assert(buffer.ByteSize <= RailConfig.MAX_MESSAGE_SIZE);
    }

    protected override void DecodePayload(BitBuffer buffer)
    {
      // Read: [Header]
      this.DecodeHeader(buffer);

      // Read: [CommandTick]
      this.CommandTick = buffer.Read(RailEncoders.Tick);

      // Read: [Events]
      this.DecodeEvents(buffer);

      // Read: [States]
      this.DecodeStates(buffer);

      CommonDebug.Assert(buffer.IsFinished);
    }

    #region States
    private void EncodeStates(BitBuffer buffer)
    {
      // Reserve: [Entity Count]
      buffer.Reserve(RailEncoders.EntityCount);

      // Write: [Entity States]
      foreach (KeyValuePair<RailEntity, Tick> pair in this.pendingEntities)
      {
        buffer.SetRollback();
        int beforeSize = buffer.ByteSize;

        pair.Key.EncodeState(
          buffer,
          this.Destination,  // Make sure this is set ahead of time!
          this.SenderTick,   // Make sure this is set ahead of time!
          pair.Value);

        int byteCost = buffer.ByteSize - beforeSize;
        if (byteCost > MAX_ENTITY_SIZE)
        {
          buffer.Rollback();
          CommonDebug.LogWarning(
            "Entity too big: " +
            pair.Key.Id +
            "(" +
            pair.Key.Type +
            ") -- " +
            byteCost + "B");
        }
        else if (buffer.ByteSize > RailConfig.MAX_MESSAGE_SIZE)
        {
          buffer.Rollback();
          break;
        }
        else
        {
          this.sentIds.Add(pair.Key.Id);
        }
      }

      // Reserved Write: [Entity Count]
      buffer.WriteReserved(RailEncoders.EntityCount, this.sentIds.Count);
    }

    private void DecodeStates(BitBuffer buffer)
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
              this.Reference,  // Make sure this is set!
              this.SenderTick);        // Make sure this is decoded first!
          if (state != null)
            this.states.Add(state);
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
