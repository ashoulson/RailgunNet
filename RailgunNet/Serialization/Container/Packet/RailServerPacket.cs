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
    // TODO: This will probably be obsolete once we get to scope/views
    private static int GetSafeBasisTick(int serverTick, int basisTick)
    {
      if (basisTick < 0)
        return RailClock.INVALID_TICK;

      int delta = serverTick - basisTick;
      int maxDelta = 
        RailConfig.DEJITTER_BUFFER_LENGTH - 
        RailConfig.NETWORK_SEND_RATE;

      if (delta > maxDelta)
        return RailClock.INVALID_TICK;
      return basisTick;
    }

    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }
    int IRailRingValue.Tick { get { return this.ServerTick; } }

    internal int ServerTick { get; private set; }
    internal int BasisTick { get; private set; }
    internal int LastProcessedCommandTick { get; private set; }
    internal IEnumerable<RailState> States { get { return this.states; } }

    // State list, available after decoding on client
    private readonly List<RailState> states;

    // Entity list, used for encoding on server
    private readonly List<RailEntity> entities;

    public RailServerPacket()
    {
      this.states = new List<RailState>();
      this.entities = new List<RailEntity>();

      this.Reset();
    }

    public void Initialize(
      int serverTick,
      int basisTick,
      int lastProcessedCommandTick,
      IEnumerable<RailEntity> entities)
    {
      this.ServerTick = serverTick;
      this.BasisTick = 
        RailServerPacket.GetSafeBasisTick(serverTick, basisTick);
      this.LastProcessedCommandTick = lastProcessedCommandTick;
      this.entities.AddRange(entities);
    }

    protected void Reset()
    {
      this.ServerTick = RailClock.INVALID_TICK;
      this.BasisTick = RailClock.INVALID_TICK;
      this.LastProcessedCommandTick = RailClock.INVALID_TICK;

      this.states.Clear();
      this.entities.Clear();
    }

    #region Encode/Decode
    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [States]
      foreach (RailEntity entity in this.entities)
        RailInterpreter.EncodeState(buffer, this.BasisTick, entity);

      // Write: [State Count]
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
      IDictionary<int, RailEntity> knownEntities)
    {
      RailServerPacket packet = RailResource.Instance.AllocateServerPacket();

      // Read: [ServerTick]
      packet.ServerTick = buffer.Pop(StandardEncoders.Tick);

      // Read: [LastAckedServerTick]
      packet.BasisTick = buffer.Pop(StandardEncoders.Tick);

      // Read: [LastProcessedCommandTick]
      packet.LastProcessedCommandTick = buffer.Pop(StandardEncoders.Tick);

      // Read: [State Count]
      int stateCount = buffer.Pop(StandardEncoders.EntityCount);

      // Read: [States]
      for (int i = 0; i < stateCount; i++)
        packet.states.Add(
          RailInterpreter.DecodeState(
            buffer,
            packet.ServerTick,
            packet.BasisTick, 
            knownEntities));

      return packet;
    }
    #endregion
  }
}
