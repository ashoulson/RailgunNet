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

namespace Railgun
{
  internal static class Encoders
  {
    // Used by StateBag
    internal static IntEncoder EntityCount = null;
    internal static IntEncoder EntityId = null;

    internal static IntEncoder StateType = null;

    internal static IntEncoder Frame = null;

    internal static IntEncoder Bit = null;

    internal static void Initialize()
    {
      // Used by StateBag
      Encoders.EntityCount = new IntEncoder(0, 1023);
      Encoders.EntityId = Encoders.EntityCount;

      Encoders.StateType = new IntEncoder(0, 31);

      Encoders.Frame = new IntEncoder(-1, 4194301);

      Encoders.Bit = new IntEncoder(0, 1);
    }
  }
}
