using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public abstract class RailState : IRailPoolable<RailState>
  {
    internal const uint FLAGS_ALL  = 0xFFFFFFFF; // All values different
    internal const uint FLAGS_NONE = 0x00000000; // No values different

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

    internal static RailState Create(int factoryType)
    {
      RailState state = RailResource.Instance.CreateState(factoryType);
      state.factoryType = factoryType;
      return state;
    }

    #region Interface
    IRailPool<RailState> IRailPoolable<RailState>.Pool { get; set; }
    void IRailPoolable<RailState>.Reset() { this.Reset(); }
    #endregion

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

      return new RailState.Delta(tick, entityId, deltaState);
    }

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

    private static IntCompressor FactoryTypeCompressor
    {
      get { return RailResource.Instance.EntityTypeCompressor; }
    }

    protected internal abstract int FlagBits { get; }

    private uint Flags { get; set; }             // Synchronized
    private bool HasControllerData { get; set; } // Synchronized
    private bool HasImmutableData { get; set; }  // Synchronized

    #region Client
    protected abstract void DecodeMutableData(ByteBuffer buffer, uint flags);
    protected abstract void DecodeControllerData(ByteBuffer buffer);
    protected abstract void DecodeImmutableData(ByteBuffer buffer);
    #endregion

    #region Server
    protected abstract void EncodeMutableData(ByteBuffer buffer, uint flags);
    protected abstract void EncodeControllerData(ByteBuffer buffer);
    protected abstract void EncodeImmutableData(ByteBuffer buffer);
    #endregion

    protected abstract void ResetAllData();

    internal abstract void ApplyMutableFrom(RailState source, uint flags);
    internal abstract void ApplyControllerFrom(RailState source);
    internal abstract void ApplyImmutableFrom(RailState source);

    internal abstract uint CompareMutableData(RailState basis);
    internal abstract bool IsControllerDataEqual(RailState basis);

    internal abstract void ApplySmoothed(RailState first, RailState second, float t);

    // Only accessible via Delta or Record
    private int factoryType;
    private EntityId entityId;
    private Tick tick;

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

    internal void ApplyDelta(RailState.Delta delta)
    {
      RailState deltaState = delta.State;
      this.ApplyMutableFrom(deltaState, deltaState.Flags);
      if (deltaState.HasControllerData)
        this.ApplyControllerFrom(deltaState);
      if (deltaState.HasImmutableData)
        this.ApplyImmutableFrom(deltaState);
    }

    internal RailState Clone()
    {
      RailState clone = RailState.Create(this.factoryType);
      clone.OverwriteFrom(this);
      return clone;
    }

    internal void OverwriteFrom(RailState.Record source)
    {
      this.OverwriteFrom(source.State);
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

    internal static void EncodeDelta(
      ByteBuffer buffer,
      RailState.Delta delta)
    {
      // Write: [EntityId]
      buffer.WriteEntityId(delta.EntityId);

      // Write: [FactoryType]
      RailState state = delta.State;
      buffer.WriteInt(RailState.FactoryTypeCompressor, state.factoryType);

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

    internal static RailState.Delta DecodeDelta(
      ByteBuffer buffer, 
      Tick packetTick)
    {
      // Write: [EntityId]
      EntityId entityId = buffer.ReadEntityId();

      // Read: [FactoryType]
      int factoryType = buffer.ReadInt(RailState.FactoryTypeCompressor);
      RailState state = RailState.Create(factoryType);

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
