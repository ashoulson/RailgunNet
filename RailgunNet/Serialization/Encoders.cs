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
  public static class Encoders
  {
    internal static int PushIf<T>(
      BitPacker bitPacker,
      bool condition,
      T value,
      IEncoder<T> encoder,
      int flag)
    {
      if (condition)
      {
        bitPacker.Push(value, encoder);
        return flag;
      }
      return 0;
    }

    internal static T PopIf<T>(
      BitPacker bitPacker,
      int flags,
      int requiredFlag,
      IEncoder<T> encoder,
      T basisVal)
    {
      if ((flags & requiredFlag) == requiredFlag)
        return bitPacker.Pop(encoder);
      return basisVal;
    }

    internal static IntEncoder   EntityFlag  = null;

    internal static IntEncoder   UserId      = null;
    internal static IntEncoder   EntityId    = null;
    internal static IntEncoder   ArchetypeId = null;
    internal static IntEncoder   Status      = null;

    internal static FloatEncoder Coordinate  = null;
    internal static FloatEncoder Angle       = null;

    public static void Initialize()
    {
      Encoders.EntityFlag  = new IntEncoder(0, (int)EntityState.FLAG_ALL);

      Encoders.UserId      = new IntEncoder(0, 4095);
      Encoders.EntityId    = new IntEncoder(0, 65535);
      Encoders.ArchetypeId = new IntEncoder(0, 255);
      Encoders.Status      = new IntEncoder(0, 0xFFF);

      Encoders.Angle       = new FloatEncoder(0.0f, 360.0f, 0.1f);
      Encoders.Coordinate  = new FloatEncoder(-2048.0f, 2048.0f, 0.01f);
    }
  }
}
