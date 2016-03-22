using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public abstract class RailState : IRailPoolable<RailState>
  {
    #region Interface
    IRailPool<RailState> IRailPoolable<RailState>.Pool { get; set; }
    void IRailPoolable<RailState>.Reset() { this.Reset(); }
    #endregion

    internal static RailState Create(int factoryType)
    {
      // TODO: ALLOCATE
      RailState state = null;
      state.factoryType = factoryType;
      return null;
    }

    internal const uint FLAGS_ALL  = 0x00000000; // All values active
    internal const uint FLAGS_NONE = 0xFFFFFFFF; // No values active

    /// <summary>
    /// Number of bits used when encoding comparison flags.
    /// </summary>
    protected internal abstract int ValueFlagsSize { get; }

    internal int FactoryType { get { return this.factoryType; } }

    internal uint ValueFlags { get; set; }   // Synchronized
    internal bool HasImmutableData { get; set; }  // Synchronized
    internal bool HasControllerData { get; set; } // Synchronized

    // Client-only
    protected abstract void DecodeMutableData(ByteBuffer buffer, uint valueFlags);
    protected abstract void DecodeImmutableData(ByteBuffer buffer);
    protected abstract void DecodeControllerData(ByteBuffer buffer);

    // Server-only
    protected abstract void EncodeMutableData(ByteBuffer buffer, uint valueFlags);
    protected abstract void EncodeControllerData(ByteBuffer buffer);
    protected abstract void EncodeImmutableData(ByteBuffer buffer);

    internal abstract void ApplyMutableFrom(RailState source, uint valueFlags);
    internal abstract void ApplyImmutableFrom(RailState source);
    internal abstract void ApplyControllerFrom(RailState source);
    internal abstract uint GetValueFlags(RailState basis);

    internal abstract uint ResetAllData();

    // States and entities share the same factory type
    private int factoryType;

    protected bool Flag(uint flags, uint flag)
    {
      // 0: the value stays the same
      // 1: the value should be overwritten
      return ((flags & flag) != flag);
    }

    protected uint Flag(bool isEqual, uint flag)
    {
      if (isEqual)
        return 0;
      return flag;
    }

    // Server-only
    internal static RailState CreateDelta(
      RailState current, 
      RailState basis,
      bool includeImmutableData,
      bool includeControllerData)
    {
      RailState delta = RailState.Create(current.factoryType);

      uint valueFlags = RailState.FLAGS_ALL;
      if (basis != null)
        valueFlags = current.GetValueFlags(basis);

      delta.ValueFlags = valueFlags;
      delta.ApplyMutableFrom(current, delta.ValueFlags);
      delta.HasImmutableData = includeImmutableData;
      delta.HasControllerData = includeControllerData;

      if (includeImmutableData)
        delta.ApplyImmutableFrom(current);
      if (includeControllerData)
        delta.ApplyControllerFrom(current);

      return delta;
    }

    internal RailState Clone()
    {
      RailState clone = RailState.Create(this.factoryType);

      clone.ValueFlags = this.ValueFlags;
      clone.HasImmutableData = this.HasImmutableData;
      clone.HasControllerData = this.HasControllerData;
      clone.ApplyMutableFrom(this, RailState.FLAGS_ALL);
      clone.ApplyImmutableFrom(this);
      clone.ApplyControllerFrom(this);

      return clone;
    }

    // Client-only
    internal void ApplyFrom(RailState source)
    {
      this.ApplyMutableFrom(source, source.ValueFlags);
      if (source.HasImmutableData)
        this.ApplyImmutableFrom(source);
      if (source.HasControllerData)
        this.ApplyControllerFrom(source);
    }

    internal void Reset()
    {
      this.ValueFlags = 0;
      this.HasImmutableData = false;
      this.HasControllerData = false;
      this.ResetAllData();
    }

    // Server-only
    internal void Encode(ByteBuffer buffer)
    {
      // Write: [PoolType] TODO: Use Int Compressor
      buffer.WriteInt(this.factoryType);

      // Write: [HasImmutableData]
      buffer.WriteBool(this.HasImmutableData);

      // Write: [HasControllerData]
      buffer.WriteBool(this.HasControllerData);

      // Write: [ValueFlags]
      buffer.Write(this.ValueFlagsSize, this.ValueFlags);

      // Write: [Mutable Data]
      this.EncodeMutableData(buffer, this.ValueFlags);

      // Write: [Immutable Data] (if applicable)
      if (this.HasImmutableData)
        this.EncodeImmutableData(buffer);

      // Write: [Controller Data] (if applicable)
      if (this.HasControllerData)
        this.EncodeControllerData(buffer);
    }

    // Client-only
    internal static RailState Decode(
      ByteBuffer buffer)
    {
      // Read: [PoolType]
      RailState state = RailState.Create(buffer.ReadInt());

      // Read: [HasImmutableData]
      state.HasImmutableData = buffer.ReadBool();

      // Read: [HasControllerData]
      state.HasControllerData = buffer.ReadBool();

      // Read: [ValueFlags]
      state.ValueFlags = buffer.Read(state.ValueFlagsSize);

      // Read: [IsFirst]
      state.DecodeMutableData(buffer, state.ValueFlags);

      // Read: [Immutable Data] (if applicable)
      if (state.HasImmutableData)
        state.DecodeImmutableData(buffer);

      // Read: [Controller Data] (if applicable)
      if (state.HasControllerData)
        state.DecodeControllerData(buffer);

      return state;
    }
  }

  public abstract class RailState<T> : RailState
    where T : RailState<T>, new()
  {
    #region Casting Overrides
    internal override void ApplyMutableFrom(RailState source, uint comparisonFlags)
    {
      this.SetMutableDataFrom((T)source, comparisonFlags);
    }

    internal override void ApplyImmutableFrom(RailState source)
    {
      this.SetImmutableDataFrom((T)source);
    }

    internal override void ApplyControllerFrom(RailState source)
    {
      this.SetControllerDataFrom((T)source);
    }

    internal override uint GetValueFlags(RailState basis)
    {
      return this.GetValueFlags((T)basis);
    }
    #endregion

    protected abstract void SetMutableDataFrom(T source, uint comparisonFlags);
    protected abstract void SetImmutableDataFrom(T source);
    protected abstract void SetControllerDataFrom(T source);

    // Server-only
    protected abstract uint GetValueFlags(T basis);
  }
}
