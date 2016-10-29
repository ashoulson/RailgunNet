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
using System.Collections.Generic;

namespace Railgun
{
  public abstract class RailRoom
  {
#if SERVER
    /// <summary>
    /// Fired when a controller has been added (i.e. player join).
    /// The controller has control of no entities at this point.
    /// </summary>
    public event Action<RailController> ControllerJoined;

    /// <summary>
    /// Fired when a controller has been removed (i.e. player leave).
    /// This event fires before the controller has control of its entities
    /// revoked (this is done immediately afterwards).
    /// </summary>
    public event Action<RailController> ControllerLeft;
#endif

    /// <summary>
    /// Fired before all entities have updated, for updating global logic.
    /// </summary>
    public event Action<Tick> PreRoomUpdate;

    /// <summary>
    /// Fired after all entities have updated, for updating global logic.
    /// </summary>
    public event Action<Tick> PostRoomUpdate;

    /// <summary>
    /// Notifies that we removed an entity.
    /// </summary>
    public event Action<RailEntity> EntityRemoved;

    public object UserData { get; set; }

    /// <summary>
    /// The current synchronized tick. On clients this will be the predicted
    /// server tick. On the server this will be the authoritative tick.
    /// </summary>
    public Tick Tick { get; internal protected set; }
    public IEnumerable<RailEntity> Entities { get { return this.entities.Values; } }

#if CLIENT
    public abstract RailController LocalController { get; }
#endif

    protected List<EntityId> toRemove; // Pre-allocated removal list
    protected virtual void HandleRemovedEntity(EntityId entityId) { }

    private readonly RailConnection connection;
    private readonly Dictionary<EntityId, RailEntity> entities;

    public bool TryGet(EntityId id, out RailEntity value)
    {
      return this.entities.TryGetValue(id, out value);
    }

    /// <summary>
    /// Queues an event to broadcast to the server (for clients) or 
    /// to all present clients (for the server) with a number of retries.
    /// Use a RailEvent.SEND_RELIABLE (-1) for the number of attempts
    /// to send the event reliable-ordered (infinite retries).
    /// </summary>
    public abstract void BroadcastEvent(RailEvent evnt, int attempts = 3);

#if SERVER
    public abstract T AddNewEntity<T>() where T : RailEntity;
    public abstract void RemoveEntity(RailEntity entity);
#endif

    internal RailRoom(RailConnection connection)
    {
      this.connection = connection;
      this.entities = new Dictionary<EntityId, RailEntity>(EntityId.Comparer);
      this.Tick = Tick.INVALID;
      this.toRemove = new List<EntityId>();
    }

    internal void Initialize(Tick tick)
    {
      this.Tick = tick;
    }

    protected void OnPreRoomUpdate(Tick tick)
    {
      this.PreRoomUpdate?.Invoke(tick);
    }

    protected void OnPostRoomUpdate(Tick tick)
    {
      this.PostRoomUpdate?.Invoke(tick);
    }

#if SERVER
    protected void OnControllerJoined(RailController controller)
    {
      this.ControllerJoined?.Invoke(controller);
    }

    protected void OnControllerLeft(RailController controller)
    {
      this.ControllerLeft?.Invoke(controller);
    }
#endif

    protected void RemoveEntity(EntityId entityId)
    {
      RailEntity entity;
      if (this.entities.TryGetValue(entityId, out entity))
      {
        this.entities.Remove(entityId);
        entity.Cleanup();
        entity.Room = null;
        // TODO: Pooling?

        this.HandleRemovedEntity(entityId);
        this.EntityRemoved?.Invoke(entity);
      }
    }

    protected IEnumerable<RailEntity> GetAllEntities()
    {
      // TODO: This makes multiple full passes, could probably optimize
      foreach (RailConfig.RailUpdateOrder order in RailConfig.Orders)
        foreach (RailEntity entity in this.entities.Values)
          if (entity.UpdateOrder == order)
            yield return entity;
    }

    protected void RegisterEntity(RailEntity entity)
    {
      this.entities.Add(entity.Id, entity);
      entity.Room = this;
    }
  }
}
