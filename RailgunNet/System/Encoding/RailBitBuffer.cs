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

using System;
using System.Collections.Generic;
using System.Text;

namespace Railgun
{
  /// <summary>
  /// A first-in-first-out (FIFO) bit encoding buffer.
  /// </summary>
  public class RailBitBuffer
  {
    public static int PutBytes(
      uint value,
      byte[] buffer,
      int start)
    {
      int first = start;

      while (value > 0x7Fu)
      {
        buffer[start] = (byte)(value | 0x80u);
        value >>= 7;
        start++;
      }

      buffer[start] = (byte)(value);
      return ((start - first) + 1);
    }

    public static uint ReadBytes(
      byte[] buffer,
      ref int position)
    {
      byte dataByte = 0;
      uint value = 0;

      do
      {
        value <<= 7;
        dataByte = buffer[position];
        value |= (dataByte & 0x7Fu);
        position++;
      }
      while ((dataByte & 0x80u) != 0);

      return value;
    }

    private static int FindHighestBitPosition(byte data)
    {
      int shiftCount = 0;
      while (data > 0)
      {
        data >>= 1;
        shiftCount++;
      }
      return shiftCount;
    }

    private static byte ToASCII(char character)
    {
      byte value = 0;

      try
      {
        value = Convert.ToByte(character);
      }
      catch (OverflowException)
      {
        RailDebug.LogWarning("Cannot convert to simple ASCII: " + character);
        return 0;
      }

      if (value > 127)
      {
        RailDebug.LogWarning("Cannot convert to simple ASCII: " + character);
        return 0;
      }

      return value;
    }

    private const int GROW_FACTOR = 2;
    private const int MIN_GROW = 1;
    private const int DEFAULT_CAPACITY = 8;

    /// <summary>
    /// The position of the next-to-be-read bit.
    /// </summary>
    private int readPos;

    /// <summary>
    /// The position of the next-to-be-written bit.
    /// </summary>
    public int writePos;

    /// <summary>
    /// Buffer of chunks for storing data.
    /// </summary>
    private uint[] chunks;

    /// <summary>
    /// Size the buffer will require in bytes.
    /// </summary>
    public int ByteSize { get { return ((this.writePos - 1) >> 3) + 1; } }

    /// <summary>
    /// Returns true iff we have read everything off of the buffer.
    /// </summary>
    public bool IsFinished { get { return (this.writePos == this.readPos); } }

    /// <summary>
    /// Capacity is in data chunks: uint = 4 bytes
    /// </summary>
    public RailBitBuffer(int capacity = RailBitBuffer.DEFAULT_CAPACITY)
    {
      this.chunks = new uint[capacity];
      this.readPos = 0;
      this.writePos = 0;
    }

    /// <summary>
    /// Clears the buffer (does not overwrite values, but doesn't need to).
    /// </summary>
    public void Clear()
    {
      this.readPos = 0;
      this.writePos = 0;
    }

    /// <summary>
    /// Takes the lower numBits from the value and stores them in the buffer.
    /// </summary>
    public void Write(int numBits, uint value)
    {
      if (numBits < 0)
        throw new ArgumentOutOfRangeException("Pushing negatve bits");
      if (numBits > 32)
        throw new ArgumentOutOfRangeException("Pushing too many bits");

      int index = this.writePos >> 5;
      int used = this.writePos & 0x0000001F;

      if ((index + 1) >= this.chunks.Length)
        this.ExpandArray();

      ulong chunkMask = ((1UL << used) - 1);
      ulong scratch = this.chunks[index] & chunkMask;
      ulong result = scratch | ((ulong)value << used);

      this.chunks[index] = (uint)result;
      this.chunks[index + 1] = (uint)(result >> 32);

      this.writePos += numBits;
    }

    /// <summary>
    /// Reads the next numBits from the buffer.
    /// </summary>
    public uint Read(int numBits)
    {
      uint result = this.Peek(numBits);
      this.readPos += numBits;
      return result;
    }

    /// <summary>
    /// Peeks at the next numBits from the buffer.
    /// </summary>
    public uint Peek(int numBits)
    {
      if (numBits < 0)
        throw new ArgumentOutOfRangeException("Pushing negatve bits");
      if (numBits > 32)
        throw new ArgumentOutOfRangeException("Pushing too many bits");

      int index = this.readPos >> 5;
      int used = this.readPos & 0x0000001F;

      ulong chunkMask = ((1UL << numBits) - 1) << used ;
      ulong scratch = (ulong)this.chunks[index];
      if ((index + 1) < this.chunks.Length)
        scratch |= (ulong)this.chunks[index + 1] << 32;
      ulong result = (scratch & chunkMask) >> used;

      return (uint)result;
    }

