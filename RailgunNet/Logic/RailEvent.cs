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
  /// States are attached to entities and contain user-defined data. They are
  /// responsible for encoding and decoding that data, and delta-compression.
  /// </summary>
  public abstract class RailEvent
    : IRailPoolable<RailEvent>
  {
    public const int SEND_RELIABLE = -1;

    #region Pooling
    IRailPool<RailEvent> IRailPoolable<RailEvent>.Pool { get; set; }
    void IRailPoolable<RailEvent>.Reset() { this.Reset(); }
    #endregion

    public static T Create<T>(RailEntity entity)
      where T : RailEvent
    {
      if (entity == null)
        throw new ArgumentNullException("entity");

      T evnt = RailEvent.Create<T>();
      evnt.EntityId = entity.Id;
      return evnt;
    }

    public static T Create<T>()
      where T : RailEvent
    {
      int factoryType = RailResource.Instance.GetEventFactoryType<T>();
      return (T)RailEvent.Create(factoryType);
    }

    internal static RailEvent Create(int factoryType)
    {
      RailEvent evnt = RailResource.Instance.CreateEvent(factoryType);
      evnt.factoryType = factoryType;
      return evnt;
    }

    private static RailIntCompressor FactoryTypeCompressor
    {
      get { return RailResource.Instance.EventTypeCompressor; }
    }

    // Settings
    protected virtual bool CanSendToFrozen { get { return false; } }

    // Bindings
    public RailController Sender { get; internal set; }

    // Synchronized
    internal SequenceId EventId { get; set; }
    internal EntityId EntityId { get; private set; }

    // Local only
    internal int Attempts { get; set; }

    internal bool IsReliable { get { return (this.Attempts == RailEvent.SEND_RELIABLE); } }
    internal bool CanSend { get { return ((this.Attempts > 0) || this.IsReliable); } }

    internal abstract void SetDataFrom(RailEvent other);

    protected abstract void EncodeData(RailBitBuffer buffer, Tick packetTick);
    protected abstract void DecodeData(RailBitBuffer buffer, Tick packetTick);
    protected abstract void ResetData();

    protected internal virtual void Invoke(RailRoom room, RailController sender) { }
    protected internal virtual void Invoke(RailRoom room, RailController sender, RailEntity entity) { }

    private int factoryType;

    internal RailEvent Clone()
    {
      RailEvent clone = RailEvent.Create(this.factoryType);
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
      RailBitBuffer buffer,
      Tick packetTick)
    {
      // Write: [EventType]
      buffer.WriteInt(RailEvent.FactoryTypeCompressor, this.factoryType);

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
      RailBitBuffer buffer,
      Tick packetTick)
    {
      // Read: [EventType]
      int factoryType = buffer.ReadInt(RailEvent.FactoryTypeCompressor);

      RailEvent evnt = RailEvent.Create(factoryType);

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
