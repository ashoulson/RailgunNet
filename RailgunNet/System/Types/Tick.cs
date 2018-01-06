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

using System.Collections.Generic;

namespace Railgun
{
  public static class TickExtensions
  {
    public static void WriteTick(this RailBitBuffer buffer, Tick tick)
    {
      tick.Write(buffer);
    }

    public static Tick ReadTick(this RailBitBuffer buffer)
    {
      return Tick.Read(buffer);
    }

    public static Tick PeekTick(this RailBitBuffer buffer)
    {
      return Tick.Peek(buffer);
    }
  }

  /// <summary>
  /// A type-safe and zero-safe wrapper for a tick int. Supports basic
  /// operations and encoding. All internal values are offset by +1 (zero
  /// is invalid, 1 is tick zero, etc.).
  /// </summary>
  public struct Tick
  {
    #region Encoding/Decoding
    internal void Write(RailBitBuffer buffer)
    {
      buffer.WriteUInt(this.tickValue);
    }

    internal static Tick Read(RailBitBuffer buffer)
    {
      return new Tick(buffer.ReadUInt());
    }

    internal static Tick Peek(RailBitBuffer buffer)
    {
      return new Tick(buffer.PeekUInt());
    }
    #endregion

    internal class TickComparer : Comparer<Tick>, IEqualityComparer<Tick>
    {
      private readonly Comparer<uint> comparer;

      public TickComparer()
      {
        this.comparer = Comparer<uint>.Default;
      }

      public override int Compare(Tick x, Tick y)
      {
        RailDebug.Assert(x.IsValid);
        RailDebug.Assert(y.IsValid);
        return this.comparer.Compare(x.tickValue, y.tickValue);
      }

      public bool Equals(Tick x, Tick y)
      {
        RailDebug.Assert(x.IsValid);
        RailDebug.Assert(y.IsValid);
        return x == y;
      }

      public int GetHashCode(Tick x)
      {
        RailDebug.Assert(x.IsValid);
        return x.GetHashCode();
      }
    }

    public static Comparer<Tick> CreateComparer()
    {
      return new TickComparer();
    }

    public static IEqualityComparer<Tick> CreateEqualityComparer()
    {
      return new TickComparer();
    }

    internal static Tick Subtract(Tick a, int b, bool warnClamp = false)
    {
      RailDebug.Assert(b >= 0);
      long result = (long)a.tickValue - b;
      if (result < 1)
      {
        if (warnClamp)
          RailDebug.LogWarning("Clamping tick subtraction");
        result = 1;
      }
      return new Tick((uint)result);
    }

    public static readonly Tick INVALID = new Tick(0);
    public static readonly Tick START = new Tick(1);

    #region Operators
    // Can't find references on these, so just delete and build to find uses

    public static Tick operator ++(Tick a)
    {
      return a.GetNext();
    }

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
      RailDebug.Assert(a.IsValid && b.IsValid);
      return (a.tickValue < b.tickValue);
    }

    public static bool operator <=(Tick a, Tick b)
    {
      RailDebug.Assert(a.IsValid && b.IsValid);
      return (a.tickValue <= b.tickValue);
    }

    public static bool operator >(Tick a, Tick b)
    {
      RailDebug.Assert(a.IsValid && b.IsValid);
      return (a.tickValue > b.tickValue);
    }

    public static bool operator >=(Tick a, Tick b)
    {
      RailDebug.Assert(a.IsValid && b.IsValid);
      return (a.tickValue >= b.tickValue);
    }

    public static int operator -(Tick a, Tick b)
    {
      RailDebug.Assert(a.IsValid && b.IsValid);
      long difference = (long)a.tickValue - (long)b.tickValue;
      return (int)difference;
    }

    public static Tick operator +(Tick a, uint b)
    {
      RailDebug.Assert(a.IsValid);
      return new Tick(a.tickValue + b);
    }

    public static Tick operator -(Tick a, int b)
    {
      return Tick.Subtract(a, b, true);
    }
    #endregion

    #region Properties
    public bool IsValid
    {
      get { return (this.tickValue > 0); }
    }

    public float ToTime(float tickDeltaTime)
    {
      RailDebug.Assert(this.IsValid);
      return (float)(this.tickValue - 1) * tickDeltaTime;
    }
    #endregion

    /// <summary>
    /// Should be used very sparingly. Otherwise it defeats type safety.
    /// </summary>
    internal uint RawValue
    {
      get 
      {
        RailDebug.Assert(this.IsValid);
        return this.tickValue - 1; 
      }
    }

    private readonly uint tickValue;

    private Tick(uint tickValue)
    {
      this.tickValue = tickValue;
    }

    public Tick GetNext()
    {
      RailDebug.Assert(this.IsValid);
      return new Tick(this.tickValue + 1);
    }

    public override int GetHashCode()
    {
      return (int)this.tickValue;
    }

    public override bool Equals(object obj)
    {
      if (obj is Tick)
        return (((Tick)obj).tickValue == this.tickValue);
      return false;
    }

    public override string ToString()
    {
      if (this.tickValue == 0)
        return "Tick:INVALID";
      return "Tick:" + (this.tickValue - 1);
    }

    internal bool IsSendTick(int tickRate)
    {
      if (this.IsValid)
        return ((this.RawValue % tickRate) == 0);
      return false;
    }
  }
}
