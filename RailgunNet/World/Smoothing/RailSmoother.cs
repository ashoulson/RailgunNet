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
  public class RailSmoother<T>
  {
    public delegate T Accessor<T>(RailState state);
    public delegate T LerpUnclamped<T>(T from, T to, float t);
    public delegate bool ShouldSnap<T>(T from, T to);

    private Accessor<T> accessor;
    private LerpUnclamped<T> lerpUnclamped;
    private ShouldSnap<T> shouldSnap;

    private float maxExtrapolationTime;

    public RailSmoother(
      Accessor<T> accessor,
      LerpUnclamped<T> lerpUnclamped,
      ShouldSnap<T> shouldSnap = null,
      float maxExtrapolationTime = float.MaxValue)
    {
      this.accessor = accessor;
      this.lerpUnclamped = lerpUnclamped;
      this.shouldSnap = shouldSnap;
      this.maxExtrapolationTime = maxExtrapolationTime;
    }

    internal T Access(
      RailState state)
    {
      return this.accessor.Invoke(state);
    }

    internal T Smooth(
      float frameDelta,
      int tick,
      RailState first,
      RailState second)
    {
      T priorVal = this.accessor(first);
      T latestVal = this.accessor(second);
      if ((this.shouldSnap != null) && this.shouldSnap(priorVal, latestVal))
        return latestVal;

      float priorTime = first.Tick * RailConfig.FIXED_DELTA_TIME;
      float latestTime = second.Tick * RailConfig.FIXED_DELTA_TIME;
      float time = (tick * RailConfig.FIXED_DELTA_TIME) + frameDelta;

      float place = time - priorTime;
      if (place > this.maxExtrapolationTime)
        place = this.maxExtrapolationTime;
      float span = latestTime - priorTime;
      float scalar = place / span;

      return this.lerpUnclamped(priorVal, latestVal, scalar);
    }
  }
}
