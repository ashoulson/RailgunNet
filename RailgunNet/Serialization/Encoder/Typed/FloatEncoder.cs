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
using System.Collections.Generic;

using Reservoir;

namespace Railgun
{
  /// <summary>
  /// Compresses floats to a given range with a given precision.
  /// http://stackoverflow.com/questions/8382629/compress-floating-point-numbers-with-specified-range-and-precision
  /// </summary>
  internal class FloatEncoder : Encoder<float>
  {
    private readonly float precision;
    private readonly float invPrecision;

    private readonly float minValue;
    private readonly float maxValue;

    private readonly int requiredBits;
    private readonly uint mask;

    internal float MinValue { get { return this.minValue; } }
    internal float MaxValue { get { return this.maxValue; } }
    internal override int RequiredBits { get { return this.requiredBits; } }

    /// <summary>
    /// Initializes a float serializer. We use shorts to retrieve the min and
    /// max values in order to avoid overflow errors during conversion.
    /// </summary>
    internal FloatEncoder(float minValue, float maxValue, float precision)
    {
      this.minValue = minValue;
      this.maxValue = maxValue;
      this.precision = precision;

      this.invPrecision = 1.0f / precision;
      this.requiredBits = this.ComputeRequiredBits();
      this.mask = (uint)((1L << requiredBits) - 1);
    }

    internal override uint Pack(float value)
    {
      value = RailgunMath.Clamp(value, this.minValue, this.maxValue);
      float adjusted = (value - this.minValue) * this.invPrecision;
      return (uint)(adjusted + 0.5f) & this.mask;
    }

    internal override float Unpack(uint data)
    {
      float adjusted = ((float)data * this.precision) + this.minValue;
      return RailgunMath.Clamp(adjusted, this.minValue, this.maxValue);
    }

    private int ComputeRequiredBits()
    {
      float range = this.maxValue - this.minValue;
      float maxVal = range * (1.0f / this.precision);
      return RailgunMath.Log2((uint)(maxVal + 0.5f)) + 1;
    }

  }
}