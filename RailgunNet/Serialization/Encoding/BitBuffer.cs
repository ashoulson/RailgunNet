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

namespace Railgun
{
  public class BitBuffer
  {
    // This class works with both uint-based and byte-based storage,
    // so you can adjust these constants accordingly with data type
    private const int SIZE_BYTE = 8;
    private const int SIZE_INPUT = sizeof(uint) * SIZE_BYTE;
    private const int SIZE_STORAGE = sizeof(uint) * SIZE_BYTE;
    private const int BYTES_PER_CHUNK = SIZE_STORAGE / SIZE_BYTE;
    private const uint STORAGE_MASK = (uint)((1L << SIZE_STORAGE) - 1);

    private const int GROW_FACTOR = 2;
    private const int MIN_GROW = 1;
    private const int DEFAULT_CAPACITY = 8;

    /// <summary>
    /// The position of the next-to-be-written bit.
    /// </summary>
    private int position;

    /// <summary>
    /// A stored potential rollback position.
    /// </summary>
    private int rollback;

    /// <summary>
    /// Buffer of chunks for storing data.
    /// </summary>
    private uint[] chunks;

    /// <summary>
    /// The number of bits currently stored in the buffer.
    /// </summary>
    public int Position { get { return this.position; } }

    /// <summary>
    /// Size the buffer will require in bytes.
    /// </summary>
    public int ByteSize { get { return (this.position / SIZE_BYTE) + 1; } }

    /// <summary>
    /// Capacity is in data chunks: uint = 4 bytes
    /// </summary>
    public BitBuffer(int capacity = BitBuffer.DEFAULT_CAPACITY)
    {
      this.chunks = new uint[capacity];
      this.Clear();
    }

    /// <summary>
    /// Clears the buffer and overwrites all stored bits to zero.
    /// </summary>
    public void Clear()
    {
      for (int i = 0; i < this.chunks.Length; i++)
        this.chunks[i] = 0;
      this.position = 0;
      this.rollback = 0;
    }

    /// <summary>
    /// Sets a rollback point for later.
    /// </summary>
    public void SetRollback()
    {
      this.rollback = this.position;
    }

    /// <summary>
    /// Returns the buffer to a previous point and clears out all data
    /// stored since that point.
    /// </summary>
    public void Rollback()
    {
      if (this.rollback > this.position)
        throw new InvalidOperationException();

      int rollbackWritten = this.rollback - 1;
      int goalIndex = rollbackWritten / BitBuffer.SIZE_STORAGE;
      int bitsRemaining = this.position - this.rollback;

      while (bitsRemaining > 0)
      {
        // Find our place
        int lastWritten = this.position - 1;
        int writtenIndex = lastWritten / BitBuffer.SIZE_STORAGE;
        int writtenUsed = (lastWritten % BitBuffer.SIZE_STORAGE) + 1;

        if (writtenIndex > goalIndex)
        {
          this.chunks[writtenIndex] = 0;
          this.position -= writtenUsed;
        }
        else
        {
          int bitsToSave = writtenUsed - bitsRemaining;
          ulong mask = (1UL << bitsToSave) - 1;
          this.chunks[writtenIndex] &= (uint)mask;
          this.position = this.rollback;
        }

        bitsRemaining = this.position - this.rollback;
      }
    }

