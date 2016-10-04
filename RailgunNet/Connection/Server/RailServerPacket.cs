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

using System.Collections.Generic;

namespace Railgun
{
#if CLIENT
  interface IRailServerPacket : IRailPacket
  {
    Tick ServerTick { get; }
    IEnumerable<RailStateDelta> Deltas { get; }
  }
#endif

  /// <summary>
  /// Packet sent from server to client.
  /// </summary>
  internal class RailServerPacket
    : RailPacket 
    , IRailPoolable<RailServerPacket>
#if CLIENT
    , IRailServerPacket
#endif
  {
    #region Pooling
    IRailPool<RailServerPacket> IRailPoolable<RailServerPacket>.Pool { get; set; }
    void IRailPoolable<RailServerPacket>.Reset() { this.Reset(); }
    #endregion

    internal static RailServerPacket Create()
    {
      return RailResource.Instance.CreateServerPacket();
    }

    #region Interface
#if CLIENT
    Tick IRailServerPacket.ServerTick { get { return this.SenderTick; } }
    IEnumerable<RailStateDelta> IRailServerPacket.Deltas { get { return this.deltas.Received; } }
#endif
    #endregion

#if CLIENT
    internal IEnumerable<RailStateDelta> Deltas { get { return this.deltas.Received; } }
#endif
#if SERVER
    internal IEnumerable<RailStateDelta> Sent { get { return this.deltas.Sent; } }
#endif

    private readonly RailPackedListS2C<RailStateDelta> deltas;

    public RailServerPacket() : base()
    {
      this.deltas = new RailPackedListS2C<RailStateDelta>();
    }

    protected override void Reset()
    {
      base.Reset();

      this.deltas.Clear();
    }

#if SERVER
    internal void Populate(
      IEnumerable<RailStateDelta> destroyedDeltas,
      IEnumerable<RailStateDelta> activeDeltas)
    {
      this.deltas.AddPending(destroyedDeltas);
      this.deltas.AddPending(activeDeltas);
    }
#endif

    #region Encode/Decode
    protected override void EncodePayload(
      RailBitBuffer buffer,
      int reservedBytes)
    {
#if SERVER
      // Write: [Deltas]
      this.EncodeDeltas(buffer, reservedBytes);
    }

    private void EncodeDeltas(
      RailBitBuffer buffer, 
      int reservedBytes)
    {
      this.deltas.Encode(
        buffer,
        RailConfig.PACKCAP_MESSAGE_TOTAL - reservedBytes,
        RailConfig.MAXSIZE_ENTITY,
        (delta) => RailState.EncodeDelta(buffer, delta));
#endif
    }

    protected override void DecodePayload(RailBitBuffer buffer)
    {
#if CLIENT
      // Read: [Deltas]
      this.DecodeDeltas(buffer);
    }

    private void DecodeDeltas(RailBitBuffer buffer)
    {
      this.deltas.Decode(
        buffer,
        () => RailState.DecodeDelta(buffer, this.SenderTick));
#endif
    }
    #endregion
  }
}
