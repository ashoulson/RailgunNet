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
  /// Commands contain input data from the client to be applied to entities.
  /// </summary>
  public abstract class RailCommand : 
    IRailPoolable, IRailRingValue, IRailCloneable<RailCommand>
  {
    public static void RegisterCommandType<TCommand>()
      where TCommand : RailCommand, new()
    {
      RailResource.Instance.RegisterCommandType<TCommand>();
    }

    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }
    Tick IRailRingValue.Tick { get { return this.Tick; } }

    internal abstract void SetDataFrom(RailCommand other);

    internal Tick Tick { get; set; }

    protected abstract void EncodeData(BitBuffer buffer);
    protected abstract void DecodeData(BitBuffer buffer);
    protected abstract void ResetData();

    protected internal abstract void Populate();

    protected internal void Reset()
    {
      this.Tick = Tick.INVALID;
      this.ResetData();
    }

    public RailCommand Clone()
    {
      RailCommand clone = RailResource.Instance.AllocateCommand();
      clone.Tick = this.Tick;
      clone.SetDataFrom(this);
      return clone;
    }

    #region Encode/Decode/etc.
    // Command encoding order: | TICK | COMMAND DATA |

    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [Command Data]
      this.EncodeData(buffer);

      // Write: [Tick]
      buffer.Push(RailEncoders.Tick, this.Tick);
    }

    internal static RailCommand Decode(
      BitBuffer buffer)
    {
      RailCommand command = RailResource.Instance.AllocateCommand();

      // Read: [Tick]
      command.Tick = buffer.Pop(RailEncoders.Tick);

      // Read: [Command Data]
      command.DecodeData(buffer);

      return command;
    }
    #endregion
  }


  /// <summary>
  /// This is the class to override to attach user-defined data to an entity.
  /// </summary>
  public abstract class RailCommand<T> : RailCommand
    where T : RailCommand<T>, new()
  {
    #region Casting Overrides
    internal override void SetDataFrom(RailCommand other)
    {
      this.SetDataFrom((T)other);
    }
    #endregion

    protected internal abstract void SetDataFrom(T other);
  }
}
