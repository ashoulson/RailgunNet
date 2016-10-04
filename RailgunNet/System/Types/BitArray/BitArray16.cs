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

using System.Collections.Generic;

namespace Railgun
{
  public struct BitArray16
  {
    public const int LENGTH = 16;

    public ushort Bits { get { return this.bitField; } }

    private readonly ushort bitField;

    public static BitArray16 operator <<(BitArray16 a, int b)
    {
      return new BitArray16((ushort)(a.bitField << b));
    }

    public static BitArray16 operator >>(BitArray16 a, int b)
    {
      return new BitArray16((ushort)(a.bitField >> b));
    }

    private BitArray16(ushort bitField)
    {
      this.bitField = bitField;
    }

    public BitArray16 Store(int value)
    {
      RailDebug.Assert(value < LENGTH);
      return new BitArray16((ushort)(this.bitField | (1U << value)));
    }

    public BitArray16 Remove(int value)
    {
      RailDebug.Assert(value < LENGTH);
      return new BitArray16((ushort)(this.bitField & ~(1U << value)));
    }

    public IEnumerable<int> GetValues()
    {
      return BitArrayHelpers.GetValues(this.bitField);
    }

    public bool Contains(int value)
    {
      return BitArrayHelpers.Contains(value, this.bitField, LENGTH);
    }
  }
}
