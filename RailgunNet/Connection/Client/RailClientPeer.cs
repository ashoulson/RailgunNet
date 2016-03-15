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

using System.Linq;

using CommonTools;

namespace Railgun
{
  /// <summary>
  /// The peer contained by the client representing the server.
  /// </summary>
  internal class RailClientPeer : RailPeer
  {
    public event Action<RailClientPeer> MessagesReady;

    /// <summary>
    /// A history of sent commands to the server. Used on the client.
    /// </summary>
    private readonly Queue<RailCommand> outgoingBuffer;

    /// <summary>
    /// Commands that have been sent to the server but not yet acked.
    /// </summary>
    public override IEnumerable<RailCommand> PendingCommands
    {
      get { return this.outgoingBuffer; }
    }

    internal RailClientPeer(IRailNetPeer netPeer)
      : base(netPeer)
    {
      this.outgoingBuffer = new Queue<RailCommand>();
    }

    internal void ProcessPacket(RailServerPacket packet)
    {
      base.ProcessPacket(packet);

      this.UpdateCommands(packet.LastProcessedCommandTick);
    }

    internal void PreparePacket(
      RailClientPacket packet,
      Tick localTick,
      IEnumerable<RailCommand> commands,
      RailView view)
    {
      base.PreparePacketBase(packet, localTick);

      packet.InitializeClient(commands, view);
    }

    internal void QueueOutgoing(RailCommand command)
    {
      if (this.outgoingBuffer.Count < RailConfig.COMMAND_BUFFER_COUNT)
        this.outgoingBuffer.Enqueue(command);
    }

    internal void UpdateCommands(Tick lastReceivedTick)
    {
      if (lastReceivedTick.IsValid == false)
        return;

      while (true)
      {
        if (this.outgoingBuffer.Count == 0)
          break;
        if (this.outgoingBuffer.Peek().Tick > lastReceivedTick)
          break;
        RailPool.Free(this.outgoingBuffer.Dequeue());
      }
    }

    protected override void OnMessagesReady(IRailNetPeer peer)
    {
      if (this.MessagesReady != null)
        this.MessagesReady(this);
    }
  }
}
