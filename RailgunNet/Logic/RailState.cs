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
  public abstract class RailState 
    : IRailPoolable<RailState>
  {
    #region Pooling
    IRailPool<RailState> IRailPoolable<RailState>.Pool { get; set; }
    void IRailPoolable<RailState>.Reset() { this.Reset(); }
    #endregion

    internal const uint FLAGS_ALL  = 0xFFFFFFFF; // All values different
    internal const uint FLAGS_NONE = 0x00000000; // No values different

    #region Creation
    internal static RailState Create(RailResource resource, int factoryType)
    {
      RailState state = resource.CreateState(factoryType);
      state.factoryType = factoryType;
      state.InitializeData();
      return state;
    }

#if SERVER
    /// <summary>
    /// Creates a delta between a state and a record. If forceUpdate is set 
    /// to false, this function will return null if there is no change between
    /// the current and basis.
    /// </summary>
    internal static RailStateDelta CreateDelta(
      RailResource resource,
      EntityId entityId,
      RailState current,
      RailStateRecord basisRecord,
      bool includeControllerData,
      bool includeImmutableData,
      Tick commandAck,
      Tick removedTick,
      bool force)
    {
      bool shouldReturn =
        force ||
        includeControllerData ||
        includeImmutableData ||
        removedTick.IsValid;

      uint flags = RailState.FLAGS_ALL;
      if ((basisRecord != null) && (basisRecord.State != null))
        flags = current.CompareMutableData(basisRecord.State);
      if ((flags == 0) && (shouldReturn == false))
        return null;

      RailState deltaState = RailState.Create(resource, current.factoryType);
      deltaState.Flags = flags;
      deltaState.ApplyMutableFrom(current, deltaState.Flags);

      deltaState.HasControllerData = includeControllerData;
      if (includeControllerData)
        deltaState.ApplyControllerFrom(current);

      deltaState.HasImmutableData = includeImmutableData;
      if (includeImmutableData)
        deltaState.ApplyImmutableFrom(current);

      // We don't need to include a tick when sending -- it's in the packet
      RailStateDelta delta = resource.CreateDelta();
      delta.Initialize(
        Tick.INVALID, 
        entityId, 
        deltaState,
        removedTick,
        commandAck,
        false);
      return delta;
    }

    /// <summary>
    /// Creates a record of the current state, taking the latest record (if
    /// any) into account. If a latest state is given, this function will
    /// return null if there is no change between the current and latest.
    /// </summary>
    internal static RailStateRecord CreateRecord(
      RailResource resource,
      Tick tick,
      RailState current,
      RailStateRecord latestRecord = null)
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

      RailStateRecord record = resource.CreateRecord();
      record.Overwrite(resource, tick, current);
      return record;
    }
