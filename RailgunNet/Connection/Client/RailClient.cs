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

using System.Linq;

using CommonTools;

namespace Railgun
{
  public class RailClient : RailConnection
  {
    private RailClientPeer serverPeer;

    /// <summary>
    /// Entities that are waiting to be added to the world.
    /// </summary>
    private Dictionary<EntityId, RailEntity> pendingEntities;

    /// <summary>
    /// All known entities, either in-world or pending.
    /// </summary>
    private Dictionary<EntityId, RailEntity> knownEntities;

    // The local simulation tick, used for commands
    private Tick localTick;

    private readonly RailView entityView;

    public RailClient()
    {
      RailConnection.IsServer = false;
      this.world.InitializeClient();
      this.serverPeer = null;

      this.localTick = Tick.START;

      this.pendingEntities = 
        new Dictionary<EntityId, RailEntity>(EntityId.Comparer);
      this.knownEntities =
        new Dictionary<EntityId, RailEntity>(EntityId.Comparer);

      this.entityView = new RailView();
    }

    public void SetPeer(IRailNetPeer netPeer)
    {
      CommonDebug.Assert(this.serverPeer == null, "Overwriting peer");
      this.serverPeer = new RailClientPeer(netPeer);
      this.serverPeer.MessagesReady += this.OnMessagesReady;
    }

    public override void Update()
    {
      if (this.serverPeer != null)
      {
        this.UpdateCommands();
        this.UpdateWorld(this.serverPeer.Update());

        if (this.localTick.IsSendTick)
          this.SendPacket();

        this.localTick = this.localTick.GetNext();
      }
    }

    #region Local Updating
    /// <summary>
    /// Polls the command generator to produce a command and adds it to the
    /// rolling outgoing command queue.
    /// </summary>
    private void UpdateCommands()
    {
      if (this.serverPeer != null)
      {
        RailCommand command = RailResource.Instance.AllocateCommand();
        command.Populate();
        command.Tick = this.localTick;

        this.serverPeer.QueueOutgoing(command);
      }
    }

    /// <summary>
    /// Updates the world a number of ticks. If we have entities waiting to be
    /// added, this function will check them and add them if applicable.
    /// </summary>
    private void UpdateWorld(int numTicks)
    {
      for (; numTicks > 0; numTicks--)
      {
        Tick serverTick = 
          (this.serverPeer.RemoteClock.EstimatedRemote - numTicks) + 1;
        this.UpdatePendingEntities(serverTick);
        this.world.UpdateClient(serverTick);
      }
    }

    /// <summary>
    /// Checks to see if any pending entities can be added to the world and
    /// adds them if applicable.
    /// </summary>
    private void UpdatePendingEntities(Tick serverTick)
    {
      // TODO: This list could be pre-allocated
      List<RailEntity> toRemove = new List<RailEntity>();

      foreach (RailEntity entity in this.pendingEntities.Values)
      {
        if (entity.HasLatest(serverTick))
        {
          this.world.AddEntity(entity);
          toRemove.Add(entity);
        }
      }

      foreach (RailEntity entity in toRemove)
        this.pendingEntities.Remove(entity.Id);
    }
    #endregion

    #region Packet I/O

    #region Packet Send
    /// <summary>
    /// Packs and sends a client-to-server packet to the server.
    /// </summary>
    private void SendPacket()
    {
      RailClientPacket packet = RailResource.Instance.AllocateClientPacket();

      this.serverPeer.PreparePacket(
        packet, 
        this.localTick,
        this.serverPeer.PendingCommands, 
        this.entityView);

      this.interpreter.SendClientPacket(this.serverPeer, packet);
      RailPool.Free(packet);
    }
    #endregion

    #region Packet Receive
    private void OnMessagesReady(RailClientPeer peer)
    {
      IEnumerable<RailServerPacket> decode =
        this.interpreter.ReceiveServerPackets(
          this.serverPeer, 
          this.knownEntities);

      foreach (RailServerPacket packet in decode)
      {
        this.serverPeer.ProcessPacket(packet);
        this.ProcessPacket(packet);
        RailPool.Free(packet);
      }
    }

    private void ProcessPacket(RailServerPacket packet)
    {
      foreach (RailState state in packet.States)
        this.ProcessState(state);
    }

    private void ProcessState(RailState state)
    {
      RailEntity entity;
      if (this.knownEntities.TryGetValue(state.EntityId, out entity) == false)
      {
        entity = 
          this.world.CreateEntity<RailEntity>(
            state.EntityType,
            state.EntityId);
        this.pendingEntities.Add(state.EntityId, entity);
        this.knownEntities.Add(state.EntityId, entity);
      }

      entity.StateBuffer.Store(state);
      this.entityView.RecordUpdate(entity.Id, state.Tick);
      this.UpdateControlStatus(entity, state);
    }

    private void UpdateControlStatus(RailEntity entity, RailState state)
    {
      if (state.IsController && (entity.Controller == null))
        this.serverPeer.GrantControl(entity);
      if ((state.IsController == false) && (entity.Controller != null))
        this.serverPeer.RevokeControl(entity);
    }

    private RailEntity GetEntity(EntityId entityId)
    {
      RailEntity entity;
      if (this.world.TryGetEntity(entityId, out entity) == true)
        return entity;
      if (this.pendingEntities.TryGetValue(entityId, out entity) == true)
        return entity;
      return null;
    }
    #endregion

    #endregion
  }
}
