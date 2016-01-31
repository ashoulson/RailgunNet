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

namespace Railgun
{
  public class BitBuffer
  {
    // This class works with both uint-based and byte-based storage,
    // so you can adjust these constants accordingly with data type
    private const int  SIZE_BYTE       = 8;
    private const int  SIZE_INPUT      = sizeof(uint) * SIZE_BYTE;
    private const int  SIZE_STORAGE    = sizeof(uint) * SIZE_BYTE;
    private const int  BYTES_PER_CHUNK = SIZE_STORAGE / SIZE_BYTE;
    private const uint STORAGE_MASK    = (uint)((1L << SIZE_STORAGE) - 1);

    private const int  DEFAULT_CAPACITY = 8;

    private static int ClampBits(int bits)
    {
      if (bits < 0)
        return 0;
      if (bits > BitBuffer.SIZE_INPUT)
        return BitBuffer.SIZE_INPUT;
      return bits;
    }

    /// <summary>
    /// The position of the next-to-be-written bit.
    /// </summary>
    private int position;

    /// <summary>
    /// Buffer of chunks for storing data.
    /// </summary>
    private uint[] chunks;

    /// <summary>
    /// The number of bits currently stored in the buffer.
    /// </summary>
    public int BitsUsed { get { return this.position; } }

    /// <summary>
    /// Capacity is in data chunks: uint = 4 bytes
    /// </summary>
    public BitBuffer(int capacity = BitBuffer.DEFAULT_CAPACITY)
    {
      this.chunks = new uint[capacity];
      this.Clear();
    }

    public BitBuffer(byte[] data)
    {
      this.chunks = new uint[(data.Length / BitBuffer.BYTES_PER_CHUNK) + 1];
      this.ReadBytes(data);
    }

    public void Clear()
    {
      for (int i = 0; i < this.chunks.Length; i++)
        this.chunks[i] = 0;
      this.position = 0;
    }

    /// <summary>
    /// Takes the lower numBits from the value and stores them in the buffer.
    /// </summary>
    public void Push(uint value, int numBits)
    {
      numBits = BitBuffer.ClampBits(numBits);

      while (numBits > 0)
      {
        // Find our place
        int index = this.position / BitBuffer.SIZE_STORAGE;
        int used = this.position % BitBuffer.SIZE_STORAGE;

        // Increase our capacity if needed
        if (index >= this.chunks.Length)
          RailgunUtil.ExpandArray(ref this.chunks);

        // Create and apply the mask
        ulong mask = (1UL << numBits) - 1;
        uint masked = value & (uint)mask;
        uint entry = masked << used;

        // Record how much was written and shift the value
        int remaining = BitBuffer.SIZE_STORAGE - used;
        int written = (numBits < remaining) ? numBits : remaining;
        value >>= written;

        // Store and advance
        this.chunks[index] |= entry;
        this.position += written;
        numBits -= written;
      }
    }

    /// <summary>
    /// Pushes an encodable value.
    /// </summary>
    internal void Push<T>(Encoder<T> encoder, T value)
    {
      uint encoded = encoder.Pack(value);
      this.Push(encoded, encoder.RequiredBits);
    }

    /// <summary>
    /// Pops the top numBits from the buffer and returns them as the lowest
    /// order bits in the return value.
    /// </summary>
    public uint Pop(int numBits)
    {
      numBits = BitBuffer.ClampBits(numBits);
      if (numBits > this.position)
        throw new AccessViolationException("BitBuffer access underrun");

      uint output = 0;
      while (numBits > 0)
      {
        // Find the position of the last written bit
        int lastWritten = this.position - 1;
        int index = lastWritten / BitBuffer.SIZE_STORAGE;
        int used = (lastWritten % BitBuffer.SIZE_STORAGE) + 1;

        // Create the mask and extract the value
        int available = (numBits < used) ? numBits : used;
        // Lower mask cuts out any data lower in the stack
        int ignoreBottom = used - available;
        uint mask = STORAGE_MASK << ignoreBottom;

        // Extract the value and flash the bits out of the data
        uint value = (this.chunks[index] & mask) >> ignoreBottom;
        this.chunks[index] &= ~mask;

        // Update our position
        numBits -= available;
        this.position -= available;

        // Merge the resulting value
        output |= value << numBits;
      }

      return output;
    }

    /// <summary>
    /// Pops a value and decodes it.
    /// </summary>
    internal T Pop<T>(Encoder<T> encoder)
    {
      uint data = this.Pop(encoder.RequiredBits);
      return encoder.Unpack(data);
    }

