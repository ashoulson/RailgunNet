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

using Reservoir;

namespace Railgun
{
  /// <summary>
  /// Entities represent physical objects in the game world.
  /// </summary>
  public class EntityState : IPoolable<EntityState>
  {
    // TODO: This struct is the sort of thing that would be great to code-
    // generate, but since there's only a couple of them at most the 
    // complexity hasn't seemed to be worth it so far
    #region Flags
    private const int  FLAG_ARCHETYPE_ID   = 0x01;
    private const int  FLAG_USER_ID        = 0x02;
    private const int  FLAG_X              = 0x04;
    private const int  FLAG_Y              = 0x08;
    private const int  FLAG_ANGLE          = 0x10;
    private const int  FLAG_STATUS         = 0x20;

    internal const int FLAG_ALL =
      FLAG_ARCHETYPE_ID |
      FLAG_USER_ID |
      FLAG_X |
      FLAG_Y |
      FLAG_ANGLE |
      FLAG_STATUS;
    #endregion

    #region IPoolable Members
    NodeList<EntityState> INode<EntityState>.List { get; set; }
    EntityState INode<EntityState>.Next { get; set; }
    EntityState INode<EntityState>.Previous { get; set; }

    Pool<EntityState> IPoolable<EntityState>.Pool { get; set; }
    #endregion

    #region Serialization Functions
    // TODO: Some of this is boilerplate and can be migrated if we want to
    // introduce a new entity type and share some read/write code between

    /// <summary>
    /// Peeks at an entity ID at the top of the bit buffer.
    /// </summary>
    /// <param name="bitPacker"></param>
    /// <returns></returns>
    public static int PeekId(BitPacker bitPacker)
    {
      return bitPacker.Peek(Encoders.EntityId);
    }

    /// <summary>
    /// Compares a state against a predecessor and returns a bit bucket of
    /// which fields are dirty.
    /// </summary>
    public static int GetDirtyFlags(
      EntityState state, 
      EntityState basis)
    {
      return
        (state.ArchetypeId == basis.ArchetypeId            ? 0 : FLAG_ARCHETYPE_ID) |
        (state.UserId == basis.UserId                      ? 0 : FLAG_USER_ID) |
        (RailgunMath.CoordinatesEqual(state.X, basis.X)    ? 0 : FLAG_X) |
        (RailgunMath.CoordinatesEqual(state.Y, basis.Y)    ? 0 : FLAG_Y) |
        (RailgunMath.AnglesEqual(state.Angle, basis.Angle) ? 0 : FLAG_ANGLE) |
        (state.Status == basis.Status                      ? 0 : FLAG_STATUS);
    }

    /// <summary>
    /// Write a fully populated encoding of this state.
    /// </summary>
    public void Encode(BitPacker bitPacker)
    {
      // Write in opposite order so we can read in constructor order
      bitPacker.Push(Encoders.Status,      this.Status);
      bitPacker.Push(Encoders.Angle,       this.Angle);
      bitPacker.Push(Encoders.Coordinate,  this.Y);
      bitPacker.Push(Encoders.Coordinate,  this.X);
      bitPacker.Push(Encoders.UserId,      this.UserId);
      bitPacker.Push(Encoders.ArchetypeId, this.ArchetypeId);

      // Add metadata
      bitPacker.Push(Encoders.EntityDirty, EntityState.FLAG_ALL);
      bitPacker.Push(Encoders.EntityId,    this.EntityId);
    }

