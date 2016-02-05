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
  public class Host
  {
    internal const int SEND_RATE = 2;
    private const int PAYLOAD_CHOKE = 3;
    private const int BUFFER_SIZE = 10;

    public event Action<ClientPeer> PeerAdded;
    //public event Action<ClientPeer> PeerRemoved;

    private Environment environment;
    private Interpreter interpreter;
    private Dictionary<IConnection, ClientPeer> connectionToPeer;
    private int nextEntityId;

    /// <summary>
    /// A complete snapshot history of all sent snapshots. Individual
    /// peers will all delta against these and can select for scope.
    /// </summary>
    internal RingBuffer<Snapshot> Snapshots { get; private set; }

    public Host(params StatePool[] statePools)
    {
      ResourceManager.Initialize(statePools);

      this.nextEntityId = 1;
      this.environment = new Environment();
      this.interpreter = new Interpreter();
      this.connectionToPeer = new Dictionary<IConnection, ClientPeer>();
      this.Snapshots = new RingBuffer<Snapshot>(BUFFER_SIZE, Host.SEND_RATE);
    }

    /// <summary>
    /// Wraps an incoming connection in a peer and stores it.
    /// </summary>
    public void AddConnection(IConnection connection)
    {
      ClientPeer peer = new ClientPeer(connection);
      this.connectionToPeer.Add(connection, peer);

      if (this.PeerAdded != null)
        this.PeerAdded.Invoke(peer);
    }

    /// <summary>
    /// Creates an entity and adds it to the environment.
    /// </summary>
    public Entity CreateEntity(int type)
    {
      State state = ResourceManager.Instance.AllocateState(type);
      Entity entity = state.CreateEntity();
      entity.Id = this.nextEntityId++;
      entity.State = state;

      this.environment.Add(entity);
      entity.OnAddedToEnvironment();
      return entity;
    }

    /// <summary>
    /// Updates all entites and dispatches a snapshot if applicable.
    /// </summary>
    public void Update()
    {
      this.ReceiveAll();
      this.environment.UpdateHost();

      if (this.ShouldSend(this.environment.Tick))
      {
        Snapshot snapshot = this.environment.Snapshot();
        this.Snapshots.Store(snapshot);
        this.Broadcast(snapshot);

        this.TransmitAll();
      }
    }

    /// <summary>
    /// Polls all peers and receives their incoming payloads for processing.
    /// </summary>
    internal void ReceiveAll()
    {
      foreach (ClientPeer peer in this.connectionToPeer.Values)
        for (int i = 0; i < Host.PAYLOAD_CHOKE; i++)
          if (peer.Incoming.Count > 0)
            this.Process(peer, peer.Incoming.Dequeue());
    }

    /// <summary>
    /// Flushes all peers outgoing queues and transmits any waiting payloads.
    /// </summary>
    internal void TransmitAll()
    {
      foreach (ClientPeer peer in this.connectionToPeer.Values)
        peer.Transmit();
    }

    /// <summary>
    /// Queues a snapshot broadcast for each peer (handles delta-compression).
    /// </summary>
    internal void Broadcast(Snapshot snapshot)
    {
      foreach (ClientPeer peer in this.connectionToPeer.Values)
        peer.QueueSend(this.PreparePayload(peer, snapshot));
    }

    /// <summary>
    /// Processes an incoming packet from a peer.
    /// </summary>
    private void Process(ClientPeer peer, byte[] data)
    {
      // TODO
    }

    /// <summary>
    /// Delta-encodes a snapshot on a per-peer basis.
    /// </summary>
    private byte[] PreparePayload(ClientPeer peer, Snapshot snapshot)
    {
      Snapshot basis;
      if (peer.LastAcked != Clock.INVALID_TICK)
        if (this.Snapshots.TryGet(peer.LastAcked, out basis))
          return this.interpreter.Encode(snapshot, basis);
      return this.interpreter.Encode(snapshot);
    }

    private bool ShouldSend(int tick)
    {
      return (tick % Host.SEND_RATE) == 0;
    }
  }
}
