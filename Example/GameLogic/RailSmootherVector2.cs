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

using UnityEngine;

namespace Railgun
{
  public class RailSmootherVector2 : RailSmoother<Vector2>
  {
    private static Vector2 LerpUnclampedVector2(
      Vector2 from, 
      Vector2 to, 
      float t)
    {
      return 
        new Vector2(
          from.x + (to.x - from.x) * t,
          from.y + (to.y - from.y) * t);
    }

    private static ShouldSnap CreateShouldSnap(
      float snappingDistance)
    {
      float snappingDistanceSqr = snappingDistance * snappingDistance;
      return delegate(Vector2 a, Vector2 b)
      {
        if ((a - b).sqrMagnitude > snappingDistanceSqr)
          return true;
        return false;
      };
    }

    public RailSmootherVector2(
      Accessor accessor,
      float snappingDistance = float.MaxValue,
      float maxExtrapolationTime = float.MaxValue)
      : base(
        accessor,
        RailSmootherVector2.LerpUnclampedVector2,
        RailSmootherVector2.CreateShouldSnap(snappingDistance),
        maxExtrapolationTime)
    {
    }
  }
}
