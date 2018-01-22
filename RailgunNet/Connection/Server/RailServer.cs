/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
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

#if SERVER
using System.Collections.Generic;

namespace Railgun
{
  /// <summary>
  /// Server is the core executing class on the server. It is responsible for
  /// managing connection contexts and payload I/O.
  /// </summary>
  public class RailServer : RailConnection
  {
    /// <summary>
    /// Collection of all participating clients.
    /// </summary>
    private Dictionary<IRailNetPeer, RailServerPeer> clients;

    /// <summary>
    /// Entities that have been removed or are about to be.
    /// </summary>
    private Dictionary<EntityId, IRailEntity> removedEntities;

    /// <summary>
    /// The server's room instance. TODO: Multiple rooms?
    /// </summary>
    private new RailServerRoom Room { get; set; }

    private readonly List<IRailEntity> toRemove; // Pre-allocated list for reuse

    public RailServer(RailRegistry registry) : base(registry)
    {
      this.clients = new Dictionary<IRailNetPeer, RailServerPeer>();
      this.removedEntities = new Dictionary<EntityId, IRailEntity>();
      this.toRemove = new List<IRailEntity>();
    }

    /// <summary>
    /// Starts the server's room.
    /// </summary>
    public void StartRoom()
    {
      this.Room = new RailServerRoom(this.resource, this);
      base.SetRoom(this.Room, Tick.START);
    }

    /// <summary>
    /// Wraps an incoming connection in a peer and stores it.
    /// </summary>
    public void AddClient(IRailNetPeer netPeer, string identifier)
    {
      if (this.clients.ContainsKey(netPeer) == false)
      {
        RailServerPeer client = 
          new RailServerPeer(
            this,
            this.resource, 
            netPeer, 
            this.Interpreter);

        client.Identifier = identifier;
        client.EventReceived += base.OnEventReceived;
        client.PacketReceived += this.OnPacketReceived;
        this.clients.Add(netPeer, client);
        this.Room.AddClient(client);
      }
    }

    /// <summary>
    /// Wraps an incoming connection in a peer and stores it.
    /// </summary>
    public void RemoveClient(IRailNetPeer netClient)
    {
      if (this.clients.ContainsKey(netClient))
      {
        RailServerPeer client = this.clients[netClient];
        this.clients.Remove(netClient);
        this.Room.RemoveClient(client);

        // Revoke control of all the entities controlled by that client
        client.Shutdown();
      }
    }

    /// <summary>
    /// Updates all entites and dispatches a snapshot if applicable. Should
    /// be called once per game simulation tick (e.g. during Unity's 
    /// FixedUpdate pass).
    /// </summary>
    public override void Update()
    {
      this.DoStart();

      foreach (RailServerPeer client in this.clients.Values)
        client.Update(this.Room.Tick);

      this.Room.ServerUpdate();
      if (this.Room.Tick.IsSendTick(RailConfig.SERVER_SEND_RATE))
      {
        this.Room.StoreStates();
        this.BroadcastPackets();
      }

      this.CleanRemovedEntities();
    }

    internal void LogRemovedEntity(IRailEntity entity)
    {
      this.removedEntities.Add(entity.Id, entity);
    }

    /// <summary>
    /// Cleans out any removed entities from the removed list
    /// if they have been acked by all clients.
    /// </summary>
    private void CleanRemovedEntities()
    {
      // TODO: Retire the Id in all of the views as well?

      foreach (var kvp in this.removedEntities)
      {
        bool canRemove = true;
        EntityId id = kvp.Key;
        IRailEntity entity = kvp.Value;

        foreach (RailServerPeer peer in this.clients.Values)
        {
          Tick lastSent = peer.Scope.GetLastSent(id);
          if (lastSent.IsValid == false)
            continue; // Was never sent in the first place

          Tick lastAcked = peer.Scope.GetLastAckedByClient(id);
          if (lastAcked.IsValid && (lastAcked >= entity.AsBase.RemovedTick))
            continue; // Remove tick was acked by the client

          // Otherwise, not safe to remove
          canRemove = false;
          break;
        }

        if (canRemove)
          this.toRemove.Add(entity);
      }

      for (int i = 0; i < this.toRemove.Count; i++)
        this.removedEntities.Remove(this.toRemove[i].Id);
      this.toRemove.Clear();
    }

    /// <summary>
    /// Packs and sends a server-to-client packet to each peer.
    /// </summary>
    private void BroadcastPackets()
    {
      foreach (RailServerPeer clientPeer in this.clients.Values)
        clientPeer.SendPacket(
          this.Room.Tick,
          this.Room.Entities,
          this.removedEntities.Values);
    }

#region Packet Receive
    private void OnPacketReceived(
      RailServerPeer peer,
      IRailClientPacket packet)
    {
      foreach (RailCommandUpdate update in packet.CommandUpdates)
        this.ProcessCommandUpdate(peer, update);
    }

    private void ProcessCommandUpdate(
      RailServerPeer peer, 
      RailCommandUpdate update)
    {
      if (this.Room.TryGet(update.EntityId, out IRailEntity entity))
      {
        bool canReceive = 
          (entity.Controller == peer) && (entity.IsRemoving == false);

        if (canReceive)
          foreach (RailCommand command in update.Commands)
            entity.AsBase.ReceiveCommand(command);
        else // Can't send commands to that entity, so dump them
          foreach (RailCommand command in update.Commands)
            RailPool.Free(command);
      }
    }
#endregion
  }
}
#endif