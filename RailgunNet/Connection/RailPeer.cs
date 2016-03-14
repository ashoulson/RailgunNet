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
    #region IRailNetPeer Wrapping
    internal IRailNetPeer NetPeer { get { return this.NetPeer; } }

    private readonly IRailNetPeer netPeer;

    internal IEnumerable<int> ReadReceived(byte[] buffer)
    {
      return this.netPeer.ReadReceived(buffer);
    }

    internal void EnqueueSend(byte[] buffer, int length)
    {
      this.netPeer.EnqueueSend(buffer, length);
    }

    protected abstract void OnMessagesReady(IRailNetPeer peer);
    #endregion

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

    internal RailPeer(IRailNetPeer netPeer)
    {
      this.netPeer = netPeer;
      this.netPeer.MessagesReady += this.OnMessagesReady;

      this.controlledEntities = new HashSet<RailEntity>();
      this.outgoingGlobalEvents = new Queue<RailEvent>();

      this.lastEventId = EventId.INVALID;
      this.lastReceivedEventId = EventId.INVALID;
      this.remoteClock = new RailClock();
    }

    public void QueueGlobal(RailEvent evnt, Tick tick)
    {
      RailEvent clone = evnt.Clone();
      clone.Initialize(
        tick,
        EventId.Increment(ref this.lastEventId));
      this.outgoingGlobalEvents.Enqueue(clone);
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

    protected virtual void PreparePacketBase(
      RailPacket packet, 
      Tick localTick)
    {
      packet.Initialize(
        localTick,
        this.remoteClock.LatestRemote,
        this.lastReceivedEventId,
        this.outgoingGlobalEvents);
    }

    /// <summary>
    /// Records acknowledging information for the packet.
    /// </summary>
    protected virtual void ProcessPacket(RailPacket packet)
    {
      this.remoteClock.UpdateLatest(packet.SenderTick);

      foreach (RailEvent evnt in this.GetNewEvents(packet))
        this.ProcessEvent(evnt);

      this.UpdateLastReceivedEvent(packet);
      this.CleanReliableEvents();
    }


    /// <summary>
    /// Gets all events that we haven't processed yet, in order.
    /// </summary>
    private IEnumerable<RailEvent> GetNewEvents(RailPacket packet)
    {
      foreach (RailEvent globalEvent in packet.GlobalEvents)
      {
        if (this.lastReceivedEventId.IsValid == false)
          yield return globalEvent;
        else if (globalEvent.EventId.IsNewerThan(this.lastReceivedEventId))
          yield return globalEvent;
        else
          break;
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

    private void UpdateLastReceivedEvent(RailPacket packet)
    {
      foreach (RailEvent globalEvent in packet.GlobalEvents)
      {
        if (this.lastReceivedEventId.IsValid == false)
          this.lastReceivedEventId = globalEvent.EventId;
        else if (globalEvent.EventId.IsNewerThan(this.lastReceivedEventId))
          this.lastReceivedEventId = globalEvent.EventId;
      }
    }

    private void CleanReliableEvents()
    {
      while (true)
      {
        if (this.lastReceivedEventId.IsValid == false) // They haven't received anything
          break;

        if (this.outgoingGlobalEvents.Count == 0)
          break;

        RailEvent next = this.outgoingGlobalEvents.Peek();
        if (next.EventId.IsNewerThan(this.lastReceivedEventId))
          break;

        RailPool.Free(this.outgoingGlobalEvents.Dequeue());
      }
    }
  }
}
