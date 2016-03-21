using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  public abstract class RailStateData : IRailPoolable<RailStateData>
  {
    #region Interface
    IRailPool<RailStateData> IRailPoolable<RailStateData>.Pool { get; set; }
    void IRailPoolable<RailStateData>.Reset() { this.Reset(); }
    #endregion

    internal static RailStateData Allocate(int type)
    {
      // TODO: ALLOCATE
      return null;
    }

    internal const uint FLAGS_ALL  = 0x00000000; // All values active
    internal const uint FLAGS_NONE = 0xFFFFFFFF; // No values active

    /// <summary>
    /// Number of bits used when encoding comparison flags.
    /// </summary>
    protected internal abstract int ComparisonFlagBitCount { get; }

    internal int ContainerType { get; set; }      // Synchronized
    internal uint ComparisonFlags { get; set; }   // Synchronized
    internal bool HasImmutableData { get; set; }  // Synchronized
    internal bool HasControllerData { get; set; } // Synchronized

    // Client-only
    protected abstract void DecodeMutableData(ByteBuffer buffer, uint comparisonFlags);
    protected abstract void DecodeImmutableData(ByteBuffer buffer);
    protected abstract void DecodeControllerData(ByteBuffer buffer);

    // Server-only
    protected abstract void EncodeMutableData(ByteBuffer buffer, uint comparisonFlags);
    protected abstract void EncodeControllerData(ByteBuffer buffer);
    protected abstract void EncodeImmutableData(ByteBuffer buffer);

    internal abstract void ApplyMutableData(RailStateData source, uint comparisonFlags);
    internal abstract void ApplyImmutableData(RailStateData source);
    internal abstract void ApplyControllerData(RailStateData source);
    internal abstract uint GetComparisonFlags(RailStateData basis);

    internal abstract uint ResetAllData();

    protected bool HasChanged(uint flags, uint flag)
    {
      // 0: the value stays the same
      // 1: the value should be overwritten
      return ((flags & flag) != flag);
    }

    // Server-only
    internal static RailStateData CreateDelta(
      RailStateData latest, 
      RailStateData basis,
      bool includeImmutableData,
      bool includeControllerData)
    {
      RailStateData delta = 
        RailStateData.Allocate(latest.ContainerType);
      delta.ComparisonFlags = latest.GetComparisonFlags(basis);
      delta.ApplyMutableData(latest, delta.ComparisonFlags);
      delta.HasImmutableData = includeImmutableData;
      delta.HasControllerData = includeControllerData;
      if (includeImmutableData)
        delta.ApplyImmutableData(latest);
      if (includeControllerData)
        delta.ApplyControllerData(latest);
      return delta;
    }

    internal RailStateData Clone()
    {
      RailStateData clone = 
        RailStateData.Allocate(this.ContainerType);
      clone.ContainerType = this.ContainerType;
      clone.ComparisonFlags = this.ComparisonFlags;
      clone.HasImmutableData = this.HasImmutableData;
      clone.HasControllerData = this.HasControllerData;
      clone.ApplyMutableData(this, RailStateData.FLAGS_ALL);
      clone.ApplyImmutableData(this);
      clone.ApplyControllerData(this);
      return clone;
    }

    // Client-only
    internal void ApplyData(RailStateData other)
    {
      this.ApplyMutableData(other, other.ComparisonFlags);
      if (other.HasImmutableData)
        this.ApplyImmutableData(other);
      if (other.HasControllerData)
        this.ApplyControllerData(other);
    }

    internal void Reset()
    {
      this.ContainerType = 0;
      this.ComparisonFlags = 0;
      this.HasImmutableData = false;
      this.HasControllerData = false;
      this.ResetAllData();
    }

    // Server-only
    internal void Encode(ByteBuffer buffer)
    {
      // Write: [Type] TODO: Use Int Compressor
      buffer.WriteInt(this.ContainerType);

      // Write: [HasImmutableData]
      buffer.WriteBool(this.HasImmutableData);

      // Write: [HasControllerData]
      buffer.WriteBool(this.HasControllerData);

      // Write: [Flags]
      buffer.Write(this.ComparisonFlagBitCount, this.ComparisonFlags);

      // Write: [Mutable Data]
      this.EncodeMutableData(buffer, this.ComparisonFlags);

      // Write: [Immutable Data] (if applicable)
      if (this.HasImmutableData)
        this.EncodeImmutableData(buffer);

      // Write: [Controller Data] (if applicable)
      if (this.HasControllerData)
        this.EncodeControllerData(buffer);
    }

    // Client-only
    internal static RailStateData Decode(
      ByteBuffer buffer)
    {
      // Read: [Type]
      int containerType = buffer.ReadInt();
      RailStateData container = 
        RailStateData.Allocate(containerType);
      container.ContainerType = containerType;

      // Read: [HasImmutableData]
      container.HasImmutableData = buffer.ReadBool();

      // Read: [HasControllerData]
      container.HasControllerData = buffer.ReadBool();

      // Read: [Flags]
      container.ComparisonFlags = buffer.Read(container.ComparisonFlagBitCount);

      // Read: [IsFirst]
      container.DecodeMutableData(buffer, container.ComparisonFlags);

      // Read: [Immutable Data] (if applicable)
      if (container.HasImmutableData)
        container.DecodeImmutableData(buffer);

      // Read: [Controller Data] (if applicable)
      if (container.HasControllerData)
        container.DecodeControllerData(buffer);

      return container;
    }
  }

  public abstract class RailDataContainer<T> : RailStateData
    where T : RailDataContainer<T>, new()
  {
    #region Casting Overrides
    internal override void ApplyMutableData(RailStateData source, uint comparisonFlags)
    {
      this.SetMutableDataFrom((T)source, comparisonFlags);
    }

    internal override void ApplyImmutableData(RailStateData source)
    {
      this.SetImmutableDataFrom((T)source);
    }

    internal override void ApplyControllerData(RailStateData source)
    {
      this.SetControllerDataFrom((T)source);
    }

    internal override uint GetComparisonFlags(RailStateData basis)
    {
      return this.GetComparisonFlags((T)basis);
    }
    #endregion

    protected abstract void SetMutableDataFrom(T source, uint comparisonFlags);
    protected abstract void SetImmutableDataFrom(T source);
    protected abstract void SetControllerDataFrom(T source);

    // Server-only
    protected abstract uint GetComparisonFlags(T basis);
  }
}
