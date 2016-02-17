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
using UnityEngine;

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
    internal int BitsUsed { get { return this.position; } }

    /// <summary>
    /// Capacity is in data chunks: uint = 4 bytes
    /// </summary>
    internal BitBuffer(int capacity = BitBuffer.DEFAULT_CAPACITY)
    {
      this.chunks = new uint[capacity];
      this.Clear();
    }

    internal BitBuffer(byte[] data)
    {
      this.chunks = new uint[(data.Length / BitBuffer.BYTES_PER_CHUNK) + 1];
      this.ReadBytes(data);
    }

    internal void Clear()
    {
      for (int i = 0; i < this.chunks.Length; i++)
        this.chunks[i] = 0;
      this.position = 0;
    }

    /// <summary>
    /// Takes the lower numBits from the value and stores them in the buffer.
    /// </summary>
    public void Push(int numBits, uint value)
    {
      numBits = BitBuffer.ClampBits(numBits);

      while (numBits > 0)
      {
        // Find our place
        int index = this.position / BitBuffer.SIZE_STORAGE;
        int used = this.position % BitBuffer.SIZE_STORAGE;

        // Increase our capacity if needed
        if (index >= this.chunks.Length)
          CommonUtil.ExpandArray(ref this.chunks);

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
    public void Push<T>(Encoder<T> encoder, T value)
    {
      uint encoded = encoder.Pack(value);
      this.Push(encoder.RequiredBits, encoded);
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
    public T Pop<T>(Encoder<T> encoder)
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
    public T Peek<T>(Encoder<T> encoder)
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

      // TODO: Pool these byte arrays!
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

    public static void StoreValue(
      byte[] array,
      int index,
      int numToWrite,
      uint value)
    {
      for (int i = 0; i < numToWrite; i++)
        array[index + i] = (byte)(value >> (BitBuffer.SIZE_BYTE * i));
    }

    public static uint ReadValue(
      byte[] array,
      int index,
      int numToRead)
    {
      uint value = 0;
      for (int i = 0; i < numToRead; i++)
        value |= (uint)array[index + i] << (BitBuffer.SIZE_BYTE * i);
      return value;
    }

    private static int GetRemainingBytes(int index, int numBytes)
    {
      int maxBytes = index + BitBuffer.BYTES_PER_CHUNK;
      int capacity = (maxBytes > numBytes) ? numBytes : maxBytes;
      return capacity - index;
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
    public void PushIf<T>(
      int flags,
      int requiredFlag,
      Encoder<T> encoder,
      T value)
    {
      if ((flags & requiredFlag) == requiredFlag)
        this.Push(encoder, value);
    }

    public T PopIf<T>(
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

    #region Debug
#if DEBUG
    public static void Test(int maxValues, int iterations)
    {
      byte[] testBytes = new byte[8];
      uint testVal = 0xB99296AD;
      BitBuffer.StoreValue(testBytes, 4, 3, testVal);
      uint readVal = BitBuffer.ReadValue(testBytes, 4, 3);
      Debug.Assert(readVal == 0x9296AD);

      BitBuffer testByteArray = new BitBuffer();
      testByteArray.Push(32, 0xFFFFFFFF);
      testByteArray.Push(8, 0xFF81);
      byte[] bytes = testByteArray.StoreBytes();
      BitBuffer testReceive = new BitBuffer(bytes);
      uint value1 = testReceive.Pop(8);
      uint value2 = testReceive.Pop(32);
      Debug.Assert(testReceive.BitsUsed == 0);
      Debug.Assert(value1 == 0x81);
      Debug.Assert(value2 == 0xFFFFFFFF);

      BitBuffer buffer = new BitBuffer(1);
      Stack<uint> values = new Stack<uint>(maxValues);
      Stack<int> bits = new Stack<int>(maxValues);

      bool push = true;
      for (int i = 0; i < iterations; i++)
      {
        if (values.Count <= 0)
        {
          push = true; // Must push
        }
        else if (values.Count >= maxValues)
        {
          push = false; // Must pop
        }
        else
        {
          float probability = UnityEngine.Random.Range(0.0f, 1.0f);
          if (probability > 0.95f)
          {
            buffer.Clear();
            values.Clear();
            bits.Clear();
            continue;
          }
          else if (probability > 0.4f)
          {
            push = true;
          }
          else
          {
            push = false;
          }
        }

        if (values.Count > 0)
          Debug.Assert(buffer.Peek(bits.Peek()) == values.Peek());

        if (push)
        {
          uint randVal = 0;
          unchecked
          {
            uint randNum =
              (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            randVal = randNum;
          }
          int randBits = UnityEngine.Random.Range(0, 32);
          uint trimmedVal = randVal & (uint)((1 << randBits) - 1);

          values.Push(trimmedVal);
          bits.Push(randBits);
          buffer.Push(randBits, trimmedVal);
        }
        else
        {
          uint expectedVal = values.Pop();
          int expectedBits = bits.Pop();
          uint retrievedVal = buffer.Pop(expectedBits);

          Debug.Assert(expectedVal == retrievedVal,
            "Expected: " +
            expectedVal +
            " Got: " +
            retrievedVal);
        }
      }
    }
#endif
    #endregion
  }
}
