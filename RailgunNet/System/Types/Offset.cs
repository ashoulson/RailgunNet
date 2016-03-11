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
  /// A type-safe and zero-safe wrapper for a tick offset. All internal values\
  /// are offset by +1 (zero is invalid, 1 is tick zero, etc.).
  /// </summary>
  internal struct Offset
  {
    internal static Offset Create(Tick latest, Tick basis)
    {
      CommonDebug.Assert(latest >= basis);

      int delta = latest - basis;
      if (delta > RailConfig.DEJITTER_BUFFER_LENGTH)
        return Offset.OUT_OF_RANGE;
      return new Offset(delta + 1);
    }

    internal static readonly Offset OUT_OF_RANGE = new Offset(-1);
    internal static readonly Offset INVALID = new Offset(0);

    internal static readonly IntEncoder Encoder =
      new IntEncoder(
        -1, 
        RailConfig.DEJITTER_BUFFER_LENGTH + 1);

    #region Properties
    public bool IsValid
    {
      get { return (this.offsetValue > 0) || (this.offsetValue == -1); }
    }

    public bool IsInRange
    {
      get { return this.offsetValue > 0; }
    }

    public bool IsOutOfRange
    {
      get { return this.offsetValue == -1; }
    }
    #endregion

    /// <summary>
    /// Should be used very sparingly. Otherwise it defeats type safety.
    /// </summary>
    internal int RawValue
    {
      get
      {
        CommonDebug.Assert(this.IsInRange);
        return this.offsetValue - 1;
      }
    }

    private readonly int offsetValue;

    private Offset(int offsetValue)
    {
      this.offsetValue = offsetValue;
    }

    public override int GetHashCode()
    {
      return this.offsetValue;
    }

    public override bool Equals(object obj)
    {
      if (obj is Offset)
        return (((Offset)obj).offsetValue == this.offsetValue);
      return false;
    }

    internal int GetCost()
    {
      return Offset.Encoder.GetCost(this.offsetValue);
    }

    internal void Write(BitBuffer buffer)
    {
      Offset.Encoder.Write(buffer, this.offsetValue);
    }

    internal static Offset Read(BitBuffer buffer)
    {
      return new Offset(Offset.Encoder.Read(buffer));
    }

    internal static Offset Peek(BitBuffer buffer)
    {
      return new Offset(Offset.Encoder.Peek(buffer));
    }
  }
}
