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
    public const int INVALID_ID = -1;

    public int Tick { get; internal protected set; }

    // TODO: Rollover? Free list?
    private int nextEntityId;

    private Dictionary<int, RailEntity> entities;

    public bool TryGetEntity(int id, out RailEntity value)
    {
      return this.entities.TryGetValue(id, out value);
    }

    internal RailWorld()
    {
      this.entities = new Dictionary<int, RailEntity>();
      this.nextEntityId = 1;
      this.Tick = 0;
    }

    internal int GetEntityId()
    {
      return this.nextEntityId++;
    }

    /// <summary>
    /// Adds an entity to the world and notifies it that it has been added.
    /// </summary>
    internal void AddEntity(RailEntity entity)
    {
      this.entities.Add(entity.Id, entity);
      entity.World = this;
      entity.AddedToWorld();
    }

    internal void RemoveEntity(RailEntity entity)
    {
      // TODO
    }
    
    internal void UpdateServer()
    {
      this.Tick++;
      foreach (RailEntity entity in this.entities.Values)
        entity.UpdateServer();
    }

    internal void UpdateClient(int serverTick)
    {
      this.Tick = serverTick;
      foreach (RailEntity entity in this.entities.Values)
        entity.UpdateClient(serverTick);
    }

    internal RailSnapshot CreateSnapshot()
    {
      RailSnapshot output = RailResource.Instance.AllocateSnapshot();

      output.Tick = this.Tick;

      foreach (RailEntity entity in this.entities.Values)
        output.Add(entity.CloneForSnapshot(this.Tick));
      return output;
    }
  }
}
