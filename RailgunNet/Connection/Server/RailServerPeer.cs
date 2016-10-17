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

#if SERVER
using System;
using System.Collections.Generic;

namespace Railgun
{
  /// <summary>
  /// A peer created by the server representing a connected client.
  /// </summary>
  internal class RailServerPeer
    : RailPeer<RailClientPacket, RailServerPacket>
    , IRailControllerServer
  {
    internal event Action<RailServerPeer, IRailClientPacket> PacketReceived;

    public string Identifier { get; set; }

    /// <summary>
    /// Used for setting the scope evaluator heuristics.
    /// </summary>
    public RailScopeEvaluator ScopeEvaluator
    {
      set { this.scope.Evaluator = value; }
    }

#if SERVER
    /// <summary>
    /// Used for up-referencing for performing server functions.
    /// </summary>
    public override IRailControllerServer AsServer
    {
      get { return this; }
    }
#endif

    private readonly RailScope scope;

    internal RailServerPeer(
      IRailNetPeer netPeer,
      RailInterpreter interpreter)
      : base(netPeer, interpreter)
    {
      this.scope = new RailScope();
    }

    internal void Shutdown()
    {
      foreach (RailEntity entity in this.controlledEntities)
        entity.AssignController(null);
      this.controlledEntities.Clear();
    }

    internal void SendPacket(
      Tick localTick,
      IEnumerable<RailEntity> active,
      IEnumerable<RailEntity> destroyed)
    {
      RailServerPacket packet = base.PrepareSend<RailServerPacket>(localTick);
      this.scope.PopulateDeltas(this, localTick, packet, active, destroyed);
      base.SendPacket(packet);

      foreach (RailStateDelta delta in packet.Sent)
        this.scope.RegisterSent(delta.EntityId, localTick, delta.IsFrozen);
    }

    protected override void ProcessPacket(RailPacket packet)
    {
      base.ProcessPacket(packet);

      RailClientPacket clientPacket = (RailClientPacket)packet;
      this.scope.IntegrateAcked(clientPacket.View);
      if (this.PacketReceived != null)
        this.PacketReceived.Invoke(this, clientPacket);
    }
  }
}
#endif