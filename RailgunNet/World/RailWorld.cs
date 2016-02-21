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

namespace Railgun
{
  public class RailWorld
  {
    public int Tick { get; internal protected set; }

    private int nextEntityId;
    private Dictionary<int, RailEntity> entities;

    internal RailWorld()
    {
      this.entities = new Dictionary<int, RailEntity>();
      this.nextEntityId = 1;
      this.Tick = 0;
    }

    /// <summary>
    /// Creates an entity of a given type. Note that this function does NOT
    /// add the entity to the environment. You should configure the entity
    /// and then call AddEntity().
    /// </summary>
    public T CreateEntity<T>(int type)
      where T : RailEntity
    {
      RailState state = RailResource.Instance.AllocateState(type);
      RailEntity entity = state.CreateEntity();

      entity.IsMaster = true;
      entity.Id = this.nextEntityId++;
      entity.State = state;

      return (T)entity;
    }

    /// <summary>
    /// Adds an entity to the host's environment. This entity will be
    /// replicated over the network to all client peers.
    /// </summary>
    public void AddEntity(RailEntity entity)
    {
      this.entities.Add(entity.Id, entity);
      entity.World = this;
      entity.OnAddedToWorld();
    }

    public void RemoveEntity(RailEntity entity)
    {
      // TODO
    }

    /// <summary>
    /// Creates a new entity that arrived via snapshot.
    /// </summary>
    internal void ReplicateEntity(RailImage image)
    {
      RailState state = RailResource.Instance.AllocateState(image.Type);
      RailEntity entity = state.CreateEntity();

      state.SetFrom(image.State);

      entity.IsMaster = false;
      entity.Id = image.Id;
      entity.State = state;

      this.AddEntity(entity);
    }

    /// <summary>
    /// Applies a snapshot by adding/removing entities or by
    /// updating existing ones.
    /// </summary>
    internal void ApplySnapshot(RailSnapshot snapshot)
    {
      foreach (RailImage image in snapshot.GetValues())
      {
        RailEntity entity;
        if (this.entities.TryGetValue(image.Id, out entity))
        {
          entity.State.SetFrom(image.State);
          entity.NotifyStateUpdated(snapshot.Tick);
        }
        else
        {
          this.ReplicateEntity(image);
        }
      }

      // TODO: Entity removal
    }
    
    internal void UpdateHost()
    {
      this.Tick++;
      foreach (RailEntity entity in this.entities.Values)
        entity.OnUpdateHost();
    }

    internal RailSnapshot CreateSnapshot()
    {
      RailSnapshot output = RailResource.Instance.AllocateSnapshot();
      output.Tick = this.Tick;
      foreach (RailEntity entity in this.entities.Values)
        output.Add(entity.CreateImage());
      return output;
    }
  }
}
