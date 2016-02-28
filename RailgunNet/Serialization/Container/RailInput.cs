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

namespace Railgun
{
  /// <summary>
  /// Input is a collection of player state data sent from client to host.
  /// </summary>
  public class RailInput : IRailPoolable, IRailRingValue
  {
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }
    int IRailRingValue.Key { get { return this.Tick; } }

    internal int Tick { get; set; }
    internal RailCommand Command { get; set; }

    protected void Reset()
    {
      this.Tick = RailClock.INVALID_TICK;
      if (this.Command != null)
        RailPool.Free(this.Command);
      this.Command = null;
    }

    #region Encode/Decode
    /// Input encoding order: | TICK | -- COMMAND -- |
    /// 
    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [Command]
      this.Command.Encode(buffer);

      // Write: [Tick]
      buffer.Push(Encoders.Tick, this.Tick);
    }

    internal static RailInput Decode(
      BitBuffer buffer)
    {
      // Read: [Tick]
      int tick = buffer.Pop(Encoders.Tick);

      RailInput input = RailResource.Instance.AllocateInput();
      RailCommand command = RailResource.Instance.AllocateCommand();

      // Read: [Command]
      command.Decode(buffer);

      input.Tick = tick;
      input.Command = command;

      return input;
    }
    #endregion
  }
}
