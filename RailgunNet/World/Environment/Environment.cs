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
  public class Environment : Snapshot
  {
    private Dictionary<int, Entity> idToEntity;

    public Environment()
    {
      this.idToEntity = new Dictionary<int, Entity>();
    }

    internal void SetFrame(int frame)
    {
      this.Frame = frame;
    }

    public void Update()
    {
      foreach (Entity entity in this.idToEntity.Values)
        entity.Update();
    }

    public void Add(Entity entity)
    {
      base.Add(entity);
      this.idToEntity.Add(entity.Id, entity);
    }

    public void Remove(Entity entity)
    {
      base.Remove(entity);
      this.idToEntity.Remove(entity.Id);
    }

    protected override void Reset()
    {
      base.Reset();
      this.idToEntity.Clear();
    }
  }
}
