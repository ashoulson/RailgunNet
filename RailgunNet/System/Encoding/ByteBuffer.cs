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
  /// A last-in-first-out (LIFO) byte encoding buffer.
  /// </summary>
  public class ByteBuffer
  {
    private const int GROW_FACTOR = 2;
    private const int MIN_GROW = 1;
    private const int DEFAULT_CAPACITY = 8;

    private byte[] data;
    private int position;

    public int Position { get { return this.position; } }
    public int Size { get { return this.position; } }
    public bool IsEmpty { get { return this.position == 0; } }

    public ByteBuffer()
    {
      this.data = new byte[DEFAULT_CAPACITY];
      this.position = 0;
    }

    public void Clear()
    {
      this.position = 0;
    }

    public void Rollback(int position)
    {
      this.position = position;
    }

    public int Store(byte[] buffer)
    {
      Array.Copy(this.data, buffer, this.position);
      return this.position;
    }

    public void Load(byte[] buffer, int length)
    {
      this.Clear();
      if (this.data.Length < length)
        this.data = new byte[length];

      Array.Copy(buffer, this.data, length);
      this.position = length;
    }

    #region Encode/Decode
    #region Byte
    public void WriteByte(byte val)
    {
      this.ExpandIfNeeded();
      this.data[this.position] = val;
      this.position++;
    }

    public byte ReadByte()
    {
      byte val = this.data[this.position - 1];
      this.position--;
      return val;
    }

    public byte PeekByte()
    {
      return this.data[this.position - 1];
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
      bool first = true;

      do
      {
        // Take the lowest 7 bits
        buffer = val & 0x7Fu;
        val >>= 7;

        // If there is more data, set the 8th bit to true
        if (first == false)
          buffer |= 0x80u;
        first = false;

        // Store the next byte
        this.WriteByte((byte)buffer);
      }
      while (val > 0);
    }

    public uint ReadUInt()
    {
      uint buffer = 0x0u;
      uint val = 0x0u;

      do
      {
        buffer = this.ReadByte();

        // Add back in the shifted 7 bits
        val <<= 7;
        val |= (buffer & 0x7Fu);

        // Continue if we're flagged for more
      } while ((buffer & 0x80u) > 0);

      return val;
    }

    public uint PeekUInt()
    {
      int tempPosition = this.position;
      uint val = this.ReadUInt();
      this.position = tempPosition;
      return val;
    }
    #endregion

    #region Int
    public void WriteInt(int val)
    {
      this.WriteUInt((uint)val);
    }

    public int ReadInt()
    {
      return (int)this.ReadUInt();
    }

    public int PeekInt()
    {
      return (int)this.PeekUInt();
    }
    #endregion

    #region Bool
    // TODO: Bool packing
    public void WriteBool(bool value)
    {
      this.WriteByte(value ? (byte)1 : (byte)0);
    }

    public bool ReadBool()
    {
      return (this.ReadByte() > 0);
    }

    public bool PeekBool()
    {
      return (this.PeekByte() > 0);
    }
    #endregion
    #endregion

    private void ExpandIfNeeded()
    {
      if (this.position >= this.data.Length)
      {
        int newCapacity =
          (this.data.Length * ByteBuffer.GROW_FACTOR) +
          ByteBuffer.MIN_GROW;

        byte[] newData = new byte[newCapacity];
        Array.Copy(this.data, newData, this.data.Length);
        this.data = newData;
      }
    }
  }
}
