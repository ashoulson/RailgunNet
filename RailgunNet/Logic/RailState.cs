using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  internal interface IRailStateDelta :
    IRailPoolable<IRailStateDelta>, IRailTimedValue
  {
    EntityId EntityId { get; }
    int FactoryType { get; }

    void Encode(ByteBuffer buffer);

    bool HasControllerData { get; }
    bool HasImmutableData { get; }
  }

  internal interface IRailStateRecord :
    IRailPoolable<IRailStateRecord>, IRailTimedValue
  {
  }

  public abstract class RailState : 
    IRailStateDelta, IRailStateRecord, IRailPoolable<RailState>
  {
    internal const uint FLAGS_ALL = 0xFFFFFFFF; // All values different
    internal const uint FLAGS_NONE = 0x00000000; // No values different

    internal static RailState Create(int factoryType)
    {
      RailState state = RailResource.Instance.CreateState(factoryType);
      state.factoryType = factoryType;
      return state;
    }

    #region Interface
    #region Pooling
    IRailPool<RailState> IRailPoolable<RailState>.Pool
    {
      get { return this.pool; }
      set { this.pool = value; }
    }

    IRailPool<IRailStateDelta> IRailPoolable<IRailStateDelta>.Pool
    {
      get { return (IRailPool<IRailStateDelta>)this.pool; }
      set { this.pool = (IRailPool<RailState>)value; }
    }

    IRailPool<IRailStateRecord> IRailPoolable<IRailStateRecord>.Pool
    {
      get { return (IRailPool<IRailStateRecord>)this.pool; }
      set { this.pool = (IRailPool<RailState>)value; }
    }

    void IRailPoolable<RailState>.Reset() { this.Reset(); }
    void IRailPoolable<IRailStateDelta>.Reset() { this.Reset(); }
    void IRailPoolable<IRailStateRecord>.Reset() { this.Reset(); }

    private IRailPool<RailState> pool = null;
    #endregion

    Tick IRailTimedValue.Tick { get { return this.tick; } }

    EntityId IRailStateDelta.EntityId { get { return this.entityId; } }
    int IRailStateDelta.FactoryType { get { return this.FactoryType; } }
    void IRailStateDelta.Encode(ByteBuffer buffer) { this.Encode(buffer); }
    bool IRailStateDelta.HasControllerData { get { return this.HasControllerData; } }
    bool IRailStateDelta.HasImmutableData { get { return this.HasImmutableData; } }
    #endregion

    /// <summary>
    /// Creates a delta between a state and a record. If forced is set to
    /// false, this function will return null if there is no change between
    /// the current and basis.
    /// </summary>
    internal static IRailStateDelta CreateDelta(
      Tick tick,
      EntityId entityId,
      RailState current,
      IRailStateRecord basisRecord,
      bool includeControllerData,
      bool includeImmutableData,
      bool forced = false)
    {
      RailState basis = (RailState)basisRecord;
      bool shouldReturn =
        forced ||
        includeControllerData ||
        includeImmutableData;

      uint flags = RailState.FLAGS_ALL;
      if (basis != null)
        flags = current.CompareMutableData(basis);

      if ((flags == 0) && (shouldReturn == false))
        return null;

      RailState delta = RailState.Create(current.factoryType);
      delta.tick = tick;
      delta.entityId = entityId;
      delta.Flags = flags;
      delta.ApplyMutableFrom(current, delta.Flags);

      delta.HasControllerData = includeControllerData;
      if (includeControllerData)
        delta.ApplyControllerFrom(current);

      delta.HasImmutableData = includeImmutableData;
      if (includeImmutableData)
        delta.ApplyImmutableFrom(current);

      return delta;
    }

    /// <summary>
    /// Creates a record of the current state, taking the latest record (if
    /// any) into account. If forced is set to false, this function will
    /// return null if there is no change between the current and latest.
    /// </summary>
    internal static IRailStateRecord CreateRecord(
      Tick tick,
      RailState current,
      IRailStateRecord latestRecord,
      bool forced = false)
    {
      RailState latest = (RailState)latestRecord;

      if (forced || (latest != null))
      {
        bool shouldReturn = 
          (current.CompareMutableData(latest) > 0) ||
          (current.IsControllerDataEqual(latest) == false);
        if (shouldReturn == false)
          return null;
      }

      RailState record = current.Clone();
      record.tick = tick;
      return record;
    }

    private static IntCompressor FactoryTypeCompressor
    {
      get { return RailResource.Instance.EntityTypeCompressor; }
    }

    protected internal abstract int FlagBits { get; }

    private uint Flags { get; set; }             // Synchronized
    private bool HasControllerData { get; set; } // Synchronized
    private bool HasImmutableData { get; set; }  // Synchronized

    // Used for creating Entities
    internal int FactoryType { get { return this.factoryType; } }

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

    private int factoryType;
    private Tick tick;
    private EntityId entityId;

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

    internal void ApplyDelta(IRailStateDelta delta)
    {
      RailState deltaState = (RailState)delta;
      this.ApplyMutableFrom(deltaState, deltaState.Flags);
      if (deltaState.HasControllerData)
        this.ApplyControllerFrom(deltaState);
      if (deltaState.HasImmutableData)
        this.ApplyImmutableFrom(deltaState);
    }

    internal RailState Clone()
    {
      RailState clone = RailState.Create(this.factoryType);

      clone.Flags = this.Flags;
      clone.ApplyMutableFrom(this, RailState.FLAGS_ALL);
      clone.ApplyControllerFrom(this);
      clone.ApplyImmutableFrom(this);
      clone.HasControllerData = this.HasControllerData;
      clone.HasImmutableData = this.HasImmutableData;

      return clone;
    }

    private void Reset()
    {
      this.Flags = 0;
      this.HasControllerData = false;
      this.HasImmutableData = false;
      this.ResetAllData();
    }

    private void Encode(ByteBuffer buffer)
    {
      // Write: [FactoryType]
      buffer.WriteInt(RailState.FactoryTypeCompressor, this.factoryType);

      // Write: [EntityId]
      buffer.WriteEntityId(this.entityId);

      // Write: [HasControllerData]
      buffer.WriteBool(this.HasControllerData);

      // Write: [HasImmutableData]
      buffer.WriteBool(this.HasImmutableData);

      // Write: [Flags]
      buffer.Write(this.FlagBits, this.Flags);

      // Write: [Mutable Data]
      this.EncodeMutableData(buffer, this.Flags);

      // Write: [Controller Data] (if applicable)
      if (this.HasControllerData)
        this.EncodeControllerData(buffer);

      // Write: [Immutable Data] (if applicable)
      if (this.HasImmutableData)
        this.EncodeImmutableData(buffer);
    }

    internal static IRailStateDelta Decode(
      ByteBuffer buffer, 
      Tick packetTick)
    {
      // Read: [FactoryType]
      int factoryType = buffer.ReadInt(RailState.FactoryTypeCompressor);

      RailState delta = RailState.Create(factoryType);

      // Write: [EntityId]
      delta.entityId = buffer.ReadEntityId();

      // Read: [HasControllerData]
      delta.HasControllerData = buffer.ReadBool();

      // Read: [HasImmutableData]
      delta.HasImmutableData = buffer.ReadBool();

      // Read: [Flags]
      delta.Flags = buffer.Read(delta.FlagBits);

      // Read: [IsFirst]
      delta.DecodeMutableData(buffer, delta.Flags);

      // Read: [Controller Data] (if applicable)
      if (delta.HasControllerData)
        delta.DecodeControllerData(buffer);

      // Read: [Immutable Data] (if applicable)
      if (delta.HasImmutableData)
        delta.DecodeImmutableData(buffer);

      return delta;
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
    #endregion

    protected abstract void ApplyMutableFrom(T source, uint flags);
    protected abstract void ApplyControllerFrom(T source);
    protected abstract void ApplyImmutableFrom(T source);

    protected abstract uint CompareMutableData(T basis);
    protected abstract bool IsControllerDataEqual(T basis);
  }
}
