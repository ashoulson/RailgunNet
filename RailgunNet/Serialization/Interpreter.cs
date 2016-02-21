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
  /// <summary>
  /// Responsible for encoding and decoding packet information.
  /// </summary>
  internal class Interpreter
  {
    private byte[] byteBuffer;
    private BitBuffer bitBuffer;

    internal Interpreter()
    {
      this.byteBuffer = new byte[RailConfig.DATA_BUFFER_SIZE];
      this.bitBuffer = new BitBuffer();
    }

    internal void EncodeSendSnapshot(
      RailPeer peer,
      RailSnapshot snapshot)
    {
      this.bitBuffer.Clear();

      // Write: [Snapshot]
      snapshot.Encode(this.bitBuffer);

      int length = this.bitBuffer.StoreBytes(this.byteBuffer);
      peer.EnqueueSend(this.byteBuffer, length);
    }

    internal void EncodeSendSnapshot(
      RailPeer peer,
      RailSnapshot snapshot, 
      RailSnapshot basis)
    {
      this.bitBuffer.Clear();
      
      // Write: [Snapshot]
      snapshot.Encode(this.bitBuffer, basis);

      int length = this.bitBuffer.StoreBytes(this.byteBuffer);
      peer.EnqueueSend(this.byteBuffer, length);
    }

    internal IEnumerable<RailSnapshot> DecodeReceivedSnapshots(
      RailPeer peer,
      RingBuffer<RailSnapshot> basisBuffer)
    {
      foreach (int length in peer.ReadReceived(this.byteBuffer))
      {
        RailSnapshot snapshot = 
          this.DecodeBufferSnapshot(length, basisBuffer);
        if (snapshot != null)
          yield return snapshot;
      }
    }

    private RailSnapshot DecodeBufferSnapshot(
      int length,
      RingBuffer<RailSnapshot> basisBuffer)
    {
      this.bitBuffer.ReadBytes(this.byteBuffer, length);

      // Peek: [Snapshot.BasisTick]
      int basisTick = RailSnapshot.PeekBasisTick(this.bitBuffer);

      // Read: [Snapshot]
      RailSnapshot result = null;
      if (basisTick != RailClock.INVALID_TICK)
      {
        // There's a slim chance the basis could have been overwritten
        // if packets arrived out of order, in which case we can't decode
        RailSnapshot basis = basisBuffer.Get(basisTick); 
        if (basis != null)
          result = RailSnapshot.Decode(this.bitBuffer, basis);
      }
      else
      {
        result = RailSnapshot.Decode(this.bitBuffer);
      }

      CommonDebug.Assert(this.bitBuffer.BitsUsed == 0);
      return result;
    }
  }
}
