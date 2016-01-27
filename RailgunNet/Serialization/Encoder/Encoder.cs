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
  internal class Encoder
  {
    #region Static Encoders (Read-Only)
    // Used by StateBag
    internal static IntEncoder   StateCount  = null;
    internal static IntEncoder   StateId     = null;

    internal static IntEncoder   EntityDirty = null;
    internal static IntEncoder   ArchetypeId = null;
    internal static IntEncoder   UserId      = null;
    internal static IntEncoder   Status      = null;

    internal static FloatEncoder Coordinate  = null;
    internal static FloatEncoder Angle       = null;

    public static void Initialize()
    {
      // Used by StateBag
      Encoder.StateCount  = new IntEncoder(0, 1023);
      Encoder.StateId     = Encoder.StateCount;

      Encoder.Angle       = new FloatEncoder(0.0f, 360.0f, 1.0f);
      Encoder.Coordinate  = new FloatEncoder(-2048.0f, 2048.0f, 0.01f);

      // Used by EntityState
      Encoder.EntityDirty = new IntEncoder(0, (int)EntityState.FLAG_ALL);
      Encoder.ArchetypeId = new IntEncoder(0, 255);
      Encoder.UserId      = new IntEncoder(0, 1023);
      Encoder.Status      = new IntEncoder(0, 0x3F);
    }
    #endregion
  }

  internal abstract class Encoder<T> : Encoder
  {
    internal abstract int RequiredBits { get; }

    internal abstract uint Pack(T value);
    internal abstract T Unpack(uint data);
  }
}
