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
  public struct Tick
  {
    internal static readonly Tick INVALID = new Tick(0);

    internal static readonly IntEncoder Encoder = 
      new IntEncoder(0, RailConfig.MAX_TICK + 1); // Tick 0 is invalid

    internal static int Cost 
    {
      get { return Tick.Encoder.GetCost(Tick.INVALID.tick); } 
    }

    #region Operators
    // Can't find references on these, so just delete and build to find uses

    public static bool operator <(Tick a, Tick b)
    {
      return (a.tick < b.tick);
    }

    public static bool operator >(Tick a, Tick b)
    {
      return (a.tick > b.tick);
    }

    public static bool operator ==(Tick a, Tick b)
    {
      return (a.tick == b.tick);
    }

    public static bool operator !=(Tick a, Tick b)
    {
      return (a.tick != b.tick);
    }

    public static int operator -(Tick a, Tick b)
    {
      return (a.tick - b.tick);
    }

    public static Tick operator +(Tick a, int b)
    {
      return new Tick(a.tick + b);
    }

    public static Tick operator -(Tick a, int b)
    {
      return new Tick(a.tick - b);
    }
    #endregion

    public bool IsValid
    {
      get { return (this.tick > 0); }
    }

    public float Time
    {
      get { return (this.tick * RailConfig.FIXED_DELTA_TIME); }
    }

    internal bool CanSend
    {
      get { return ((this.tick % RailConfig.NETWORK_SEND_RATE) == 0); }
    }

    /// <summary>
    /// Should be used very sparingly. Otherwise it defeats type safety.
    /// </summary>
    internal int ToInt
    {
      get { return this.tick; }
    }

    private readonly int tick;

    private Tick(int tick)
    {
      this.tick = tick;
    }

    public Tick GetNext()
    {
      return new Tick(this.tick + 1);
    }

    public override int GetHashCode()
    {
      return this.tick;
    }

    public override bool Equals(object obj)
    {
      if (obj is Tick)
        return (((Tick)obj).tick == this.tick);
      return false;
    }

    internal void Write(BitBuffer buffer)
    {
      Tick.Encoder.Write(buffer, this.tick);
    }

    internal static Tick Read(BitBuffer buffer)
    {
      return new Tick(Tick.Encoder.Read(buffer));
    }

    internal static Tick Peek(BitBuffer buffer)
    {
      return new Tick(Tick.Encoder.Peek(buffer));
    }
  }
}
