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
    protected override void EncodePayload(ByteBuffer buffer)
    {
      // Write: [CommandTick]
      buffer.WriteTick(this.CommandTick);

      // Write: [States]
      this.EncodeStates(buffer);
    }

    protected override void DecodePayload(
      ByteBuffer buffer,
      IRailLookup<EntityId, RailEntity> entityLookup)
    {
      // Read: [CommandTick]
      this.CommandTick = buffer.ReadTick();

      // Read: [States]
      this.DecodeStates(buffer, entityLookup);
    }

    #region States
    private void EncodeStates(ByteBuffer buffer)
    {
      IRailController destination = this.Destination;
      Tick tick = this.SenderTick;

      buffer.PackToSize(
        RailConfig.MESSAGE_MAX_SIZE,
        RailConfig.ENTITY_MAX_SIZE,
        this.pendingEntities,
        (pair) => pair.Key.EncodeState(buffer, destination, tick, pair.Value),
        (pair) => this.sentIds.Add(pair.Key.Id));
    }

    private void DecodeStates(
      ByteBuffer buffer,
      IRailLookup<EntityId, RailEntity> entityLookup)
    {
      IEnumerable<RailState> decoded =
        buffer.UnpackAll(
          () => RailEntity.DecodeState(buffer, this.SenderTick, entityLookup));

      try // The enumerator is lazy evaluated, so we exception check here
      {
        foreach (RailState state in decoded)
          if (state != null)
            this.states.Add(state);
      }
      catch (BasisNotFoundException bnfe)
      {
        CommonDebug.LogWarning(bnfe);
      }
    }
    #endregion
    #endregion
  }
}
