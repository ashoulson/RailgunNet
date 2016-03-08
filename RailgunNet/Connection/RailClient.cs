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
    private RailPeerServer serverPeer;

    private readonly RailClock serverClock;
    private readonly RailControllerClient localController;

    /// <summary>
    /// Entities that are waiting to be added to the world.
    /// </summary>
    private Dictionary<EntityId, RailEntity> pendingEntities;

    /// <summary>
    /// All known entities, either in-world or pending.
    /// </summary>
    private Dictionary<EntityId, RailEntity> knownEntities;

    // The local simulation tick, used for commands
    private int localTick;

    // The last received event id, for reliable sequencing
    private EventId lastReceivedEventId;

    public RailClient(
      RailCommand commandToRegister, 
      RailState[] statesToRegister,
      RailEvent[] eventsToRegister)
      : base(commandToRegister, statesToRegister, eventsToRegister)
    {
      this.serverPeer = null;
      this.serverClock = new RailClock();

      this.localTick = 0;
      this.lastReceivedEventId = EventId.INVALID;
      this.localController = new RailControllerClient();

      this.pendingEntities = 
        new Dictionary<EntityId, RailEntity>(EntityId.Comparer);
      this.knownEntities =
        new Dictionary<EntityId, RailEntity>(EntityId.Comparer);
    }

    public void SetPeer(IRailNetPeer netPeer)
    {
      this.serverPeer = new RailPeerServer(netPeer);
      this.serverPeer.MessagesReady += this.OnMessagesReady;
    }

    public override void Update()
    {
      this.localTick++;

      this.UpdateCommands();
      this.UpdateWorld(this.serverClock.Tick());

      if ((this.serverPeer != null) && this.ShouldSend(this.localTick))
        this.SendPacket();
    }

    /// <summary>
    /// Updates the world a number of ticks. If we have entities waiting to be
    /// added, this function will check them and add them if applicable.
    /// </summary>
    private void UpdateWorld(int numTicks)
    {
      for (; numTicks > 0; numTicks--)
      {
        int serverTick = (this.serverClock.EstimatedRemote - numTicks) + 1;
        this.UpdatePending(serverTick);
        this.world.UpdateClient(serverTick);
      }
    }

    /// <summary>
    /// Polls the command generator to produce a command and adds it to the
    /// rolling outgoing command queue. TODO: Local prediction!
    /// </summary>
    private void UpdateCommands()
    {
      RailCommand command = RailResource.Instance.AllocateCommand();
      command.Populate();
      command.Tick = this.localTick;
      this.localController.QueueOutgoing(command);
    }

    /// <summary>
    /// Checks to see if any pending entities can be added to the world and
    /// adds them if applicable.
    /// </summary>
    private void UpdatePending(int serverTick)
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

    /// <summary>
    /// Packs and sends a client-to-server packet to the server.
    /// </summary>
    private void SendPacket()
    {
      RailClientPacket packet = RailResource.Instance.AllocateClientPacket();
      packet.Initialize(
        this.localTick,
        this.serverClock.LastReceivedRemote,
        this.lastReceivedEventId,
        this.localController.OutgoingCommands);
      this.interpreter.SendClientPacket(this.serverPeer, packet);
    }

    private void OnMessagesReady(RailPeerServer peer)
    {
      IEnumerable<RailServerPacket> decode =
        this.interpreter.ReceiveServerPackets(
          this.serverPeer, 
          this.knownEntities);

      foreach (RailServerPacket packet in decode)
      {
        this.ProcessPacket(packet);
        this.serverClock.UpdateLatest(packet.ServerTick);
      }
    }

    private void ProcessPacket(RailServerPacket packet)
    {
      foreach (RailState state in packet.States)
        this.ProcessState(state);

      foreach (RailEvent evnt in packet.Events)
        this.ProcessEvent(evnt);

      this.localController.CleanCommands(packet.LastProcessedCommandTick);
    }

    private void ProcessState(RailState state)
    {
      RailEntity entity;
      if (this.World.TryGetEntity(state.Id, out entity) == false)
        if (this.pendingEntities.TryGetValue(state.Id, out entity) == false)
          entity = this.ReplicateEntity(state);
      entity.StateBuffer.Store(state);
    }

    /// <summary>
    /// Creates an entity and adds it to the pending entity list.
    /// </summary>
    private RailEntity ReplicateEntity(RailState state)
    {
      RailEntity entity = state.CreateEntity();
      entity.InitializeClient(state);

      this.pendingEntities.Add(state.Id, entity);
      this.knownEntities.Add(state.Id, entity);

      return entity;
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

    private void ProcessEvent(RailEvent evnt)
    {
      // Skip already processed events
      if (evnt.EventId.IsReliable)
      {
        if (this.lastReceivedEventId.IsValid == false)
          this.lastReceivedEventId = evnt.EventId;
        else if (evnt.EventId.IsNewerThan(this.lastReceivedEventId))
          this.lastReceivedEventId = evnt.EventId;
        else
          return;
      }

      // TODO: Move this to a more comprehensive solution
      switch (evnt.EventType)
      {
        case RailEventTypes.TYPE_CONTROL:
          RailControlEvent controlEvent = (RailControlEvent)evnt;
          RailEntity entity = this.GetEntity(controlEvent.EntityId);
          if (entity != null)
            this.localController.AddEntity(entity);
          break;

        default:
          CommonDebug.LogWarning("Unrecognized event: " + evnt.EventType);
          break;
      }
    }
  }
}