    /// <summary>
    /// Copies the buffer to a byte buffer.
    /// </summary>
    public int Store(byte[] data)
    {
      // Write a sentinel bit to find our position and flash out bad data
      this.Write(1, 1);

      int numChunks = (this.writePos >> 5) + 1;
      RailDebug.Assert(data.Length >= numChunks * 4, "Buffer too small");

      for (int i = 0; i < numChunks; i++)
      {
        int dataIdx = i * 4;
        uint chunk = this.chunks[i];
        data[dataIdx] = (byte)(chunk);
        data[dataIdx + 1] = (byte)(chunk >> 8);
        data[dataIdx + 2] = (byte)(chunk >> 16);
        data[dataIdx + 3] = (byte)(chunk >> 24);
      }

      return this.ByteSize;
    }

    /// <summary>
    /// Overwrites this buffer with an array of byte data.
    /// </summary>
    public void Load(byte[] data, int length)
    {
      int numChunks = (length / 4) + 1;
      if (this.chunks.Length < numChunks)
        this.chunks = new uint[numChunks];

      for (int i = 0; i < numChunks; i++)
      {
        int dataIdx = i * 4;
        uint chunk =
          (uint)data[dataIdx]           |
          (uint)data[dataIdx + 1] << 8  |
          (uint)data[dataIdx + 2] << 16 |
          (uint)data[dataIdx + 3] << 24;
        this.chunks[i] = chunk;
      }

      int positionInByte = RailBitBuffer.FindHighestBitPosition(data[length - 1]);

      // Take one off the position to backtrack from the sentinel bit
      this.writePos = ((length - 1) * 8) + (positionInByte - 1);
      this.readPos = 0;
    }

    /// <summary>
    /// Inserts data at a given position. Reserve the space first by writing
    /// a given number of zero bits and storing the position.
    /// </summary>
    private void Insert(int position, int numBits, uint value)
    {
      if (numBits < 0)
        throw new ArgumentOutOfRangeException("Pushing negatve bits");
      if (numBits > 32)
        throw new ArgumentOutOfRangeException("Pushing too many bits");

      int index = position >> 5;
      int used = position & 0x0000001F;

      ulong valueMask = (1UL << numBits) - 1;
      ulong prepared = (ulong)(value & valueMask) << used;
      ulong scratch = 
        (ulong)this.chunks[index] |
        (ulong)this.chunks[index + 1] << 32;
      ulong result = scratch | prepared;

      this.chunks[index] = (uint)result;
      this.chunks[index + 1] = (uint)(result >> 32);
    }

    private void ExpandArray()
    {
      int newCapacity =
        (this.chunks.Length * RailBitBuffer.GROW_FACTOR) +
        RailBitBuffer.MIN_GROW;

      uint[] newChunks = new uint[newCapacity];
      Array.Copy(this.chunks, newChunks, this.chunks.Length);
      this.chunks = newChunks;
    }

    #region Enumerables
    /// <summary>
    /// Packs all elements of an enumerable.
    /// Max 255 elements.
    /// </summary>
    public byte PackAll<T>(
      IEnumerable<T> elements,
      Action<T> encode)
    {
      byte count = 0;

      // Reserve: [Count]
      int countPosition = this.writePos;
      this.Write(8, 0);

      // Write: [Elements]
      foreach (T val in elements)
      {
        if (count == 255)
          break;

        encode.Invoke(val);
        count++;
      }

      // Deferred Write: [Count]
      this.Insert(countPosition, 8, count);
      return count;
    }

    /// <summary>
    /// Packs all elements of an enumerable up to a given size. 
    /// Max 255 elements.
    /// </summary>
    public byte PackToSize<T>(
      int maxTotalBytes,
      int maxIndividualBytes,
      IEnumerable<T> elements,
      Action<T> encode,
      Action<T> packed = null)
    {
      const int MAX_SIZE = 255;
      const int SIZE_BITS = 8;

      maxTotalBytes -= 1; // Sentinel bit can blow this up
      byte count = 0;

      // Reserve: [Count]
      int countWritePos = this.writePos;
      this.Write(SIZE_BITS, 0);

      // Write: [Elements]
      foreach (T val in elements)
      {
        if (count == MAX_SIZE)
          break;
        int rollback = this.writePos;
        int startByteSize = this.ByteSize;

        encode.Invoke(val);

        int endByteSize = this.ByteSize;
        int writeByteSize = (endByteSize - startByteSize);
        if (writeByteSize > maxIndividualBytes)
        {
          this.writePos = rollback;
          RailDebug.LogWarning(
            "Skipping " + val + " (" + writeByteSize + "B)");
        }
        else if (endByteSize > maxTotalBytes)
        {
          this.writePos = rollback;
          break;
        }
        else
        {
          if (packed != null)
            packed.Invoke(val);
          count++;
        }
      }

      // Deferred Write: [Count]
      this.Insert(countWritePos, SIZE_BITS, count);
      return count;
    }

