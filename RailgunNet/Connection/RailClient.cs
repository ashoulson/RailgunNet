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
    private int lastApplied;
    private byte[] dataBuffer;

    private RailClock serverClock;
    private bool shouldUpdateClock = false;
    private bool shouldUpdate = false;


    /// <summary>
    /// A history of inputs sent (or waiting to be sent) to the client.
    /// </summary>
    // TODO: This should probably be just a regular queue
    internal readonly RailRingBuffer<RailInput> inputBuffer;

    // TODO: This is clumsy
    private int localTick;

    public RailClient(
      RailCommand commandToRegister, 
      params RailState[] statestoRegister)
      : base(commandToRegister, statestoRegister)
    {
      this.hostPeer = null;
      this.lastReceived = RailClock.INVALID_TICK;
      this.lastApplied = RailClock.INVALID_TICK;
      this.dataBuffer = new byte[RailConfig.DATA_BUFFER_SIZE];

      this.serverClock = new RailClock(RailConfig.NETWORK_SEND_RATE);
      this.shouldUpdate = false;
      this.shouldUpdateClock = false;

      this.localTick = 0;
      this.inputBuffer =
        new RailRingBuffer<RailInput>(
          RailConfig.DEJITTER_BUFFER_LENGTH);
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
        if (this.shouldUpdateClock)
          this.serverClock.Tick(this.lastReceived);
        else
          this.serverClock.Tick();

        this.SelectAndApplySnapshot();

        if (this.hostPeer != null)
        {
          RailInput input = this.inputBuffer.Get(this.localTick);
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
      RailInput input = RailResource.Instance.AllocateInput();
      input.Tick = this.localTick;
      input.Command = command;
      this.inputBuffer.Store(input);
    }

    private void SelectAndApplySnapshot()
    {
      RailSnapshot snapshot =
        this.snapshotBuffer.GetOrFirstBefore(
          this.serverClock.RemoteTick);

      if ((snapshot != null) && (snapshot.Tick > this.lastApplied))
      {
        this.world.ApplySnapshot(snapshot);
        this.lastApplied = snapshot.Tick;
      }
    }

    private void OnMessagesReady(RailPeerHost peer)
    {
      IEnumerable<RailSnapshot> decode =
        this.interpreter.ReceiveSnapshots(
          this.hostPeer, 
          this.snapshotBuffer);

      foreach (RailSnapshot snapshot in decode)
      {
        this.snapshotBuffer.Store(snapshot);

        // See if we should update the clock with a new received tick
        if (snapshot.Tick > this.lastReceived)
        {
          this.lastReceived = snapshot.Tick;
          this.shouldUpdateClock = true;
          this.shouldUpdate = true;
        }
      }
    }
  }
}
