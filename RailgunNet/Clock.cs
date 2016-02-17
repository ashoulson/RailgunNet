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

using CommonTools;
using UnityEngine;

namespace Railgun
{
  /// <summary>
  /// Used for keeping track of the remote peer's clock.
  /// </summary>
  internal class Clock
  {
    public const int INVALID_TICK = -1;
    private int remoteRate;
    private int tick;

    internal Clock(int remoteSendRate)
    {
      this.remoteRate = remoteSendRate;
      this.tick = 0;
    }

    // TODO: See http://www.gamedev.net/topic/652186-de-jitter-buffer-on-both-the-client-and-server/
    public void Tick(int latestTick, bool bufferActive)
    {
      if (latestTick != Clock.INVALID_TICK)
      {
        this.tick += 1;

        if (bufferActive == true)
        {
          // We want to stay (remoteRate * 2) behind the last received tick
          // At 50Hz tick with 25Hz send, this is a delay of roughly 80ms
          int diff = latestTick - this.tick;

          if (diff >= (this.remoteRate * 3))
          {
            // If we are 3 or more packets behind, increment our local tick
            // at most (remoteRate * 2) extra ticks
            int incr = 
              Math.Min(diff - (this.remoteRate * 2), (this.remoteRate * 2));
            Debug.Log("Clock: T+" + incr + " (Behind) @ T" + this.tick);

            this.tick += incr;
          }
          else if ((diff >= 0) && (diff < this.remoteRate))
          {
            // If we have drifted slightly closer to being ahead
            // Stall one tick by decrementing the tick counter
            Debug.Log("Clock: T-1 @ (Stall) T" + this.tick);

            this.tick -= 1;
          }
          else if (diff < 0)
          {
            // If we are ahead of the remote tick, we need to step back
            if (Math.Abs(diff) <= (this.remoteRate * 2))
            {
              // Slightly ahead (<= 2 packets) -- step one packet closer
              Debug.Log(
                "Clock: T-" + this.remoteRate + " (Ahead) @ T" + this.tick);

              this.tick -= this.remoteRate;
            }
            else
            {
              // We're way off, just reset entirely and start over
              int newTick = latestTick - (this.remoteRate * 2);
              Debug.Log(
                "Clock: T=" + latestTick + " (Reset) @ T" + this.tick);

              this.tick = latestTick;
            }
          }
        }
      }
    }
  }
}
