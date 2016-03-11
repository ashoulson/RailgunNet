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
  public enum EventType
  {
    InstantReliable,
    InstantUnreliable,
    // TODO: DelayedUnreliable
  }

  /// <summary>
  /// States are attached to entities and contain user-defined data. They are
  /// responsible for encoding and decoding that data, and delta-compression.
  /// </summary>
  public abstract class RailEvent : IRailPoolable, IRailRingValue
  {
    RailPool IRailPoolable.Pool { get; set; }
    void IRailPoolable.Reset() { this.Reset(); }
    Tick IRailRingValue.Tick { get { return this.Tick; } }

    /// <summary>
    /// The tick this command was generated on (client or server).
    /// </summary>
    internal Tick Tick { get; private set; }

    /// <summary>
    /// An optional id assigned to this event, used for reliability.
    /// </summary>
    internal EventId EventId { get; private set; }

    /// <summary>
    /// The int index for the type of event.
    /// </summary>
    protected internal abstract int EventType { get; }

    internal abstract void SetDataFrom(RailEvent other);
    internal abstract RailPoolEvent CreatePool();

    protected abstract void EncodeData(BitBuffer buffer);
    protected abstract void DecodeData(BitBuffer buffer);
    protected abstract void ResetData();

    internal void Initialize(
      Tick tick,
      EventId eventId)
    {
      this.Tick = tick;
      this.EventId = eventId;
    }

    internal RailEvent Clone()
    {
      RailEvent clone = RailResource.Instance.AllocateEvent(this.EventType);
      clone.Tick = this.Tick;
      clone.EventId = this.EventId;
      clone.SetDataFrom(this);
      return clone;
    }

    protected internal void Reset()
    {
      this.Tick = Tick.INVALID;
      this.EventId = EventId.INVALID;
      this.ResetData();
    }

    #region Encode/Decode/etc.
    // Command encoding order: | TICK | COMMAND DATA |

    internal void Encode(
      BitBuffer buffer)
    {
      // Write: [EventData]
      this.EncodeData(buffer);

      // Write: [EventId]
      buffer.Push(RailEncoders.EventId, this.EventId);

      // Write: [Tick]
      buffer.Push(RailEncoders.Tick, this.Tick);

      // Write: [EventType]
      buffer.Push(RailEncoders.EventType, this.EventType);
    }

    internal static RailEvent Decode(
      BitBuffer buffer)
    {
      // Read: [EventType]
      int eventType = buffer.Pop(RailEncoders.EventType);
      RailEvent evnt = RailResource.Instance.AllocateEvent(eventType);

      // Read: [Tick]
      evnt.Tick = buffer.Pop(RailEncoders.Tick);

      // Read: [EventId]
      evnt.EventId = buffer.Pop(RailEncoders.EventId);

      // Read: [EventData]
      evnt.DecodeData(buffer);

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

    internal override RailPoolEvent CreatePool()
    {
      return new RailPoolEvent<T>();
    }
    #endregion

    protected internal abstract void SetDataFrom(T other);
  }
}
