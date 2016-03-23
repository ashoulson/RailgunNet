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
  public abstract class RailEvent : 
    IRailPoolable<RailEvent>, IRailKeyedValue<EventId>, IRailTimedValue
  {
    #region Allocation
    [ThreadStatic]
    private static Dictionary<int, IRailPool<RailEvent>> pools;

    private static Dictionary<int, IRailPool<RailEvent>> Pools
    {
      get
      {
        if (RailEvent.pools == null)
          RailEvent.pools = RailResource.Instance.CloneEventPools();
        return RailEvent.pools;
      }
    }

    internal static RailEvent Create(int factoryType)
    {
      RailEvent evnt = RailEvent.Pools[factoryType].Allocate();
      evnt.factoryType = factoryType;
      return evnt;
    }

    public static T Create<T>()
      where T : RailEvent
    {
      return (T)RailEvent.Create(RailResource.Instance.EventTypeToKey<T>());
    }

    public static T Create<T>(RailEntity entity)
      where T : RailEvent
    {
      if (entity == null)
        throw new ArgumentNullException("entity");

      T evnt = RailEvent.Create<T>();
      evnt.Entity = entity;
      evnt.EntityId = entity.Id;
      return evnt;
    }
    #endregion

    #region Interface
    IRailPool<RailEvent> IRailPoolable<RailEvent>.Pool { get; set; }
    void IRailPoolable<RailEvent>.Reset() { this.Reset(); }
    EventId IRailKeyedValue<EventId>.Key { get { return this.EventId; } }
    Tick IRailTimedValue.Tick { get { return this.Tick; } }
    #endregion

    private static IntCompressor FactoryTypeCompressor
    {
      get { return RailResource.Instance.EventTypeCompressor; }
    }

    // Settings
    protected virtual bool CanSendToFrozen { get { return false; } }

    // Synchronized
    internal EventId EventId { get; set; }
    internal Tick Tick { get; set; }
    internal bool IsReliable { get; set; }

    // Entity targeting
    internal EntityId EntityId { get; private set; }
    internal RailEntity Entity { get; private set; }

    // Local only
    internal Tick Expiration { get; set; }

    internal abstract void SetDataFrom(RailEvent other);

    protected abstract void EncodeData(ByteBuffer buffer);
    protected abstract void DecodeData(ByteBuffer buffer);
    protected abstract void ResetData();

    protected internal virtual void Invoke() { }
    protected internal virtual void Invoke(RailEntity entity) { }

    private int factoryType;

    internal RailEvent Clone()
    {
      RailEvent clone = RailEvent.Create(this.factoryType);
      clone.EventId = this.EventId;
      clone.Tick = this.Tick;
      clone.IsReliable = this.IsReliable;
      clone.Entity = this.Entity;
      clone.EntityId = this.EntityId;
      clone.Expiration = this.Expiration;
      clone.SetDataFrom(this);
      return clone;
    }

    protected internal void Reset()
    {
      this.EventId = EventId.INVALID;
      this.Tick = Tick.INVALID;
      this.IsReliable = false;
      this.Entity = null;
      this.EntityId = EntityId.INVALID;
      this.Expiration = Tick.INVALID;
      this.ResetData();
    }

    #region Encode/Decode/etc.
    internal void Encode(ByteBuffer buffer, Tick packetSenderTick)
    {
      EntityId entityId = EntityId.INVALID;
      if (this.Entity != null)
        entityId = this.Entity.Id;

      // Write: [EventType]
      buffer.WriteInt(RailEvent.FactoryTypeCompressor, this.factoryType);

      // Write: [IsReliable]
      buffer.WriteBool(this.IsReliable);

      if (this.IsReliable)
      {
        // Write: [Tick]
        buffer.WriteTick(this.Tick);
      }
      else
      {
        // Write: [TickSpan]
        TickSpan span = TickSpan.Create(packetSenderTick, this.Tick);
        CommonDebug.Assert(span.IsInRange);
        buffer.WriteTickSpan(span);
      }

      // Write: [EventId]
      buffer.WriteEventId(this.EventId);

      // Write: [EntityId]
      buffer.WriteEntityId(entityId);

      // Write: [EventData]
      this.EncodeData(buffer);
    }

    internal static RailEvent Decode(
      ByteBuffer buffer, 
      Tick packetSenderTick)
    {
      // Read: [EventType]
      int factoryType = buffer.ReadInt(RailEvent.FactoryTypeCompressor);

      RailEvent evnt = RailEvent.Create(factoryType);

      // Read: [IsReliable]
      evnt.IsReliable = buffer.ReadBool();

      if (evnt.IsReliable)
      {
        // Read: [Tick]
        evnt.Tick = buffer.ReadTick();
      }
      else
      {
        // Read: [TickSpan]
        TickSpan span = buffer.ReadTickSpan();
        CommonDebug.Assert(span.IsInRange);
        evnt.Tick = Tick.Create(packetSenderTick, span);
      }

      // Read: [EventId]
      evnt.EventId = buffer.ReadEventId();

      // Read: [EntityId]
      EntityId entityId = buffer.ReadEntityId();

      // Read: [EventData]
      evnt.DecodeData(buffer);

      // Dereference the entity Id and make sure everything is valid
      if (entityId.IsValid)
      {
        // TODO: REENABLE FOR ENTITY DEREFERENCE
        //if (entityLookup.TryGet(entityId, out evnt.entity) == false)
        //  return null;
        // TODO: REENABLE FOR FREEZING
        //if ((evnt.CanSendToFrozenEntities == false) && evnt.entity.IsFrozen)
        //  return null;
      }

      return evnt;
    }
    #endregion
  }

  /// <summary>
  /// This is the class to override to attach user-defined data to an entity.
  /// </summary>
  public abstract class RailEvent<T> : RailEvent
    where T : RailEvent<T>, new()
  {
    #region Casting Overrides
    internal override void SetDataFrom(RailEvent other)
    {
      this.SetDataFrom((T)other);
    }
    #endregion

    protected internal abstract void SetDataFrom(T other);
  }
}
