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
  public class RailWorld
  {
    public Tick Tick { get; internal protected set; }
    public IEnumerable<RailEntity> Entities 
    { 
      get { return this.entities.Values; } 
    }

    // TODO: Rollover? Free list?
    private EntityId nextEntityId;

    private Dictionary<EntityId, RailEntity> entities;

    public bool TryGetEntity(EntityId id, out RailEntity value)
    {
      return this.entities.TryGetValue(id, out value);
    }

    internal RailWorld()
    {
      this.entities = new Dictionary<EntityId, RailEntity>(EntityId.Comparer);
      this.nextEntityId = EntityId.INVALID.GetNext();
      this.Tick = Tick.INVALID;
    }

    internal void InitializeServer()
    {
      this.Tick = Tick.START;
    }

    internal void InitializeClient()
    {
      this.Tick = Tick.INVALID;
    }

    internal T CreateEntity<T>(int type)
      where T : RailEntity
    {
      // TODO: Get some #defines in for this kind of thing
      CommonDebug.Assert(RailConnection.IsServer);

      RailEntity entity = RailResource.Instance.CreateEntity(type);
      entity.AssignId(this.nextEntityId);
      this.nextEntityId = this.nextEntityId.GetNext();

      return (T)entity;
    }

    internal T CreateEntity<T>(int type, EntityId id)
      where T : RailEntity
    {
      // TODO: Get some #defines in for this kind of thing
      CommonDebug.Assert(RailConnection.IsServer == false);

      RailEntity entity = RailResource.Instance.CreateEntity(type);
      entity.AssignId(id);

      return (T)entity;
    }

    internal void AddEntity(RailEntity entity)
    {
      this.entities.Add(entity.Id, entity);
      entity.World = this;
    }

    internal void RemoveEntity(RailEntity entity)
    {
      // TODO
    }
    
    internal void UpdateServer()
    {
      this.Tick = this.Tick.GetNext();
      foreach (RailEntity entity in this.entities.Values)
        entity.UpdateServer(this.Tick);
    }

    internal void StoreStates()
    {
      foreach (RailEntity entity in this.entities.Values)
        entity.StoreState(this.Tick);
    }

    internal void UpdateClient(Tick serverTick)
    {
      this.Tick = serverTick;
      foreach (RailEntity entity in this.entities.Values)
        entity.UpdateClient(serverTick);
    }
  }
}
