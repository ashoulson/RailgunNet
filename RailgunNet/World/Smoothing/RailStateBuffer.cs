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
  /// <summary>
  /// Some notes about state buffering on the client and server:
  /// 
  /// ---
  /// WHAT IS THE OLDEST "SAFE" STATE TO DELTA AGAINST FOR THE CLIENT?
  /// ---
  /// Note that we have to decode before we store the state on the client.
  /// This means that the oldest "safe" theoretical packet tick available
  /// at decode time in the buffer is (ServerTick - DEJITTER_BUFFER_LENGTH).
  ///
  /// As long as you never send a delta packet with a basis older than
  /// (ServerTick - DEJITTER_BUFFER_LENGTH) you can count on it not being
  /// replaced before your send arrives. If you try to delta against an
  /// older packet than (ServerTick - DEJITTER_BUFFER_LENGTH) and your lag
  /// is (admittedly, very) high, then there's a chance that the state you
  /// encoded against may be overwritten on the client before the state you
  /// just sent will arrive.
  ///
  /// Worked out worst case example with buffer length 10 and send rate 2:
  /// [00] [02] [04] [06] [08] -> Ack 8
  /// [00] [02] [04] [06] [08] <- 10 Basis 8
  /// [10] [02] [04] [06] [08] -> Ack 10 (LOST)
  /// [10] [02] [04] [06] [08] <- 12 Basis 8
  /// [10] [12] [04] [06] [08] -> Ack 12 (LOST)
  /// [10] [12] [04] [06] [08] <- 14 Basis 8
  /// [10] [12] [14] [06] [08] -> Ack 14 (LOST)
  /// [10] [12] [14] [06] [08] <- 16 Basis 8
  /// [10] [12] [14] [16] [08] -> Ack 16 (LOST)
  /// [10] [12] [14] [16] [08] <- 18 Basis 8    // 8 still in buffer
  /// [10] [12] [14] [16] [18] -> Ack 18 (LOST) // 8 replaced after receive
  /// [10] [12] [14] [16] [18] <- 20 Full       // 8 < 20 - 10
  /// [20] [12] [14] [16] [18] -> Ack 20
  /// 
  /// 
  /// ---
  /// WHAT IS THE OLDEST STATE AVAILABLE ON THE SERVER?
  /// ---
  /// Assuming states are always stored for every tick (or send), the oldest
  /// tick available will be (ServerTick - DEJITTER_BUFFER_LENGTH) before you
  /// store a state, and (ServerTick - (DEJITTER_BUFFER_LENGTH - SEND_RATE))
  /// after you store a state. Because of this discrepancy, it's a good idea
  /// to not store the state on the server until after you're done processing
  /// everything and are about to send.
  /// </summary>
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
