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
  public abstract class RailState : IRailPoolable
  {
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }

    internal abstract RailEntity CreateEntity();
    internal abstract RailPoolState CreatePool();

    internal abstract void SetDataFrom(RailState other);
    internal abstract bool EncodeData(BitBuffer buffer, RailState basis);
    internal abstract void DecodeData(BitBuffer buffer, RailState basis);

    public int Id { get; internal set; }

    /// <summary>
    /// Should return an int code for this type of state.
    /// </summary>
    protected internal abstract int Type { get; }

    protected internal abstract void EncodeData(BitBuffer buffer);
    protected internal abstract void DecodeData(BitBuffer buffer);

    protected internal abstract void Reset();

    #region Encode/Decode/etc.
    /// State encoding order:
    /// If new: | ID | TYPE | ----- STATE DATA ----- |
    /// If old: | ID | ----- STATE DATA ----- |

    internal static int PeekId(
      BitBuffer buffer)
    {
      return buffer.Peek(Encoders.EntityId);
    }

    internal RailState Clone()
    {
      RailState clone = RailResource.Instance.AllocateState(this.Type);
      clone.Id = this.Id;
      clone.SetDataFrom(this);
      return clone;
    }

    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [Data]
      this.EncodeData(buffer);

      // Write: [Type]
      buffer.Push(Encoders.StateType, this.Type);

      // Write: [Id]
      buffer.Push(Encoders.EntityId, this.Id);
    }

    internal bool Encode(
      BitBuffer buffer,
      RailState basis)
    {
      // Write: [State Data] -- May not write anything if no change
      if (this.EncodeData(buffer, basis) == false)
        return false;

      // (No [Type] for delta states)

      // Write: [Id]
      buffer.Push(Encoders.EntityId, this.Id);
      return true;
    }

    internal static RailState Decode(
      BitBuffer buffer)
    {
      // Read: [Id]
      int stateId = buffer.Pop(Encoders.EntityId);

      // Read: [Type]
      int stateType = buffer.Pop(Encoders.StateType);

      RailState state = RailResource.Instance.AllocateState(stateType);
      state.Id = stateId;

      // Read: [State Data]
      state.DecodeData(buffer);

      return state;
    }

    internal static RailState Decode(
      BitBuffer buffer,
      RailState basis)
    {
      // Read: [Id]
      int stateId = buffer.Pop(Encoders.EntityId);

      // (No [Type] for delta images)

      RailState state = RailResource.Instance.AllocateState(basis.Type);
      state.Id = stateId;

      // Read: [State Data]
      state.DecodeData(buffer, basis);

      return state;
    }
    #endregion
  }

  /// <summary>
  /// This is the class to override to attach user-defined data to an entity.
  /// </summary>
  public abstract class RailState<T, TEntity> : RailState
    where T : RailState<T, TEntity>, new()
    where TEntity : RailEntity<T>, new()
  {
    #region Casting Overrides
    internal override void SetDataFrom(RailState other)
    {
      this.SetDataFrom((T)other);
    }

    internal override bool EncodeData(BitBuffer buffer, RailState basis)
    {
      return this.EncodeData(buffer, (T)basis);
    }

    internal override void DecodeData(BitBuffer buffer, RailState basis)
    {
      this.DecodeData(buffer, (T)basis);
    }

    internal override RailPoolState CreatePool()
    {
      return new RailPoolState<T>();
    }
    #endregion

    internal override RailEntity CreateEntity() { return new TEntity(); }

    protected internal abstract void SetDataFrom(T other);
    protected internal abstract bool EncodeData(BitBuffer buffer, T basis);
    protected internal abstract void DecodeData(BitBuffer buffer, T basis);
  }
}
