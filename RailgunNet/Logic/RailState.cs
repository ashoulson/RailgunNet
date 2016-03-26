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
  /// <summary>
  /// States are the fundamental data management class of Railgun. They 
  /// contain all of the synchronized information that an Entity needs to
  /// function. States have multiple sub-fields that are sent at different
  /// cadences, as follows:
  /// 
  ///    Mutable Data:
  ///       Sent whenever the state differs from the client's view.
  ///       Delta-encoded against the client's view.
  ///    
  ///    Controller Data:
  ///       Sent to the controller of the entity every update.
  ///       Not delta-encoded -- always sent full-encode.
  ///       
  ///    Immutable Data:
  ///       Sent only once at creation. Can not be changed after.
  ///       
  ///    Removal Data (Not currently implemented):
  ///       Sent when the state is removed. Arrives at the time of removal.
  ///       
  /// In order to register a State class with Railgun, tag it with the
  /// [RegisterState] attribute. See RailRegistry.cs for more information.
  /// </summary>
  public abstract class RailState : IRailPoolable<RailState>
  {
    internal const uint FLAGS_ALL  = 0xFFFFFFFF; // All values different
    internal const uint FLAGS_NONE = 0x00000000; // No values different

    #region Delta/Record Internal Classes
    /// <summary>
    /// Used to differentiate/typesafe state deltas. Not strictly necessary.
    /// </summary>
    internal class Delta : IRailTimedValue
    {
      #region Interface
      Tick IRailTimedValue.Tick { get { return this.state.tick; } }
      #endregion

      internal RailState State { get { return this.state; } }
      internal EntityId EntityId { get { return this.state.entityId; } }
      internal Tick Tick { get { return this.state.tick; } }

      internal int FactoryType { get { return this.state.factoryType; } }
      internal bool HasControllerData { get { return this.state.HasControllerData; } }
      internal bool HasImmutableData { get { return this.state.HasImmutableData; } }

      internal bool IsDestroyed { get { return this.state.DestroyedTick.IsValid; } }
      internal Tick RemovedTick { get { return this.state.DestroyedTick; } }

      private readonly RailState state;

      public Delta(
        Tick tick,
        EntityId entityId,
        RailState state)
      {
        this.state = state;
        this.state.tick = tick;
        this.state.entityId = entityId;
      }
    }

    /// <summary>
    /// Used to differentiate/typesafe state records. Not strictly necessary.
    /// </summary>
    internal class Record : IRailTimedValue
    {
      #region Interface
      Tick IRailTimedValue.Tick { get { return this.state.tick; } }
      #endregion

      internal RailState State { get { return this.state; } }
      internal Tick Tick { get { return this.state.tick; } }

      private readonly RailState state;

      public Record(
        Tick tick,
        RailState state)
      {
        this.state = state;
        this.state.tick = tick;
      }
    }
    #endregion

    #region Interface
    IRailPool<RailState> IRailPoolable<RailState>.Pool { get; set; }
    void IRailPoolable<RailState>.Reset() { this.Reset(); }
    #endregion

    #region Creation
    internal static RailState Create(int factoryType)
    {
      RailState state = RailResource.Instance.CreateState(factoryType);
      state.factoryType = factoryType;
      return state;
    }

#if SERVER
    /// <summary>
    /// Creates a delta between a state and a record. If forced is set to
    /// false, this function will return null if there is no change between
    /// the current and basis.
    /// </summary>
    internal static RailState.Delta CreateDelta(
      Tick tick,
      EntityId entityId,
      RailState current,
      RailState.Record basisRecord,
      bool includeControllerData,
      bool includeImmutableData,
      Tick destroyedTick,
      bool forced = false)
    {
      bool shouldReturn =
        forced ||
        includeControllerData ||
        includeImmutableData;

      uint flags = RailState.FLAGS_ALL;
      if ((basisRecord != null) && (basisRecord.State != null))
        flags = current.CompareMutableData(basisRecord.State);
      if ((flags == 0) && (shouldReturn == false))
        return null;

      RailState deltaState = RailState.Create(current.factoryType);
      deltaState.Flags = flags;
      deltaState.ApplyMutableFrom(current, deltaState.Flags);

      deltaState.HasControllerData = includeControllerData;
      if (includeControllerData)
        deltaState.ApplyControllerFrom(current);

      deltaState.HasImmutableData = includeImmutableData;
      if (includeImmutableData)
        deltaState.ApplyImmutableFrom(current);

      deltaState.DestroyedTick = destroyedTick;

      return new RailState.Delta(tick, entityId, deltaState);
    }
#endif

    /// <summary>
    /// Creates a record of the current state, taking the latest record (if
    /// any) into account. If a latest state is given, this function will
    /// return null if there is no change between the current and latest.
    /// </summary>
    internal static RailState.Record CreateRecord(
      Tick tick,
      RailState current,
      RailState.Record latestRecord = null)
    {
      if (latestRecord != null)
      {
        RailState latest = latestRecord.State;
        bool shouldReturn = 
          (current.CompareMutableData(latest) > 0) ||
          (current.IsControllerDataEqual(latest) == false);
        if (shouldReturn == false)
          return null;
      }

      RailState recordState = current.Clone();
      return new RailState.Record(tick, recordState);
    }
    #endregion

    private static IntCompressor FactoryTypeCompressor
    {
      get { return RailResource.Instance.EntityTypeCompressor; }
    }

    protected internal abstract int FlagBits { get; }

    private uint Flags { get; set; }             // Synchronized
    private bool HasControllerData { get; set; } // Synchronized
    private bool HasImmutableData { get; set; }  // Synchronized
    private Tick DestroyedTick { get; set; }     // Synchronized

    #region Client
    protected abstract void DecodeMutableData(BitBuffer buffer, uint flags);
    protected abstract void DecodeControllerData(BitBuffer buffer);
    protected abstract void DecodeImmutableData(BitBuffer buffer);
    #endregion

    #region Server
    protected abstract void EncodeMutableData(BitBuffer buffer, uint flags);
    protected abstract void EncodeControllerData(BitBuffer buffer);
    protected abstract void EncodeImmutableData(BitBuffer buffer);
    #endregion

    protected abstract void ResetAllData();

    internal abstract void ApplyMutableFrom(RailState source, uint flags);
    internal abstract void ApplyControllerFrom(RailState source);
    internal abstract void ApplyImmutableFrom(RailState source);

    internal abstract uint CompareMutableData(RailState basis);
    internal abstract bool IsControllerDataEqual(RailState basis);

    internal abstract void ApplySmoothed(RailState first, RailState second, float t);

    // Only accessible (read or write) via Delta or Record
    private EntityId entityId;
    private Tick tick;

    private int factoryType;

    protected bool GetFlag(uint flags, uint flag)
    {
      return ((flags & flag) == flag);
    }

    protected uint SetFlag(bool isEqual, uint flag)
    {
      if (isEqual == false)
        return flag;
      return 0;
    }

    internal RailState Clone()
    {
      RailState clone = RailState.Create(this.factoryType);
      clone.OverwriteFrom(this);
      return clone;
    }

    internal void OverwriteFrom(RailState source)
    {
      this.Flags = source.Flags;
      this.ApplyMutableFrom(source, RailState.FLAGS_ALL);
      this.ApplyControllerFrom(source);
      this.ApplyImmutableFrom(source);
      this.HasControllerData = source.HasControllerData;
      this.HasImmutableData = source.HasImmutableData;
    }

    private void Reset()
    {
      this.Flags = 0;
      this.HasControllerData = false;
      this.HasImmutableData = false;
      this.ResetAllData();
    }

#if CLIENT
    internal void ApplyDelta(RailState.Delta delta)
    {
      RailState deltaState = delta.State;
      this.ApplyMutableFrom(deltaState, deltaState.Flags);
      if (deltaState.HasControllerData)
        this.ApplyControllerFrom(deltaState);
      if (deltaState.HasImmutableData)
        this.ApplyImmutableFrom(deltaState);
    }
#endif

#if SERVER
    internal static void EncodeDelta(
      BitBuffer buffer,
      RailState.Delta delta)
    {
      // Write: [EntityId]
      buffer.WriteEntityId(delta.EntityId);

      // Write: [FactoryType]
      RailState state = delta.State;
      buffer.WriteInt(RailState.FactoryTypeCompressor, state.factoryType);

      // Write: [IsDestroyed]
      buffer.WriteBool(state.DestroyedTick.IsValid);

      if (state.DestroyedTick.IsValid)
      {
        // Write: [DestroyedTick]
        buffer.WriteTick(state.DestroyedTick);

        // End Write
        return;
      }

      // Write: [HasControllerData]
      buffer.WriteBool(state.HasControllerData);

      // Write: [HasImmutableData]
      buffer.WriteBool(state.HasImmutableData);

      // Write: [Flags]
      buffer.Write(state.FlagBits, state.Flags);

      // Write: [Mutable Data]
      state.EncodeMutableData(buffer, state.Flags);

      // Write: [Controller Data] (if applicable)
      if (state.HasControllerData)
        state.EncodeControllerData(buffer);

      // Write: [Immutable Data] (if applicable)
      if (state.HasImmutableData)
        state.EncodeImmutableData(buffer);
    }
#endif
#if CLIENT
    internal static RailState.Delta DecodeDelta(
      BitBuffer buffer, 
      Tick packetTick)
    {
      // Write: [EntityId]
      EntityId entityId = buffer.ReadEntityId();

      // Read: [FactoryType]
      int factoryType = buffer.ReadInt(RailState.FactoryTypeCompressor);
      RailState state = RailState.Create(factoryType);

      // Read: [IsDestroyed]
      bool isDestroyed = buffer.ReadBool();

      if (isDestroyed)
      {
        // Read: [DestroyedTick]
        state.DestroyedTick = buffer.ReadTick();

        // End Read
        return new RailState.Delta(packetTick, entityId, state);
      }

      // Read: [HasControllerData]
      state.HasControllerData = buffer.ReadBool();

      // Read: [HasImmutableData]
      state.HasImmutableData = buffer.ReadBool();

      // Read: [Flags]
      state.Flags = buffer.Read(state.FlagBits);

      // Read: [Mutable Data]
      state.DecodeMutableData(buffer, state.Flags);

      // Read: [Controller Data] (if applicable)
      if (state.HasControllerData)
        state.DecodeControllerData(buffer);

      // Read: [Immutable Data] (if applicable)
      if (state.HasImmutableData)
        state.DecodeImmutableData(buffer);

      return new RailState.Delta(packetTick, entityId, state);
    }
#endif
  }

  public abstract class RailState<T> : RailState
    where T : RailState<T>, new()
  {
    #region Casting Overrides
    internal override void ApplyMutableFrom(RailState source, uint flags)
    {
      this.ApplyMutableFrom((T)source, flags);
    }

    internal override void ApplyControllerFrom(RailState source)
    {
      this.ApplyControllerFrom((T)source);
    }

    internal override void ApplyImmutableFrom(RailState source)
    {
      this.ApplyImmutableFrom((T)source);
    }

    internal override uint CompareMutableData(RailState basis)
    {
      return CompareMutableData((T)basis);
    }

    internal override bool IsControllerDataEqual(RailState basis)
    {
      return IsControllerDataEqual((T)basis);
    }

    internal override void ApplySmoothed(RailState first, RailState second, float t)
    {
      this.ApplySmoothed((T)first, (T)second, t);
    }
    #endregion

    protected abstract void ApplyMutableFrom(T source, uint flags);
    protected abstract void ApplyControllerFrom(T source);
    protected abstract void ApplyImmutableFrom(T source);

    protected abstract uint CompareMutableData(T basis);
    protected abstract bool IsControllerDataEqual(T basis);

    protected virtual void ApplySmoothed(T first, T second, float t)
    {
      // Do nothing -- will just use whatever the current state is
    }
  }
}
