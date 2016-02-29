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

namespace Railgun
{
  public class RailPeerClient : RailPeer
  {
    public event Action<RailPeerClient> MessagesReady;

    /// <summary>
    /// A history of received inputs from the client. Used for dejitter.
    /// </summary>
    internal readonly RailRingBuffer<RailPacketC2S> inputBuffer;

    internal int LastAckedTick { get; set; }

    // TODO: Temp!
    internal RailPacketC2S latestInput;

    internal RailPeerClient(IRailNetPeer netPeer) : base(netPeer)
    {
      this.LastAckedTick = RailClock.INVALID_TICK;

      // We use no divisor for storing inputs because inputs are sent in
      // batches that we can use to fill in the holes between send frames
      this.inputBuffer =
        new RailRingBuffer<RailPacketC2S>(
          RailConfig.DEJITTER_BUFFER_LENGTH);
    }

    protected override void OnMessagesReady(IRailNetPeer peer)
    {
      if (this.MessagesReady != null)
        this.MessagesReady(this);
    }

    internal void StoreInput(RailPacketC2S input)
    {
      this.inputBuffer.Store(input);

      if (this.latestInput == null || this.latestInput.Tick < input.Tick)
        this.latestInput = input;
    }
  }
}
