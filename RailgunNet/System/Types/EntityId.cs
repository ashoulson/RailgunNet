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
  public struct EntityId
  {
    public class EntityIdComparer : IEqualityComparer<EntityId>
    {
      public bool Equals(EntityId x, EntityId y)
      {
        return (x.entityId == y.entityId);
      }

      public int GetHashCode(EntityId x)
      {
        return x.entityId;
      }
    }

    internal static readonly EntityId INVALID = new EntityId(0);

    internal static readonly EntityIdComparer Comparer = 
      new EntityIdComparer();

    internal static readonly IntEncoder Encoder =
      new IntEncoder(0, RailConfig.MAX_ENTITY_COUNT + 1); // ID 0 is invalid

    /// <summary>
    /// Cost is static for all possible values within range.
    /// </summary>
    internal static int Cost
    {
      get { return EntityId.Encoder.GetCost(EntityId.INVALID.entityId); }
    }

    public bool IsValid 
    { 
      get { return this.entityId > 0; } 
    }

    private readonly int entityId;

    internal EntityId(int entityId)
    {
      this.entityId = entityId;
    }

    public EntityId GetNext()
    {
      return new EntityId(this.entityId + 1);
    }

    public override int GetHashCode()
    {
      return this.entityId;
    }

    public override bool Equals(object obj)
    {
      if (obj is EntityId)
        return (((EntityId)obj).entityId == this.entityId);
      return false;
    }

    internal void Write(BitBuffer buffer)
    {
      EntityId.Encoder.Write(buffer, this.entityId);
    }

    internal static EntityId Read(BitBuffer buffer)
    {
      return new EntityId(EntityId.Encoder.Read(buffer));
    }

    internal static EntityId Peek(BitBuffer buffer)
    {
      return new EntityId(EntityId.Encoder.Peek(buffer));
    }
  }
}
