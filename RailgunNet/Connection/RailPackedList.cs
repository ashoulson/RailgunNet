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
  internal class RailPackedListS2C<T>
    where T : IRailPoolable<T>
  {
#if CLIENT
    internal IEnumerable<T> Received { get { return this.received; } }
    private readonly List<T> received;
#endif
#if SERVER
    internal IEnumerable<T> Pending { get { return this.pending; } }
    internal IEnumerable<T> Sent { get { return this.sent; } }
    private readonly List<T> pending;
    private readonly List<T> sent;
#endif

    public RailPackedListS2C()
    {
#if CLIENT
      this.received = new List<T>();
#endif
#if SERVER
      this.pending = new List<T>();
      this.sent = new List<T>();
#endif
    }

    public void Clear()
    {
#if CLIENT
      // We don't free the received values as they will be passed elsewhere
      this.received.Clear();
#endif
#if SERVER
      this.FreeOutgoing();
      this.pending.Clear();
      this.sent.Clear();
#endif
    }

#if CLIENT
    public void Decode(
      RailBitBuffer buffer, 
      Func<T> decode)
    {
      IEnumerable<T> decoded = buffer.UnpackAll(decode);
      foreach (T delta in decoded)
        this.received.Add(delta);
    }
#endif
#if SERVER
    public void AddPending(T value)
    {
      this.pending.Add(value);
    }

    public void AddPending(IEnumerable<T> values)
    {
      this.pending.AddRange(values);
    }

    public void Encode(
      RailBitBuffer buffer,
      int maxTotalSize,
      int maxIndividualSize,
      Action<T> encode)
    {
      buffer.PackToSize(
        maxTotalSize,
        maxIndividualSize,
        this.pending,
        encode,
        (val) => this.sent.Add(val));
    }

    public void FreeOutgoing()
    {
      foreach (T value in this.pending)
        RailPool.Free(value);
      foreach (T value in this.sent)
        RailPool.Free(value);
    }
#endif
  }

  internal class RailPackedListC2S<T>
    where T : IRailPoolable<T>
  {
#if SERVER
    internal IEnumerable<T> Received { get { return this.received; } }
    private readonly List<T> received;
#endif
#if CLIENT
    internal IEnumerable<T> Pending { get { return this.pending; } }
    internal IEnumerable<T> Sent { get { return this.sent; } }
    private readonly List<T> pending;
    private readonly List<T> sent;
#endif

    public RailPackedListC2S()
    {
#if SERVER
      // We don't free the received values as they will be passed elsewhere
      this.received = new List<T>();
#endif
#if CLIENT
      this.pending = new List<T>();
      this.sent = new List<T>();
#endif
    }

    public void Clear()
    {
#if SERVER
      this.received.Clear();
#endif
#if CLIENT
      this.FreeOutgoing();
      this.pending.Clear();
      this.sent.Clear();
#endif
    }

#if SERVER
    public void Decode(
      RailBitBuffer buffer,
      Func<T> decode)
    {
      IEnumerable<T> decoded = buffer.UnpackAll(decode);
      foreach (T delta in decoded)
        this.received.Add(delta);
    }
#endif
#if CLIENT
    public void AddPending(T value)
    {
      this.pending.Add(value);
    }

    public void AddPending(IEnumerable<T> values)
    {
      this.pending.AddRange(values);
    }

    public void Encode(
      RailBitBuffer buffer,
      int maxTotalSize,
      int maxIndividualSize,
      Action<T> encode)
    {
      buffer.PackToSize(
        maxTotalSize,
        maxIndividualSize,
        this.pending,
        encode,
        (val) => this.sent.Add(val));
    }

    public void FreeOutgoing()
    {
      foreach (T value in this.pending)
        RailPool.Free(value);
      foreach (T value in this.sent)
        RailPool.Free(value);
    }
#endif
  }
}