    /// <summary>
    /// Takes the lower numBits from the value and stores them in the buffer.
    /// </summary>
    public void Push(int numBits, uint value)
    {
      if (numBits < 0)
        throw new ArgumentOutOfRangeException("Pushing negatve bits");
      if (numBits > BitBuffer.SIZE_INPUT)
        throw new ArgumentOutOfRangeException("Pushing too many bits");

      while (numBits > 0)
      {
        // Find our place
        int index = this.position / BitBuffer.SIZE_STORAGE;
        int used = this.position % BitBuffer.SIZE_STORAGE;

        // Increase our capacity if needed
        if (index >= this.chunks.Length)
          this.ExpandArray();

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
    /// Pops the top numBits from the buffer and returns them as the lowest
    /// order bits in the return value.
    /// </summary>
    public uint Pop(int numBits)
    {
      if (numBits < 0)
        throw new ArgumentOutOfRangeException("Popping negatve bits");
      if (numBits > BitBuffer.SIZE_INPUT)
        throw new ArgumentOutOfRangeException("Popping too many bits");
      if (numBits > this.position)
        throw new AccessViolationException("BitBuffer pop underrun");

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
    /// Pops the top numBits from the buffer and returns them as the lowest
    /// order bits in the return value.
    /// </summary>
    public uint Peek(int numBits)
    {
      if (numBits < 0)
        throw new ArgumentOutOfRangeException("Peeking negatve bits");
      if (numBits > BitBuffer.SIZE_INPUT)
        throw new ArgumentOutOfRangeException("Peeking too many bits");
      if (numBits > this.position)
        throw new AccessViolationException("BitBuffer peek underrun");

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
    /// Converts the buffer to a byte array.
    /// </summary>
    public int StoreBytes(byte[] buffer)
    {
      // Push a sentinel bit for finding position
      this.Push(1, 1);

      // Find the position of the last written bit
      int lastWritten = this.position - 1;
      int numChunks = (lastWritten / BitBuffer.SIZE_STORAGE) + 1;
      int numBytes = (lastWritten / BitBuffer.SIZE_BYTE) + 1;

      if (buffer.Length < numBytes)
        throw new ArgumentException("Buffer too small for StoreBytes");

      int numWritten = 0;
      for (int i = 0; i < numChunks; i++)
      {
        int index = i * BitBuffer.BYTES_PER_CHUNK;
        int bytesRemaining = BitBuffer.GetBytesForChunk(index, numBytes);
        BitBuffer.StoreValue(buffer, index, bytesRemaining, this.chunks[i]);
        numWritten += bytesRemaining;
      }

      // Remove the sentinel bit from our working copy
      this.Pop(1);

      return numWritten;
    }

    /// <summary>
    /// Overwrites this buffer with an array of byte data.
    /// </summary>
    public void ReadBytes(byte[] data, int length)
    {
      this.Clear();

      int numBytes = length;
      int numChunks = (length / BitBuffer.BYTES_PER_CHUNK) + 1;

      if (this.chunks.Length < numChunks)
        this.chunks = new uint[numChunks];

      for (int i = 0; i < numChunks; i++)
      {
        int index = i * BitBuffer.BYTES_PER_CHUNK;
        int bytesForChunk = BitBuffer.GetBytesForChunk(index, numBytes);
        this.chunks[i] = BitBuffer.ReadValue(data, index, bytesForChunk);
      }

      // Find position and pop the sentinel bit
      this.position = BitBuffer.FindPosition(data, length);
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

    private static int GetBytesForChunk(int index, int numBytes)
    {
      int maxBytes = index + BitBuffer.BYTES_PER_CHUNK;
      int capacity = (maxBytes > numBytes) ? numBytes : maxBytes;
      return capacity - index;
    }

    private static int FindPosition(byte[] data, int length)
    {
      if (length == 0)
        return 0;

      byte last = data[length - 1];
      int shiftCount = 0;
      while (last != 0)
      {
        last >>= 1;
        shiftCount++;
      }

      return ((length - 1) * BitBuffer.SIZE_BYTE) + shiftCount;
    }

    private void ExpandArray()
    {
      int newCapacity = 
        (this.chunks.Length * BitBuffer.GROW_FACTOR) + 
        BitBuffer.MIN_GROW;

      uint[] newChunks = new uint[newCapacity];
      Array.Copy(this.chunks, newChunks, this.chunks.Length);
      this.chunks = newChunks;
    }

    #region Encoder Helpers
    /// <summary>
    /// Pushes an encodable value.
    /// </summary>
    public void Push<T>(Encoder<T> encoder, T value)
    {
      encoder.Write(this, value);
    }

    /// <summary>
    /// Pops a value and decodes it.
    /// </summary>
    public T Pop<T>(Encoder<T> encoder)
    {
      return encoder.Read(this);
    }

    /// <summary>
    /// Peeks at a value and decodes it.
    /// </summary>
    public T Peek<T>(Encoder<T> encoder)
    {
      return encoder.Peek(this);
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
    #endregion
  }
}