    /// <summary>
    /// Decodes all packed items. 
    /// Max 255 elements.
    /// </summary>
    public IEnumerable<T> UnpackAll<T>(
      Func<T> decode)
    {
      // Read: [Count]
      byte count = this.ReadByte();

      // Read: [Elements]
      for (uint i = 0; i < count; i++)
        yield return decode.Invoke();
    }
    #endregion

    #region Encode/Decode
    #region Byte
    public void WriteByte(byte val)
    {
      this.Write(8, val);
    }

    public byte ReadByte()
    {
      return (byte)this.Read(8);
    }

    public byte PeekByte()
    {
      return (byte)this.Peek(8);
    }
    #endregion

    #region UInt
    /// <summary>
    /// Writes using an elastic number of bytes based on number size:
    /// 
    ///    Bits   Min Dec    Max Dec     Max Hex     Bytes Used
    ///    0-7    0          127         0x0000007F  1 byte
    ///    8-14   128        1023        0x00003FFF  2 bytes
    ///    15-21  1024       2097151     0x001FFFFF  3 bytes
    ///    22-28  2097152    268435455   0x0FFFFFFF  4 bytes
    ///    28-32  268435456  4294967295  0xFFFFFFFF  5 bytes
    /// 
    /// </summary>
    public void WriteUInt(uint val)
    {
      uint buffer = 0x0u;

      do
      {
        // Take the lowest 7 bits
        buffer = val & 0x7Fu;
        val >>= 7;

        // If there is more data, set the 8th bit to true
        if (val > 0)
          buffer |= 0x80u;

        // Store the next byte
        this.Write(8, buffer);
      }
      while (val > 0);
    }

    public uint ReadUInt()
    {
      uint buffer = 0x0u;
      uint val = 0x0u;
      int s = 0;

      do
      {
        buffer = this.Read(8);

        // Add back in the shifted 7 bits
        val |= (buffer & 0x7Fu) << s;
        s += 7;

        // Continue if we're flagged for more
      } while ((buffer & 0x80u) > 0);

      return val;
    }

    public uint PeekUInt()
    {
      int tempPosition = this.readPos;
      uint val = this.ReadUInt();
      this.readPos = tempPosition;
      return val;
    }
    #endregion

    #region Int
    public void WriteInt(int val)
    {
      uint zigzag = (uint)((val << 1) ^ (val >> 31));
      this.WriteUInt(zigzag);
    }

    public int ReadInt()
    {
      uint val = this.ReadUInt();
      int zagzig = (int)((val >> 1) ^ (-(val & 1)));
      return zagzig;
    }

    public int PeekInt()
    {
      uint val = this.PeekUInt();
      int zagzig = (int)((val >> 1) ^ (-(val & 1)));
      return zagzig;
    }
    #endregion

    #region Bool
    public void WriteBool(bool value)
    {
      this.Write(1, value ? 1U : 0U);
    }

    public bool ReadBool()
    {
      return (this.Read(1) > 0);
    }

    public bool PeekBool()
    {
      return (this.Peek(1) > 0);
    }
    #endregion

    #region String
    // 7 bits for 0-127 on the simple ASCII table
    private const int ASCII_BITS = 7;
    private static readonly int STRING_LENGTH_BITS = 
      RailUtil.Log2(RailConfig.STRING_LENGTH_MAX);

    public void WriteString(string value)
    {
      if (value == null)
        throw new ArgumentNullException("value");

      uint length = (uint)value.Length;
      RailDebug.Assert(length <= RailConfig.STRING_LENGTH_MAX, value);
      if (value.Length > RailConfig.STRING_LENGTH_MAX)
        length = RailConfig.STRING_LENGTH_MAX;

      this.Write(RailBitBuffer.STRING_LENGTH_BITS, length);
      for (int i = 0; i < length; i++)
        this.Write(RailBitBuffer.ASCII_BITS, RailBitBuffer.ToASCII(value[i]));
    }

    public string ReadString()
    {
      StringBuilder builder = new StringBuilder("");
      uint length = this.Read(RailBitBuffer.STRING_LENGTH_BITS);
      for (int i = 0; i < length; i++)
        builder.Append((char)this.Read(RailBitBuffer.ASCII_BITS));
      return builder.ToString();
    }
    #endregion
    #endregion

    public override string ToString()
    {
      StringBuilder raw = new StringBuilder();
      for (int i = this.chunks.Length - 1; i >= 0; i--)
        raw.Append(Convert.ToString(this.chunks[i], 2).PadLeft(32, '0'));

      StringBuilder spaced = new StringBuilder();
      for (int i = 0; i < raw.Length; i++)
      {
        spaced.Append(raw[i]);
        if (((i + 1) % 8) == 0)
          spaced.Append(" ");
      }

      return spaced.ToString();
    }
  }
}