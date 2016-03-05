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

namespace Railgun
{
  public class RailPeerClient : RailPeer
  {
    public event Action<RailPeerClient> MessagesReady;

    /// <summary>
    /// A history of received packets from the client. Used for dejitter.
    /// </summary>
    internal readonly RailRingBuffer<RailCommand> commandBuffer;

    /// <summary>
    /// The last tick that the client received a snapshot from the server.
    /// </summary>
    internal int LastAckedServerTick { get; set; }

    /// <summary>
    /// The last client tick that the server processed from the client.
    /// </summary>
    internal int LastReceivedClientTick { get; set; }

    private RailClock clientClock;

    internal RailPeerClient(IRailNetPeer netPeer) : base(netPeer)
    {
      this.LastAckedServerTick = RailClock.INVALID_TICK;
      this.clientClock = new RailClock();

      // We use no divisor for storing commands because commands are sent in
      // batches that we can use to fill in the holes between send frames
      this.commandBuffer =
        new RailRingBuffer<RailCommand>(RailConfig.DEJITTER_BUFFER_LENGTH);
    }

    protected override void OnMessagesReady(IRailNetPeer peer)
    {
      if (this.MessagesReady != null)
        this.MessagesReady(this);
    }

    internal void Update()
    {
      this.clientClock.Tick();
    }

    internal void ProcessPacket(RailClientPacket packet)
    {
      this.LastAckedServerTick = packet.LastReceivedServerTick;
      this.clientClock.UpdateLatest(packet.ClientTick);
      this.LastReceivedClientTick = this.clientClock.LastReceivedRemote;

      foreach (RailCommand command in packet.Commands)
        this.commandBuffer.Store(command);
    }

    internal T GetLatestCommand<T>()
      where T : RailCommand
    {
      int tick = this.clientClock.EstimatedRemote;
      return (T)this.commandBuffer.GetLatest(tick);
    }
  }
}