    /// <summary>
    /// Pops the top numBits from the buffer and returns them as the lowest
    /// order bits in the return value.
    /// </summary>
    public uint Peek(int numBits)
    {
      numBits = BitBuffer.ClampBits(numBits);
      if (numBits > this.position)
        throw new AccessViolationException("BitBuffer access underrun");

      int startingPosition = this.position;
      uint output = 0;
      while (numBits > 0)
      {
        // Find the position of the last written bit
        int lastWritten = this.position - 1;
        int index = lastWritten / BitBuffer.SIZE_STORAGE;

        // Add the +1 here because used is a count, not an index
        int used = (lastWritten % BitBuffer.SIZE_STORAGE) + 1;

        // Create the mask and extract the value
        int available = (numBits < used) ? numBits : used;
        // Lower mask cuts out any data lower in the stack
        int ignoreBottom = used - available;
        uint mask = STORAGE_MASK << ignoreBottom;

        // Extract the value, but don't flash out the data
        uint value = (this.chunks[index] & mask) >> ignoreBottom;

        // Update our position
        numBits -= available;
        this.position -= available;

        // Merge the resulting value
        output |= value << numBits;
      }

      this.position = startingPosition;
      return output;
    }

    /// <summary>
    /// Peeks at a value and decodes it.
    /// </summary>
    internal T Peek<T>(Encoder<T> encoder)
    {
      uint data = this.Peek(encoder.RequiredBits);
      return encoder.Unpack(data);
    }

    /// <summary>
    /// Converts the buffer to a byte array.
    /// </summary>
    public byte[] StoreBytes()
    {
      // Push a sentinel bit for finding position
      this.Push(1, 1);

      // Find the position of the last written bit
      int lastWritten = this.position - 1;
      int numChunks = (lastWritten / BitBuffer.SIZE_STORAGE) + 1;
      int numBytes = (lastWritten / BitBuffer.SIZE_BYTE) + 1;

      byte[] data = new byte[numBytes];
      for (int i = 0; i < numChunks; i++)
      {
        int index = i * BitBuffer.BYTES_PER_CHUNK;
        int bytesRemaining = BitBuffer.GetRemainingBytes(index, numBytes);
        BitBuffer.StoreValue(data, index, bytesRemaining, this.chunks[i]);
      }

      // Remove the sentinel bit from our working copy
      this.Pop(1);

      return data;
    }

    /// <summary>
    /// Overwrites this buffer with an array of byte data.
    /// </summary>
    public void ReadBytes(byte[] data)
    {
      this.Clear();

      int numBytes = data.Length;
      int numChunks = (data.Length / BitBuffer.BYTES_PER_CHUNK) + 1;

      if (this.chunks.Length < numChunks)
        this.chunks = new uint[numChunks];

      for (int i = 0; i < numChunks; i++)
      {
        int index = i * BitBuffer.BYTES_PER_CHUNK;
        int bytesRemaining = BitBuffer.GetRemainingBytes(index, numBytes);
        this.chunks[i] = BitBuffer.ReadValue(data, index, bytesRemaining);
      }

      // Find position and pop the sentinel bit
      this.position = BitBuffer.FindPosition(data);
      this.Pop(1);
    }

    private static int GetRemainingBytes(int index, int numBytes)
    {
      int maxBytes = index + BitBuffer.BYTES_PER_CHUNK;
      int capacity = (maxBytes > numBytes) ? numBytes : maxBytes;
      return capacity - index;
    }

    internal static void StoreValue(
      byte[] array, 
      int index, 
      int numToWrite,
      uint value)
    {
      for (int i = 0; i < numToWrite; i++)
        array[index + i] = (byte)(value >> (BitBuffer.SIZE_BYTE * i));
    }

    internal static uint ReadValue(
      byte[] array, 
      int index, 
      int numToRead)
    {
      uint value = 0;
      for (int i = 0; i < numToRead; i++)
        value |= (uint)array[index + i] << (BitBuffer.SIZE_BYTE * i);
      return value;
    }

    private static int FindPosition(byte[] data)
    {
      if (data.Length == 0)
        return 0;

      byte last = data[data.Length - 1];
      int shiftCount = 0;
      while (last != 0)
      {
        last >>= 1;
        shiftCount++;
      }

      return ((data.Length - 1) * BitBuffer.SIZE_BYTE) + shiftCount;
    }

    #region Conditional Serialization Helpers
    internal void PushIf<T>(
      int flags,
      int requiredFlag,
      Encoder<T> encoder,
      T value)
    {
      if ((flags & requiredFlag) == requiredFlag)
        this.Push(encoder, value);
    }

    internal T PopIf<T>(
      int flags,
      int requiredFlag,
      Encoder<T> encoder,
      T basisVal)
    {
      if ((flags & requiredFlag) == requiredFlag)
        return this.Pop(encoder);
      return basisVal;
    }
    #endregion
  }
}
