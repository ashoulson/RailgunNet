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

[RegisterCommand]
public class DemoCommand : RailCommand<DemoCommand>
{
  public bool Up { get; set; }
  public bool Down { get; set; }
  public bool Left { get; set; }
  public bool Right { get; set; }
  public bool Action { get; set; }

  public void SetData(
    bool up,
    bool down,
    bool left,
    bool right,
    bool action)
  {
    this.Up = up;
    this.Down = down;
    this.Left = left;
    this.Right = right;
    this.Action = action;
  }

  protected override void EncodeData(BitBuffer buffer)
  {
    buffer.WriteBool(this.Up);
    buffer.WriteBool(this.Down);
    buffer.WriteBool(this.Left);
    buffer.WriteBool(this.Right);
    buffer.WriteBool(this.Action);
  }

  protected override void DecodeData(BitBuffer buffer)
  {
    this.SetData(
      buffer.ReadBool(),
      buffer.ReadBool(),
      buffer.ReadBool(),
      buffer.ReadBool(),
      buffer.ReadBool());
  }

  protected override void ResetData()
  {
    this.SetData(false, false, false, false, false);
  }

  protected override void SetDataFrom(DemoCommand other)
  {
    this.SetData(
      other.Up,
      other.Down,
      other.Left,
      other.Right,
      other.Action);
  }

  protected override void Populate()
  {
#if CLIENT
    this.SetData(
      Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W),
      Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S),
      Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A),
      Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D),
      Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.T));
#endif
  }
}