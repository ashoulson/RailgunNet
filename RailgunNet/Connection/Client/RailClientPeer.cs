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

#if CLIENT
using System;
using System.Collections.Generic;

namespace Railgun
{
  /// <summary>
  /// The peer created by the client representing the server.
  /// </summary>
  internal class RailClientPeer
    : RailPeer<RailServerPacket, RailClientPacket>
  {
    internal event Action<IRailServerPacket> PacketReceived;

    private readonly RailView localView;

    private List<RailEntity> sortingList;

    internal RailClientPeer(
      IRailNetPeer netPeer,
      RailInterpreter interpreter)
      : base(netPeer, interpreter)
    {
      this.localView = new RailView();
      this.sortingList = new List<RailEntity>();
    }

    internal void SendPacket(
      Tick localTick,
      IEnumerable<RailEntity> controlledEntities)
    {
      // TODO: Sort controlledEntities by most recently sent

      RailClientPacket packet = base.PrepareSend<RailClientPacket>(localTick);
      packet.Populate(
        this.ProduceCommandUpdates(controlledEntities), 
        this.localView);

      // Send the packet
      base.SendPacket(packet);

      foreach (RailCommandUpdate commandUpdate in packet.Sent)
        commandUpdate.Entity.LastSentCommandTick = localTick;
    }

    protected override void ProcessPacket(RailPacket packet)
    {
      base.ProcessPacket(packet);

      RailServerPacket serverPacket = (RailServerPacket)packet;
      foreach (RailStateDelta delta in serverPacket.Deltas)
        this.localView.RecordUpdate(
          delta.EntityId, 
          packet.SenderTick, 
          delta.IsFrozen);
      if (this.PacketReceived != null)
        this.PacketReceived.Invoke(serverPacket);
    }

    private IEnumerable<RailCommandUpdate> ProduceCommandUpdates(
      IEnumerable<RailEntity> entities)
    {
      // If we have too many entities to fit commands for in a packet,
      // we want to round-robin sort them to avoid starvation
      this.sortingList.Clear();
      this.sortingList.AddRange(entities);
      this.sortingList.Sort(
        (x, y) => Tick.Comparer.Compare(
          x.LastSentCommandTick,
          y.LastSentCommandTick));

      foreach (RailEntity entity in sortingList)
      {
        RailCommandUpdate commandUpdate = 
          RailCommandUpdate.Create(
            entity.Id,
            entity.OutgoingCommands);
        commandUpdate.Entity = entity;
        yield return commandUpdate;
      }
    }
  }
}
#endif