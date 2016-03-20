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
    private const int EVENT_HISTORY_DELAY = 
      RailConfig.DEJITTER_BUFFER_LENGTH;

    object IRailController.UserData { get; set; }

    protected Tick LocalTick { get { return this.localTick; } }

    public IEnumerable<RailEntity> ControlledEntities
    {
      get { return this.controlledEntities; }
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
    /// A reference to all known entities available. Used in decoding.
    /// Provided by the connection.
    /// </summary>
    private IRailLookup<EntityId, RailEntity> entityLookup;

    /// <summary>
    /// An estimator for the remote peer's current tick.
    /// </summary>
    private readonly RailClock remoteClock;

    /// <summary>
    /// Interpreter for converting byte input to a BitBuffer.
    /// </summary>
    private readonly RailInterpreter interpreter;

    /// <summary>
    /// The current local tick. Used for queuing events.
    /// </summary>
    private Tick localTick;

    #region Event-Related
    /// <summary>
    /// The last read reliable event, for acking.
    /// </summary>
    private EventId lastReadReliableEventId;

    /// <summary>
    /// The last read unrelable event, for sequencing.
    /// </summary>
    private EventId lastReadUnreliableEventId;

    /// <summary>
    /// Used for uniquely identifying outgoing events.
    /// </summary>
    private EventId lastQueuedReliableEventId;

    /// <summary>
    /// Used for uniquely identifying outgoing events.
    /// </summary>
    private EventId lastQueuedUnreliableEventId;

    /// <summary>
    /// A rolling queue for outgoing reliable events, in order.
    /// </summary>
    private readonly Queue<RailEvent> outgoingReliable;

    /// <summary>
    /// A rolling queue for outgoing unreliable events, in order.
    /// </summary>
    private readonly Queue<RailEvent> outgoingUnreliable;

    /// <summary>
    /// A history buffer of received unreliable events.
    /// </summary>
    private readonly RailHeapBuffer<EventId, RailEvent> unreliableHistory;
    #endregion

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
      RailInterpreter interpreter,
      IRailLookup<EntityId, RailEntity> entityLookup)
    {
      this.netPeer = netPeer;
      this.interpreter = interpreter;
      this.entityLookup = entityLookup;

      this.outgoingReliable = new Queue<RailEvent>();
      this.outgoingUnreliable = new Queue<RailEvent>();
      this.unreliableHistory = 
        new RailHeapBuffer<EventId, RailEvent>(EventId.Comparer);

      this.controlledEntities = new HashSet<RailEntity>();
      this.remoteClock = new RailClock();

      // We pretend that one event has already been transmitted
      this.lastQueuedReliableEventId = EventId.START.Next;
      this.lastQueuedUnreliableEventId = EventId.START.Next;
      this.lastReadReliableEventId = EventId.START;
      this.lastReadUnreliableEventId = EventId.START;

      this.netPeer.MessagesReady += this.OnMessagesReady;
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

    internal virtual int Update(Tick localTick)
    {
      this.localTick = localTick;
      this.CleanUnreliable(localTick);
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

        packet.Decode(buffer, this.entityLookup);

        if (buffer.IsFinished)
          this.ProcessPacket(packet);
        else
          CommonDebug.LogError("Bad packet read, discarding...");
      }
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
        this.lastReadReliableEventId,
        this.GetOutgoingEvents(localTick));
      return (T)packet;
    }

    /// <summary>
    /// Records acknowledging information for the packet.
    /// </summary>
    protected virtual void ProcessPacket(RailPacket packet)
    {
      this.remoteClock.UpdateLatest(packet.SenderTick);
      this.CleanReliable(packet.AckEventId);

      foreach (RailEvent evnt in this.FilterEvents(packet.Events))
      {
        if (evnt.Entity != null)
          evnt.Invoke(evnt.Entity);
        else
          evnt.Invoke();
      }
    }

    #region Events
    /// <summary>
    /// Queues an event to send directly to this peer.
    /// </summary>
    public void QueueReliable(RailEvent evnt)
    {
      // All global events are sent reliably
      RailEvent clone = evnt.Clone();

      clone.EventId = this.lastQueuedReliableEventId;
      clone.Tick = this.localTick;
      clone.Expiration = Tick.INVALID;
      clone.IsReliable = true;

      this.outgoingReliable.Enqueue(clone);
      this.lastQueuedReliableEventId = 
        this.lastQueuedReliableEventId.Next;
      RailPool.Free(evnt);
    }

    /// <summary>
    /// Queues an event to send directly to this peer.
    /// </summary>
    public void QueueUnreliable(RailEvent evnt, int timeToLive)
    {
      // All global events are sent reliably
      RailEvent clone = evnt.Clone();

      clone.EventId = this.lastQueuedUnreliableEventId;
      clone.Tick = this.localTick;
      clone.Expiration = this.localTick + timeToLive;
      clone.IsReliable = false;

      this.outgoingUnreliable.Enqueue(clone);
      this.lastQueuedUnreliableEventId = 
        this.lastQueuedUnreliableEventId.Next;
      RailPool.Free(evnt);
    }

    /// <summary>
    /// Interleaves reliable and unreliable events, in order.
    /// </summary>
    private IEnumerable<RailEvent> GetOutgoingEvents(Tick localTick)
    {
      return 
        Iteration.Interleave(
          this.outgoingReliable,
          this.GetNonExpired(localTick));
    }

    /// <summary>
    /// Returns all non-expired outgoing unreliable events.
    /// </summary>
    private IEnumerable<RailEvent> GetNonExpired(Tick localTick)
    {
      foreach (RailEvent evnt in this.outgoingUnreliable)
        if (evnt.Expiration >= localTick)
          yield return evnt;
    }

    /// <summary>
    /// Gets all events that we haven't processed yet, in order with no gaps.
    /// </summary>
    private IEnumerable<RailEvent> FilterEvents(
      IEnumerable<RailEvent> events)
    {
      foreach (RailEvent evnt in events)
      {
        if (evnt.IsReliable && this.FilterReliable(evnt))
          yield return evnt;
        if ((evnt.IsReliable == false) && this.FilterUnreliable(evnt))
          yield return evnt;
      }
    }

    /// <summary>
    /// Logs the reliable event in the history.
    /// </summary>
    private bool FilterReliable(RailEvent evnt)
    {
      bool isExpected =
        (this.lastReadReliableEventId.IsValid == false) ||
        (this.lastReadReliableEventId.Next == evnt.EventId);

      if (isExpected)
      {
        this.lastReadReliableEventId = this.lastReadReliableEventId.Next;
        return true;
      }
      return false;
    }

    /// <summary>
    /// Logs the unreliable event in the history.
    /// </summary>
    private bool FilterUnreliable(RailEvent evnt)
    {
      return this.unreliableHistory.Record(evnt);
    }

    /// <summary>
    /// Removes any acked outgoing reliable events.
    /// </summary>
    private void CleanReliable(EventId ackedId)
    {
      if (ackedId.IsValid == false)
        return;

      while (this.outgoingReliable.Count > 0)
      {
        RailEvent top = this.outgoingReliable.Peek();
        if (top.EventId > ackedId)
          break;
        RailPool.Free(this.outgoingReliable.Dequeue());
      }
    }

    private void CleanUnreliable(Tick localTick)
    {
      if (localTick.IsValid == false)
        return;

      while (this.outgoingUnreliable.Count > 0)
      {
        RailEvent top = this.outgoingUnreliable.Peek();
        if (top.Expiration > localTick)
          break;
        RailPool.Free(this.outgoingUnreliable.Dequeue());
      }

      // Also clear out the history for some past tick number
      int delay = RailPeer.EVENT_HISTORY_DELAY;
      this.unreliableHistory.Advance(Tick.ClampSubtract(localTick, delay));
    }
    #endregion
  }
}
