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
  // This struct is the sort of thing that would be great to code-generate, but
  // since there's only a couple of them at most the complexity isn't worth it
  public struct EntityState
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

    #region Serialization Functions
    /// <summary>
    /// Write a fully populated encoding of the state.
    /// </summary>
    public static void Encode(BitPacker bitPacker, ref EntityState state)
    {
      bitPacker.Push(state.userId,      Encoders.UserId);
      bitPacker.Push(state.entityId,    Encoders.EntityId);
      bitPacker.Push(state.archetypeId, Encoders.ArchetypeId);
      bitPacker.Push(state.x,           Encoders.Coordinate);
      bitPacker.Push(state.y,           Encoders.Coordinate);
      bitPacker.Push(state.angle,       Encoders.Angle);
      bitPacker.Push(state.status,      Encoders.Status);

      bitPacker.Push(EntityState.FLAG_ALL, Encoders.EntityFlag);
    }

    /// <summary>
    /// Delta-encode this state relative to the given basis.
    /// </summary>
    public static void Encode(BitPacker bitPacker, ref EntityState state, ref EntityState basis)
    {
      int flags =
        bitPacker.PushIf(state.userId != basis.userId,                       state.userId,      Encoders.UserId,      FLAG_USER_ID) |
        bitPacker.PushIf(state.entityId != basis.entityId,                   state.entityId,    Encoders.EntityId,    FLAG_ENTITY_ID) |
        bitPacker.PushIf(state.archetypeId != basis.archetypeId,             state.archetypeId, Encoders.ArchetypeId, FLAG_ARCHETYPE_ID) |
        bitPacker.PushIf(!RailgunMath.CoordinatesEqual(state.x, basis.x),    state.x,           Encoders.Coordinate,  FLAG_X) |
        bitPacker.PushIf(!RailgunMath.CoordinatesEqual(state.y, basis.y),    state.y,           Encoders.Coordinate,  FLAG_Y) |
        bitPacker.PushIf(!RailgunMath.AnglesEqual(state.angle, basis.angle), state.angle,       Encoders.Angle,       FLAG_ANGLE) |
        bitPacker.PushIf(state.status != basis.status,                       state.status,      Encoders.Status,      FLAG_STATUS);
      bitPacker.Push(flags, Encoders.EntityFlag);
    }

    /// <summary>
    /// Decode a fully populated data packet.
    /// </summary>
    public static EntityState Decode(BitPacker bitPacker)
    {
      int flags = bitPacker.Pop(Encoders.EntityFlag);
      Debug.Assert(flags == EntityState.FLAG_ALL);

      // Use the backwards constructor
      return new EntityState(
        bitPacker.Pop(Encoders.Status),
        bitPacker.Pop(Encoders.Angle),
        bitPacker.Pop(Encoders.Coordinate),
        bitPacker.Pop(Encoders.Coordinate),
        bitPacker.Pop(Encoders.ArchetypeId),
        bitPacker.Pop(Encoders.EntityId),
        bitPacker.Pop(Encoders.UserId));
    }

    /// <summary>
    /// Decode a delta-encoded packet against a given basis.
    /// </summary>
    public static EntityState Decode(BitPacker bitPacker, ref EntityState basis)
    {
      int flags = bitPacker.Pop(Encoders.EntityFlag);

      return new EntityState(
        bitPacker.PopIf(flags, FLAG_STATUS,       Encoders.Status,      basis.status),
        bitPacker.PopIf(flags, FLAG_ANGLE,        Encoders.Angle,       basis.angle),
        bitPacker.PopIf(flags, FLAG_Y,            Encoders.Coordinate,  basis.y),
        bitPacker.PopIf(flags, FLAG_X,            Encoders.Coordinate,  basis.x),
        bitPacker.PopIf(flags, FLAG_ARCHETYPE_ID, Encoders.ArchetypeId, basis.archetypeId),
        bitPacker.PopIf(flags, FLAG_ENTITY_ID,    Encoders.EntityId,    basis.entityId),
        bitPacker.PopIf(flags, FLAG_USER_ID,      Encoders.UserId,      basis.userId));
    }
    #endregion

    private readonly int userId;
    private readonly int entityId;
    private readonly int archetypeId;
    private readonly float x;
    private readonly float y;
    private readonly float angle;
    private readonly int status;

    public int UserId { get { return this.userId; } }
    public int EntityId { get { return this.entityId; } }
    public int ArchetypeId { get { return this.archetypeId; } }
    public float X { get { return this.x; } }
    public float Y { get { return this.y; } }
    public float Angle { get { return this.angle; } }
    public int Status { get { return this.status; } }

    public EntityState(
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
    /// Backwards-parameter constructor. A cheeky, probably stupid trick for
    /// code clarity in the serialization functions.
    /// </summary>
    public EntityState(
      int status,
      float angle,
      float y,
      float x,
      int archetypeId,
      int entityId,
      int userId)
    {
      this.userId = userId;
      this.entityId = entityId;
      this.archetypeId = archetypeId;

      this.x = x;
      this.y = y;
      this.angle = angle;

      this.status = status;
    }

    #region Debug
    public static void Test(int iterations)
    {
      BitPacker bitPacker = new BitPacker();
      EntityState basis = new EntityState();
      int maxBitsUsed = 0;
      float sum = 0.0f;

      for (int i = 0; i < iterations; i++)
      {
        EntityState current =
          new EntityState(
            UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.UserId.MinValue, Encoders.UserId.MaxValue) : basis.UserId,
            UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.EntityId.MinValue, Encoders.EntityId.MaxValue) : basis.EntityId,
            UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.ArchetypeId.MinValue, Encoders.ArchetypeId.MaxValue) : basis.ArchetypeId,
            UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.Coordinate.MinValue, Encoders.Coordinate.MaxValue) : basis.X,
            UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.Coordinate.MinValue, Encoders.Coordinate.MaxValue) : basis.Y,
            UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.Angle.MinValue, Encoders.Angle.MaxValue) : basis.Angle,
            UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.Status.MinValue, Encoders.Status.MaxValue) : basis.Status);

        float probability = UnityEngine.Random.Range(0.0f, 1.0f);
        if (probability > 0.5f)
        {
          EntityState.Encode(bitPacker, ref current);
          maxBitsUsed = bitPacker.BitsUsed;
          sum += (float)bitPacker.BitsUsed;
          EntityState decoded = EntityState.Decode(bitPacker);
          EntityState.TestCompare(current, decoded);
        }
        else
        {
          EntityState.Encode(bitPacker, ref current, ref basis);
          sum += (float)bitPacker.BitsUsed;
          EntityState decoded = EntityState.Decode(bitPacker, ref basis);
          EntityState.TestCompare(current, decoded);
        }

        basis = current;
      }

      Debug.Log("EntityState Max: " + maxBitsUsed + ", Avg: " + (sum / (float)iterations));
    }

    private static void TestCompare(EntityState a, EntityState b)
    {
      Debug.Assert(a.UserId == b.UserId, "UserId mismatch: " + (a.UserId - b.UserId));
      Debug.Assert(a.EntityId == b.EntityId, "EntityId mismatch: " + (a.EntityId - b.EntityId));
      Debug.Assert(a.ArchetypeId == b.ArchetypeId, "ArchetypeId mismatch: " + (a.ArchetypeId - b.ArchetypeId));
      Debug.Assert(RailgunMath.CoordinatesEqual(a.X, b.X), "X mismatch: " + (a.X - b.X));
      Debug.Assert(RailgunMath.CoordinatesEqual(a.Y, b.Y), "Y mismatch: " + (a.Y - b.Y));
      Debug.Assert(RailgunMath.AnglesEqual(a.Angle, b.Angle), "Angle mismatch: " + (a.Angle - b.Angle));
      Debug.Assert(a.Status == b.Status, "Status mismatch: " + (a.Status - b.Status));
    }
    #endregion
  }
}
