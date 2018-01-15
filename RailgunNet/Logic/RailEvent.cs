/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
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
  public enum RailPolicy
  {
    All,
#if SERVER
    NoProxy,
#endif
#if CLIENT
    NoFrozen,
#endif
  }

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

    internal static TEvent Create<TEvent>(RailResource resource)
      where TEvent : RailEvent
    {
      int factoryType = resource.GetEventFactoryType<TEvent>();
      TEvent evnt = (TEvent)RailEvent.Create(resource, factoryType);
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

    // Local only
    internal int Attempts { get; set; }

    public RailRoom Room { get; private set; }
    public RailController Sender { get; private set; }

    public TEntity Find<TEntity>(
      EntityId id,
      RailPolicy policy = RailPolicy.All)
      where TEntity : class, IRailEntity
    {
      if (this.Room == null)
        return null;
      if (id.IsValid == false)
        return null;
#if SERVER
      if ((policy == RailPolicy.NoProxy) && (this.Sender == null))
        return null;
#endif

      if (this.Room.TryGet(id, out IRailEntity entity) == false)
        return null;
#if CLIENT
      if ((policy == RailPolicy.NoFrozen) && (entity.IsFrozen))
        return null;
#endif
#if SERVER
      if ((policy == RailPolicy.NoProxy) && (entity.Controller != this.Sender))
        return null;
#endif
      if (entity is TEntity cast)
        return cast;
      return null;
    }

    internal abstract void SetDataFrom(RailEvent other);

    protected abstract void EncodeData(RailBitBuffer buffer, Tick packetTick);
    protected abstract void DecodeData(RailBitBuffer buffer, Tick packetTick);
    protected abstract void ResetData();

    protected virtual bool Validate() { return true; }

    protected virtual void Execute(
      RailRoom room,
      RailController sender)
    {
      // Override this to process events
    }

    private int factoryType;

    public void Free()
    {
      RailPool.Free(this);
    }

    internal RailEvent Clone(RailResource resource)
    {
      RailEvent clone = RailEvent.Create(resource, this.factoryType);
      clone.EventId = this.EventId;
      clone.Attempts = this.Attempts;
      clone.Room = this.Room;
      clone.Sender = this.Sender;
      clone.SetDataFrom(this);
      return clone;
    }

    protected virtual void Reset()
    {
      this.EventId = SequenceId.INVALID;
      this.Attempts = 0;
      this.Room = null;
      this.Sender = null;
      this.ResetData();
    }

    internal void Invoke(
      RailRoom room,
      RailController sender)
    {
      this.Room = room;
      this.Sender = sender;
      if (this.Validate())
        this.Execute(room, sender);
    }

    internal void RegisterSent()
    {
      if (this.Attempts > 0)
        this.Attempts--;
    }

    internal void RegisterSkip()
    {
      this.RegisterSent();
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
}
