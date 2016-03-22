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
    public static T OpenEvent<T>()
      where T : RailEvent
    {
      return RailResource.Instance.AllocateEvent<T>();
    }

    public static T OpenEvent<T>(RailEntity entity)
      where T : RailEvent
    {
      T evnt = RailResource.Instance.AllocateEvent<T>();
      evnt.entity = entity;
      return evnt;
    }

    IRailPool<RailEvent> IRailPoolable<RailEvent>.Pool { get; set; }
    void IRailPoolable<RailEvent>.Reset() { this.Reset(); }

    EventId IRailKeyedValue<EventId>.Key { get { return this.EventId; } }
    Tick IRailTimedValue.Tick { get { return this.Tick; } }

    protected virtual bool CanSendToFrozenEntities { get { return false; } }

    /// <summary>
    /// An id assigned to this event, used for reliability.
    /// </summary>
    internal EventId EventId { get; set; }

    /// <summary>
    /// The tick (sender-side) that this event was generated.
    /// </summary>
    internal Tick Tick { get; set; }

    /// <summary>
    /// Whether or not the event should be delivered reliably.
    /// </summary>
    internal bool IsReliable { get; set; }

    /// <summary>
    /// The maximum age for this event. Used for entity events.
    /// This value is not synchronized.
    /// </summary>
    internal Tick Expiration { get; set; }

    /// <summary>
    /// The int index for the type of event.
    /// </summary>
    protected internal int EventType { get; set; }

    /// <summary>
    /// The entity associated with this event, if any.
    /// </summary>
    internal RailEntity Entity
    {
      get { return this.entity; }
    }

    private RailEntity entity;

    internal abstract void SetDataFrom(RailEvent other);

    protected abstract void EncodeData(ByteBuffer buffer);
    protected abstract void DecodeData(ByteBuffer buffer);
    protected abstract void ResetData();

    protected internal virtual void Invoke() { }
    protected internal virtual void Invoke(RailEntity entity) { }

    internal void Initialize(int eventType)
    {
      this.EventType = eventType;
    }

    internal RailEvent Clone()
    {
      RailEvent clone = RailResource.Instance.AllocateEvent(this.EventType);
      clone.EventId = this.EventId;
      clone.Tick = this.Tick;
      clone.Expiration = this.Expiration;
      clone.entity = this.entity;
      clone.SetDataFrom(this);
      return clone;
    }

    protected internal void Reset()
    {
      this.EventId = EventId.INVALID;
      this.Tick = Tick.INVALID;
      this.Expiration = Tick.INVALID;
      this.entity = null;
      this.ResetData();
    }

    #region Encode/Decode/etc.
    internal void Encode(ByteBuffer buffer, Tick packetSenderTick)
    {
      EntityId entityId = EntityId.INVALID;
      if (this.Entity != null)
        entityId = this.Entity.Id;

      // Write: [EventType]
      buffer.WriteInt(
        RailResource.Instance.EventTypeCompressor, 
        this.EventType);

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
      int eventType = buffer.ReadInt(
        RailResource.Instance.EventTypeCompressor);

      RailEvent evnt = RailResource.Instance.AllocateEvent(eventType);

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
