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

using Reservoir;

namespace Railgun.User
{
  /// <summary>
  /// Entities represent physical objects in the game world.
  /// </summary>
  public class UserState : State<UserState>
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
      UserState state,
      UserState basis)
    {
      return
        (state.ArchetypeId == basis.ArchetypeId ? 0 : FLAG_ARCHETYPE_ID) |
        (state.UserId == basis.UserId ? 0 : FLAG_USER_ID) |
        (UserMath.CoordinatesEqual(state.X, basis.X) ? 0 : FLAG_X) |
        (UserMath.CoordinatesEqual(state.Y, basis.Y) ? 0 : FLAG_Y) |
        (UserMath.AnglesEqual(state.Angle, basis.Angle) ? 0 : FLAG_ANGLE) |
        (state.Status == basis.Status ? 0 : FLAG_STATUS);
    }

    protected internal override byte Type { get { return UserTypes.TYPE_USER_STATE; } }

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
    protected internal override void SetFrom(UserState other)
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
    protected internal override void Encode(BitPacker bitPacker)
    {
      // Write in opposite order so we can read in SetData order
      bitPacker.Push(UserEncoders.Status, this.Status);
      bitPacker.Push(UserEncoders.Angle, this.Angle);
      bitPacker.Push(UserEncoders.Coordinate, this.Y);
      bitPacker.Push(UserEncoders.Coordinate, this.X);
      bitPacker.Push(UserEncoders.UserId, this.UserId);
      bitPacker.Push(UserEncoders.ArchetypeId, this.ArchetypeId);

      // Add metadata
      bitPacker.Push(UserEncoders.EntityDirty, UserState.FLAG_ALL);
    }

    /// <summary>
    /// Delta-encode this state relative to the given basis state.
    /// Returns true iff the state was encoded (will bypass if no change).
    /// </summary>
    protected internal override bool Encode(BitPacker bitPacker, UserState basis)
    {
      int dirty = UserState.GetDirtyFlags(this, basis);
      if (dirty == 0)
        return false;

      // Write in opposite order so we can read in SetData order
      bitPacker.PushIf(dirty, FLAG_STATUS, UserEncoders.Status, this.Status);
      bitPacker.PushIf(dirty, FLAG_ANGLE, UserEncoders.Angle, this.Angle);
      bitPacker.PushIf(dirty, FLAG_Y, UserEncoders.Coordinate, this.Y);
      bitPacker.PushIf(dirty, FLAG_X, UserEncoders.Coordinate, this.X);
      bitPacker.PushIf(dirty, FLAG_USER_ID, UserEncoders.UserId, this.UserId);
      bitPacker.PushIf(dirty, FLAG_ARCHETYPE_ID, UserEncoders.ArchetypeId, this.ArchetypeId);

      // Add metadata
      bitPacker.Push(UserEncoders.EntityDirty, dirty);
      return true;
    }

    /// <summary>
    /// Decode a fully populated data packet and set values to this object.
    /// </summary>
    protected internal override void Decode(BitPacker bitPacker)
    {
      int dirty = bitPacker.Pop(UserEncoders.EntityDirty);
      RailgunUtil.Assert(dirty == UserState.FLAG_ALL);

      this.SetData(
        bitPacker.Pop(UserEncoders.ArchetypeId),
        bitPacker.Pop(UserEncoders.UserId),
        bitPacker.Pop(UserEncoders.Coordinate),
        bitPacker.Pop(UserEncoders.Coordinate),
        bitPacker.Pop(UserEncoders.Angle),
        bitPacker.Pop(UserEncoders.Status));
    }

    /// <summary>
    /// Decode a delta-encoded packet against a given basis and set values
    /// to this object.
    /// </summary>
    protected internal override void Decode(BitPacker bitPacker, UserState basis)
    {
      int dirty = bitPacker.Pop(UserEncoders.EntityDirty);

      this.SetData(
        bitPacker.PopIf(dirty, FLAG_ARCHETYPE_ID, UserEncoders.ArchetypeId, basis.ArchetypeId),
        bitPacker.PopIf(dirty, FLAG_USER_ID, UserEncoders.UserId, basis.UserId),
        bitPacker.PopIf(dirty, FLAG_X, UserEncoders.Coordinate, basis.X),
        bitPacker.PopIf(dirty, FLAG_Y, UserEncoders.Coordinate, basis.Y),
        bitPacker.PopIf(dirty, FLAG_ANGLE, UserEncoders.Angle, basis.Angle),
        bitPacker.PopIf(dirty, FLAG_STATUS, UserEncoders.Status, basis.Status));
    }
  }
}
