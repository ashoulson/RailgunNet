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

namespace Railgun
{
  public static class RailIntCompressorExtensions
  {
    public static void WriteInt(
      this RailBitBuffer buffer,
      RailIntCompressor compressor,
      int value)
    {
      if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
        buffer.WriteUInt(compressor.Pack(value));
      else
        buffer.Write(compressor.RequiredBits, compressor.Pack(value));
    }

    public static int ReadInt(
      this RailBitBuffer buffer,
      RailIntCompressor compressor)
    {
      if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
        return compressor.Unpack(buffer.ReadUInt());
      else
        return compressor.Unpack(buffer.Read(compressor.RequiredBits));
    }

    public static int PeekInt(
      this RailBitBuffer buffer,
      RailIntCompressor compressor)
    {
      if (compressor.RequiredBits > RailConfig.VARINT_FALLBACK_SIZE)
        return compressor.Unpack(buffer.PeekUInt());
      else
        return compressor.Unpack(buffer.Peek(compressor.RequiredBits));
    }
  }

  public class RailIntCompressor
  {
    private readonly int minValue;
    private readonly int maxValue;

    private readonly int requiredBits;
    private readonly uint mask;

    internal int RequiredBits { get { return this.requiredBits; } }

    public RailIntCompressor(int minValue, int maxValue)
    {
      this.minValue = minValue;
      this.maxValue = maxValue;

      this.requiredBits = this.ComputeRequiredBits();
      this.mask = (uint)((1L << requiredBits) - 1);
    }

    public uint Pack(int value)
    {
      if ((value < this.minValue) || (value > this.maxValue))
        RailDebug.LogWarning(
          "Clamping value for send! " +
          value +
          " vs. [" +
          this.minValue +
          "," +
          this.maxValue +
          "]");
      return (uint)(value - this.minValue) & this.mask;
    }

    public int Unpack(uint data)
    {
      return (int)(data + this.minValue);
    }

    private int ComputeRequiredBits()
    {
      if (this.minValue >= this.maxValue)
        return 0;

      long minLong = (long)this.minValue;
      long maxLong = (long)this.maxValue;
      uint range = (uint)(maxLong - minLong);
      return RailUtil.Log2(range) + 1;
    }
  }
}