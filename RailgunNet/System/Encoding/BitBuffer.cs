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
using System.Text;

using CommonTools;

namespace Railgun
{
  /// <summary>
  /// A first-in-first-out (FIFO) bit encoding buffer.
  /// </summary>
  public class BitBuffer
  {
    private const int SIZE_BYTE = 8;
    private const int SIZE_INPUT = sizeof(uint) * SIZE_BYTE;
    private const int SIZE_STORAGE = sizeof(uint) * SIZE_BYTE;
    private const int BYTES_PER_CHUNK = SIZE_STORAGE / SIZE_BYTE;
    private const ulong STORAGE_MASK = (1L << SIZE_STORAGE) - 1;

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
    private int writePos;

    /// <summary>
    /// Buffer of chunks for storing data.
    /// </summary>
    private uint[] chunks;

    /// <summary>
    /// Size the buffer will require in bytes.
    /// </summary>
    public int ByteSize { get { return (this.writePos / SIZE_BYTE) + 1; } }

    /// <summary>
    /// Returns true iff we have read everything off of the buffer.
    /// </summary>
    public bool IsFinished { get { return (this.writePos == this.readPos); } }

    /// <summary>
    /// Collection of saved points for bookmarks or reserved writes.
    /// </summary>
    private readonly Dictionary<int, int> bookmarks;

    /// <summary>
    /// Capacity is in data chunks: uint = 4 bytes
    /// </summary>
    public BitBuffer(int capacity = BitBuffer.DEFAULT_CAPACITY)
    {
      this.chunks = new uint[capacity];
      this.bookmarks = new Dictionary<int, int>();

      this.Clear();
    }

    /// <summary>
    /// Clears the buffer and overwrites all stored bits to zero.
    /// </summary>
    public void Clear()
    {
      for (int i = 0; i < this.chunks.Length; i++)
        this.chunks[i] = 0;

      this.readPos = 0;
      this.writePos = 0;

      this.bookmarks.Clear();
    }

    /// <summary>
    /// Sets a rollback point for later.
    /// </summary>
    public void ClearBookmark(int key)
    {
      this.bookmarks.Remove(key);
    }

    /// <summary>
    /// Checks to see if the buffer has a bookmark for a given key.
    /// </summary>
    public bool IsAvailable(int key)
    {
      return (this.bookmarks.ContainsKey(key) == false);
    }

    /// <summary>
    /// Sets a rollback point for later.
    /// </summary>
    public void SetRollback(int key)
    {
      this.bookmarks[key] = this.writePos;
    }

    /// <summary>
    /// Reserves some room for writing later.
    /// </summary>
    public void SetReserved(int key, int numBits)
    {
      this.bookmarks[key] = this.writePos;
      this.writePos += numBits;
    }

    /// <summary>
    /// Returns the buffer to a previous point and clears out all data
    /// stored since that point.
    /// </summary>
    public void Rollback(int key)
    {
      int rollbackPos = this.bookmarks[key];
      if (rollbackPos > this.writePos)
        throw new InvalidOperationException();

      int rollbackWritten = rollbackPos - 1;
      int goalIndex = rollbackWritten / BitBuffer.SIZE_STORAGE;
      int bitsRemaining = this.writePos - rollbackPos;

      while (bitsRemaining > 0)
      {
        // Find our place
        int lastWritten = this.writePos - 1;
        int index = lastWritten / BitBuffer.SIZE_STORAGE;
        int used = (lastWritten % BitBuffer.SIZE_STORAGE) + 1;

        if (index > goalIndex)
        {
          this.chunks[index] = 0;
          this.writePos -= used;
        }
        else
        {
          int bitsToSave = used - bitsRemaining;
          ulong mask = (1UL << bitsToSave) - 1;
          this.chunks[index] &= (uint)mask;
          this.writePos = rollbackPos;
        }

        bitsRemaining = this.writePos - rollbackPos;
      }

      this.bookmarks.Remove(key);
    }

    /// <summary>
    /// Takes the lower numBits from the value and stores them in the buffer.
    /// </summary>
    public void Write(int numBits, uint value)
    {
      this.Write(numBits, value, this.writePos);
      this.writePos += numBits;
    }

    /// <summary>
    /// Takes the lower numBits from the value and stores them in the buffer.
    /// </summary>
    public void WriteReserved(int key, int numBits, uint value)
    {
      int reservedPos = this.bookmarks[key];
      this.Write(numBits, value, reservedPos);
      this.bookmarks.Remove(key);
    }

