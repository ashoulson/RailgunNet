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
using System.Linq;

using CommonTools;

namespace Railgun
{
  internal abstract class RailPeer : IRailControllerInternal
  {
    object IRailController.UserData { get; set; }

    /// <summary>
    /// The network I/O peer for sending/receiving data.
    /// </summary>
    private readonly IRailNetPeer netPeer;

    /// <summary>
    /// The entities controlled by this controller.
    /// </summary>
    protected readonly HashSet<RailEntity> controlledEntities;

    /// <summary>
    /// A rolling queue for outgoing reliable events, in order.
    /// </summary>
    private readonly Queue<RailEvent> outgoingGlobalEvents;

    /// <summary>
    /// Used for uniquely identifying and ordering reliable events.
    /// </summary>
    private EventId lastEventId;

    /// <summary>
    /// The last received event id from the remote peer.
    /// </summary>
    private EventId lastReceivedEventId;

    /// <summary>
    /// An estimator for the remote peer's current tick.
    /// </summary>
    private readonly RailClock remoteClock;

    /// <summary>
    /// Interpreter for converting byte input to a BitBuffer.
    /// </summary>
    private readonly RailInterpreter interpreter;

    public IEnumerable<RailEntity> ControlledEntities
    {
      get { return this.controlledEntities; }
    }

    internal RailClock RemoteClock 
    {
      get { return this.remoteClock; } 
    }

    // Server-only
    public virtual RailCommand LatestCommand
    {
      get { throw new NotImplementedException(); }
    }

    // Client-only
    public virtual IEnumerable<RailCommand> PendingCommands
    {
      get { throw new NotImplementedException(); }
    }

    protected abstract RailPacket AllocateIncoming();
    protected abstract RailPacket AllocateOutgoing();

    internal RailPeer(
      IRailNetPeer netPeer,
      RailInterpreter interpreter)
    {
      this.netPeer = netPeer;
      this.netPeer.MessagesReady += this.OnMessagesReady;

      this.controlledEntities = new HashSet<RailEntity>();
      this.outgoingGlobalEvents = new Queue<RailEvent>();

      this.lastEventId = EventId.START;
      this.lastReceivedEventId = EventId.INVALID;
      this.remoteClock = new RailClock();

      this.interpreter = interpreter;
    }

    public void QueueGlobal(RailEvent evnt)
    {
      RailEvent clone = evnt.Clone();
      clone.EventId = this.lastEventId;
      this.outgoingGlobalEvents.Enqueue(clone);
      this.lastEventId = this.lastEventId.Next;
    }

    /// <summary>
    /// Adds an entity to be controlled by this peer.
    /// </summary>
    public void GrantControl(RailEntity entity)
    {
      if (entity.Controller == this)
        return;

      CommonDebug.Assert(entity.Controller == null);
      this.controlledEntities.Add(entity);

      entity.SetController(this);
    }

    /// <summary>
    /// Remove an entity from being controlled by this peer.
    /// </summary>
    public void RevokeControl(RailEntity entity)
    {
      CommonDebug.Assert(entity.Controller == this);
      this.controlledEntities.Remove(entity);

      entity.SetController(null);
    }

    internal virtual int Update()
    {
      return this.remoteClock.Update();
    }

    protected void SendPacket(RailPacket packet)
    {
      this.interpreter.SendPacket(this.netPeer, packet);
    }

    protected void OnMessagesReady(IRailNetPeer peer)
    {
      foreach (BitBuffer buffer in this.interpreter.BeginReads(this.netPeer))
      {
        RailPacket packet = this.AllocateIncoming();
        this.PreparePacketForRead(packet);

        packet.Decode(buffer);

        if (buffer.IsFinished)
          this.ProcessPacket(packet);
        else
          CommonDebug.LogError("Bad packet read, discarding...");
      }
    }

    /// <summary>
    /// For adding pre-read information like an entity reference dictionary.
    /// </summary>
    protected virtual void PreparePacketForRead(RailPacket packet) 
    { 
    }

    /// <summary>
    /// Allocates a packet and writes common boilerplate information to it.
    /// </summary>
    protected T AllocatePacketSend<T>(Tick localTick)
      where T : RailPacket
    {
      RailPacket packet = this.AllocateOutgoing();
      packet.Initialize(
        localTick,
        this.remoteClock.LatestRemote,
        this.lastReceivedEventId,
        this.outgoingGlobalEvents);
      return (T)packet;
    }

    /// <summary>
    /// Records acknowledging information for the packet.
    /// </summary>
    protected virtual void ProcessPacket(RailPacket packet)
    {
      this.remoteClock.UpdateLatest(packet.SenderTick);
      this.RemoveAckedEvents(packet.AckEventId);

      foreach (RailEvent evnt in this.GetNewEvents(packet))
        this.ProcessEvent(evnt);
    }

    /// <summary>
    /// Removes any events from the pending queue that have been acked.
    /// </summary>
    private void RemoveAckedEvents(EventId peerAcked)
    {
      if (peerAcked.IsValid == false)
        return;

      while (this.outgoingGlobalEvents.Count > 0)
      {
        RailEvent next = this.outgoingGlobalEvents.Peek();
        if (next.EventId > peerAcked)
          break;
        RailPool.Free(this.outgoingGlobalEvents.Dequeue());
      }
    }

    /// <summary>
    /// Gets all events that we haven't processed yet, in order.
    /// </summary>
    private IEnumerable<RailEvent> GetNewEvents(RailPacket packet)
    {
      foreach (RailEvent globalEvent in packet.Events)
      {
        bool isExpected =
          (this.lastReceivedEventId.IsValid == false) ||
          (this.lastReceivedEventId.Next == globalEvent.EventId);

        if (isExpected)
        {
          this.lastReceivedEventId = globalEvent.EventId;
          yield return globalEvent;
        }
      }
    }

    private void ProcessEvent(RailEvent evnt)
    {
      // TODO: Move this to a more comprehensive solution
      switch (evnt.EventType)
      {
        default:
          CommonDebug.LogWarning("Unrecognized event: " + evnt.EventType);
          break;
      }
    }
  }
}
