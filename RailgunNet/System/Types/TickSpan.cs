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
  internal static class TickSpanExtensions
  {
    private const int BITS_USED = 6; // Assuming MAX_RANGE is 50

    public static void WriteTickSpan(this ByteBuffer buffer, TickSpan eventId)
    {
      buffer.Write(TickSpanExtensions.BITS_USED, eventId.Pack());
    }

    public static TickSpan ReadTickSpan(this ByteBuffer buffer)
    {
      return TickSpan.Unpack((byte)buffer.Read(TickSpanExtensions.BITS_USED));
    }

    public static TickSpan PeekTickSpan(this ByteBuffer buffer)
    {
      return TickSpan.Unpack((byte)buffer.Peek(TickSpanExtensions.BITS_USED));
    }
  }

  /// <summary>
  /// A type-safe and zero-safe wrapper for a tick offset. All internal values\
  /// are offset by +1 (zero is invalid, 1 is tick zero, etc.).
  /// </summary>
  internal struct TickSpan
  {
    private const int MAX_RANGE = RailConfig.DEJITTER_BUFFER_LENGTH;

    #region Encoding/Decoding
    internal byte Pack()
    {
      return this.spanValue;
    }

    internal static TickSpan Unpack(byte value)
    {
      return new TickSpan(value);
    }
    #endregion

    internal static TickSpan Create(Tick latest, Tick basis)
    {
      CommonDebug.Assert(latest >= basis);

      int delta = latest - basis;
      if (delta > TickSpan.MAX_RANGE)
        return TickSpan.OUT_OF_RANGE;
      return new TickSpan((byte)(delta + 2));
    }

    internal static readonly TickSpan OUT_OF_RANGE = new TickSpan(1);
    internal static readonly TickSpan INVALID = new TickSpan(0);

    #region Properties
    public bool IsValid
    {
      get { return this.spanValue > 0; }
    }

    public bool IsInRange
    {
      get { return this.spanValue > 1; }
    }

    public bool IsOutOfRange
    {
      get { return this.spanValue == 1; }
    }
    #endregion

    /// <summary>
    /// Should be used very sparingly. Otherwise it defeats type safety.
    /// </summary>
    internal byte RawValue
    {
      get
      {
        CommonDebug.Assert(this.IsInRange);
        return (byte)(this.spanValue - 2);
      }
    }

    private readonly byte spanValue;

    private TickSpan(byte spanValue)
    {
      this.spanValue = spanValue;
    }

    public override int GetHashCode()
    {
      return this.spanValue;
    }

    public override bool Equals(object obj)
    {
      if (obj is TickSpan)
        return (((TickSpan)obj).spanValue == this.spanValue);
      return false;
    }

    public override string ToString()
    {
      if (this.spanValue == 0)
        return "TickSpan:INVALID";
      if (this.spanValue == 1)
        return "TickSpan:OUTOFRANGE";
      return "TickSpan:" + (this.spanValue - 2);
    }
  }
}
