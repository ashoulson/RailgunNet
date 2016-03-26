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

namespace Railgun
{
#if CLIENT
  interface IRailServerPacket : IRailPacket
  {
    Tick ServerTick { get; }
    IEnumerable<RailState.Delta> Deltas { get; }
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
    internal static RailServerPacket Create()
    {
      return RailResource.Instance.CreateServerPacket();
    }

    #region Interface
    IRailPool<RailServerPacket> IRailPoolable<RailServerPacket>.Pool { get; set; }
    void IRailPoolable<RailServerPacket>.Reset() { this.Reset(); }

#if CLIENT
    Tick IRailServerPacket.ServerTick { get { return this.SenderTick; } }
    IEnumerable<RailState.Delta> IRailServerPacket.Deltas { get { return this.deltas; } }
#endif
    #endregion

    internal Tick CommandAck { get; private set; }

#if CLIENT
    internal IEnumerable<RailState.Delta> Deltas { get { return this.deltas; } }
    private readonly List<RailState.Delta> deltas;
#endif

#if SERVER
    internal IEnumerable<RailState.Delta> Sent { get { return this.sent; } }
    internal IEnumerable<RailState.Delta> Pending { get { return this.pending; } }

    // Input/output information for entity scoping
    private readonly List<RailState.Delta> pending;
    private readonly List<RailState.Delta> sent;
#endif

    public RailServerPacket() : base()
    {
      this.CommandAck = Tick.INVALID;

#if CLIENT
      this.deltas = new List<RailState.Delta>();
#endif
#if SERVER
      this.pending = new List<RailState.Delta>();
      this.sent = new List<RailState.Delta>();
#endif
    }

    protected override void Reset()
    {
      base.Reset();

      this.CommandAck = Tick.INVALID;

#if CLIENT
      this.deltas.Clear();
#endif
#if SERVER
      this.pending.Clear();
      this.sent.Clear();
#endif
    }

#if SERVER
    internal void Populate(
      Tick commandAck,
      IEnumerable<RailState.Delta> destroyedDeltas,
      IEnumerable<RailState.Delta> activeDeltas)
    {
      this.CommandAck = commandAck;
      this.pending.AddRange(destroyedDeltas);
      this.pending.AddRange(activeDeltas);
    }
#endif

    #region Encode/Decode
    protected override void EncodePayload(BitBuffer buffer)
    {
#if SERVER
      // Write: [CommandAck]
      buffer.WriteTick(this.CommandAck);

      // Write: [States]
      this.EncodeDeltas(buffer);
    }

    private void EncodeDeltas(BitBuffer buffer)
    {
      buffer.PackToSize(
        RailConfig.MESSAGE_MAX_SIZE,
        RailConfig.ENTITY_MAX_SIZE,
        this.pending,
        (delta) => RailState.EncodeDelta(buffer, delta),
        (delta) => this.sent.Add(delta));
#endif
    }

    protected override void DecodePayload(BitBuffer buffer)
    {
#if CLIENT
      // Read: [CommandAck]
      this.CommandAck = buffer.ReadTick();

      // Read: [States]
      this.DecodeDeltas(buffer);
     }

    private void DecodeDeltas(BitBuffer buffer)
    {
      IEnumerable<RailState.Delta> decoded =
        buffer.UnpackAll(
          () => RailState.DecodeDelta(buffer, this.SenderTick));
      foreach (RailState.Delta delta in decoded)
        this.deltas.Add(delta);
#endif
    }
    #endregion
  }
}
