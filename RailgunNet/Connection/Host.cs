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
  /// Host is the core executing class on the server. It is responsible for
  /// managing connection contexts and payload I/O.
  /// </summary>
  public abstract class Host
  {
    private Interpreter interpreter;
    private Dictionary<IConnection, Peer> connectionToPeer;

    /// <summary>
    /// A complete snapshot history of all sent snapshots. Individual
    /// peers will all delta against these and can select for scope.
    /// </summary>
    internal SnapshotBuffer Snapshots { get; private set; }

    public Host()
    {
      this.interpreter = new Interpreter();
      this.connectionToPeer = new Dictionary<IConnection, Peer>();
      this.Snapshots = new SnapshotBuffer();
    }

    public void AddConnection(IConnection connection)
    {
      this.connectionToPeer.Add(connection, this.CreatePeer(connection));
    }

    internal void Broadcast(Snapshot snapshot)
    {
      foreach (Peer peer in this.connectionToPeer.Values)
        peer.QueueSend(this.PreparePayload(peer, snapshot));
    }

    private byte[] PreparePayload(Peer peer, Snapshot snapshot)
    {
      Snapshot basis;
      if (peer.LastAcked != Clock.INVALID_FRAME)
        if (this.Snapshots.TryGetValue(peer.LastAcked, out basis))
          return this.interpreter.Encode(snapshot, basis);
      return this.interpreter.Encode(snapshot);
    }

    public abstract Peer CreatePeer(IConnection connection);

    public virtual void OnPeerConnected(Peer peer) { }
  }
}
