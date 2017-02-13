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
using System.Collections.Generic;

namespace Railgun
{
  public class RailClient 
    : RailConnection
  {
    /// <summary>
    /// The peer for our connection to the server.
    /// </summary>
    private RailClientPeer serverPeer;

    /// <summary>
    /// The local simulation tick, used for commands
    /// </summary>
    private Tick localTick;

    /// <summary>
    /// The client's room instance. TODO: Multiple rooms?
    /// </summary>
    private RailClientRoom clientRoom;
    private new RailClientRoom Room { get { return this.clientRoom; } }

    public RailClient(RailRegistry registry)
      : base(registry)
    {
      this.serverPeer = null;
      this.localTick = Tick.START;
      this.clientRoom = null;
    }

    public void StartRoom()
    {
      this.clientRoom = new RailClientRoom(this.resource, this);
      this.SetRoom(this.clientRoom, Tick.INVALID);
    }

    /// <summary>
    /// Sets the current server peer.
    /// </summary>
    public void SetPeer(IRailNetPeer netPeer)
    {
      if (netPeer == null)
      {
        if (this.serverPeer != null)
        {
          this.serverPeer.PacketReceived -= this.OnPacketReceived;
          this.serverPeer.EventReceived -= base.OnEventReceived;
        }

        this.serverPeer = null;
      }
      else
      {
        RailDebug.Assert(this.serverPeer == null, "Overwriting peer");
        this.serverPeer = 
          new RailClientPeer(this.resource, netPeer, this.Interpreter);
        this.serverPeer.PacketReceived += this.OnPacketReceived;
        this.serverPeer.EventReceived += base.OnEventReceived;
      }
    }

    public override void Update()
    {
      if (this.serverPeer != null)
      {
        this.DoStart();
        this.serverPeer.Update(this.localTick);

        if (this.Room != null)
        {
          this.Room.ClientUpdate(
            this.localTick,
            this.serverPeer.EstimatedRemoteTick);

          int sendRate = RailConfig.CLIENT_SEND_RATE;
          if (this.localTick.IsSendTick(sendRate))
            this.serverPeer.SendPacket(
              this.localTick,
              this.Room.LocalEntities);

          this.localTick++;
        }
      }
    }

    /// <summary>
    /// Queues an event to sent to the server.
    /// </summary>
    internal void RaiseEvent(RailEvent evnt, ushort attempts = 3)
    {
      RailDebug.Assert(this.serverPeer != null);
      if (this.serverPeer != null)
        this.serverPeer.SendEvent(evnt, attempts);
    }

    private void OnPacketReceived(IRailServerPacket packet)
    {
      if (this.Room == null)
      {
        foreach (RailStateDelta delta in packet.Deltas)
          RailPool.Free(delta);
      }
      else
      {
        foreach (RailStateDelta delta in packet.Deltas)
          if (this.Room.ProcessDelta(delta) == false)
            RailPool.Free(delta);
      }
    }
  }
}
#endif