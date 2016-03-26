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

using Railgun;
using UnityEngine;

public class DemoState : RailState<DemoState>
{
  // TODO: This class is the sort of thing that would be great to code-
  // generate, but since there's only a couple of them at most the 
  // complexity hasn't seemed to be worth it so far...

  #region Flags
  private const uint FLAG_X = 0x01;
  private const uint FLAG_Y = 0x02;
  private const uint FLAG_ANGLE = 0x04;
  private const uint FLAG_STATUS = 0x08;

  internal const uint FLAG_ALL =
    FLAG_X |
    FLAG_Y |
    FLAG_ANGLE |
    FLAG_STATUS;

  protected override int FlagBits { get { return 4; } }
  #endregion

  // These should be properties, but we can't pass properties by ref
  public int ArchetypeId;
  public int UserId;
  public float X;
  public float Y;
  public float Angle;
  public byte Status;

  protected override void DecodeMutableData(BitBuffer buffer, uint flags)
  {
    if (this.GetFlag(flags, FLAG_X)) this.X = buffer.ReadFloat(DemoCompressors.Coordinate);
    if (this.GetFlag(flags, FLAG_Y)) this.Y = buffer.ReadFloat(DemoCompressors.Coordinate);
    if (this.GetFlag(flags, FLAG_ANGLE)) this.Angle = buffer.ReadFloat(DemoCompressors.Angle);
    if (this.GetFlag(flags, FLAG_STATUS)) this.Status = buffer.ReadByte();
  }

  protected override void DecodeControllerData(BitBuffer buffer)
  {
  }

  protected override void DecodeImmutableData(BitBuffer buffer)
  {
    this.ArchetypeId = buffer.ReadInt();
    this.UserId = buffer.ReadInt();
  }

  protected override void EncodeMutableData(BitBuffer buffer, uint flags)
  {
    if (this.GetFlag(flags, FLAG_X)) buffer.WriteFloat(DemoCompressors.Coordinate, this.X);
    if (this.GetFlag(flags, FLAG_Y)) buffer.WriteFloat(DemoCompressors.Coordinate, this.Y);
    if (this.GetFlag(flags, FLAG_ANGLE)) buffer.WriteFloat(DemoCompressors.Angle, this.Angle);
    if (this.GetFlag(flags, FLAG_STATUS)) buffer.WriteByte(this.Status);
  }

  protected override void EncodeControllerData(BitBuffer buffer)
  {
  }

  protected override void EncodeImmutableData(BitBuffer buffer)
  {
    buffer.WriteInt(this.ArchetypeId);
    buffer.WriteInt(this.UserId);
  }

  protected override void ResetAllData()
  {
    this.ArchetypeId = 0;
    this.UserId = 0;
    this.X = 0.0f;
    this.Y = 0.0f;
    this.Angle = 0.0f;
    this.Status = 0;
  }

  protected override void ApplyMutableFrom(DemoState other, uint flags)
  {
    if (this.GetFlag(flags, FLAG_X)) this.X = other.X;
    if (this.GetFlag(flags, FLAG_Y)) this.Y = other.Y;
    if (this.GetFlag(flags, FLAG_ANGLE)) this.Angle = other.Angle;
    if (this.GetFlag(flags, FLAG_STATUS)) this.Status = other.Status;
  }

  protected override void ApplyControllerFrom(DemoState other)
  {
  }

  protected override void ApplyImmutableFrom(DemoState other)
  {
    this.ArchetypeId = other.ArchetypeId;
    this.UserId = other.UserId;
  }

  protected override uint CompareMutableData(DemoState basis)
  {
    return
      this.SetFlag(DemoMath.CoordinatesEqual(this.X, basis.X), FLAG_X) |
      this.SetFlag(DemoMath.CoordinatesEqual(this.Y, basis.Y), FLAG_Y) |
      this.SetFlag(DemoMath.AnglesEqual(this.Angle, basis.Angle), FLAG_ANGLE) |
      this.SetFlag(this.Status == basis.Status, FLAG_STATUS);
  }

  protected override bool IsControllerDataEqual(DemoState basis)
  {
    return true;
  }

  protected override void ApplySmoothed(DemoState first, DemoState second, float t)
  {
    this.X = DemoMath.LerpUnclampedFloat(first.X, second.X, t);
    this.Y = DemoMath.LerpUnclampedFloat(first.Y, second.Y, t);
  }
}