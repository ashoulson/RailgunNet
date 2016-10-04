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
  internal class RailServerPeer : 
    RailPeer, IRailControllerServer
  {
    internal event Action<RailServerPeer, IRailClientPacket> PacketReceived;

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

    // The latest entity tick acks from the client
    private readonly RailView ackedView;

    internal RailServerPeer(
      IRailNetPeer netPeer,
      RailInterpreter interpreter)
      : base(netPeer, interpreter)
    {
      this.scope = new RailScope();
      this.ackedView = new RailView();
    }

    internal void Shutdown()
    {
      foreach (RailEntity entity in this.controlledEntities)
        entity.AssignController(null);
      this.controlledEntities.Clear();
    }

    internal void SendPacket(
      Tick localTick,
      IEnumerable<RailEntity> activeEntities,
      IEnumerable<RailEntity> destroyedEntities)
    {
      RailServerPacket packet =
        base.AllocatePacketSend<RailServerPacket>(localTick);

      packet.Populate(
        this.ProduceDeltas(this.FilterDestroyed(destroyedEntities)),
        this.ProduceDeltas(this.FilterActive(localTick, activeEntities)));

      base.SendPacket(packet);

      foreach (RailStateDelta delta in packet.Sent)
        this.scope.RegisterSent(delta.EntityId, localTick);
      RailPool.Free(packet);
    }

    private IEnumerable<RailStateDelta> ProduceDeltas(
      IEnumerable<RailEntity> entities)
    {
      foreach (RailEntity entity in entities)
      {
        RailStateDelta delta =
          entity.ProduceDelta(
            this.ackedView.GetLatest(entity.Id),
            this);
        if (delta != null)
          yield return delta;
      }
    }

    private IEnumerable<RailEntity> FilterDestroyed(
      IEnumerable<RailEntity> destroyedEntities)
    {
      foreach (RailEntity entity in destroyedEntities)
      {
        Tick latest = this.ackedView.GetLatest(entity.Id);
        if (latest.IsValid && (latest < entity.RemovedTick))
          yield return entity;
      }
    }

    private IEnumerable<RailEntity> FilterActive(
      Tick localTick,
      IEnumerable<RailEntity> activeEntities)
    {
      return this.scope.Evaluate(localTick, activeEntities);
    }

    protected override void ProcessPacket(RailPacket packet)
    {
      base.ProcessPacket(packet);
      RailClientPacket clientPacket = (RailClientPacket)packet;

      this.ackedView.Integrate(clientPacket.View);

      if (this.PacketReceived != null)
        this.PacketReceived.Invoke(this, clientPacket);
      RailPool.Free(clientPacket);
    }

    protected override RailPacket AllocateIncoming()
    {
      return RailClientPacket.Create();
    }

    protected override RailPacket AllocateOutgoing()
    {
      return RailServerPacket.Create();
    }
  }
}
#endif