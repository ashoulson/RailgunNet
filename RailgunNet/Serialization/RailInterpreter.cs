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

    #region ClientPacket
    internal void SendClientPacket(
      RailPeer destinationPeer,
      RailClientPacket packet)
    {
      this.bitBuffer.Clear();

      // Write: [Packet]
      packet.Encode(this.bitBuffer);

      int length = this.bitBuffer.StoreBytes(this.byteBuffer);
      destinationPeer.EnqueueSend(this.byteBuffer, length);
    }

    internal IEnumerable<RailClientPacket> ReceiveClientPackets(
      RailPeer sourcePeer)
    {
      foreach (int length in sourcePeer.ReadReceived(this.byteBuffer))
      {
        this.bitBuffer.ReadBytes(this.byteBuffer, length);

        // Read: [Packet]
        RailClientPacket result = RailClientPacket.Decode(this.bitBuffer);

        CommonDebug.Assert(this.bitBuffer.Position == 0, "Bad packet read");
        yield return result;
      }
    }
    #endregion

    #region ServerPacket
    internal void SendServerPacket(
      RailPeer destinationPeer,
      RailServerPacket packet)
    {
      this.bitBuffer.Clear();

      // Write: [Packet]
      packet.Encode(this.bitBuffer, destinationPeer);

      int length = this.bitBuffer.StoreBytes(this.byteBuffer);
      destinationPeer.EnqueueSend(this.byteBuffer, length);
    }

    internal IEnumerable<RailServerPacket> ReceiveServerPackets(
      RailPeer sourcePeer,
      IDictionary<EntityId, RailEntity> knownEntities)
    {
      foreach (int length in sourcePeer.ReadReceived(this.byteBuffer))
      {
        this.bitBuffer.ReadBytes(this.byteBuffer, length);

        // Read: [Packet]
        RailServerPacket result = 
          RailServerPacket.Decode(
            this.bitBuffer,
            knownEntities);

        CommonDebug.Assert(this.bitBuffer.Position == 0, "Bad packet read");
        yield return result;
      }
    }
    #endregion
  }
}
