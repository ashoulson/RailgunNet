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
  public class RailRoom
  {
    /// <summary>
    /// Fired before all entities have updated, for updating global logic.
    /// </summary>
    public event Action<Tick> PreRoomUpdate;

    /// <summary>
    /// Fired after all entities have updated, for updating global logic.
    /// </summary>
    public event Action<Tick> PostRoomUpdate;

    public object UserData { get; set; }

    /// <summary>
    /// The current synchronized tick. On clients this will be the predicted
    /// server tick. On the server this will be the authoritative tick.
    /// </summary>
    public Tick Tick { get; internal protected set; }
    public IEnumerable<RailEntity> Entities 
    { 
      get { return this.entities.Values; } 
    }

    private Dictionary<EntityId, RailEntity> entities;
    private List<EntityId> toRemove; // Pre-allocated removal list

    public bool TryGet(EntityId id, out RailEntity value)
    {
      return this.entities.TryGetValue(id, out value);
    }

    internal RailRoom()
    {
      this.entities = new Dictionary<EntityId, RailEntity>(EntityId.Comparer);
      this.Tick = Tick.INVALID;
      this.toRemove = new List<EntityId>();
    }

    internal void Initialize(Tick tick)
    {
      this.Tick = tick;
    }

    internal void AddEntity(RailEntity entity)
    {
      this.entities.Add(entity.Id, entity);
      entity.Room = this;
    }

    private void RemoveEntity(EntityId entityId)
    {
      RailEntity entity;
      if (this.entities.TryGetValue(entityId, out entity))
      {
        this.entities.Remove(entityId);
        entity.Room = null;
        entity.Cleanup();
      }
    }

#if SERVER
    internal void ServerUpdate()
    {
      this.Tick = this.Tick.GetNext();

      if (this.PreRoomUpdate != null)
        this.PreRoomUpdate.Invoke(this.Tick);

      foreach (RailEntity entity in this.GetAllEntities())
      {
        Tick removedTick = entity.RemovedTick;
        if (removedTick.IsValid && (removedTick <= this.Tick))
          this.toRemove.Add(entity.Id);
        else
          entity.ServerUpdate();
      }

      // Cleanup all entities marked for removal
      foreach (EntityId id in this.toRemove)
        this.RemoveEntity(id);
      this.toRemove.Clear();

      if (this.PostRoomUpdate != null)
        this.PostRoomUpdate.Invoke(this.Tick);
    }

    internal void StoreStates()
    {
      foreach (RailEntity entity in this.entities.Values)
        entity.StoreRecord();
    }
#endif
#if CLIENT
    internal void ClientUpdate(Tick localTick, Tick estimatedServerTick)
    {
      this.Tick = estimatedServerTick;

      if (this.PreRoomUpdate != null)
        this.PreRoomUpdate.Invoke(this.Tick);

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

      if (this.PostRoomUpdate != null)
        this.PostRoomUpdate.Invoke(this.Tick);
    }
#endif

    private IEnumerable<RailEntity> GetAllEntities()
    {
      // TODO: This makes multiple full passes, could probably optimize
      foreach (RailConfig.RailUpdateOrder order in RailConfig.Orders)
        foreach (RailEntity entity in this.entities.Values)
          if (entity.UpdateOrder == order)
            yield return entity;
    }
  }
}
