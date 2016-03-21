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
  public class RailStateTracker
  {
    internal RailRingDelta<RailState> Delta { get; private set; }

    public RailState Prior { get { return this.Delta.Prior; } }
    public RailState Current { get { return this.Delta.Current; } }
    public RailState Next { get { return this.Delta.Next; } }

    public RailStateTracker()
    {
      this.Delta = new RailRingDelta<RailState>();
    }

    public void Set(RailState prior, RailState current, RailState next)
    {
      this.Delta.Set(prior, current, next);
    }

    public void Update(RailStateBuffer buffer, Tick currentTick)
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
      RailState current = null;
      RailState prior = null;
      RailState popped = null;

      if (this.Next != null)
      {
        if (this.Current != null)
        {
          if (this.Prior != null)
          {
            popped = this.Prior;
          }

          prior = this.Current;
        }

        current = this.Next;
      }

      next = state;

      this.Delta.Set(prior, current, next);
      return popped;
    }

    public bool CanInterpolate()
    {
      return (this.Current != null) && (this.Next != null);
    }

    public bool CanExtrapolate()
    {
      return (this.Current != null) && (this.Prior != null);
    }
  }
}
