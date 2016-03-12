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
    private readonly List<EntityUpdate> updates;

    // Client-only
    internal IEnumerable<RailState> States { get { return this.states; } }
    private readonly List<RailState> states;

    public RailServerPacket()
    {
      this.events = new List<RailEvent>();
      this.updates = new List<EntityUpdate>();
      this.states = new List<RailState>();
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
      this.updates.Add(new EntityUpdate(entity, basisTick));
    }

    protected void Reset()
    {
      this.LatestTick = Tick.INVALID;
      this.LastProcessedCommandTick = Tick.INVALID;

      this.events.Clear();
      this.updates.Clear();
      this.states.Clear();
    }

    #region Encode/Decode
    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [Entity States]
      foreach (EntityUpdate pair in this.updates)
        pair.Entity.EncodeState(buffer, this.LatestTick, pair.BasisTick);

      // Write: [Entity Count]
      buffer.Push(RailEncoders.EntityCount, this.updates.Count);

      // Write: [Events]
      foreach (RailEvent evnt in this.events)
        evnt.Encode(buffer);

      // Write: [EventCount]
      buffer.Push(RailEncoders.EventCount, this.events.Count);

      // Write: [LastProcessedCommandTick]
      buffer.Push(RailEncoders.Tick, this.LastProcessedCommandTick);

      // Write: [LatestTick]
      buffer.Push(RailEncoders.Tick, this.LatestTick);
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

      // Read: [EventCount]
      int eventCount = buffer.Pop(RailEncoders.EventCount);

      // Read: [Events]
      for (int i = 0; i < eventCount; i++)
        packet.events.Add(RailEvent.Decode(buffer));

      // We need to reverse the events to restore the send order
      packet.events.Reverse();

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
              packet.LatestTick, 
              knownEntities);
          if (state != null)
            packet.states.Add(state);
        }
        catch (BasisNotFoundException bnfe)
        {
          CommonDebug.LogWarning(bnfe);
          break;
        }
      }

      return packet;
    }
    #endregion
  }
}
