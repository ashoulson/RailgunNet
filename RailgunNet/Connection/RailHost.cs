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
  /// <summary>
  /// Host is the core executing class on the server. It is responsible for
  /// managing connection contexts and payload I/O.
  /// </summary>
  public class RailHost
  {
    internal const int SEND_RATE = 2;
    private const int PAYLOAD_CHOKE = 3;
    private const int BUFFER_SIZE = 10;

    public event RailPeerEvent PeerAdded;
    //public event Action<ClientPeer> PeerRemoved;

    private RailEnvironment environment;
    private Interpreter interpreter;
    private HashSet<RailPeer> peers;
    private int nextEntityId;

    /// <summary>
    /// A complete snapshot history of all sent snapshots. Individual
    /// peers will all delta against these and can select for scope.
    /// </summary>
    internal RingBuffer<RailSnapshot> Snapshots { get; private set; }

    public RailHost(params RailFactory[] factories)
    {
      RailResource.Initialize(factories);

      this.nextEntityId = 1;
      this.environment = new RailEnvironment();
      this.interpreter = new Interpreter();
      this.peers = new HashSet<RailPeer>();
      this.Snapshots = new RingBuffer<RailSnapshot>(BUFFER_SIZE, RailHost.SEND_RATE);
    }

    /// <summary>
    /// Wraps an incoming connection in a peer and stores it.
    /// </summary>
    public void AddPeer(INetPeer peer)
    {
      RailPeer railPeer = new RailPeer(peer);

      this.peers.Add(railPeer);
      if (this.PeerAdded != null)
        this.PeerAdded.Invoke(railPeer);
    }

    /// <summary>
    /// Creates an entity of a given type. Note that this function does NOT
    /// add the entity to the environment. You should configure the entity
    /// and then call AddEntity().
    /// </summary>
    public T CreateEntity<T>(int type)
      where T : RailEntity
    {
      RailState state = RailResource.Instance.AllocateState(type);
      RailEntity entity = state.CreateEntity();

      entity.IsMaster = true;
      entity.Id = this.nextEntityId++;
      entity.State = state;

      return (T)entity;
    }

    /// <summary>
    /// Adds an entity to the host's environment. This entity will be
    /// replicated over the network to all client peers.
    /// </summary>
    public void AddEntity(RailEntity entity)
    {
      this.environment.Add(entity);
    }

    /// <summary>
    /// Updates all entites and dispatches a snapshot if applicable. Should
    /// be called once per game simulation tick (e.g. during Unity's 
    /// FixedUpdate pass).
    /// </summary>
    public void Update()
    {
      this.environment.UpdateHost();

      if (this.ShouldSend(this.environment.Tick))
      {
        RailSnapshot snapshot = this.environment.Snapshot();
        this.Snapshots.Store(snapshot);
        this.Broadcast(snapshot);
      }
    }

    /// <summary>
    /// Queues a snapshot broadcast for each peer (handles delta-compression).
    /// </summary>
    internal void Broadcast(RailSnapshot snapshot)
    {
      foreach (RailPeer peer in this.peers)
      {
        RailSnapshot basis;
        if (peer.LastAckedTick != RailClock.INVALID_TICK)
        {
          if (this.Snapshots.TryGet(peer.LastAckedTick, out basis))
          {
            this.interpreter.EncodeSend(peer, snapshot, basis);
            return;
          }
        }

        this.interpreter.EncodeSend(peer, snapshot);
      }
    }

    /// <summary>
    /// Processes an incoming packet from a peer.
    /// </summary>
    private void Process(INetPeer peer, byte[] data)
    {
      // TODO
    }

    private bool ShouldSend(int tick)
    {
      return (tick % RailHost.SEND_RATE) == 0;
    }
  }
}
