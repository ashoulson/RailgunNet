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
  /// <summary>
  /// A peer created by the server representing a connected client.
  /// </summary>
  internal class RailServerPeer : 
    RailPeer, IRailControllerServer, IRailControllerInternal
  {
    internal event Action<IRailClientPacket> PacketReceived;

    /// <summary>
    /// The latest usable command in the dejitter buffer.
    /// </summary>
    public override RailCommand LatestCommand
    {
      get { return this.latestCommand; }
    }

    /// <summary>
    /// Used for setting the scope evaluator heuristics.
    /// </summary>
    public RailScopeEvaluator ScopeEvaluator
    {
      set { this.scope.Evaluator = value; }
    }

    private readonly RailDejitterBuffer<RailCommand> commandBuffer;
    private readonly RailScope scope;

    // The latest entity tick acks from the client
    private readonly RailView ackedView;

    // The latest command received from the client
    private RailCommand latestCommand;

    internal RailServerPeer(
      IRailNetPeer netPeer,
      RailInterpreter interpreter)
      : base(netPeer, interpreter)
    {
      // We use no divisor for storing commands because commands are sent in
      // batches that we can use to fill in the holes between send ticks
      this.commandBuffer =
        new RailDejitterBuffer<RailCommand>(
          RailConfig.DEJITTER_BUFFER_LENGTH);
      this.scope = new RailScope();

      this.ackedView = new RailView();
      this.latestCommand = null;
    }

    internal override int Update(Tick localTick)
    {
      int ticks = base.Update(localTick);

      this.latestCommand =
        this.commandBuffer.GetLatestAt(
          this.RemoteClock.EstimatedRemote);
      return ticks;
    }

    internal void Shutdown()
    {
      foreach (RailEntity entity in this.controlledEntities)
        entity.AssignController(null);
      this.controlledEntities.Clear();
    }

    internal void SendPacket(
      IEnumerable<RailEntity> activeEntities,
      IEnumerable<RailEntity> destroyedEntities)
    {
      RailServerPacket packet =
        base.AllocatePacketSend<RailServerPacket>(this.LocalTick);

      Tick commandAck = Tick.INVALID;
      if (this.latestCommand != null)
        commandAck = this.latestCommand.Tick;
      packet.Populate(commandAck, this.ProduceDeltas(activeEntities));

      base.SendPacket(packet);

      foreach (RailState.Delta delta in packet.Sent)
        this.scope.RegisterSent(delta.EntityId, this.LocalTick);

      foreach (RailState.Delta delta in packet.Pending)
        RailPool.Free(delta);
      RailPool.Free(packet);
    }

    private IEnumerable<RailState.Delta> ProduceDeltas(
      IEnumerable<RailEntity> activeEntities)
    {
      IEnumerable<RailEntity> scopedEntities =
        this.scope.Evaluate(activeEntities, this.LocalTick);
      foreach (RailEntity entity in scopedEntities)
        yield return entity.ProduceDelta(
          this.ackedView.GetLatest(entity.Id), 
          this);
    }

    protected override void ProcessPacket(RailPacket packet)
    {
      base.ProcessPacket(packet);

      RailClientPacket clientPacket = (RailClientPacket)packet;

      this.ackedView.Integrate(clientPacket.View);
      foreach (RailCommand command in clientPacket.Commands)
        this.commandBuffer.Store(command);

      if (this.PacketReceived != null)
        this.PacketReceived.Invoke(clientPacket);
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
