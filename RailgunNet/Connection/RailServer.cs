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
  /// Server is the core executing class on the server. It is responsible for
  /// managing connection contexts and payload I/O.
  /// </summary>
  public class RailServer : RailConnection
  {
    /// <summary>
    /// Fired when a controller has been added (i.e. player join).
    /// </summary>
    public event Action<RailController> ControllerAdded;

    /// <summary>
    /// Fired when a controller has been removed (i.e. player leave).
    /// This event fires before the controller has control of its entities
    /// revoked (this is done immediately afterwards).
    /// </summary>
    public event Action<RailController> ControllerRemoved;

    /// <summary>
    /// Collection of all participating clients.
    /// </summary>
    private Dictionary<IRailNetPeer, RailPeerClient> clients;

    public RailServer(
      RailCommand commandToRegister,
      RailState[] statesToRegister,
      RailEvent[] eventsToRegister)
      : base(commandToRegister, statesToRegister, eventsToRegister)
    {
      this.clients = new Dictionary<IRailNetPeer, RailPeerClient>();
    }

    /// <summary>
    /// Wraps an incoming connection in a peer and stores it.
    /// </summary>
    public void AddPeer(IRailNetPeer peer)
    {
      if (this.clients.ContainsKey(peer) == false)
      {
        RailPeerClient railPeer = new RailPeerClient(peer);
        this.clients.Add(peer, railPeer);

        if (this.ControllerAdded != null)
          this.ControllerAdded.Invoke(railPeer.Controller);

        railPeer.MessagesReady += this.OnMessagesReady;
      }
    }

    /// <summary>
    /// Wraps an incoming connection in a peer and stores it.
    /// </summary>
    public void RemovePeer(IRailNetPeer peer)
    {
      if (this.clients.ContainsKey(peer))
      {
        RailPeerClient client = this.clients[peer];
        this.clients.Remove(peer);

        if (this.ControllerRemoved != null)
          this.ControllerRemoved.Invoke(client.Controller);

        // Revoke control of all the entities controlled by that controller
        client.Controller.Shutdown();
      }
    }

    /// <summary>
    /// Updates all entites and dispatches a snapshot if applicable. Should
    /// be called once per game simulation tick (e.g. during Unity's 
    /// FixedUpdate pass).
    /// </summary>
    public override void Update()
    {
      foreach (RailPeerClient client in this.clients.Values)
        client.Update();

      this.world.UpdateServer();

      if (this.ShouldSend(this.world.Tick))
      {
        this.BroadcastPackets();

        // Store only after everything else, this lets us keep the full
        // history for reference for as long as we need to process it
        this.world.StoreStates();
      }
    }

    /// <summary>
    /// Creates a state of a given type for use in creating an entity.
    /// </summary>
    public T CreateState<T>()
      where T : RailState, new()
    {
      return (T)RailResource.Instance.AllocateState((new T()).EntityType);
    }

    /// <summary>
    /// Creates an entity of a given type. Does not add ie to the world.
    /// </summary>
    public T CreateEntity<T>(RailState state)
      where T : RailEntity
    {
      // Entity states don't have a tick since they are reused every frame
      state.Initialize(this.world.GetEntityId(), RailClock.INVALID_TICK);

      RailEntity entity = state.CreateEntity();
      entity.InitializeServer(state);

      return (T)entity;
    }

    /// <summary>
    /// Adds an entity to the server's world.
    /// </summary>
    public void AddEntity(RailEntity entity)
    {
      this.world.AddEntity(entity);
      entity.TickCreated = this.world.Tick;
    }

    public void AssignControl(RailController controller, RailEntity entity)
    {
      RailControllerServer serverController = (RailControllerServer)controller;
      serverController.AddEntity(entity, this.world.Tick);
    }

    /// <summary>
    /// Packs and sends a server-to-client packet to each peer.
    /// TODO: Scope/packing maximum
    /// </summary>
    private void BroadcastPackets()
    {
      foreach (RailPeerClient clientPeer in this.clients.Values)
      {
        RailServerPacket packet = RailResource.Instance.AllocateServerPacket();

        packet.Initialize(
          this.world.Tick,
          clientPeer.LastAckedServerTick,
          clientPeer.LastProcessedCommandTick,
          this.world.Entities,
          clientPeer.Controller.AllEvents);

        this.interpreter.SendServerPacket(clientPeer, packet);
      }
    }

    private void OnMessagesReady(RailPeerClient peer)
    {
      IEnumerable<RailClientPacket> decode = 
        this.interpreter.ReceiveClientPackets(peer);
      foreach (RailClientPacket input in decode)
        peer.ProcessPacket(input);
    }
  }
}