    /// <summary>
    /// Delta-encode this state relative to the given basis state.
    /// Returns true iff the state was encoded (will bypass if no change).
    /// </summary>
    public bool Encode(BitPacker bitPacker, EntityState basis)
    {
      Debug.Assert(this.EntityId == basis.EntityId);
      int dirty = EntityState.GetDirtyFlags(this, basis);
      if (dirty == 0)
        return false;

      // Write in opposite order so we can read in constructor order
      bitPacker.PushIf(dirty, FLAG_STATUS,       Encoders.Status,      this.Status);
      bitPacker.PushIf(dirty, FLAG_ANGLE,        Encoders.Angle,       this.Angle);
      bitPacker.PushIf(dirty, FLAG_Y,            Encoders.Coordinate,  this.Y);
      bitPacker.PushIf(dirty, FLAG_X,            Encoders.Coordinate,  this.X);
      bitPacker.PushIf(dirty, FLAG_USER_ID,      Encoders.UserId,      this.UserId);
      bitPacker.PushIf(dirty, FLAG_ARCHETYPE_ID, Encoders.ArchetypeId, this.ArchetypeId);

      // Add metadata
      bitPacker.Push(Encoders.EntityDirty, dirty);
      bitPacker.Push(Encoders.EntityId,    this.EntityId);
      return true;
    }

    /// <summary>
    /// Decode a fully populated data packet and set values to this object.
    /// </summary>
    public void Decode(BitPacker bitPacker)
    {
      int entityId = bitPacker.Pop(Encoders.EntityId);
      int dirty = bitPacker.Pop(Encoders.EntityDirty);
      Debug.Assert(dirty == EntityState.FLAG_ALL);

      this.Set(
        entityId,
        bitPacker.Pop(Encoders.ArchetypeId),
        bitPacker.Pop(Encoders.UserId),
        bitPacker.Pop(Encoders.Coordinate),
        bitPacker.Pop(Encoders.Coordinate),
        bitPacker.Pop(Encoders.Angle),
        bitPacker.Pop(Encoders.Status));
    }

    /// <summary>
    /// Decode a delta-encoded packet against a given basis and set values
    /// to this object.
    /// </summary>
    public void Decode(BitPacker bitPacker, EntityState basis)
    {
      int entityId = bitPacker.Pop(Encoders.EntityId);
      int dirty = bitPacker.Pop(Encoders.EntityDirty);
      Debug.Assert(entityId == basis.EntityId);

      this.Set(
        entityId,
        bitPacker.PopIf(dirty, FLAG_ARCHETYPE_ID, Encoders.ArchetypeId, basis.ArchetypeId),
        bitPacker.PopIf(dirty, FLAG_USER_ID,      Encoders.UserId,      basis.UserId),
        bitPacker.PopIf(dirty, FLAG_X,            Encoders.Coordinate,  basis.X),
        bitPacker.PopIf(dirty, FLAG_Y,            Encoders.Coordinate,  basis.Y),
        bitPacker.PopIf(dirty, FLAG_ANGLE,        Encoders.Angle,       basis.Angle),
        bitPacker.PopIf(dirty, FLAG_STATUS,       Encoders.Status,      basis.Status));
    }
    #endregion

    public int   EntityId    { get; private set; }
    public int   ArchetypeId { get; private set; }
    public int   UserId      { get; private set; }
    public float X           { get; private set; }
    public float Y           { get; private set; }
    public float Angle       { get; private set; }
    public int   Status      { get; private set; }

    public void Set(
      int entityId,
      int archetypeId,
      int userId,
      float x,
      float y,
      float angle,
      int status)
    {
      this.EntityId = entityId;
      this.ArchetypeId = archetypeId;
      this.UserId = userId;
      this.X = x;
      this.Y = y;
      this.Angle = angle;
      this.Status = status;
    }

    public void Set(EntityState other)
    {
      this.EntityId = other.EntityId;
      this.ArchetypeId = other.ArchetypeId;
      this.UserId = other.UserId;
      this.X = other.X;
      this.Y = other.Y;
      this.Angle = other.Angle;
      this.Status = other.Status;
    }

    public void Initialize() { }
    public void Reset() { this.Set(0, 0, 0, 0.0f, 0.0f, 0.0f, 0); }

