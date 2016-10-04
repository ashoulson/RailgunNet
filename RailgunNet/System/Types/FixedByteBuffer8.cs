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

namespace Railgun
{
  public static class FixedByteBuffer8Extensions
  {
    public static void WriteFixedByteBuffer8(
      this RailBitBuffer buffer, 
      FixedByteBuffer8 array)
    {
      array.Write(buffer);
    }

    public static FixedByteBuffer8 ReadFixedByteBuffer8(
      this RailBitBuffer buffer)
    {
      return FixedByteBuffer8.Read(buffer);
    }
  }

  public struct FixedByteBuffer8
  {
    #region Encoding/Decoding
    internal void Write(RailBitBuffer buffer)
    {
      uint first = 
        FixedByteBuffer8.Pack(this.val0, this.val1, this.val2, this.val3);
      uint second =
        FixedByteBuffer8.Pack(this.val4, this.val5, this.val6, this.val7);
      bool writeSecond = (second > 0);

      buffer.WriteUInt(first);
      buffer.WriteBool(writeSecond);
      if (writeSecond)
        buffer.WriteUInt(second);
    }

    internal static FixedByteBuffer8 Read(RailBitBuffer buffer)
    {
      uint first = 0;
      uint second = 0;

      first = buffer.ReadUInt();
      if (buffer.ReadBool())
        second = buffer.ReadUInt();
      return new FixedByteBuffer8(first, second);
    }
    #endregion

    [ThreadStatic]
    private static byte[] BYTE_BUFFER = null;

    private static uint Pack(
      byte a, 
      byte b, 
      byte c, 
      byte d)
    {
      return
        ((uint)a << 0)  |
        ((uint)b << 8)  |
        ((uint)c << 16) |
        ((uint)d << 24);
    }

    private static void Unpack(
      uint value, 
      out byte a, 
      out byte b, 
      out byte c, 
      out byte d)
    {
      a = (byte)(value >> 0);
      b = (byte)(value >> 8);
      c = (byte)(value >> 16);
      d = (byte)(value >> 24);
    }

    public readonly byte val0;
    public readonly byte val1;
    public readonly byte val2;
    public readonly byte val3;
    public readonly byte val4;
    public readonly byte val5;
    public readonly byte val6;
    public readonly byte val7;

    public FixedByteBuffer8(
      byte val0 = 0,
      byte val1 = 0,
      byte val2 = 0,
      byte val3 = 0,
      byte val4 = 0,
      byte val5 = 0,
      byte val6 = 0,
      byte val7 = 0)
    {
      this.val0 = val0;
      this.val1 = val1;
      this.val2 = val2;
      this.val3 = val3;
      this.val4 = val4;
      this.val5 = val5;
      this.val6 = val6;
      this.val7 = val7;
    }

    private FixedByteBuffer8(uint first, uint second)
    {
      FixedByteBuffer8.Unpack(
        first, 
        out this.val0, 
        out this.val1, 
        out this.val2, 
        out this.val3);
      FixedByteBuffer8.Unpack(
        second, 
        out this.val4, 
        out this.val5, 
        out this.val6, 
        out this.val7);
    }

    public FixedByteBuffer8(byte[] buffer, int count)
    {
      if (count < buffer.Length)
        throw new ArgumentException("count < buffer.Length");

      this.val0 = 0;
      this.val1 = 0;
      this.val2 = 0;
      this.val3 = 0;
      this.val4 = 0;
      this.val5 = 0;
      this.val6 = 0;
      this.val7 = 0;

      if (count > 0)
        this.val0 = buffer[0];
      if (count > 1)
        this.val1 = buffer[1];
      if (count > 2)
        this.val2 = buffer[2];
      if (count > 3)
        this.val3 = buffer[3];
      if (count > 4)
        this.val4 = buffer[4];
      if (count > 5)
        this.val5 = buffer[5];
      if (count > 6)
        this.val6 = buffer[6];
      if (count > 7)
        this.val7 = buffer[7];
    }

    public void Output(byte[] buffer)
    {
      buffer[0] = this.val0;
      buffer[1] = this.val1;
      buffer[2] = this.val2;
      buffer[3] = this.val3;
      buffer[4] = this.val4;
      buffer[5] = this.val5;
      buffer[6] = this.val6;
      buffer[7] = this.val7;
    }

    /// <summary>
    /// Outputs to a pre-allocated per-thread byte buffer. Note that this
    /// buffer is reused between calls and may be overwritten.
    /// </summary>
    public byte[] OutputBuffered()
    {
      if (FixedByteBuffer8.BYTE_BUFFER == null)
        FixedByteBuffer8.BYTE_BUFFER = new byte[8];

      this.Output(FixedByteBuffer8.BYTE_BUFFER);
      return FixedByteBuffer8.BYTE_BUFFER;
    }
  }
}
