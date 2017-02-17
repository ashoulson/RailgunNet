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

namespace Railgun
{
  public static class ByteBuffer8Extensions
  {
    public static void WriteFixedByteBuffer8(
      this RailBitBuffer buffer, 
      ByteBuffer8 array)
    {
      array.Write(buffer);
    }

    public static ByteBuffer8 ReadFixedByteBuffer8(
      this RailBitBuffer buffer)
    {
      return ByteBuffer8.Read(buffer);
    }
  }

  public struct ByteBuffer8
  {
    public const int MAX_COUNT = 8;

    public static bool operator ==(ByteBuffer8 a, ByteBuffer8 b)
    {
      return
        (a.count == b.count) &&
        (a.val0 == b.val0) &&
        (a.val1 == b.val1) &&
        (a.val2 == b.val2) &&
        (a.val3 == b.val3) &&
        (a.val4 == b.val4) &&
        (a.val5 == b.val5) &&
        (a.val6 == b.val6) &&
        (a.val7 == b.val7);
    }

    public static bool operator !=(ByteBuffer8 a, ByteBuffer8 b)
    {
      return !(a == b);
    }

    #region Encoding/Decoding
    internal void Write(RailBitBuffer buffer)
    {
      buffer.Write(4, (uint)(this.count & 0xF));

      switch (this.count)
      {
        case 0:
          break;

        case 1:
          buffer.WriteByte(this.val0);
          break;

        case 2:
          buffer.WriteByte(this.val0);
          buffer.WriteByte(this.val1);
          break;

        case 3:
          buffer.WriteByte(this.val0);
          buffer.WriteByte(this.val1);
          buffer.WriteByte(this.val2);
          break;

        case 4:
          buffer.WriteByte(this.val0);
          buffer.WriteByte(this.val1);
          buffer.WriteByte(this.val2);
          buffer.WriteByte(this.val3);
          break;

        case 5:
          buffer.WriteByte(this.val0);
          buffer.WriteByte(this.val1);
          buffer.WriteByte(this.val2);
          buffer.WriteByte(this.val3);
          buffer.WriteByte(this.val4);
          break;

        case 6:
          buffer.WriteByte(this.val0);
          buffer.WriteByte(this.val1);
          buffer.WriteByte(this.val2);
          buffer.WriteByte(this.val3);
          buffer.WriteByte(this.val4);
          buffer.WriteByte(this.val5);
          break;

        case 7:
          buffer.WriteByte(this.val0);
          buffer.WriteByte(this.val1);
          buffer.WriteByte(this.val2);
          buffer.WriteByte(this.val3);
          buffer.WriteByte(this.val4);
          buffer.WriteByte(this.val5);
          buffer.WriteByte(this.val6);
          break;

        case 8:
          buffer.WriteByte(this.val0);
          buffer.WriteByte(this.val1);
          buffer.WriteByte(this.val2);
          buffer.WriteByte(this.val3);
          buffer.WriteByte(this.val4);
          buffer.WriteByte(this.val5);
          buffer.WriteByte(this.val6);
          buffer.WriteByte(this.val7);
          break;

        default:
          throw new ArgumentOutOfRangeException("count = " + count);
      }
    }

    internal static ByteBuffer8 Read(RailBitBuffer buffer)
    {
      int count = (int)buffer.Read(4);

      byte val0 = 0;
      byte val1 = 0;
      byte val2 = 0;
      byte val3 = 0;
      byte val4 = 0;
      byte val5 = 0;
      byte val6 = 0;
      byte val7 = 0;

      switch (count)
      {
        case 0:
          break;

        case 1:
          val0 = buffer.ReadByte();
          break;

        case 2:
          val0 = buffer.ReadByte();
          val1 = buffer.ReadByte();
          break;

        case 3:
          val0 = buffer.ReadByte();
          val1 = buffer.ReadByte();
          val2 = buffer.ReadByte();
          break;

        case 4:
          val0 = buffer.ReadByte();
          val1 = buffer.ReadByte();
          val2 = buffer.ReadByte();
          val3 = buffer.ReadByte();
          break;

        case 5:
          val0 = buffer.ReadByte();
          val1 = buffer.ReadByte();
          val2 = buffer.ReadByte();
          val3 = buffer.ReadByte();
          val4 = buffer.ReadByte();
          break;

        case 6:
          val0 = buffer.ReadByte();
          val1 = buffer.ReadByte();
          val2 = buffer.ReadByte();
          val3 = buffer.ReadByte();
          val4 = buffer.ReadByte();
          val5 = buffer.ReadByte();
          break;

        case 7:
          val0 = buffer.ReadByte();
          val1 = buffer.ReadByte();
          val2 = buffer.ReadByte();
          val3 = buffer.ReadByte();
          val4 = buffer.ReadByte();
          val5 = buffer.ReadByte();
          val6 = buffer.ReadByte();
          break;

        case 8:
          val0 = buffer.ReadByte();
          val1 = buffer.ReadByte();
          val2 = buffer.ReadByte();
          val3 = buffer.ReadByte();
          val4 = buffer.ReadByte();
          val5 = buffer.ReadByte();
          val6 = buffer.ReadByte();
          val7 = buffer.ReadByte();
          break;

        default:
          throw new ArgumentOutOfRangeException("count = " + count);
      }

      return new ByteBuffer8(
        count,
        val0,
        val1,
        val2,
        val3,
        val4,
        val5,
        val6,
        val7);
    }
    #endregion

