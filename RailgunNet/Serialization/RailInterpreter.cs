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
  internal class RailInterpreter
  {
    private byte[] byteBuffer;
    private BitBuffer bitBuffer;

    internal RailInterpreter()
    {
      this.byteBuffer = new byte[RailConfig.DATA_BUFFER_SIZE];
      this.bitBuffer = new BitBuffer();
    }

    #region Input
    internal void SendInput(
      RailPeerHost peer,
      RailInput input)
    {
      this.bitBuffer.Clear();

      // Write: [Input]
      input.Encode(this.bitBuffer);

      int length = this.bitBuffer.StoreBytes(this.byteBuffer);
      peer.EnqueueSend(this.byteBuffer, length);
    }

    internal IEnumerable<RailInput> ReceiveInputs(
      RailPeerClient peer)
    {
      foreach (int length in peer.ReadReceived(this.byteBuffer))
      {
        this.bitBuffer.ReadBytes(this.byteBuffer, length);

        // Read: [Input]
        RailInput result = RailInput.Decode(this.bitBuffer);

        CommonDebug.Assert(this.bitBuffer.BitsUsed == 0);
        yield return result;
      }
    }
    #endregion

    #region Snapshot
    internal void SendSnapshot(
      RailPeerClient peer,
      RailSnapshot snapshot,
      RailRingBuffer<RailSnapshot> basisBuffer)
    {
      this.bitBuffer.Clear();

      // Write: [Snapshot] (full or delta)
      RailSnapshot basis =
        RailInterpreter.GetBasis(peer.LastAckedTick, basisBuffer);
      if (basis != null)
        snapshot.Encode(this.bitBuffer, basis);
      else
        snapshot.Encode(this.bitBuffer);

      int length = this.bitBuffer.StoreBytes(this.byteBuffer);
      peer.EnqueueSend(this.byteBuffer, length);
    }

    internal IEnumerable<RailSnapshot> ReceiveSnapshots(
      RailPeerHost peer,
      RailRingBuffer<RailSnapshot> basisBuffer)
    {
      foreach (int length in peer.ReadReceived(this.byteBuffer))
      {
        this.bitBuffer.ReadBytes(this.byteBuffer, length);

        // Peek: [Snapshot.BasisTick]
        int basisTick = RailSnapshot.PeekBasisTick(this.bitBuffer);

        // Read: [Snapshot]
        RailSnapshot result = null;
        RailSnapshot basis = RailInterpreter.GetBasis(basisTick, basisBuffer);
        if (basis != null)
          result = RailSnapshot.Decode(this.bitBuffer, basis);
        else if (basisTick == RailClock.INVALID_TICK)
          result = RailSnapshot.Decode(this.bitBuffer);
        else
          CommonDebug.LogWarning("Missing basis for delta snapshot decode");

        CommonDebug.Assert(this.bitBuffer.BitsUsed == 0);
        yield return result;
      }
    }

    private static RailSnapshot GetBasis(
      int basisTick,
      RailRingBuffer<RailSnapshot> basisBuffer)
    {
      RailSnapshot basis = null;
      if (basisTick != RailClock.INVALID_TICK)
        if (basisBuffer.TryGet(basisTick, out basis))
          return basis;
      return null;
    }
    #endregion
  }
}
