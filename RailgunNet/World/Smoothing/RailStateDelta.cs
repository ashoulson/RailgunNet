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
  public class RailStateDelta
  {
    internal RailRingDelta<RailState> Delta { get; private set; }

    public RailState Prior { get { return this.Delta.Prior; } }
    public RailState Latest { get { return this.Delta.Latest; } }
    public RailState Next { get { return this.Delta.Next; } }

    public RailStateDelta()
    {
      this.Delta = new RailRingDelta<RailState>();
    }

    public void Set(RailState prior, RailState latest, RailState next)
    {
      this.Delta.Set(prior, latest, next);
    }

    public void Update(RailStateBuffer buffer, int currentTick)
    {
      buffer.PopulateDelta(this.Delta, currentTick);
    }

    public bool CanInterpolate()
    {
      return (this.Latest != null) && (this.Next != null);
    }

    public bool CanExtrapolate()
    {
      return (this.Latest != null) && (this.Prior != null);
    }

    public void GetInterpolationParams(
      int currentTick, 
      float frameDelta,
      out float interpolationScalar,
      float fixedDeltaTime = RailConfig.FIXED_DELTA_TIME)
    {
      float latestTime = this.Latest.Tick * fixedDeltaTime;
      float nextTime = this.Next.Tick * fixedDeltaTime;
      float currentTime = (currentTick * fixedDeltaTime) + frameDelta;

      float place = currentTime - latestTime;
      float span = nextTime - latestTime;

      interpolationScalar = place / span;
    }

    public void GetExtrapolationParams(
      int currentTick, 
      float frameDelta,
      out float timeSincePrior,
      out float velocityScale,
      float fixedDeltaTime = RailConfig.FIXED_DELTA_TIME)
    {
      float priorTime = this.Prior.Tick * fixedDeltaTime;
      float latestTime = this.Latest.Tick * fixedDeltaTime;
      float currentTime = (currentTick * fixedDeltaTime) + frameDelta;

      timeSincePrior = currentTime - priorTime;
      velocityScale = latestTime - priorTime;
    }
  }
}
