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
  public class RailClient : RailConnection
  {
    public int RemoteTick { get { return this.serverClock.RemoteTickEstimated; } }

    private RailPeerServer serverPeer;
    private RailClock serverClock;

    /// <summary>
    /// A history of commands sent (or waiting to be sent) to the client.
    /// </summary>
    internal readonly Queue<RailCommand> commandBuffer;

    /// <summary>
    /// Entities that are waiting to be added to the world.
    /// </summary>
    private Dictionary<int, RailEntity> pendingEntities;

    // Reusable removal list
    private List<RailEntity> toRemove;

    // The local simulation tick, used for commands
    private int localTick;

    public RailClient(
      RailCommand commandToRegister, 
      params RailState[] statesToRegister)
      : base(commandToRegister, statesToRegister)
    {
      this.serverPeer = null;
      this.serverClock = new RailClock();

      this.localTick = 1;
      this.commandBuffer = 
        new Queue<RailCommand>(RailConfig.COMMAND_BUFFER_SIZE + 1);

      this.pendingEntities = new Dictionary<int, RailEntity>();
      this.toRemove = new List<RailEntity>();
    }

    public void SetPeer(IRailNetPeer netPeer)
    {
      this.serverPeer = new RailPeerServer(netPeer);
      this.serverPeer.MessagesReady += this.OnMessagesReady;
    }

    public override void Update()
    {
      this.UpdateWorld(this.serverClock.Tick());
      this.UpdateCommands();
      if ((this.serverPeer != null) && this.ShouldSend(this.localTick))
        this.SendPacket();

      this.localTick++;
    }

    /// <summary>
    /// Packs and sends a client-to-server packet to the server.
    /// </summary>
    private void SendPacket()
    {
      RailClientPacket packet = RailResource.Instance.AllocateClientPacket();
      packet.Initialize(
        this.localTick, 
        this.serverClock.RemoteTickLatest, 
        this.commandBuffer);
      this.interpreter.SendClientPacket(this.serverPeer, packet);
    }

    /// <summary>
    /// Updates the world a number of ticks. If we have entities waiting to be
    /// added, this function will check them and add them if applicable.
    /// </summary>
    private void UpdateWorld(int numTicks)
    {
      for (; numTicks > 0; numTicks--)
      {
        int serverTick = (this.serverClock.RemoteTickEstimated - numTicks) + 1;
        this.UpdatePendingEntities(serverTick);
        this.world.UpdateClient(serverTick);
      }
    }

    /// <summary>
    /// Polls the command generator to produce a command and adds it to the
    /// rolling outgoing command queue. TODO: Local prediction!
    /// </summary>
    private void UpdateCommands()
    {
      RailCommand command = RailResource.Instance.AllocateCommand();

      command.Populate();
      command.Tick = this.localTick;

      this.commandBuffer.Enqueue(command);

      if (this.commandBuffer.Count > RailConfig.COMMAND_BUFFER_SIZE)
      {
        RailCommand oldest = this.commandBuffer.Dequeue();
        RailPool.Free(oldest);
      }
    }

    private void UpdatePendingEntities(int serverTick)
    {
      foreach (RailEntity entity in this.pendingEntities.Values)
      {
        if (entity.CheckDelta(serverTick))
        {
          this.world.AddEntity(entity);
          this.AddRemove(entity);
        }
      }

      this.DoRemove();
    }

    private void AddRemove(RailEntity entity)
    {
      this.toRemove.Add(entity);
    }

    private void DoRemove()
    {
      foreach (RailEntity entity in this.toRemove)
        this.pendingEntities.Remove(entity.Id);
      this.toRemove.Clear();
    }

    private void OnMessagesReady(RailPeerServer peer)
    {
      IEnumerable<RailSnapshot> decode =
        this.interpreter.ReceiveSnapshots(
          this.serverPeer, 
          this.snapshotBuffer);

      foreach (RailSnapshot snapshot in decode)
      {
        this.ProcessSnapshot(snapshot);
        this.serverClock.UpdateLatest(snapshot.Tick);
      }
    }

    private void ProcessSnapshot(RailSnapshot snapshot)
    {
      this.snapshotBuffer.Store(snapshot);
      foreach (RailState state in snapshot.Values)
        this.ProcessState(state);
    }

    private void ProcessState(RailState state)
    {
      RailEntity entity;
      if (this.World.TryGetEntity(state.Id, out entity) == false)
        if (this.pendingEntities.TryGetValue(state.Id, out entity) == false)
          entity = this.ReplicateEntity(state);
      entity.StateBuffer.Store(state.Clone(state.Tick));
    }

    /// <summary>
    /// Creates an entity and adds it to the pending entity list.
    /// </summary>
    private RailEntity ReplicateEntity(RailState state)
    {
      RailEntity entity = state.CreateEntity();
      entity.InitializeClient();
      this.pendingEntities.Add(state.Id, entity);
      return entity;
    }
  }
}
