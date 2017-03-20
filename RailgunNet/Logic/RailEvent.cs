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

namespace Railgun
{
  /// <summary>
  /// Events are sent attached to entities and represent temporary changes
  /// in status. They can be sent to specific controllers or broadcast to all
  /// controllers for whom the entity is in scope.
  /// </summary>
  public abstract class RailEvent
    : IRailPoolable<RailEvent>
  {
    #region Pooling
    IRailPool<RailEvent> IRailPoolable<RailEvent>.Pool { get; set; }
    void IRailPoolable<RailEvent>.Reset() { this.Reset(); }
    #endregion

    internal static TEvent Create<TEvent>(RailResource resource, RailEntity entity)
      where TEvent : RailEvent
    {
      if (entity == null)
        throw new ArgumentNullException("entity");

      int factoryType = resource.GetEventFactoryType<TEvent>();
      TEvent evnt = (TEvent)RailEvent.Create(resource, factoryType);
      evnt.EntityId = entity.Id;
      return evnt;
    }

    private static RailEvent Create(RailResource resource, int factoryType)
    {
      RailEvent evnt = resource.CreateEvent(factoryType);
      evnt.factoryType = factoryType;
      return evnt;
    }

    /// <summary>
    /// Whether or not this event can be sent to a frozen entity.
    /// TODO: NOT FULLY IMPLEMENTED
    /// </summary>
    protected virtual bool CanSendToFrozen { get { return false; } }

    /// <summary>
    /// Whether or not this event can be sent by someone other than the
    /// controller of the entity. Ignored on clients.
    /// </summary>
    protected virtual bool CanProxySend { get { return false; } }

    // Synchronized
    internal SequenceId EventId { get; set; }
    internal EntityId EntityId { get; private set; }

    // Local only
    internal int Attempts { get; set; }

    internal abstract void SetDataFrom(RailEvent other);

    protected abstract void EncodeData(RailBitBuffer buffer, Tick packetTick);
    protected abstract void DecodeData(RailBitBuffer buffer, Tick packetTick);
    protected abstract void ResetData();

    protected virtual void Execute(RailRoom room, RailController sender, RailEntity entity) {}

    private int factoryType;

    public void Free()
    {
      RailPool.Free(this);
    }

    internal RailEvent Clone(RailResource resource)
    {
      RailEvent clone = RailEvent.Create(resource, this.factoryType);
      clone.EventId = this.EventId;
      clone.EntityId = this.EntityId;
      clone.Attempts = this.Attempts;
      clone.SetDataFrom(this);
      return clone;
    }

    private void Reset()
    {
      this.EventId = SequenceId.INVALID;
      this.EntityId = EntityId.INVALID;
      this.Attempts = 0;
      this.ResetData();
    }

    internal void Invoke(
      RailRoom room, 
      RailController sender, 
      RailEntity entity)
    {
      if (entity == null)
      {
        RailDebug.LogWarning("No entity for event " + this.GetType());
        return;
      }

      // Don't allow events to be sent to frozen entities if applicable
      if ((this.CanSendToFrozen == false) && entity.IsFrozen)
      {
        return;
      }

#if SERVER
      // Check proxy permissions and only accept from controllers if so
      if ((this.CanProxySend == false) && (entity.Controller != sender))
      {
        RailDebug.LogError("Invalid permissions for " + this.GetType());
        return; 
      }
#endif

      this.Execute(room, sender, entity);
    }

    internal void RegisterSent()
    {
      if (this.Attempts > 0)
        this.Attempts--;
    }

    #region Encode/Decode/etc.
    /// <summary>
    /// Note that the packetTick may not be the tick this event was created on
    /// if we're re-trying to send this event in subsequent packets. This tick
    /// is intended for use in tick diffs for compression.
    /// </summary>
    internal void Encode(
      RailResource resource,
      RailBitBuffer buffer,
      Tick packetTick)
    {
      // Write: [EventType]
      buffer.WriteInt(resource.EventTypeCompressor, this.factoryType);

      // Write: [EventId]
      buffer.WriteSequenceId(this.EventId);

      // Write: [HasEntityId]
      buffer.WriteBool(this.EntityId.IsValid);

      if (this.EntityId.IsValid)
      {
        // Write: [EntityId]
        buffer.WriteEntityId(this.EntityId);
      }

      // Write: [EventData]
      this.EncodeData(buffer, packetTick);
    }

    /// <summary>
    /// Note that the packetTick may not be the tick this event was created on
    /// if we're re-trying to send this event in subsequent packets. This tick
    /// is intended for use in tick diffs for compression.
    /// </summary>
    internal static RailEvent Decode(
      RailResource resource,
      RailBitBuffer buffer,
      Tick packetTick)
    {
      // Read: [EventType]
      int factoryType = buffer.ReadInt(resource.EventTypeCompressor);

      RailEvent evnt = RailEvent.Create(resource, factoryType);

      // Read: [EventId]
      evnt.EventId = buffer.ReadSequenceId();

      // Read: [HasEntityId]
      bool hasEntityId = buffer.ReadBool();

      if (hasEntityId)
      {
        // Read: [EntityId]
        evnt.EntityId = buffer.ReadEntityId();
      }

      // Read: [EventData]
      evnt.DecodeData(buffer, packetTick);

      return evnt;
    }
    #endregion
  }

  /// <summary>
  /// This is the class to override to attach user-defined data to an entity.
  /// </summary>
  public abstract class RailEvent<TDerived> : RailEvent
    where TDerived : RailEvent<TDerived>, new()
  {
    #region Casting Overrides
    internal override void SetDataFrom(RailEvent other)
    {
      this.SetDataFrom((TDerived)other);
    }
    #endregion

    protected internal abstract void SetDataFrom(TDerived other);
  }

  public abstract class RailEvent<TDerived, TEntity> : RailEvent<TDerived>
    where TDerived : RailEvent<TDerived>, new()
    where TEntity : RailEntity
  {
    protected override void Execute(
      RailRoom room, 
      RailController sender, 
      RailEntity entity)
    {
      TEntity cast = entity as TEntity;
      if (cast != null)
        this.Execute(room, sender, cast);
      else
        RailDebug.LogError("Can't cast event entity to " + typeof(TEntity));
    }

    protected virtual void Execute(RailRoom room, RailController sender, TEntity entity) { }
  }
}
