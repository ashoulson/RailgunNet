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
  public static class EntityIdExtensions
  {
    public static void WriteEntityId(this ByteBuffer buffer, EntityId eventId)
    {
      buffer.WriteInt(eventId.Pack());
    }

    public static EntityId ReadEntityId(this ByteBuffer buffer)
    {
      return EntityId.Unpack(buffer.ReadInt());
    }

    public static EntityId PeekEntityId(this ByteBuffer buffer)
    {
      return EntityId.Unpack(buffer.PeekInt());
    }
  }

  public struct EntityId
  {
    #region Encoding/Decoding
    internal int Pack()
    {
      return this.idValue;
    }

    internal static EntityId Unpack(int value)
    {
      return new EntityId(value);
    }
    #endregion

    public class EntityIdComparer : IEqualityComparer<EntityId>
    {
      public bool Equals(EntityId x, EntityId y)
      {
        return (x.idValue == y.idValue);
      }

      public int GetHashCode(EntityId x)
      {
        return x.idValue;
      }
    }

    internal static readonly EntityId INVALID = new EntityId(0);
    internal static readonly EntityIdComparer Comparer = new EntityIdComparer();

    public bool IsValid 
    { 
      get { return this.idValue > 0; } 
    }

    private readonly int idValue;

    private EntityId(int idValue)
    {
      this.idValue = idValue;
    }

    public EntityId GetNext()
    {
      return new EntityId(this.idValue + 1);
    }

    public override int GetHashCode()
    {
      return this.idValue;
    }

    public override bool Equals(object obj)
    {
      if (obj is EntityId)
        return (((EntityId)obj).idValue == this.idValue);
      return false;
    }

    public override string ToString()
    {
      return "EntityId:" + this.idValue;
    }
  }
}