    public readonly int count;
    public readonly byte val0;
    public readonly byte val1;
    public readonly byte val2;
    public readonly byte val3;
    public readonly byte val4;
    public readonly byte val5;
    public readonly byte val6;
    public readonly byte val7;

    public ByteBuffer8(
      int count = 0,
      byte val0 = 0,
      byte val1 = 0,
      byte val2 = 0,
      byte val3 = 0,
      byte val4 = 0,
      byte val5 = 0,
      byte val6 = 0,
      byte val7 = 0)
    {
      if (count < 0 || count > ByteBuffer8.MAX_COUNT)
        throw new ArgumentOutOfRangeException("count = " + count);
      this.count = count;

      this.val0 = val0;
      this.val1 = val1;
      this.val2 = val2;
      this.val3 = val3;
      this.val4 = val4;
      this.val5 = val5;
      this.val6 = val6;
      this.val7 = val7;
    }

    public ByteBuffer8(byte[] buffer, int count)
    {
      if (count < 0 || count > ByteBuffer8.MAX_COUNT)
        throw new ArgumentOutOfRangeException("count = " + count);
      this.count = count;

      this.val0 = 0;
      this.val1 = 0;
      this.val2 = 0;
      this.val3 = 0;
      this.val4 = 0;
      this.val5 = 0;
      this.val6 = 0;
      this.val7 = 0;

      switch (this.count)
      {
        case 0:
          break;

        case 1:
          this.val0 = buffer[0];
          break;

        case 2:
          this.val0 = buffer[0];
          this.val1 = buffer[1];
          break;

        case 3:
          this.val0 = buffer[0];
          this.val1 = buffer[1];
          this.val2 = buffer[2];
          break;

        case 4:
          this.val0 = buffer[0];
          this.val1 = buffer[1];
          this.val2 = buffer[2];
          this.val3 = buffer[3];
          break;

        case 5:
          this.val0 = buffer[0];
          this.val1 = buffer[1];
          this.val2 = buffer[2];
          this.val3 = buffer[3];
          this.val4 = buffer[4];
          break;

        case 6:
          this.val0 = buffer[0];
          this.val1 = buffer[1];
          this.val2 = buffer[2];
          this.val3 = buffer[3];
          this.val4 = buffer[4];
          this.val5 = buffer[5];
          break;

        case 7:
          this.val0 = buffer[0];
          this.val1 = buffer[1];
          this.val2 = buffer[2];
          this.val3 = buffer[3];
          this.val4 = buffer[4];
          this.val5 = buffer[5];
          this.val6 = buffer[6];
          break;

        case 8:
          this.val0 = buffer[0];
          this.val1 = buffer[1];
          this.val2 = buffer[2];
          this.val3 = buffer[3];
          this.val4 = buffer[4];
          this.val5 = buffer[5];
          this.val6 = buffer[6];
          this.val7 = buffer[7];
          break;

        default:
          throw new ArgumentOutOfRangeException("count = " + count);
      }
    }

    public IEnumerable<byte> GetValues()
    {
      switch(this.count)
      {
        case 0:
          break;

        case 1:
          yield return this.val0;
          break;

        case 2:
          yield return this.val0;
          yield return this.val1;
          break;

        case 3:
          yield return this.val0;
          yield return this.val1;
          yield return this.val2;
          break;

        case 4:
          yield return this.val0;
          yield return this.val1;
          yield return this.val2;
          yield return this.val3;
          break;

        case 5:
          yield return this.val0;
          yield return this.val1;
          yield return this.val2;
          yield return this.val3;
          yield return this.val4;
          break;

        case 6:
          yield return this.val0;
          yield return this.val1;
          yield return this.val2;
          yield return this.val3;
          yield return this.val4;
          yield return this.val5;
          break;

        case 7:
          yield return this.val0;
          yield return this.val1;
          yield return this.val2;
          yield return this.val3;
          yield return this.val4;
          yield return this.val5;
          yield return this.val6;
          break;

        case 8:
          yield return this.val0;
          yield return this.val1;
          yield return this.val2;
          yield return this.val3;
          yield return this.val4;
          yield return this.val5;
          yield return this.val6;
          yield return this.val7;
          break;

        default:
          throw new ArgumentOutOfRangeException("count = " + count);
      }
    }

    public override bool Equals(object obj)
    {
      if (obj is ByteBuffer8)
        return (this == (ByteBuffer8)obj);
      return false;
    }

    public override int GetHashCode()
    {
      throw new NotImplementedException();
    }
  }
}
