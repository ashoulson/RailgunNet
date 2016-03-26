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
using System.Collections;
using System.Collections.Generic;

using System.Linq;

namespace Railgun
{
  /// <summary>
  /// The peer created by the client representing the server.
  /// </summary>
  internal class RailClientPeer
    : RailPeer
    , IRailControllerInternal
  {
    internal event Action<IRailServerPacket> PacketReceived;

    /// <summary>
    /// Commands that have been sent to the server but not yet acked.
    /// </summary>
    public override IEnumerable<RailCommand> PendingCommands
    {
      get { return this.pendingCommands; }
    }

    private readonly Queue<RailCommand> pendingCommands;
    private readonly RailView localView;

    internal RailClientPeer(
      IRailNetPeer netPeer,
      RailInterpreter interpreter)
      : base(netPeer, interpreter)
    {
      this.pendingCommands = new Queue<RailCommand>();
      this.localView = new RailView();
    }

    internal override int Update(Tick localTick)
    {
      return base.Update(localTick);
    }

    internal void QueueCommand(RailCommand command)
    {
      if (this.pendingCommands.Count < RailConfig.COMMAND_BUFFER_COUNT)
        this.pendingCommands.Enqueue(command);
    }

    internal void SendPacket()
    {
      RailClientPacket packet =
        base.AllocatePacketSend<RailClientPacket>(this.LocalTick);

      // Set data
      packet.AddCommands(this.pendingCommands);
      packet.AddView(this.localView);

      // Send the packet
      base.SendPacket(packet);

      RailPool.Free(packet);
    }

    protected override void ProcessPacket(RailPacket packet)
    {
      base.ProcessPacket(packet);
      RailServerPacket serverPacket = (RailServerPacket)packet;

      this.UpdateCommands(serverPacket.CommandAck);
      foreach (RailState.Delta delta in serverPacket.Deltas)
        this.localView.RecordUpdate(delta.EntityId, packet.SenderTick);

      if (this.PacketReceived != null)
        this.PacketReceived.Invoke(serverPacket);
    }

    protected override RailPacket AllocateIncoming()
    {
      return RailServerPacket.Create();
    }

    protected override RailPacket AllocateOutgoing()
    {
      return RailClientPacket.Create();
    }

    private void UpdateCommands(Tick lastReceivedTick)
    {
      if (lastReceivedTick.IsValid == false)
        return;

      while (this.pendingCommands.Count > 0)
      {
        if (this.pendingCommands.Peek().Tick > lastReceivedTick)
          break;
        RailPool.Free(this.pendingCommands.Dequeue());
      }
    }
  }
}
#endif