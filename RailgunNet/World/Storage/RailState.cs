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
  /// States are attached to entities and contain user-defined data. They are
  /// responsible for encoding and decoding that data, and delta-compression.
  /// </summary>
  public abstract class RailState : IPoolable
  {
    Pool IPoolable.Pool { get; set; }
    void IPoolable.Reset() { this.Reset(); }

    internal RailState Clone()
    {
      RailState clone = RailResource.Instance.AllocateState(this.Type);
      clone.SetFrom(this);
      return clone;
    }

    internal abstract RailEntity CreateEntity();
    internal abstract void SetFrom(RailState other);
    internal abstract bool Encode(BitBuffer buffer, RailState basis);
    internal abstract void Decode(BitBuffer buffer, RailState basis);

    /// <summary>
    /// Should return an int code for this type of state.
    /// </summary>
    protected internal abstract int Type { get; }

    protected internal abstract void Encode(BitBuffer buffer);
    protected internal abstract void Decode(BitBuffer buffer);

    protected internal virtual void Reset() { }
  }

  /// <summary>
  /// This is the class to override to attach user-defined data to an entity.
  /// </summary>
  public abstract class RailState<T, TEntity> : RailState
    where T : RailState<T, TEntity>
    where TEntity : RailEntity<T>, new()
  {
    #region Casting Overrides
    internal override void SetFrom(RailState other)
    {
      this.SetFrom((T)other);
    }

    internal override bool Encode(BitBuffer buffer, RailState basis)
    {
      return this.Encode(buffer, (T)basis);
    }

    internal override void Decode(BitBuffer buffer, RailState basis)
    {
      this.Decode(buffer, (T)basis);
    }
    #endregion

    internal override RailEntity CreateEntity() { return new TEntity(); }

    protected internal abstract void SetFrom(T other);
    protected internal abstract bool Encode(BitBuffer buffer, T basis);
    protected internal abstract void Decode(BitBuffer buffer, T basis);
  }
}
