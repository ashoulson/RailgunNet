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
  internal class RailClientRoom : RailRoom
  {
    /// <summary>
    /// Returns all locally-controlled entities in the room.
    /// </summary>
    internal IEnumerable<RailEntity> LocalEntities
    {
      get { return this.localPeer.ControlledEntities; }
    }

    /// <summary>
    /// Entities that are waiting to be added to the world.
    /// </summary>
    private Dictionary<EntityId, RailEntity> pendingEntities;

    /// <summary>
    /// All known entities, either in-world or pending.
    /// </summary>
    private Dictionary<EntityId, RailEntity> knownEntities;

    /// <summary>
    /// The local controller for predicting control and authority.
    /// This is a dummy peer that can't send or receive events.
    /// </summary>
    private readonly RailController localPeer;

    /// <summary>
    /// The local Railgun client.
    /// </summary>
    private readonly RailClient client;

    internal RailClientRoom(RailResource resource, RailClient client) 
      : base(resource, client)
    {
      IEqualityComparer<EntityId> entityIdComparer = 
        EntityId.CreateEqualityComparer();

      this.pendingEntities =
        new Dictionary<EntityId, RailEntity>(entityIdComparer);
      this.knownEntities =
        new Dictionary<EntityId, RailEntity>(entityIdComparer);
      this.localPeer = new RailController(resource);
      this.client = client;
    }

    protected override void HandleRemovedEntity(EntityId entityId)
    {
      this.knownEntities.Remove(entityId);
    }

    /// <summary>
    /// Queues an event to broadcast to the server with a number of retries.
    /// Caller should call Free() on the event when done sending.
    /// </summary>
    public override void RaiseEvent(RailEvent evnt, ushort attempts = 3)
    {
      this.client.RaiseEvent(evnt, attempts);
    }

    /// <summary>
    /// Updates the room a number of ticks. If we have entities waiting to be
    /// added, this function will check them and add them if applicable.
    /// </summary>
    internal void ClientUpdate(Tick localTick, Tick estimatedServerTick)
    {
      this.Tick = estimatedServerTick;
      this.UpdatePendingEntities(estimatedServerTick);
      this.OnPreRoomUpdate(this.Tick);

      // Perform regular update cadence and mark entities for removal
      foreach (RailEntity entity in this.GetAllEntities())
      {
        Tick removedTick = entity.RemovedTick;
        if (removedTick.IsValid && (removedTick <= this.Tick))
          this.toRemove.Add(entity.Id);
        else
          entity.ClientUpdate(localTick);
      }

      // Cleanup all entities marked for removal
      foreach (EntityId id in this.toRemove)
        this.RemoveEntity(id);
      this.toRemove.Clear();

      this.OnPostRoomUpdate(this.Tick);
    }

    /// <summary>
    /// Returns true iff we stored the delta.
    /// </summary>
    internal bool ProcessDelta(RailStateDelta delta)
    {
      RailEntity entity;
      if (this.knownEntities.TryGetValue(delta.EntityId, out entity) == false)
      {
        RailDebug.Assert(delta.IsFrozen == false, "Frozen unknown entity");
        if (delta.IsFrozen || delta.IsRemoving)
          return false;

        entity = delta.ProduceEntity(this.resource);
        entity.AssignId(delta.EntityId);
        entity.PrimeState(delta);
        this.pendingEntities.Add(entity.Id, entity);
        this.knownEntities.Add(entity.Id, entity);
      }

      // If we're already removing the entity, we don't care about other deltas
      bool stored = false;
      if (entity.IsRemoving == false)
      {
        stored = entity.ReceiveDelta(delta);
        this.UpdateControlStatus(entity, delta);
      }
      return stored;
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
          this.RegisterEntity(entity);
          this.toRemove.Add(entity.Id);
        }
      }

      foreach (EntityId entityId in this.toRemove)
        this.pendingEntities.Remove(entityId);
      this.toRemove.Clear();
    }

    private void UpdateControlStatus(RailEntity entity, RailStateDelta delta)
    {
      // Can't infer anything if the delta is an empty frozen update
      if (delta.IsFrozen)
        return;

      if (delta.IsRemoving)
      {
        if (entity.Controller != null)
          this.localPeer.RevokeControlInternal(entity);
      }
      else if (delta.HasControllerData)
      {
        if (entity.Controller == null)
          this.localPeer.GrantControlInternal(entity);
      }
      else
      {
        if (entity.Controller != null)
          this.localPeer.RevokeControlInternal(entity);
      }
    }
  }
}
#endif
