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
  /// <summary>
  /// A context for maintaining connection data on the host.
  /// Clients do not use this class.
  /// </summary>
  public class Peer
  {
    /// <summary>
    /// For attaching arbitrary data to this peer.
    /// </summary>
    public object UserData { get; set; }

    /// <summary>
    /// The peer's connection context, used for I/O.
    /// </summary>
    public IConnection Connection { get; private set; }

    internal Queue<byte[]> Incoming { get; private set; }
    internal Queue<byte[]> Outgoing { get; private set; }

    private Peer() { throw new NotSupportedException(); }

    public Peer(IConnection connection)
    {
      this.UserData = null;
      this.Connection = connection;

      this.Incoming = new Queue<byte[]>();
      this.Outgoing = new Queue<byte[]>();

      connection.Receive += this.OnConnectionReceive;
    }

    /// <summary>
    /// Flushes the outgoing queue and sends all payloads.
    /// </summary>
    internal void Transmit()
    {
      while (this.Outgoing.Count > 0)
        this.Connection.Send(this.Outgoing.Dequeue());
    }

    /// <summary>
    /// Puts a payload on the outgoing queue.
    /// </summary>
    internal void QueueSend(byte[] payload)
    {
      this.Outgoing.Enqueue(payload);
    }

    /// <summary>
    /// Called when the receive event fires, queues the incoming payload.
    /// </summary>
    private void OnConnectionReceive(byte[] payload)
    {
      this.Incoming.Enqueue(payload);
    }
  }
}
