using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  /// <summary>
  /// Deltas are wrappers for changes to be applied to a state.
  /// Synchronized over the network.
  /// </summary>
  public class RailStateDelta : 
    IRailPoolable<RailStateDelta>, IRailTimedValue
  {
    #region Interface
    IRailPool<RailStateDelta> IRailPoolable<RailStateDelta>.Pool { get; set; }
    void IRailPoolable<RailStateDelta>.Reset() { this.Reset(); }
    Tick IRailTimedValue.Tick { get { return this.Tick; } }
    #endregion

    internal static RailStateDelta Create()
    {
      // TODO: ALLOCATE
      return null;
    }

    internal static RailStateDelta Create(
      EntityId entityId, 
      RailState state)
    {
      RailStateDelta delta = RailStateDelta.Create();
      delta.EntityId = entityId;
      delta.State = state.Clone();
      return delta;
    }

    internal static RailStateDelta Create(
      EntityId entityId,
      RailState current, 
      RailState basis,
      bool includeImmutableData,
      bool includeControllerData)
    {
      return
        RailStateDelta.Create(
          entityId,
          RailState.CreateDelta(
            current,
            basis,
            includeImmutableData,
            includeControllerData));
    }

    internal EntityId EntityId { get; private set; }  // Synchronized
    internal RailState State { get; private set; }    // Synchronized

    // Client-only
    internal Tick Tick { get; private set; }          // Taken from packet

    internal int FactoryType { get { return this.State.FactoryType; } }
    internal bool IsController { get { return this.State.HasControllerData; } }

    public RailStateDelta()
    {
      this.EntityId = EntityId.INVALID;
      this.Tick = Tick.INVALID;

      this.State = null;
    }

    internal void ApplyTo(RailState state)
    {
      state.ApplyFrom(this.State);
    }

    private void Reset()
    {
      this.EntityId = EntityId.INVALID;
      this.Tick = Tick.INVALID;

      RailPool.Free(this.State);
      this.State = null;
    }

    // Server-only
    internal void Encode(ByteBuffer buffer)
    {
      // Write: [EntityId]
      buffer.WriteEntityId(this.EntityId);

      // Encode: [Data]
      this.State.Encode(buffer);
    }

    // Client-only
    internal static RailStateDelta Decode(ByteBuffer buffer, Tick packetTick)
    {
      RailStateDelta delta = RailStateDelta.Create();

      // Write: [EntityId]
      delta.EntityId = buffer.ReadEntityId();

      // Decode: [Data]
      delta.State = RailState.Decode(buffer);

      // Set the tick from the packet
      delta.Tick = packetTick;
      return delta;
    }
  }
}
