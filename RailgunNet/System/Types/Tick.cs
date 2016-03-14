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
  /// A type-safe and zero-safe wrapper for a tick int. Supports basic
  /// operations and encoding. All internal values are offset by +1 (zero
  /// is invalid, 1 is tick zero, etc.).
  /// </summary>
  public struct Tick : IEncodableType<Tick>
  {
    internal static Tick Create(Tick latest, TickSpan offset)
    {
      return latest - offset.RawValue;
    }

    internal static Tick ClampSubtract(Tick a, int b)
    {
      int result = a.tickValue - b;
      if (result < 1)
        result = 1;
      return new Tick(result);
    }

    internal static readonly Tick INVALID = new Tick(0);
    internal static readonly Tick START = new Tick(1);

    internal static readonly IntEncoder Encoder = 
      new IntEncoder(
        0, 
        RailConfig.MAX_TICK + 1); // Tick 0 is invalid

    #region Operators
    // Can't find references on these, so just delete and build to find uses

    public static bool operator ==(Tick a, Tick b)
    {
      return (a.tickValue == b.tickValue);
    }

    public static bool operator !=(Tick a, Tick b)
    {
      return (a.tickValue != b.tickValue);
    }

    public static bool operator <(Tick a, Tick b)
    {
      CommonDebug.Assert(a.IsValid && b.IsValid);
      return (a.tickValue < b.tickValue);
    }

    public static bool operator <=(Tick a, Tick b)
    {
      CommonDebug.Assert(a.IsValid && b.IsValid);
      return (a.tickValue <= b.tickValue);
    }

    public static bool operator >(Tick a, Tick b)
    {
      CommonDebug.Assert(a.IsValid && b.IsValid);
      return (a.tickValue > b.tickValue);
    }

    public static bool operator >=(Tick a, Tick b)
    {
      CommonDebug.Assert(a.IsValid && b.IsValid);
      return (a.tickValue >= b.tickValue);
    }

    public static int operator -(Tick a, Tick b)
    {
      CommonDebug.Assert(a.IsValid && b.IsValid);
      return (a.tickValue - b.tickValue);
    }

    public static Tick operator +(Tick a, int b)
    {
      CommonDebug.Assert(a.IsValid);
      return new Tick(a.tickValue + b);
    }

    public static Tick operator -(Tick a, int b)
    {
      int result = a.tickValue - b;
      if (result < 1)
      {
        CommonDebug.LogWarning("Clamping tick subtraction");
        result = 1;
      }

      return new Tick(result);
    }
    #endregion

    #region Properties
    public bool IsValid
    {
      get { return (this.tickValue > 0); }
    }

    public float Time
    {
      get { return ((this.tickValue - 1) * RailConfig.FIXED_DELTA_TIME); }
    }
    #endregion

    /// <summary>
    /// Should be used very sparingly. Otherwise it defeats type safety.
    /// </summary>
    internal int RawValue
    {
      get 
      {
        CommonDebug.Assert(this.IsValid);
        return this.tickValue - 1; 
      }
    }

    internal bool IsSendTick
    {
      get
      {
        if (this.IsValid)
          return ((this.RawValue % RailConfig.NETWORK_SEND_RATE) == 0);
        return false;
      }
    }

    private readonly int tickValue;

    private Tick(int tick)
    {
      this.tickValue = tick;
    }

    public Tick GetNext()
    {
      CommonDebug.Assert(this.IsValid);
      return new Tick(this.tickValue + 1);
    }

    public override int GetHashCode()
    {
      return this.tickValue;
    }

    public override bool Equals(object obj)
    {
      if (obj is Tick)
        return (((Tick)obj).tickValue == this.tickValue);
      return false;
    }

    #region IEncodableType Members
    int IEncodableType<Tick>.RequiredBits
    {
      get { return Tick.Encoder.RequiredBits; }
    }

    uint IEncodableType<Tick>.Pack()
    {
      return Tick.Encoder.Pack(this.tickValue);
    }

    Tick IEncodableType<Tick>.Unpack(uint data)
    {
      return new Tick(Tick.Encoder.Unpack(data));
    }
    #endregion

    public override string ToString()
    {
      if (this.tickValue == 0)
        return "Tick:INVALID";
      return "Tick:" + (this.tickValue - 1);
    }
  }
}
