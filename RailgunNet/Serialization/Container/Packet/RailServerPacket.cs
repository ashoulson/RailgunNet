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
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }
    Tick IRailRingValue.Tick { get { return this.ServerTick; } }

    internal Tick ServerTick { get; private set; }

    // TODO: This will be obsolete when we do entity ticks
    internal Tick BasisTick { get; private set; }

    internal Tick LastProcessedCommandTick { get; private set; }

    internal IEnumerable<RailState> States { get { return this.states; } }
    internal IEnumerable<RailEvent> Events { get { return this.events; } }

    // State list, available after decoding on client
    private readonly List<RailState> states;

    // Entity list, used for encoding on server
    private readonly List<RailEntity> entities;

    // Event list, includes both reliable and unreliable
    private readonly List<RailEvent> events;

    public RailServerPacket()
    {
      this.states = new List<RailState>();
      this.entities = new List<RailEntity>();
      this.events = new List<RailEvent>();

      this.Reset();
    }

    public void Initialize(
      Tick serverTick,
      Tick basisTick,
      Tick lastProcessedCommandTick,
      IEnumerable<RailEntity> entities,
      IEnumerable<RailEvent> events)
    {
      this.ServerTick = serverTick;
      this.BasisTick = basisTick;
      this.LastProcessedCommandTick = lastProcessedCommandTick;

      this.entities.AddRange(entities);
      this.events.AddRange(events);
    }

    protected void Reset()
    {
      this.ServerTick = Tick.INVALID;
      this.BasisTick = Tick.INVALID;
      this.LastProcessedCommandTick = Tick.INVALID;

      this.states.Clear();
      this.entities.Clear();
      this.events.Clear();
    }

    #region Encode/Decode
    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [Events]
      foreach (RailEvent evnt in this.events)
        evnt.Encode(buffer);

      // Write: [EventCount]
      buffer.Push(StandardEncoders.EventCount, this.events.Count);

      // Write: [States]
      foreach (RailEntity entity in this.entities)
        RailInterpreter.EncodeState(buffer, this.BasisTick, entity);

      // Write: [StateCount]
      buffer.Push(StandardEncoders.EntityCount, this.entities.Count);

      // Write: [LastProcessedCommandTick]
      buffer.Push(StandardEncoders.Tick, this.LastProcessedCommandTick);

      // Write: [LastAckedServerTick]
      buffer.Push(StandardEncoders.Tick, this.BasisTick);

      // Write: [ServerTick]
      buffer.Push(StandardEncoders.Tick, this.ServerTick);
    }

    internal static RailServerPacket Decode(
      BitBuffer buffer,
      IDictionary<EntityId, RailEntity> knownEntities)
    {
      RailServerPacket packet = RailResource.Instance.AllocateServerPacket();

      // Read: [ServerTick]
      packet.ServerTick = buffer.Pop(StandardEncoders.Tick);

      // Read: [LastAckedServerTick]
      packet.BasisTick = buffer.Pop(StandardEncoders.Tick);

      // Read: [LastProcessedCommandTick]
      packet.LastProcessedCommandTick = buffer.Pop(StandardEncoders.Tick);

      // Read: [StateCount]
      int stateCount = buffer.Pop(StandardEncoders.EntityCount);

      // Read: [States]
      for (int i = 0; i < stateCount; i++)
        packet.states.Add(
          RailInterpreter.DecodeState(
            buffer,
            packet.ServerTick,
            packet.BasisTick, 
            knownEntities));

      // Read: [EventCount]
      int eventCount = buffer.Pop(StandardEncoders.EventCount);

      // Read: [Events]
      for (int i = 0; i < eventCount; i++)
        packet.events.Add(RailEvent.Decode(buffer));

      // We need to reverse the events to restore the original order
      packet.events.Reverse();

      return packet;
    }
    #endregion
  }
}
