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
using System;
using System.Collections.Generic;

namespace Railgun
{
  internal class RailServerRoom : RailRoom
  {
    /// <summary>
    /// Used for creating new entities and assigning them unique ids.
    /// </summary>
    private EntityId nextEntityId = EntityId.START;

    /// <summary>
    /// All client controllers involved in this room. 
    /// Does not include the server's controller.
    /// </summary>
    private readonly HashSet<RailController> clients;

    /// <summary>
    /// The local Railgun server.
    /// </summary>
    private readonly RailServer server;

    internal RailServerRoom(RailResource resource, RailServer server)
      : base(resource, server)
    {
      this.clients = new HashSet<RailController>();
      this.server = server;
    }

    /// <summary>
    /// Adds an entity to the room. Cannot be done during the update pass.
    /// </summary>
    public override T AddNewEntity<T>()
    {
      T entity = this.CreateEntity<T>();
      this.RegisterEntity(entity);
      return entity;
    }

    /// <summary>
    /// Marks an entity for removal from the room and presumably destruction.
    /// This is deferred until the next frame.
    /// </summary>
    public override void MarkForRemoval(IRailEntity entity)
    {
      if (entity.IsRemoving == false)
      {
        entity.AsBase.MarkForRemoval();
        this.server.LogRemovedEntity(entity);
      }
    }

    public override void BroadcastEvent(
      RailEvent evnt,
      ushort attempts = 3,
      bool freeWhenDone = true)
    {
      foreach (RailController client in this.clients)
        client.SendEvent(evnt, attempts);
      if (freeWhenDone)
        evnt.Free();
    }

    internal void AddClient(RailController client)
    {
      this.clients.Add(client);
      this.OnClientJoined(client);
    }

    internal void RemoveClient(RailController client)
    {
      this.clients.Remove(client);
      this.OnClientLeft(client);
    }

    internal void ServerUpdate()
    {
      this.Tick = this.Tick.GetNext();
      this.OnPreRoomUpdate(this.Tick);

      // Collect the entities in the priority order and
      // separate them out for either update or removal
      foreach (RailEntity entity in this.GetAllEntities())
        if (entity.ShouldRemove)
          this.toRemove.Add(entity);
        else
          this.toUpdate.Add(entity);

      // Wave 0: Remove all sunsetted entities
      for (int i = 0; i < this.toRemove.Count; i++)
        this.RemoveEntity(toRemove[i]);

      // Wave 1: Start/initialize all entities
      for (int i = 0; i < this.toUpdate.Count; i++)
        this.toUpdate[i].Startup();

      // Wave 2: Update all entities
      for (int i = 0; i < this.toUpdate.Count; i++)
        this.toUpdate[i].ServerUpdate();

      // Wave 3: Post-update all entities
      for (int i = 0; i < this.toUpdate.Count; i++)
        this.toUpdate[i].PostUpdate();

      this.toRemove.Clear();
      this.toUpdate.Clear();
      this.OnPostRoomUpdate(this.Tick);
    }

    internal void StoreStates()
    {
      foreach (RailEntity entity in this.Entities)
        entity.StoreRecord();
    }

    private T CreateEntity<T>() where T : RailEntity
    {
      T entity = RailEntity.Create<T>(this.resource);
      entity.AssignId(this.nextEntityId);
      this.nextEntityId = this.nextEntityId.GetNext();
      return (T)entity;
    }
  }
}
#endif
