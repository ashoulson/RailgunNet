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
  public class Client
  {
    private const int PAYLOAD_CHOKE = 3;
    private const int BUFFER_SIZE = 10;

    //public event Action Connected;
    //public event Action Disconnected;

    public Peer HostPeer { get; private set; }
    private Interpreter interpreter;
    private int lastReceived;

    /// <summary>
    /// A complete snapshot history of all received snapshots. 
    /// New incoming snapshots from the host will be reconstructed from these.
    /// </summary>
    internal RingBuffer<Snapshot> Snapshots { get; private set; }

    public Client(Peer hostPeer)
    {
      this.HostPeer = hostPeer;
      this.Snapshots = new RingBuffer<Snapshot>(BUFFER_SIZE, Host.SEND_RATE);
      this.interpreter = new Interpreter();
      this.lastReceived = Clock.INVALID_TICK;
    }

    internal void Receive()
    {
      for (int i = 0; i < Client.PAYLOAD_CHOKE; i++)
        if (this.HostPeer.Incoming.Count > 0)
          this.Process(this.HostPeer.Incoming.Dequeue());
    }

    private void Process(byte[] data)
    {
      Snapshot snapshot = this.interpreter.Decode(data, this.Snapshots);
      this.Snapshots.Store(snapshot);
      if (snapshot.Tick > this.lastReceived)
        this.lastReceived = snapshot.Tick;
    }
  }
}
