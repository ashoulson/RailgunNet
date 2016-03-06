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

    internal void Clear()
    {
      this.Delta.Set(null, null, null);
    }

    internal RailState Push(RailState state)
    {
      RailState next = null;
      RailState latest = null;
      RailState prior = null;
      RailState popped = null;

      if (this.Next != null)
      {
        if (this.Latest != null)
        {
          if (this.Prior != null)
          {
            popped = this.Prior;
          }

          prior = this.Latest;
        }

        latest = this.Next;
      }

      next = state;

      this.Delta.Set(prior, latest, next);
      return popped;
    }

    public bool CanInterpolate()
    {
      return (this.Latest != null) && (this.Next != null);
    }

    public bool CanExtrapolate()
    {
      return (this.Latest != null) && (this.Prior != null);
    }


    public void GetExtrapolationParams(
      int currentTick, 
      float frameDelta,
      out float timeSincePrior,
      out float velocityScale,
      float fixedDeltaTime = RailConfig.FIXED_DELTA_TIME)
    {
      // If we're predicting, advance to the prediction tick.
      // Note that this assumes we'll only ever have a 1-tick difference
      // between any two states in the delta when doing prediction.
      if (this.Latest.IsPredicted)
        currentTick = this.Latest.Tick;

      float priorTime = this.Prior.Tick * fixedDeltaTime;
      float latestTime = this.Latest.Tick * fixedDeltaTime;
      float currentTime = (currentTick * fixedDeltaTime) + frameDelta;

      timeSincePrior = currentTime - priorTime;
      velocityScale = latestTime - priorTime;
    }
  }
}