    /// <summary>
    /// Returns the next number of bits from the beginning of the buffer.
    /// </summary>
    public uint Read(int numBits)
    {
      if (numBits < 0)
        throw new ArgumentOutOfRangeException("Reading negatve bits");
      if (numBits > BitBuffer.SIZE_INPUT)
        throw new ArgumentOutOfRangeException("Reading too many bits");
      if ((numBits + this.readPos) > this.writePos)
        throw new AccessViolationException("BitBuffer read underrun");

      uint output = 0;
      int bitsConsumed = 0;
      while (numBits > 0)
      {
        // Find the position of the last read bit
        int index = this.readPos / BitBuffer.SIZE_STORAGE;
        int read = this.readPos % BitBuffer.SIZE_STORAGE;
        int remaining = BitBuffer.SIZE_STORAGE - read;

        // Create the mask and extract the value
        int available = (numBits < remaining) ? numBits : remaining;

        // Lower mask cuts out any data lower in the chunk
        int ignoreTop = BitBuffer.SIZE_STORAGE - (remaining - available);
        uint mask = (uint)(STORAGE_MASK << ignoreTop);

        // Extract the value
        uint value = (this.chunks[index] & ~mask) >> read;

        // Merge the resulting value
        output |= value << bitsConsumed;

        // Update our position and tracking
        numBits -= available;
        bitsConsumed += available;
        this.readPos += available;
      }

      return output;
    }

    /// <summary>
    /// Reads the top numBits from the buffer and returns them.
    /// </summary>
    public uint Peek(int numBits)
    {
      int currentReadPos = this.readPos;
      uint output = this.Read(numBits);
      this.readPos = currentReadPos;
      return output;
    }

    /// <summary>
    /// Converts the buffer to a byte array.
    /// </summary>
    public int StoreBytes(byte[] buffer)
    {
      // Push a sentinel bit for finding position
      this.PushSentinel();

      // Find the position of the last written bit
      int lastWritten = this.writePos - 1;
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
      this.PopSentinel();

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
      this.writePos = BitBuffer.FindPosition(data, length);
      this.readPos = 0;
      this.PopSentinel();
    }

    /// <summary>
    /// Takes the lower numBits from the value and stores them in the buffer.
    /// </summary>
    private void Write(int numBits, uint value, int position)
    {
      if (numBits < 0)
        throw new ArgumentOutOfRangeException("Pushing negatve bits");
      if (numBits > BitBuffer.SIZE_INPUT)
        throw new ArgumentOutOfRangeException("Pushing too many bits");

      while (numBits > 0)
      {
        // Find our place
        int index = position / BitBuffer.SIZE_STORAGE;
        int used = position % BitBuffer.SIZE_STORAGE;

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
        position += written;
        numBits -= written;
      }
    }

    /// <summary>
    /// Adds a sentinel bit for finding position.
    /// </summary>
    private void PushSentinel()
    {
      // Find our place
      int index = this.writePos / BitBuffer.SIZE_STORAGE;
      int used = this.writePos % BitBuffer.SIZE_STORAGE;

      // Increase our capacity if needed
      if (index >= this.chunks.Length)
        this.ExpandArray();

      this.chunks[index] |= (uint)(1 << used);
      this.writePos++;
    }

    /// <summary>
    /// Adds a sentinel bit for finding position.
    /// </summary>
    private void PopSentinel()
    {
      // Find our place
      int lastWritten = this.writePos - 1;
      int index = lastWritten / BitBuffer.SIZE_STORAGE;
      int used = lastWritten % BitBuffer.SIZE_STORAGE;

      this.chunks[index] &= ~(uint)(1 << used);
      this.writePos--;
    }

    private static void StoreValue(
      byte[] array,
      int index,
      int numToWrite,
      uint value)
    {
      for (int i = 0; i < numToWrite; i++)
        array[index + i] = (byte)(value >> (BitBuffer.SIZE_BYTE * i));
    }

    private static uint ReadValue(
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
    public void Write<T>(Encoder<T> encoder, T value)
    {
      encoder.Write(this, value);
    }

    /// <summary>
    /// Reads a value and decodes it.
    /// </summary>
    public T Read<T>(Encoder<T> encoder)
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

    /// <summary>
    /// Reserves a number of bits in the buffer.
    /// </summary>
    public void Reserve<T>(int key, Encoder<T> encoder)
    {
      encoder.Reserve(key, this);
    }

    /// <summary>
    /// Writes to a previously reserved space.
    /// </summary>
    public void WriteReserved<T>(int key, Encoder<T> encoder, T value)
    {
      encoder.WriteReserved(key, this, value);
    }

    #region Conditional Serialization Helpers
    public void WriteIf<T>(
      uint flags,
      uint requiredFlag,
      Encoder<T> encoder,
      T value)
    {
      if ((flags & requiredFlag) == requiredFlag)
        this.Write(encoder, value);
    }

    public void ReadIf<T>(
      uint flags,
      uint requiredFlag,
      Encoder<T> encoder,
      ref T destination)
    {
      if ((flags & requiredFlag) == requiredFlag)
        destination = this.Read(encoder);
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
