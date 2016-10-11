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
using System.Collections.Generic;

namespace Railgun
{
  internal delegate void EventReceived(RailEvent evnt, RailPeer sender);

  internal abstract class RailPeer : IRailController
  {
    internal event EventReceived EventReceived;

    object IRailController.UserData { get; set; }

#if SERVER
    public virtual IRailControllerServer AsServer
    {
      get { throw new InvalidOperationException(); }
    }
#endif

    public IEnumerable<RailEntity> ControlledEntities
    {
      get { return this.controlledEntities; }
    }

    public Tick EstimatedRemoteTick
    {
      get { return this.remoteClock.EstimatedRemote; }
    }

    internal RailClock RemoteClock
    {
      get { return this.remoteClock; }
    }

    /// <summary>
    /// The network I/O peer for sending/receiving data.
    /// </summary>
    private readonly IRailNetPeer netPeer;

    /// <summary>
    /// The entities controlled by this controller.
    /// </summary>
    protected readonly HashSet<RailEntity> controlledEntities;

    /// <summary>
    /// An estimator for the remote peer's current tick.
    /// </summary>
    private readonly RailClock remoteClock;

    /// <summary>
    /// Interpreter for converting byte input to a BitBuffer.
    /// </summary>
    private readonly RailInterpreter interpreter;

    #region Event-Related
    /// <summary>
    /// Used for uniquely identifying outgoing events.
    /// </summary>
    private SequenceId lastQueuedEventId;

    /// <summary>
    /// A rolling queue for outgoing reliable events, in order.
    /// </summary>
    private readonly Queue<RailEvent> outgoingEvents;

    /// <summary>
    /// A history buffer of received unreliable events.
    /// </summary>
    private SequenceWindow processedEventHistory;
    #endregion

    protected abstract RailPacket AllocateIncoming();
    protected abstract RailPacket AllocateOutgoing();

    internal RailPeer(
      IRailNetPeer netPeer,
      RailInterpreter interpreter)
    {
      this.netPeer = netPeer;
      this.interpreter = interpreter;

      this.controlledEntities = new HashSet<RailEntity>();
      this.remoteClock = new RailClock();

      this.outgoingEvents = new Queue<RailEvent>();
      this.lastQueuedEventId = SequenceId.START.Next;
      this.processedEventHistory = new SequenceWindow(SequenceId.START);

      this.netPeer.PayloadReceived += this.OnPayloadReceived;
    }

    /// <summary>
    /// Adds an entity to be controlled by this peer.
    /// </summary>
    public virtual void GrantControl(RailEntity entity)
    {
      if (entity.Controller == this)
        return;

      RailDebug.Assert(entity.Controller == null);
      this.controlledEntities.Add(entity);

      entity.AssignController(this);
    }

    /// <summary>
    /// Remove an entity from being controlled by this peer.
    /// </summary>
    public virtual void RevokeControl(RailEntity entity)
    {
      RailDebug.Assert(entity.Controller == this);
      this.controlledEntities.Remove(entity);

      entity.AssignController(null);
    }

    /// <summary>
    /// Returns the number of frames we should simulate to sync up with
    /// the predicted remote peer clock, if any.
    /// </summary>
    internal virtual void Update()
    {
      this.remoteClock.Update();
    }

    protected void SendPacket(RailPacket packet)
    {
      this.interpreter.SendPacket(this.netPeer, packet);
    }

    protected void OnPayloadReceived(IRailNetPeer peer, byte[] buffer, int length)
    {
      RailBitBuffer bitBuffer = this.interpreter.LoadData(buffer, length);
      RailPacket packet = this.AllocateIncoming();

      packet.Decode(bitBuffer);
      if (bitBuffer.IsFinished)
        this.ProcessPacket(packet);
      else
        RailDebug.LogError("Bad packet read, discarding...");
      // TODO: Free packet?
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
        this.processedEventHistory.Latest,
        this.FilterOutgoingEvents());
      return (T)packet;
    }

