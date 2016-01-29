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

using Reservoir;

namespace Railgun
{
  /// <summary>
  /// States are attached to entities and contain user-defined data. They are
  /// responsible for encoding and decoding that data, and delta-compression.
  /// </summary>
  public abstract class State : INode<State>
  {
    #region INode<State> Members
    NodeList<State> INode<State>.List { get; set; }
    State INode<State>.Next { get; set; }
    State INode<State>.Previous { get; set; }
    #endregion

    #region Factory-Related
    internal Factory Factory { get; set; }
    internal void Free() { this.Factory.Deallocate(this); }

    internal State Clone()
    {
      State state = this.Factory.Allocate();
      state.SetFrom(this);
      return state;
    }
    #endregion

    internal abstract void SetFrom(State other);
    internal abstract bool Encode(BitPacker bitPacker, State basis);
    internal abstract void Decode(BitPacker bitPacker, State basis);

    protected internal abstract byte Type { get; }
    protected internal abstract void Encode(BitPacker bitPacker);
    protected internal abstract void Decode(BitPacker bitPacker);

    protected internal virtual void Initialize() { }
    protected internal virtual void Reset() { }
  }

  /// <summary>
  /// This is the class to override to attach user-defined data to an entity.
  /// </summary>
  public abstract class State<T> : State
    where T : State<T>
  {
    #region Casting Overrides
    internal override void SetFrom(State other)
    {
      this.SetFrom((T)other);
    }

    internal override bool Encode(BitPacker bitPacker, State basis)
    {
      return this.Encode(bitPacker, (T)basis);
    }

    internal override void Decode(BitPacker bitPacker, State basis)
    {
      this.Decode(bitPacker, (T)basis);
    }
    #endregion

    protected internal abstract void SetFrom(T other);
    protected internal abstract bool Encode(BitPacker bitPacker, T basis);
    protected internal abstract void Decode(BitPacker bitPacker, T basis);
  }
}