    #region Debug
    public static void Test(int iterations)
    {
      int entityId = UnityEngine.Random.Range(Encoders.EntityId.MinValue, Encoders.EntityId.MaxValue);

      BitPacker bitPacker = new BitPacker();

      // Normally these are pooled, but we'll just allocate some here
      EntityState basis = new EntityState();
      EntityState current = new EntityState();
      EntityState decoded = new EntityState();
      basis.Set(entityId, 0, 0, 0.0f, 0.0f, 0.0f, 0);

      int maxBitsUsed = 0;
      float sum = 0.0f;

      for (int i = 0; i < iterations; i++)
      {
        EntityState.GenerateState(basis, current);

        float probability = UnityEngine.Random.Range(0.0f, 1.0f);
        if (probability > 0.5f)
        {
          current.Encode(bitPacker);
          maxBitsUsed = bitPacker.BitsUsed;
          sum += (float)bitPacker.BitsUsed;
          decoded.Decode(bitPacker);
          EntityState.TestCompare(current, decoded);
        }
        else
        {
          if (current.Encode(bitPacker, basis))
          {
            sum += (float)bitPacker.BitsUsed;
            decoded.Decode(bitPacker, basis);
            EntityState.TestCompare(current, decoded);
          }
        }

        basis.Set(current);
      }

      Debug.Log("EntityState Max: " + maxBitsUsed + ", Avg: " + (sum / (float)iterations));
    }

    internal static void TestCompare(EntityState a, EntityState b)
    {
      RailgunUtil.Assert(a.EntityId == b.EntityId, "EntityId mismatch: " + (a.EntityId - b.EntityId));
      RailgunUtil.Assert(a.ArchetypeId == b.ArchetypeId, "ArchetypeId mismatch: " + (a.ArchetypeId - b.ArchetypeId));
      RailgunUtil.Assert(a.UserId == b.UserId, "UserId mismatch: " + (a.UserId - b.UserId));
      RailgunUtil.Assert(RailgunMath.CoordinatesEqual(a.X, b.X), "X mismatch: " + (a.X - b.X));
      RailgunUtil.Assert(RailgunMath.CoordinatesEqual(a.Y, b.Y), "Y mismatch: " + (a.Y - b.Y));
      RailgunUtil.Assert(RailgunMath.AnglesEqual(a.Angle, b.Angle), "Angle mismatch: " + (a.Angle - b.Angle));
      RailgunUtil.Assert(a.Status == b.Status, "Status mismatch: " + (a.Status - b.Status));
    }

    internal static void GenerateState(int entityId, EntityState destination)
    {
      destination.Set(
        entityId,
        UnityEngine.Random.Range(Encoders.ArchetypeId.MinValue, Encoders.ArchetypeId.MaxValue),
        UnityEngine.Random.Range(Encoders.UserId.MinValue, Encoders.UserId.MaxValue),
        UnityEngine.Random.Range(Encoders.Coordinate.MinValue, Encoders.Coordinate.MaxValue),
        UnityEngine.Random.Range(Encoders.Coordinate.MinValue, Encoders.Coordinate.MaxValue),
        UnityEngine.Random.Range(Encoders.Angle.MinValue, Encoders.Angle.MaxValue),
        UnityEngine.Random.Range(Encoders.Status.MinValue, Encoders.Status.MaxValue));
    }

    internal static void GenerateState(EntityState basis, EntityState destination)
    {
      destination.Set(
        basis.EntityId,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.ArchetypeId.MinValue, Encoders.ArchetypeId.MaxValue) : basis.ArchetypeId,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.UserId.MinValue, Encoders.UserId.MaxValue) : basis.UserId,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.Coordinate.MinValue, Encoders.Coordinate.MaxValue) : basis.X,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.Coordinate.MinValue, Encoders.Coordinate.MaxValue) : basis.Y,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.Angle.MinValue, Encoders.Angle.MaxValue) : basis.Angle,
        UnityEngine.Random.Range(0.0f, 1.0f) > 0.7f ? UnityEngine.Random.Range(Encoders.Status.MinValue, Encoders.Status.MaxValue) : basis.Status);
    }
    #endregion
  }
}
