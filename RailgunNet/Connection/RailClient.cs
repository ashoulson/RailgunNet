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
  public class RailClient
  {
    private const int PAYLOAD_CHOKE = 3;
    private const int SNAPSHOT_BUFFER_LENGTH = 10;

    //public event Action Connected;
    //public event Action Disconnected;

    private RailPeer hostPeer;
    private Interpreter interpreter;
    private int lastReceived;
    private byte[] dataBuffer;

    /// <summary>
    /// A complete snapshot history of all received snapshots. 
    /// New incoming snapshots from the host will be reconstructed from these.
    /// </summary>
    internal RingBuffer<RailSnapshot> Snapshots { get; private set; }

    public RailClient(INetPeer netPeer)
    {
      this.hostPeer = new RailPeer(netPeer);
      this.hostPeer.MessagesReady += this.OnMessagesReady;

      this.Snapshots = 
        new RingBuffer<RailSnapshot>(
          RailClient.SNAPSHOT_BUFFER_LENGTH, 
          RailHost.SEND_RATE);

      this.interpreter = new Interpreter();
      this.lastReceived = RailClock.INVALID_TICK;
      this.dataBuffer = new byte[RailConfig.DATA_BUFFER_SIZE];
    }

    private void OnMessagesReady(RailPeer peer)
    {
      IEnumerable<RailSnapshot> decode = 
        this.interpreter.DecodeReceived(
          this.hostPeer, 
          this.Snapshots);

      foreach (RailSnapshot snapshot in decode)
      {
        this.Snapshots.Store(snapshot);
        if (snapshot.Tick > this.lastReceived)
          this.lastReceived = snapshot.Tick;
      }
    }
  }
}
