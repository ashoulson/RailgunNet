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
    //public event Action Connected;
    //public event Action Disconnected;

    public int RemoteTick { get { return this.serverClock.RemoteTick; } }

    private RailPeerHost hostPeer;
    private int lastReceived;
    private byte[] dataBuffer;

    private RailClock serverClock;
    private bool shouldUpdateClock = false;
    private bool shouldUpdate = false;

    /// <summary>
    /// A history of inputs sent (or waiting to be sent) to the client.
    /// </summary>
    // TODO: This should probably be just a regular queue
    internal readonly RailRingBuffer<RailPacketC2S> inputBuffer;

    /// <summary>
    /// Entities that are waiting to be added to the world.
    /// </summary>
    private Dictionary<int, RailEntity> pendingEntities;

    // Reusable removal list
    private List<RailEntity> toRemove;

    // TODO: This is clumsy
    private int localTick;

    public RailClient(
      RailCommand commandToRegister, 
      params RailState[] statestoRegister)
      : base(commandToRegister, statestoRegister)
    {
      this.hostPeer = null;
      this.lastReceived = RailClock.INVALID_TICK;
      this.dataBuffer = new byte[RailConfig.DATA_BUFFER_SIZE];

      this.serverClock = new RailClock(RailConfig.NETWORK_SEND_RATE);
      this.shouldUpdate = false;
      this.shouldUpdateClock = false;

      this.localTick = 0;
      this.inputBuffer =
        new RailRingBuffer<RailPacketC2S>(
          RailConfig.DEJITTER_BUFFER_LENGTH);

      this.pendingEntities = new Dictionary<int, RailEntity>();
      this.toRemove = new List<RailEntity>();
    }

    public void SetPeer(IRailNetPeer netPeer)
    {
      this.hostPeer = new RailPeerHost(netPeer);
      this.hostPeer.MessagesReady += this.OnMessagesReady;
    }

    public override void Update()
    {
      if (this.shouldUpdate)
      {
        this.UpdateWorld(this.UpdateClock());

        if (this.hostPeer != null)
        {
          RailPacketC2S input = this.inputBuffer.Get(this.localTick);
          if (input != null)
            this.interpreter.SendInput(this.hostPeer, input);
        }
      }

      this.localTick++;
    }

    public T CreateCommand<T>()
      where T : RailCommand<T>, new()
    {
      return (T)RailResource.Instance.AllocateCommand();
    }

    public void RegisterCommand(RailCommand command)
    {
      RailPacketC2S input = RailResource.Instance.AllocateInput();
      input.Tick = this.localTick;
      input.Command = command;
      this.inputBuffer.Store(input);
    }

    private int UpdateClock()
    {
      if (this.shouldUpdateClock)
        return this.serverClock.Tick(this.lastReceived);
      return this.serverClock.Tick();
    }

    private void UpdateWorld(int numTicks)
    {
      for (; numTicks > 0; numTicks--)
      {
        int serverTick = (this.serverClock.RemoteTick - numTicks) + 1;
        this.UpdatePendingEntities(serverTick);
        this.world.UpdateClient(serverTick);
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

    private void OnMessagesReady(RailPeerHost peer)
    {
      IEnumerable<RailSnapshot> decode =
        this.interpreter.ReceiveSnapshots(
          this.hostPeer, 
          this.snapshotBuffer);

      foreach (RailSnapshot snapshot in decode)
      {
        this.ProcessSnapshot(snapshot);

        // See if we should update the clock with a new received tick
        if (snapshot.Tick > this.lastReceived)
        {
          this.lastReceived = snapshot.Tick;
          this.shouldUpdateClock = true;
          this.shouldUpdate = true;
        }
      }
    }

    internal void ProcessSnapshot(RailSnapshot snapshot)
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
