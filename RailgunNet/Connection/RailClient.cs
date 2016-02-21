﻿/*
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
  public class RailClient : RailConnection
  {
    //public event Action Connected;
    //public event Action Disconnected;

    private RailPeer hostPeer;
    private int lastReceived;
    private byte[] dataBuffer;

    private RailClock serverClock;
    private bool shouldUpdateClock = false;
    private bool shouldUpdate = false;

    public RailClient(params RailStateFactory[] factories) : base(factories)
    {
      this.hostPeer = null;
      this.lastReceived = RailClock.INVALID_TICK;
      this.dataBuffer = new byte[RailConfig.DATA_BUFFER_SIZE];

      this.serverClock = new RailClock(RailConnection.SEND_RATE);
      this.shouldUpdate = false;
      this.shouldUpdateClock = false;
    }

    public void SetPeer(INetPeer netPeer)
    {
      this.hostPeer = new RailPeer(netPeer);
      this.hostPeer.MessagesReady += this.OnMessagesReady;
    }

    public override void Update()
    {
      if (this.shouldUpdate)
      {
        if (this.shouldUpdateClock)
          this.serverClock.Tick(this.lastReceived);
        else
          this.serverClock.Tick();
      }

      // TEMP:
      if (this.hostPeer != null)
        this.hostPeer.EnqueueSend(new byte[] {}, 0);
    }

    private void OnMessagesReady(RailPeer peer)
    {
      IEnumerable<RailSnapshot> decode = 
        this.interpreter.DecodeReceivedSnapshots(
          this.hostPeer, 
          this.snapshots);

      foreach (RailSnapshot snapshot in decode)
      {
        this.snapshots.Store(snapshot);
        if (snapshot.Tick > this.lastReceived)
        {
          this.lastReceived = snapshot.Tick;
          this.shouldUpdateClock = true;
        }

        this.shouldUpdate = true;

        // Naive: Apply every snapshot in received order
        this.World.ApplySnapshot(snapshot);
      }
    }
  }
}
