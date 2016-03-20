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
  public struct EventId : IEncodableType<EventId>
  {
    public class EventIdComparer : IEqualityComparer<EventId>
    {
      public bool Equals(EventId x, EventId y)
      {
        return (x.idValue == y.idValue);
      }

      public int GetHashCode(EventId x)
      {
        return x.idValue;
      }
    }

    // "Max" isn't exactly an accurate term since this rolls over
    private const int LOG_MAX_EVENTS = 10; // 10 -> 1023 max events
    private const int MAX_EVENTS = (1 << EventId.LOG_MAX_EVENTS) - 1;
    private const int HALF_WAY_POINT = EventId.MAX_EVENTS / 2;
    private const int BIT_SHIFT = 32 - EventId.LOG_MAX_EVENTS;

    internal static readonly EventId INVALID = new EventId(0);
    internal static readonly EventId START = new EventId(1);
    internal static readonly EventIdComparer Comparer = new EventIdComparer();

    // ID 0 is invalid so the valid ID range is [0, count] instead of [0, count - 1]
    internal static readonly IntEncoder Encoder = new IntEncoder(0, EventId.MAX_EVENTS);
    internal static readonly IntEncoder CountEncoder = new IntEncoder(0, EventId.MAX_EVENTS - 1);

    #region Operators
    public static bool operator >(EventId a, EventId b)
    {
      CommonDebug.Assert(a.IsValid);
      CommonDebug.Assert(b.IsValid);

      int difference =
        (int)(((uint)a.idValue << EventId.BIT_SHIFT) -
              ((uint)b.idValue << EventId.BIT_SHIFT));
      return difference > 0;
    }

    public static bool operator <(EventId a, EventId b)
    {
      CommonDebug.Assert(a.IsValid);
      CommonDebug.Assert(b.IsValid);

      int difference =
        (int)(((uint)a.idValue << EventId.BIT_SHIFT) -
              ((uint)b.idValue << EventId.BIT_SHIFT));
      return difference < 0;
    }

    public static bool operator >=(EventId a, EventId b)
    {
      CommonDebug.Assert(a.IsValid);
      CommonDebug.Assert(b.IsValid);

      int difference =
        (int)(((uint)a.idValue << EventId.BIT_SHIFT) -
              ((uint)b.idValue << EventId.BIT_SHIFT));
      return difference >= 0;
    }

    public static bool operator <=(EventId a, EventId b)
    {
      CommonDebug.Assert(a.IsValid);
      CommonDebug.Assert(b.IsValid);

      int difference =
        (int)(((uint)a.idValue << EventId.BIT_SHIFT) -
              ((uint)b.idValue << EventId.BIT_SHIFT));
      return difference <= 0;
    }

    public static bool operator ==(EventId a, EventId b)
    {
      CommonDebug.Assert(a.IsValid);
      CommonDebug.Assert(b.IsValid);

      return a.idValue == b.idValue;
    }

    public static bool operator !=(EventId a, EventId b)
    {
      CommonDebug.Assert(a.IsValid);
      CommonDebug.Assert(b.IsValid);

      return a.idValue != b.idValue;
    }
    #endregion

    public EventId Next 
    {
      get
      {
        CommonDebug.Assert(this.IsValid);

        int nextId = this.idValue + 1;
        if (nextId > EventId.MAX_EVENTS)
          nextId = 1;
        return new EventId(nextId);
      }
    }

    public bool IsValid
    {
      get { return (this.idValue > 0); }
    }

    private readonly int idValue;

    private EventId(int EventId)
    {
      this.idValue = EventId;
    }

    public override int GetHashCode()
    {
      return this.idValue;
    }

    public override bool Equals(object obj)
    {
      if (obj is EventId)
        return (((EventId)obj).idValue == this.idValue);
      return false;
    }

    public override string ToString()
    {
      if (this.IsValid)
        return "EVENTID:" + this.idValue;
      return "EVENTID:INVALID";
    }

    #region IEncodableType Members
    int IEncodableType<EventId>.RequiredBits
    {
      get { return EventId.Encoder.RequiredBits; }
    }

    uint IEncodableType<EventId>.Pack()
    {
      return EventId.Encoder.Pack(this.idValue);
    }

    EventId IEncodableType<EventId>.Unpack(uint data)
    {
      return new EventId(EventId.Encoder.Unpack(data));
    }
    #endregion
  }
}
