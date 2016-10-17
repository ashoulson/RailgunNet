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

#if CLIENT
using System.Collections.Generic;

namespace Railgun
{
  public class RailClient 
    : RailConnection
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

    // Pre-allocated removal list
    List<RailEntity> toRemove;

    public RailClient()
    {
      RailConnection.IsServer = false;
      this.serverPeer = null;
      this.localTick = Tick.START;
      this.Room.Initialize(Tick.INVALID);

      this.pendingEntities = 
        new Dictionary<EntityId, RailEntity>(EntityId.Comparer);
      this.knownEntities =
        new Dictionary<EntityId, RailEntity>(EntityId.Comparer);

      this.toRemove = new List<RailEntity>();
    }

    public void SetPeer(IRailNetPeer netPeer)
    {
      RailDebug.Assert(this.serverPeer == null, "Overwriting peer");
      this.serverPeer = new RailClientPeer(netPeer, this.Interpreter);
      this.serverPeer.PacketReceived += this.OnPacketReceived;
      this.serverPeer.EventReceived += base.OnEventReceived;
    }

    public override void Update()
    {
      if (this.serverPeer != null)
      {
        this.DoStart();
        this.serverPeer.Update();
        this.UpdateRoom(this.localTick, this.serverPeer.EstimatedRemoteTick);

        if (this.localTick.IsSendTick)
          this.serverPeer.SendPacket(
            this.localTick,
            this.serverPeer.ControlledEntities); // TODO: Sort me by most recently sent
        this.localTick++;
      }
    }

    /// <summary>
    /// Queues an event to broadcast to all clients.
    /// Use a RailEvent.SEND_RELIABLE (-1) for the number of attempts
    /// to send the event reliable-ordered (infinite retries).
    /// </summary>
    public void QueueEvent(RailEvent evnt, int attempts = 3)
    {
      this.serverPeer.QueueEvent(evnt, attempts);
    }

#region Local Updating
    /// <summary>
    /// Updates the room a number of ticks. If we have entities waiting to be
    /// added, this function will check them and add them if applicable.
    /// </summary>
    private void UpdateRoom(
      Tick localTick, 
      Tick estimatedServerTick)
    {
      this.UpdatePendingEntities(estimatedServerTick);
      this.Room.ClientUpdate(localTick, estimatedServerTick);
    }

    /// <summary>
    /// Checks to see if any pending entities can be added to the world and
    /// adds them if applicable.
    /// </summary>
    private void UpdatePendingEntities(Tick serverTick)
    {
      foreach (RailEntity entity in this.pendingEntities.Values)
      {
        if (entity.HasReadyState(serverTick))
        {
          this.Room.AddEntity(entity);
          this.toRemove.Add(entity);
        }
      }

      foreach (RailEntity entity in this.toRemove)
        this.pendingEntities.Remove(entity.Id);
      this.toRemove.Clear();
    }
#endregion

#region Packet Receive
    private void OnPacketReceived(IRailServerPacket packet)
    {
      foreach (RailStateDelta delta in packet.Deltas)
        this.ProcessDelta(delta);
    }

    private void ProcessDelta(RailStateDelta delta)
    {
      RailEntity entity;
      if (this.knownEntities.TryGetValue(delta.EntityId, out entity) == false)
      {
        RailDebug.Assert(delta.IsFrozen == false, "Frozen unknown entity");
        if (delta.IsFrozen)
          return;
          
        entity = delta.ProduceEntity();
        entity.AssignId(delta.EntityId);
        entity.PrimeState(delta);
        this.pendingEntities.Add(entity.Id, entity);
        this.knownEntities.Add(entity.Id, entity);
      }

      entity.ReceiveDelta(delta);
      this.UpdateControlStatus(entity, delta);
    }

    private void UpdateControlStatus(RailEntity entity, RailStateDelta delta)
    {
      // Can't infer anything if the delta is an empty frozen update
      if (delta.IsFrozen)
        return;

      if (delta.HasControllerData)
      {
        if (entity.Controller == null)
          this.serverPeer.GrantControl(entity);
      }
      else
      {
        if (entity.Controller != null)
          this.serverPeer.RevokeControl(entity);
      }
    }
#endregion
  }
}
#endif