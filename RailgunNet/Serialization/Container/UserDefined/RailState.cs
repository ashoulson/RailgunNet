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
  public abstract class RailState : IRailPoolable, IRailRingValue
  {
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }
    int IRailRingValue.Tick { get { return this.Tick; } }

    public RailState()
    {
      this.Reset();
    }

    // N.B.: Does not automatically add the state to the entity!
    internal abstract RailEntity CreateEntity();
    internal abstract RailPoolState CreatePool();
    internal abstract void SetDataFrom(RailState other);

    /// <summary>
    /// The object's network ID.
    /// </summary>
    public EntityId Id { get; set; }

    /// <summary>
    /// The server tick this state was generated on.
    /// </summary>
    internal int Tick { get; set; }

    /// <summary>
    /// The int index for the type of entity this state applies to.
    /// </summary>
    protected internal abstract int EntityType { get; }

    /// <summary>
    /// Whether or not this state is predicted. Always defaults to false and
    /// must be manually set to true after a clone. Not synchronized.
    /// </summary>
    internal bool IsPredicted { get; set; }

    protected abstract void EncodeData(BitBuffer buffer);
    protected abstract void DecodeData(BitBuffer buffer);
    protected abstract void EncodeData(BitBuffer buffer, RailState basis);
    protected abstract void DecodeData(BitBuffer buffer, RailState basis);
    protected abstract void ResetData();

    protected internal void Reset() 
    {
      this.Id = EntityId.INVALID;
      this.Tick = RailClock.INVALID_TICK;
      this.IsPredicted = false;
      this.ResetData();
    }

    internal void Initialize(EntityId id, int tick)
    {
      this.Id = id;
      this.Tick = tick;
      this.IsPredicted = false;
      this.ResetData();
    }

    #region Encode/Decode/etc.
    internal static EntityId PeekId(
      BitBuffer buffer)
    {
      return buffer.Peek(StandardEncoders.EntityId);
    }

    internal RailState Clone()
    {
      RailState clone = RailResource.Instance.AllocateState(this.EntityType);
      clone.Initialize(
        this.Id,
        this.Tick);
      clone.SetDataFrom(this);
      return clone;
    }

    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [Data]
      this.EncodeData(buffer);

      // Write: [Type]
      buffer.Push(StandardEncoders.EntityType, this.EntityType);

      // Write: [Id]
      buffer.Push(StandardEncoders.EntityId, this.Id);
    }

    internal void Encode(
      BitBuffer buffer,
      RailState basis)
    {
      // Write: [Data]
      this.EncodeData(buffer, basis);

      // (No [Type] for delta states)

      // Write: [Id]
      buffer.Push(StandardEncoders.EntityId, this.Id);
    }

    internal static RailState Decode(
      BitBuffer buffer,
      int snapshotTick)
    {
      // Read: [Id]
      EntityId stateId = buffer.Pop(StandardEncoders.EntityId);

      // Read: [Type]
      int entityType = buffer.Pop(StandardEncoders.EntityType);

      RailState state = RailResource.Instance.AllocateState(entityType);
      state.Initialize(stateId, snapshotTick);

      // Read: [State Data]
      state.DecodeData(buffer);

      return state;
    }

    internal static RailState Decode(
      BitBuffer buffer,
      int snapshotTick,
      RailState basis)
    {
      // Read: [Id]
      EntityId stateId = buffer.Pop(StandardEncoders.EntityId);

      // (No [Type] for delta images)

      RailState state = RailResource.Instance.AllocateState(basis.EntityType);
      state.Initialize(stateId, snapshotTick);

      // Read: [State Data]
      state.DecodeData(buffer, basis);

      return state;
    }
    #endregion

    #region DEBUG
    public virtual string DEBUG_FormatDebug() { return ""; }
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

    protected override void EncodeData(BitBuffer buffer, RailState basis)
    {
      this.EncodeData(buffer, (T)basis);
    }

    protected override void DecodeData(BitBuffer buffer, RailState basis)
    {
      this.DecodeData(buffer, (T)basis);
    }

    internal override RailPoolState CreatePool()
    {
      return new RailPoolState<T>();
    }
    #endregion

    /// <summary>
    /// N.B.: Does not automatically add the state to the entity!
    /// </summary>
    internal override RailEntity CreateEntity() { return new TEntity(); }

    protected internal abstract void SetDataFrom(T other);
    protected internal abstract void EncodeData(BitBuffer buffer, T basis);
    protected internal abstract void DecodeData(BitBuffer buffer, T basis);
  }
}
