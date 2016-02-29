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

public static class DemoEncoders
{
  public static readonly IntEncoder EntityDirty = new IntEncoder(0, (int)DemoState.FLAG_ALL);
  public static readonly IntEncoder ArchetypeId = new IntEncoder(0, 255);
  public static readonly IntEncoder UserId = new IntEncoder(0, 1023);
  public static readonly IntEncoder Status = new IntEncoder(0, 0x3F);

  public static readonly FloatEncoder Coordinate = new FloatEncoder(-2048.0f, 2048.0f, 0.01f);
  public static readonly FloatEncoder Angle = new FloatEncoder(0.0f, 360.0f, 0.01f);

  public static readonly BoolEncoder Bool = new BoolEncoder();
}