#endif
    #endregion

    protected internal abstract int FlagBits { get; }

    private uint Flags { get; set; }              // Synchronized
    internal bool HasControllerData { get; set; } // Synchronized
    internal bool HasImmutableData { get; set; }  // Synchronized

    #region Client
    protected abstract void DecodeMutableData(RailBitBuffer buffer, uint flags);
    protected abstract void DecodeControllerData(RailBitBuffer buffer);
    protected abstract void DecodeImmutableData(RailBitBuffer buffer);
    #endregion

    #region Server
    protected abstract void EncodeMutableData(RailBitBuffer buffer, uint flags);
    protected abstract void EncodeControllerData(RailBitBuffer buffer);
    protected abstract void EncodeImmutableData(RailBitBuffer buffer);
    #endregion

    protected virtual void InitializeData() { }
    protected abstract void ResetAllData();
    protected abstract void ResetControllerData();

    internal abstract void ApplyMutableFrom(RailState source, uint flags);
    internal abstract void ApplyControllerFrom(RailState source);
    internal abstract void ApplyImmutableFrom(RailState source);

    internal abstract uint CompareMutableData(RailState basis);
    internal abstract bool IsControllerDataEqual(RailState basis);

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

    internal RailEntity ProduceEntity(RailResource resource)
    {
      return RailEntity.Create(resource, this.factoryType);
    }

    internal RailState Clone(RailResource resource)
    {
      RailState clone = RailState.Create(resource, this.factoryType);
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
    internal void ApplyDelta(RailStateDelta delta)
    {
      RailState deltaState = delta.State;
      this.ApplyMutableFrom(deltaState, deltaState.Flags);

      this.ResetControllerData();
      if (deltaState.HasControllerData)
        this.ApplyControllerFrom(deltaState);
      this.HasControllerData = delta.HasControllerData;

      this.HasImmutableData =
        delta.HasImmutableData || this.HasImmutableData;
      if (deltaState.HasImmutableData)
        this.ApplyImmutableFrom(deltaState);
    }
#endif

#if SERVER
    internal static void EncodeDelta(
      RailResource resource,
      RailBitBuffer buffer,
      RailStateDelta delta)
    {
      // Write: [EntityId]
      buffer.WriteEntityId(delta.EntityId);

      // Write: [IsFrozen]
      buffer.WriteBool(delta.IsFrozen);

      if (delta.IsFrozen == false)
      {
        // Write: [FactoryType]
        RailState state = delta.State;
        buffer.WriteInt(resource.EntityTypeCompressor, state.factoryType);

        // Write: [IsRemoved]
        buffer.WriteBool(delta.RemovedTick.IsValid);

        if (delta.RemovedTick.IsValid)
        {
          // Write: [RemovedTick]
          buffer.WriteTick(delta.RemovedTick);
        }

        // Write: [HasControllerData]
        buffer.WriteBool(state.HasControllerData);

        // Write: [HasImmutableData]
        buffer.WriteBool(state.HasImmutableData);

        // Write: [Flags]
        buffer.Write(state.FlagBits, state.Flags);

        // Write: [Mutable Data]
        state.EncodeMutableData(buffer, state.Flags);

        if (state.HasControllerData)
        {
          // Write: [Controller Data]
          state.EncodeControllerData(buffer);

          // Write: [Command Ack]
          buffer.WriteTick(delta.CommandAck);
        }

        if (state.HasImmutableData)
        {
          // Write: [Immutable Data]
          state.EncodeImmutableData(buffer);
        }
      }
    }
#endif
#if CLIENT
    internal static RailStateDelta DecodeDelta(
      RailResource resource,
      RailBitBuffer buffer, 
      Tick packetTick)
    {
      RailStateDelta delta = resource.CreateDelta();
      RailState state = null;

      Tick commandAck = Tick.INVALID;
      Tick removedTick = Tick.INVALID;

      // Read: [EntityId]
      EntityId entityId = buffer.ReadEntityId();

      // Read: [IsFrozen]
      bool isFrozen = buffer.ReadBool();

      if (isFrozen == false)
      {
        // Read: [FactoryType]
        int factoryType = buffer.ReadInt(resource.EntityTypeCompressor);
        state = RailState.Create(resource, factoryType);

        // Read: [IsRemoved]
        bool isRemoved = buffer.ReadBool();

        if (isRemoved)
        {
          // Read: [RemovedTick]
          removedTick = buffer.ReadTick();
        }

        // Read: [HasControllerData]
        state.HasControllerData = buffer.ReadBool();

        // Read: [HasImmutableData]
        state.HasImmutableData = buffer.ReadBool();

        // Read: [Flags]
        state.Flags = buffer.Read(state.FlagBits);

        // Read: [Mutable Data]
        state.DecodeMutableData(buffer, state.Flags);

        if (state.HasControllerData)
        {
          // Read: [Controller Data]
          state.DecodeControllerData(buffer);

          // Read: [Command Ack]
          commandAck = buffer.ReadTick();
        }

        if (state.HasImmutableData)
        {
          // Read: [Immutable Data]
          state.DecodeImmutableData(buffer);
        }
      }

      delta.Initialize(
        packetTick, 
        entityId, 
        state,
        removedTick,
        commandAck,
        isFrozen);
      return delta;
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
    #endregion

    protected abstract void ApplyMutableFrom(T source, uint flags);
    protected abstract void ApplyControllerFrom(T source);
    protected abstract void ApplyImmutableFrom(T source);

    protected abstract uint CompareMutableData(T basis);
    protected abstract bool IsControllerDataEqual(T basis);
  }
}
