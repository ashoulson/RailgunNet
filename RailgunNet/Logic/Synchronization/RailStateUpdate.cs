using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Railgun
{
  /// <summary>
  /// A wrapper for a data container containing tick and entity information
  /// for applying deltas to entities to change their state.
  /// </summary>
  public class RailStateUpdate : 
    IRailPoolable<RailStateUpdate>, IRailTimedValue
  {
    #region Interface
    IRailPool<RailStateUpdate> IRailPoolable<RailStateUpdate>.Pool { get; set; }
    void IRailPoolable<RailStateUpdate>.Reset() { this.Reset(); }
    Tick IRailTimedValue.Tick { get { return this.Tick; } }
    #endregion

    internal static RailStateUpdate Allocate(int entityType)
    {
      // TODO: ALLOCATE
      return null;
    }

    internal int EntityType { get; private set; }     // Synchronized
    internal EntityId EntityId { get; private set; }  // Synchronized

    // Client-only
    internal Tick Tick { get; private set; }          // Synchronized to client

    /// <summary>
    /// This class maintain this container and will free it on reset.
    /// </summary>
    internal RailStateData Data { get; set; }

    public RailStateUpdate()
    {
      this.EntityType = -1;
      this.EntityId = EntityId.INVALID;
      this.Tick = Tick.INVALID;

      this.Data = null;
    }

    protected internal void Reset()
    {
      this.EntityType = -1;
      this.EntityId = EntityId.INVALID;
      this.Tick = Tick.INVALID;

      RailPool.Free(this.Data);
      this.Data = null;
    }

    // Server-only
    internal void Encode(ByteBuffer buffer)
    {
      // Write: [Type] TODO: Use Int Compressor
      buffer.WriteInt(this.EntityType);

      // Write: [EntityId]
      buffer.WriteEntityId(this.EntityId);

      // Encode: [Data]
      this.Data.Encode(buffer);
    }

    // Client-only
    internal static RailStateUpdate Decode(ByteBuffer buffer, Tick packetTick)
    {
      // Read: [Type]
      int entityType = buffer.ReadInt();
      RailStateUpdate update = RailStateUpdate.Allocate(entityType);
      update.EntityType = entityType;

      // Write: [EntityId]
      update.EntityId = buffer.ReadEntityId();

      // Decode: [Data]
      update.Data = RailStateData.Decode(buffer);

      // Set the tick from the packet
      update.Tick = packetTick;
      return update;
    }
  }
}
