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
  public static class BitArray8Extensions
  {
    public static void WriteBitArray8(this RailBitBuffer buffer, BitArray8 array)
    {
      array.Write(buffer);
    }

    public static BitArray8 ReadBitArray8(this RailBitBuffer buffer)
    {
      return BitArray8.Read(buffer);
    }

    public static BitArray8 PeekBitArray8(this RailBitBuffer buffer)
    {
      return BitArray8.Peek(buffer);
    }
  }

  public struct BitArray8
  {
    #region Encoding/Decoding
    internal void Write(RailBitBuffer buffer)
    {
      buffer.WriteByte(this.bitField);
    }

    internal static BitArray8 Read(RailBitBuffer buffer)
    {
      return new BitArray8(buffer.ReadByte());
    }

    internal static BitArray8 Peek(RailBitBuffer buffer)
    {
      return new BitArray8(buffer.PeekByte());
    }
    #endregion

    private const int LENGTH = 8;
    public static readonly BitArray8 EMPTY = new BitArray8(0);

    private readonly byte bitField;

    public static BitArray8 operator <<(BitArray8 a, int b)
    {
      return new BitArray8((byte)(a.bitField << b));
    }

    public static BitArray8 operator >>(BitArray8 a, int b)
    {
      return new BitArray8((byte)(a.bitField >> b));
    }

    public static bool operator ==(BitArray8 a, BitArray8 b)
    {
      return a.bitField == b.bitField;
    }

    public static bool operator !=(BitArray8 a, BitArray8 b)
    {
      return a.bitField != b.bitField;
    }

    private BitArray8(byte bitField)
    {
      this.bitField = bitField;
    }

    public BitArray8 Store(int value)
    {
      RailDebug.Assert(value < LENGTH);
      return new BitArray8((byte)(this.bitField | (1U << value)));
    }

    public BitArray8 Remove(int value)
    {
      RailDebug.Assert(value < LENGTH);
      return new BitArray8((byte)(this.bitField & ~(1U << value)));
    }

    public IEnumerable<int> GetValues()
    {
      return BitArrayHelpers.GetValues(this.bitField);
    }

    public bool Contains(int value)
    {
      return BitArrayHelpers.Contains(value, this.bitField, LENGTH);
    }

    public bool IsEmpty()
    {
      return this.bitField == 0;
    }

    public override bool Equals(object obj)
    {
      if (obj is BitArray8)
        return ((BitArray8)obj).bitField == this.bitField;
      return false;
    }

    public override int GetHashCode()
    {
      return this.bitField;
    }
  }
}
