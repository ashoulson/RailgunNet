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
  public class RailStateBuffer
  {
    internal RailState Latest { get { return this.latest; } }

    internal IEnumerable<RailState> Values 
    { 
      get { return this.buffer.Values; } 
    }

    private RailRingBuffer<RailState> buffer;
    private RailState latest;

    public RailStateBuffer()
    {
      this.buffer =
        new RailRingBuffer<RailState>(
          RailConfig.DEJITTER_BUFFER_LENGTH,
          RailConfig.NETWORK_SEND_RATE);
    }

    public void Store(RailState state)
    {
      this.buffer.Store(state);
      if ((this.latest == null) || (this.latest.Tick < state.Tick))
        this.latest = state;
    }

    internal void PopulateDelta(RailRingDelta<RailState> delta, int currentTick)
    {
      this.buffer.PopulateDelta(delta, currentTick);
    }

    internal bool TryGet(int tick, out RailState state)
    {
      return this.buffer.TryGet(tick, out state);
    }

    internal RailState Get(int tick)
    {
      return this.buffer.Get(tick);
    }

    internal RailState GetLatest(int tick)
    {
      return this.buffer.GetLatest(tick);
    }
  }
}
