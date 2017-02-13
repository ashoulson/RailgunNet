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

using System.Collections.Generic;

namespace Railgun
{
  public static class EntityIdExtensions
  {
    public static void WriteEntityId(this RailBitBuffer buffer, EntityId entityId)
    {
      entityId.Write(buffer);
    }

    public static EntityId ReadEntityId(this RailBitBuffer buffer)
    {
      return EntityId.Read(buffer);
    }

    public static EntityId PeekEntityId(this RailBitBuffer buffer)
    {
      return EntityId.Peek(buffer);
    }
  }

  public struct EntityId
  {
    #region Encoding/Decoding

    #region Byte Writing
    public int PutBytes(
      byte[] buffer, 
      int startIndex)
    {
      return RailUtil.PutBytes(this.idValue, buffer, startIndex);
    }

    public static EntityId ReadBytes(
      byte[] buffer, 
      int startIndex, 
      out int length)
    {
      return new EntityId(RailUtil.ReadBytes(buffer, startIndex, out length));
    }
    #endregion

    internal void Write(RailBitBuffer buffer)
    {
      buffer.WriteUInt(this.idValue);
    }

    internal static EntityId Read(RailBitBuffer buffer)
    {
      return new EntityId(buffer.ReadUInt());
    }

    internal static EntityId Peek(RailBitBuffer buffer)
    {
      return new EntityId(buffer.PeekUInt());
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
        return (int)x.idValue;
      }
    }

    public static readonly EntityId INVALID = new EntityId(0);
    internal static readonly EntityId START = new EntityId(1);

    public static IEqualityComparer<EntityId> CreateEqualityComparer()
    {
      return new EntityIdComparer();
    }

    public bool IsValid 
    { 
      get { return this.idValue > 0; } 
    }

    private readonly uint idValue;

    private EntityId(uint idValue)
    {
      this.idValue = idValue;
    }

    public EntityId GetNext()
    {
      return new EntityId(this.idValue + 1);
    }

    public override int GetHashCode()
    {
      return (int)this.idValue;
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