    /// <summary>
    /// Records acknowledging information for the packet.
    /// </summary>
    protected virtual void ProcessPacket(RailPacket packet)
    {
      this.remoteClock.UpdateLatest(packet.SenderTick);
      foreach (RailEvent evnt in this.FilterIncomingEvents(packet.Events))
        this.ProcessEvent(evnt);
      this.CleanOutgoingEvents(packet.AckEventId);
    }

    #region Events
    /// <summary>
    /// Queues an event to send directly to this peer.
    /// </summary>
    public void QueueEvent(RailEvent evnt, int attempts)
    {
      // TODO: SCOPING

      // All global events are sent reliably
      RailEvent clone = evnt.Clone();

      clone.EventId = this.lastQueuedEventId;
      clone.Attempts = attempts;

      this.outgoingEvents.Enqueue(clone);
      this.lastQueuedEventId = this.lastQueuedEventId.Next;
    }

    /// <summary>
    /// Removes any acked or expired outgoing events.
    /// </summary>
    private void CleanOutgoingEvents(
      SequenceId ackedEventId)
    {
      if (ackedEventId.IsValid == false)
        return;

      while (this.outgoingEvents.Count > 0)
      {
        RailEvent top = this.outgoingEvents.Peek();

        // Stop if we hit an un-acked reliable event
        if (top.IsReliable)
        {
          if (top.EventId > ackedEventId)
            break;
        }
        // Stop if we hit an unreliable event with remaining attempts
        else
        {
          if (top.Attempts > 0)
            break;
        }

        RailPool.Free(this.outgoingEvents.Dequeue());
      }
    }

    /// <summary>
    /// Selects outgoing events to send.
    /// </summary>
    private IEnumerable<RailEvent> FilterOutgoingEvents()
    {
      // The receiving client can only store SequenceWindow.HISTORY_LENGTH
      // events in its received buffer, and will skip any events older than
      // its latest received minus that history length, including reliable
      // events. In order to make sure we don't force the client to skip a
      // reliable event, we will throttle the outgoing events if we've been
      // sending them too fast. For example, if we have a reliable event
      // with ID 3 pending, the highest ID we can send would be ID 67. If we
      // send an event with ID 68, then the client may ignore ID 3 when it
      // comes in for being too old, even though it's reliable. 
      //
      // In practice this shouldn't be a problem unless we're sending way 
      // more events than is reasonable(/possible) in a single packet, or 
      // something is wrong with reliable event acking.

      SequenceId firstReliable = SequenceId.INVALID;
      foreach (RailEvent evnt in this.outgoingEvents)
      {
        if (evnt.IsReliable)
        {
          if (firstReliable.IsValid == false)
            firstReliable = evnt.EventId;
          RailDebug.Assert(firstReliable <= evnt.EventId);
        }

        if (firstReliable.IsValid)
        {
          if (SequenceWindow.AreInRange(firstReliable, evnt.EventId) == false)
          {
            string current = "Throttling events due to unacked reliable\n";
            foreach (RailEvent evnt2 in this.outgoingEvents)
              current += evnt2.EventId + " ";
            RailDebug.LogWarning(current);
            break;
          }
        }

        if (evnt.CanSend)
        {
          yield return evnt;
        }
      }
    }

    /// <summary>
    /// Gets all events that we haven't processed yet, in order with no gaps.
    /// </summary>
    private IEnumerable<RailEvent> FilterIncomingEvents(
      IEnumerable<RailEvent> events)
    {
      foreach (RailEvent evnt in events)
        if (this.processedEventHistory.IsNewId(evnt.EventId))
          yield return evnt;
    }

    /// <summary>
    /// Handles the execution of an incoming event.
    /// </summary>
    private void ProcessEvent(RailEvent evnt)
    {
      if (this.EventReceived != null)
        this.EventReceived.Invoke(evnt, this);
      this.processedEventHistory =
        this.processedEventHistory.Store(evnt.EventId);
    }
    #endregion
  }
}
