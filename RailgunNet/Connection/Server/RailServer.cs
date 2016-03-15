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
    /// The controller has control of no entities at this point.
    /// </summary>
    public event Action<IRailControllerServer> ControllerJoined;

    /// <summary>
    /// Fired when a controller has been removed (i.e. player leave).
    /// This event fires before the controller has control of its entities
    /// revoked (this is done immediately afterwards).
    /// </summary>
    public event Action<IRailControllerServer> ControllerLeft;

    /// <summary>
    /// Collection of all participating clients.
    /// </summary>
    private Dictionary<IRailNetPeer, RailServerPeer> clients;

    public RailServer() : base()
    {
      RailConnection.IsServer = true;
      this.World.InitializeServer();
      this.clients = new Dictionary<IRailNetPeer, RailServerPeer>();
    }

    /// <summary>
    /// Wraps an incoming connection in a peer and stores it.
    /// </summary>
    public void AddPeer(IRailNetPeer peer)
    {
      if (this.clients.ContainsKey(peer) == false)
      {
        RailServerPeer client = new RailServerPeer(peer, this.Interpreter);
        this.clients.Add(peer, client);

        if (this.ControllerJoined != null)
          this.ControllerJoined.Invoke(client);
      }
    }

    /// <summary>
    /// Wraps an incoming connection in a peer and stores it.
    /// </summary>
    public void RemovePeer(IRailNetPeer peer)
    {
      if (this.clients.ContainsKey(peer))
      {
        RailServerPeer client = this.clients[peer];
        this.clients.Remove(peer);

        if (this.ControllerLeft != null)
          this.ControllerLeft.Invoke(client);

        // Revoke control of all the entities controlled by that controller
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
      foreach (RailServerPeer client in this.clients.Values)
        client.Update();

      this.World.UpdateServer();

      if (this.World.Tick.IsSendTick)
      {
        this.BroadcastPackets();

        // Store only after everything else, this lets us keep the full
        // history for reference for as long as we need to process it
        this.World.StoreStates();
      }
    }

    /// <summary>
    /// Creates an entity of a given type and adds it to the world.
    /// </summary>
    public T AddNewEntity<T>(int type)
      where T : RailEntity
    {
      RailEntity entity = this.World.CreateEntity<T>(type);
      this.World.AddEntity(entity);
      return (T)entity;
    }

    /// <summary>
    /// Packs and sends a server-to-client packet to each peer.
    /// </summary>
    private void BroadcastPackets()
    {
      foreach (RailServerPeer clientPeer in this.clients.Values)
        clientPeer.SendPacket(this.World.Tick, this.World.Entities);
    }
  }
}
