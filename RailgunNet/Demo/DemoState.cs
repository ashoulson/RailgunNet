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

namespace Railgun.User
{
  /// <summary>
  /// Entities represent physical objects in the game world.
  /// </summary>
  public class DemoState : State<DemoState>
  {
    // TODO: This class is the sort of thing that would be great to code-
    // generate, but since there's only a couple of them at most the 
    // complexity hasn't seemed to be worth it so far
    #region Flags
    private const int FLAG_ARCHETYPE_ID = 0x01;
    private const int FLAG_USER_ID = 0x02;
    private const int FLAG_X = 0x04;
    private const int FLAG_Y = 0x08;
    private const int FLAG_ANGLE = 0x10;
    private const int FLAG_STATUS = 0x20;

    internal const int FLAG_ALL =
      FLAG_ARCHETYPE_ID |
      FLAG_USER_ID |
      FLAG_X |
      FLAG_Y |
      FLAG_ANGLE |
      FLAG_STATUS;
    #endregion

    /// <summary>
    /// Compares a state against a predecessor and returns a bit bucket of
    /// which fields are dirty.
    /// </summary>
    public static int GetDirtyFlags(
      DemoState state,
      DemoState basis)
    {
      return
        (state.ArchetypeId == basis.ArchetypeId ? 0 : FLAG_ARCHETYPE_ID) |
        (state.UserId == basis.UserId ? 0 : FLAG_USER_ID) |
        (DemoMath.CoordinatesEqual(state.X, basis.X) ? 0 : FLAG_X) |
        (DemoMath.CoordinatesEqual(state.Y, basis.Y) ? 0 : FLAG_Y) |
        (DemoMath.AnglesEqual(state.Angle, basis.Angle) ? 0 : FLAG_ANGLE) |
        (state.Status == basis.Status ? 0 : FLAG_STATUS);
    }

    protected internal override int Type { get { return DemoTypes.TYPE_USER_STATE; } }

    public int ArchetypeId { get; set; }
    public int UserId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Angle { get; set; }
    public int Status { get; set; }

    public void SetData(
      int archetypeId,
      int userId,
      float x,
      float y,
      float angle,
      int status)
    {
      this.ArchetypeId = archetypeId;
      this.UserId = userId;
      this.X = x;
      this.Y = y;
      this.Angle = angle;
      this.Status = status;
    }

    /// <summary>
    /// Writes the values from another PawnState to this one.
    /// </summary>
    protected internal override void SetFrom(DemoState other)
    {
      this.ArchetypeId = other.ArchetypeId;
      this.UserId = other.UserId;
      this.X = other.X;
      this.Y = other.Y;
      this.Angle = other.Angle;
      this.Status = other.Status;
    }

    /// <summary>
    /// Write a fully populated encoding of this state.
    /// </summary>
    protected internal override void Encode(BitBuffer buffer)
    {
      // Write in opposite order so we can read in SetData order
      buffer.Push(DemoEncoders.Status, this.Status);
      buffer.Push(DemoEncoders.Angle, this.Angle);
      buffer.Push(DemoEncoders.Coordinate, this.Y);
      buffer.Push(DemoEncoders.Coordinate, this.X);
      buffer.Push(DemoEncoders.UserId, this.UserId);
      buffer.Push(DemoEncoders.ArchetypeId, this.ArchetypeId);

      // Add metadata
      buffer.Push(DemoEncoders.EntityDirty, DemoState.FLAG_ALL);
    }

    /// <summary>
    /// Delta-encode this state relative to the given basis state.
    /// Returns true iff the state was encoded (will bypass if no change).
    /// </summary>
    protected internal override bool Encode(BitBuffer buffer, DemoState basis)
    {
      int dirty = DemoState.GetDirtyFlags(this, basis);
      if (dirty == 0)
        return false;

      // Write in opposite order so we can read in SetData order
      buffer.PushIf(dirty, FLAG_STATUS, DemoEncoders.Status, this.Status);
      buffer.PushIf(dirty, FLAG_ANGLE, DemoEncoders.Angle, this.Angle);
      buffer.PushIf(dirty, FLAG_Y, DemoEncoders.Coordinate, this.Y);
      buffer.PushIf(dirty, FLAG_X, DemoEncoders.Coordinate, this.X);
      buffer.PushIf(dirty, FLAG_USER_ID, DemoEncoders.UserId, this.UserId);
      buffer.PushIf(dirty, FLAG_ARCHETYPE_ID, DemoEncoders.ArchetypeId, this.ArchetypeId);

      // Add metadata
      buffer.Push(DemoEncoders.EntityDirty, dirty);
      return true;
    }

    /// <summary>
    /// Decode a fully populated data packet and set values to this object.
    /// </summary>
    protected internal override void Decode(BitBuffer buffer)
    {
      int dirty = buffer.Pop(DemoEncoders.EntityDirty);
      RailgunUtil.Assert(dirty == DemoState.FLAG_ALL);

      this.SetData(
        buffer.Pop(DemoEncoders.ArchetypeId),
        buffer.Pop(DemoEncoders.UserId),
        buffer.Pop(DemoEncoders.Coordinate),
        buffer.Pop(DemoEncoders.Coordinate),
        buffer.Pop(DemoEncoders.Angle),
        buffer.Pop(DemoEncoders.Status));
    }

    /// <summary>
    /// Decode a delta-encoded packet against a given basis and set values
    /// to this object.
    /// </summary>
    protected internal override void Decode(BitBuffer buffer, DemoState basis)
    {
      int dirty = buffer.Pop(DemoEncoders.EntityDirty);

      this.SetData(
        buffer.PopIf(dirty, FLAG_ARCHETYPE_ID, DemoEncoders.ArchetypeId, basis.ArchetypeId),
        buffer.PopIf(dirty, FLAG_USER_ID, DemoEncoders.UserId, basis.UserId),
        buffer.PopIf(dirty, FLAG_X, DemoEncoders.Coordinate, basis.X),
        buffer.PopIf(dirty, FLAG_Y, DemoEncoders.Coordinate, basis.Y),
        buffer.PopIf(dirty, FLAG_ANGLE, DemoEncoders.Angle, basis.Angle),
        buffer.PopIf(dirty, FLAG_STATUS, DemoEncoders.Status, basis.Status));
    }
  }
}
