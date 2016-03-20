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
    Tick LatestServerTick { get; }
    IEnumerable<RailState> States { get; }
  }

  /// <summary>
  /// Packet sent from server to client.
  /// </summary>
  internal class RailServerPacket : 
    RailPacket, IRailServerPacket, IRailPoolable<RailServerPacket>
  {
    // String hashes (md5):
    private const int KEY_RESERVE = 0x024BE210;
    private const int KEY_ROLLBACK = 0x667C36AC;

    IRailPool<RailServerPacket> IRailPoolable<RailServerPacket>.Pool { get; set; }
    void IRailPoolable<RailServerPacket>.Reset() { this.Reset(); }

    public IEnumerable<RailState> States 
    { 
      get { return this.states; }
    }

    public Tick LatestServerTick
    {
      get { return this.SenderTick; }
    }

    private readonly List<RailState> states;

    // Values controlled by the peer
    internal Tick CommandTick { get; set; }
    internal IRailController Destination { private get; set; }
    internal IEnumerable<EntityId> SentIds { get { return this.sentIds; } }

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
    }

    protected override void Reset()
    {
      base.Reset();

      this.states.Clear();

      this.CommandTick = Tick.INVALID;
      this.Destination = null;

      this.pendingEntities.Clear();
      this.sentIds.Clear();
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
      // Write: [CommandTick]
      buffer.Write(RailEncoders.Tick, this.CommandTick);

      // Write: [States]
      this.EncodeStates(buffer);
    }

    protected override void DecodePayload(
      BitBuffer buffer,
      IRailLookup<EntityId, RailEntity> entityLookup)
    {
      // Read: [CommandTick]
      this.CommandTick = buffer.Read(RailEncoders.Tick);

      // Read: [States]
      this.DecodeStates(buffer, entityLookup);
    }

    #region States
    private void EncodeStates(BitBuffer buffer)
    {
      // Reserve: [Count]
      buffer.Reserve(RailServerPacket.KEY_RESERVE, RailEncoders.EntityCount);

      // Write: [States]
      IEnumerable<KeyValuePair<RailEntity, Tick>> packed = 
        buffer.PackToSize(
          RailServerPacket.KEY_RESERVE,
          RailServerPacket.KEY_ROLLBACK,
          RailEncoders.EntityCount,
          this.pendingEntities,
          RailConfig.MESSAGE_MAX_SIZE,
          this.EncodeState);

      foreach (var pair in packed)
        this.sentIds.Add(pair.Key.Id);
    }

    private void EncodeState(BitBuffer buffer, KeyValuePair<RailEntity, Tick> pair)
    {
      Tick basisTick = pair.Value;
      pair.Key.EncodeState(
        buffer,
        this.Destination,  // Make sure this is set ahead of time!
        this.SenderTick,   // Make sure this is set ahead of time!
        basisTick);
    }

    private void DecodeStates(
      BitBuffer buffer,
      IRailLookup<EntityId, RailEntity> entityLookup)
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
            RailEntity.DecodeState(buffer, this.SenderTick, entityLookup);
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
