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

    internal RailControllerServer Controller { get; private set; }

    /// <summary>
    /// The last tick that the client received a packet from the server.
    /// Not all entities will be up to date with this tick.
    /// </summary>
    internal Tick LastAckedServerTick { get; set; }

    /// <summary>
    /// The last command tick that the server processed.
    /// </summary>
    internal Tick LastProcessedCommandTick { get; set; }

    private readonly RailView view;
    private readonly RailClock clientClock;

    internal RailPeerClient(IRailNetPeer netPeer) : base(netPeer)
    {
      this.Controller = new RailControllerServer();
      this.LastAckedServerTick = Tick.INVALID;
      this.LastProcessedCommandTick = Tick.INVALID;

      this.clientClock = new RailClock();
      this.view = new RailView();
    }

    protected override void OnMessagesReady(IRailNetPeer peer)
    {
      if (this.MessagesReady != null)
        this.MessagesReady(this);
    }

    internal Tick GetLatestEntityTick(EntityId id)
    {
      return this.view.GetLatest(id);
    }

    internal void Update()
    {
      this.clientClock.Update();
      this.Controller.Update(this.clientClock.EstimatedRemote);

      if (this.Controller.LatestCommand != null)
        this.LastProcessedCommandTick = this.Controller.LatestCommand.Tick;
    }

    internal void ProcessPacket(RailClientPacket packet)
    {
      this.LastAckedServerTick = packet.LastReceivedServerTick;
      this.clientClock.UpdateLatest(packet.ClientTick);
      this.view.Integrate(packet.View);

      this.Controller.StoreIncoming(packet.Commands);
      this.Controller.CleanReliableEvents(packet.LastReceivedEventId);
      this.Controller.CleanUnreliableEvents();
    }
  }
}
