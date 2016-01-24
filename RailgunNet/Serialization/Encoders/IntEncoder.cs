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

using UnityEngine;

namespace Railgun
{
  internal class IntEncoder : IEncoder<int>
  {
    private readonly int minValue;
    private readonly int maxValue;

    private readonly int requiredBits;
    private readonly uint mask;

    public int MinValue { get { return this.minValue; } }
    public int MaxValue { get { return this.maxValue; } }
    public int RequiredBits { get { return this.requiredBits; } }

    /// <summary>
    /// Initializes a float serializer. We use shorts to retrieve the min and
    /// max values in order to avoid overflow errors during conversion.
    /// </summary>
    public IntEncoder(int minValue, int maxValue)
    {
      this.minValue = minValue;
      this.maxValue = maxValue;

      this.requiredBits = this.ComputeRequiredBits();
      this.mask = (uint)((1L << requiredBits) - 1);
    }

    public uint Pack(int value)
    {
      return (uint)(value - this.minValue) & this.mask;
    }

    public int Unpack(uint data)
    {
      return (int)(data + this.minValue);
    }

    private int ComputeRequiredBits()
    {
      int range = this.maxValue - this.minValue;
      return RailgunMath.Log2((int)range) + 1;
    }

    #region Debug
    public static void Test(int outerIter, int innerIter)
    {
      for (int i = 0; i < outerIter; i++)
      {
        int a = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        int b = UnityEngine.Random.Range(int.MinValue, int.MaxValue);

        if (a > b)
          RailgunUtil.Swap(ref a, ref b);
        IntEncoder serializer = new IntEncoder(a, b);

        for (int j = 0; j < innerIter; j++)
        {
          int random = UnityEngine.Random.Range(a, b);
          uint packed = serializer.Pack(random);
          int unpacked = serializer.Unpack(packed);

          Debug.Assert(random == unpacked,
            random +
            " " +
            unpacked +
            " " +
            (int)Mathf.Abs(random - unpacked) +
            " Min: " + a +
            " Max: " + b);
        }
      }

      // Test extreme cases
      IntEncoder extreme1 = new IntEncoder(0, 0);
      Debug.Assert(extreme1.Unpack(extreme1.Pack(0)) == 0);
      Debug.Assert(extreme1.Unpack(extreme1.Pack(1)) == 0);

      IntEncoder extreme2 = new IntEncoder(int.MinValue, int.MaxValue);
      Debug.Assert(extreme2.Unpack(extreme2.Pack(0)) == 0);
      Debug.Assert(extreme2.Unpack(extreme2.Pack(1024)) == 1024);
      Debug.Assert(extreme2.Unpack(extreme2.Pack(int.MaxValue)) == int.MaxValue);
      Debug.Assert(extreme2.Unpack(extreme2.Pack(int.MinValue)) == int.MinValue);
    }
    #endregion
  }
}