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
    private readonly byte[] byteBuffer;
    private readonly BitBuffer bitBuffer;

    internal RailInterpreter()
    {
      this.byteBuffer = new byte[RailConfig.DATA_BUFFER_SIZE];
      this.bitBuffer = new BitBuffer();
    }

    internal void SendPacket(IRailNetPeer peer, IRailPacket packet)
    {
      this.bitBuffer.Clear();

      packet.Encode(this.bitBuffer);

      int length = this.bitBuffer.StoreBytes(this.byteBuffer);
      CommonDebug.Assert(length <= RailConfig.MESSAGE_MAX_SIZE);
      peer.EnqueueSend(this.byteBuffer, length);
    }

    internal IEnumerable<BitBuffer> BeginReads(IRailNetPeer peer)
    {
      foreach (int length in peer.ReadReceived(this.byteBuffer))
      {
        this.bitBuffer.ReadBytes(this.byteBuffer, length);
        yield return this.bitBuffer;
      }
    }
  }
}
