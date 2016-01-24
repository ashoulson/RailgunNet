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

using UnityEngine;

namespace Railgun
{
  internal struct EntityState
  {
    #region Flags
    private const int  FLAG_USER_ID        = 0x01;
    private const int  FLAG_ENTITY_ID      = 0x02;
    private const int  FLAG_ARCHETYPE_ID   = 0x04;
    private const int  FLAG_X              = 0x08;
    private const int  FLAG_Y              = 0x10;
    private const int  FLAG_ANGLE          = 0x20;
    private const int  FLAG_STATUS         = 0x40;

    internal const int FLAG_ALL            = 0x7F;
    #endregion

    private readonly int userId;
    private readonly int entityId;
    private readonly int archetypeId;

    private readonly float x;
    private readonly float y;
    private readonly float angle;

    private readonly int status;

    internal EntityState(
      int userId,
      int entityId,
      int archetypeId,
      float x,
      float y,
      float angle,
      int status)
    {
      this.userId = userId;
      this.entityId = entityId;
      this.archetypeId = archetypeId;

      this.x = x;
      this.y = y;
      this.angle = angle;

      this.status = status;
    }

    /// <summary>
    /// Write a fully populated encoding of the state.
    /// </summary>
    internal void Encode(BitPacker bitPacker)
    {
      bitPacker.Push(this.userId,      Encoders.UserIdEncoder);
      bitPacker.Push(this.entityId,    Encoders.EntityIdEncoder);
      bitPacker.Push(this.archetypeId, Encoders.ArchetypeIdEncoder);
      bitPacker.Push(this.x,           Encoders.CoordinateEncoder);
      bitPacker.Push(this.y,           Encoders.CoordinateEncoder);
      bitPacker.Push(this.angle,       Encoders.AngleEncoder);
      bitPacker.Push(this.status,      Encoders.StatusEncoder);

      bitPacker.Push(EntityState.FLAG_ALL, Encoders.EntityFlagEncoder);
    }

    /// <summary>
    /// Delta-encode this state relative to the given basis.
    /// </summary>
    internal void Encode(BitPacker bitPacker, EntityState basis)
    {
      int flags = 
        PushIf(userId != basis.userId,           userId,      Encoders.UserIdEncoder,      bitPacker, FLAG_USER_ID) |
        PushIf(entityId != basis.entityId,       entityId,    Encoders.EntityIdEncoder,    bitPacker, FLAG_ENTITY_ID) |
        PushIf(archetypeId != basis.archetypeId, archetypeId, Encoders.ArchetypeIdEncoder, bitPacker, FLAG_ARCHETYPE_ID) |
        PushIf(CoordinateCompare(x, basis.x),    x,           Encoders.CoordinateEncoder,  bitPacker, FLAG_X) |
        PushIf(CoordinateCompare(y, basis.y),    y,           Encoders.CoordinateEncoder,  bitPacker, FLAG_Y) |
        PushIf(AngleCompare(angle, basis.angle), angle,       Encoders.AngleEncoder,       bitPacker, FLAG_ANGLE) |
        PushIf(status != basis.status,           status,      Encoders.StatusEncoder,      bitPacker, FLAG_STATUS);

      bitPacker.Push(flags, Encoders.EntityFlagEncoder);
    }

    private bool CoordinateCompare(float a, float b)
    {
      return Mathf.Abs(a - b) > Config.COORDINATE_EPSILON;
    }

    private bool AngleCompare(float a, float b)
    {
      return Mathf.Abs(a - b) > Config.ANGLE_EPSILON;
    }

    private int PushIf<T>(
      bool condition, 
      float value, 
      IEncoder<T> encoder, 
      BitPacker bitPacker, 
      int flag)
    {
      if (condition)
      {
        bitPacker.Push(this.userId, Encoders.UserIdEncoder);
        return flag;
      }
      return 0;
    }
  }
}
