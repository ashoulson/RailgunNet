///*
// *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
// *  Copyright (c) 2016 - Alexander Shoulson - http://ashoulson.com
// *
// *  This software is provided 'as-is', without any express or implied
// *  warranty. In no event will the authors be held liable for any damages
// *  arising from the use of this software.
// *  Permission is granted to anyone to use this software for any purpose,
// *  including commercial applications, and to alter it and redistribute it
// *  freely, subject to the following restrictions:
// *  
// *  1. The origin of this software must not be misrepresented; you must not
// *     claim that you wrote the original software. If you use this software
// *     in a product, an acknowledgment in the product documentation would be
// *     appreciated but is not required.
// *  2. Altered source versions must be plainly marked as such, and must not be
// *     misrepresented as being the original software.
// *  3. This notice may not be removed or altered from any source distribution.
//*/

//using System;
//using System.Collections;
//using System.Collections.Generic;

//namespace Railgun
//{
//  public class RailSmoother<T>
//  {
//    public delegate T Accessor(RailStateRecord state);
//    public delegate T LerpUnclamped(T from, T to, float t);
//    public delegate bool ShouldSnap(T from, T to);

//    private Accessor accessor;
//    private LerpUnclamped lerpUnclamped;
//    private ShouldSnap shouldSnap;

//    private float maxExtrapolationTime;

//    public RailSmoother(
//      Accessor accessor,
//      LerpUnclamped lerpUnclamped,
//      ShouldSnap shouldSnap = null,
//      float maxExtrapolationTime = float.MaxValue)
//    {
//      this.accessor = accessor;
//      this.lerpUnclamped = lerpUnclamped;
//      this.shouldSnap = shouldSnap;
//      this.maxExtrapolationTime = maxExtrapolationTime;
//    }

//    internal T Access(
//      RailStateRecord state)
//    {
//      return this.accessor.Invoke(state);
//    }

//    internal T Smooth(
//      float frameDelta,
//      Tick tick,
//      RailStateRecord first,
//      RailStateRecord second)
//    {
//      T priorVal = this.accessor(first);
//      T latestVal = this.accessor(second);
//      if ((this.shouldSnap != null) && this.shouldSnap(priorVal, latestVal))
//        return latestVal;

//      float priorTime = first.Tick.Time;
//      float latestTime = second.Tick.Time;
//      float time = tick.Time + frameDelta;

//      float place = time - priorTime;
//      if (place > this.maxExtrapolationTime)
//        place = this.maxExtrapolationTime;
//      float span = latestTime - priorTime;
//      float scalar = place / span;

//      return this.lerpUnclamped(priorVal, latestVal, scalar);
//    }
//  }
//}
