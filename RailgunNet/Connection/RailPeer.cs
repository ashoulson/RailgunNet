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

    #region IRailControllerInternal Members
    RailCommand IRailControllerInternal.LatestCommand
    {
      get { return this.LatestCommand; }
    }

    IEnumerable<RailCommand> IRailControllerInternal.PendingCommands
    {
      get { return this.PendingCommands; }
    }
    #endregion

    /// <summary>
    /// The entities controlled by this controller.
    /// </summary>
    protected readonly HashSet<RailEntity> controlledEntities;

    /// <summary>
    /// A rolling queue for outgoing reliable events, in order.
    /// </summary>
    private readonly Queue<RailEvent> outgoingReliable;

    /// <summary>
    /// A buffer for outgoing unreliable events, cleared each send.
    /// </summary>
    private readonly List<RailEvent> outgoingUnreliable;

    /// <summary>
    /// Used for uniquely identifying and ordering reliable events.
    /// </summary>
    private EventId lastEventId;

    public IEnumerable<RailEntity> ControlledEntities
    {
      get { return this.controlledEntities; }
    }

    internal IEnumerable<RailEvent> ReliableEvents
    {
      get { return this.outgoingReliable; }
    }

    internal IEnumerable<RailEvent> UnreliableEvents
    {
      get { return this.outgoingUnreliable; }
    }

    internal IEnumerable<RailEvent> AllEvents
    {
      get { return this.UnreliableEvents.Concat(this.ReliableEvents); }
    }

    // Server-only
    protected internal virtual RailCommand LatestCommand
    {
      get { throw new NotImplementedException(); }
    }

    // Client-only
    protected internal virtual IEnumerable<RailCommand> PendingCommands
    {
      get { throw new NotImplementedException(); }
    }

    internal RailPeer(IRailNetPeer netPeer)
    {
      this.netPeer = netPeer;
      this.netPeer.MessagesReady += this.OnMessagesReady;

      this.controlledEntities = new HashSet<RailEntity>();
      this.outgoingReliable = new Queue<RailEvent>();
      this.outgoingUnreliable = new List<RailEvent>();

      this.lastEventId = EventId.INVALID;
    }

    public void QueueUnreliable(RailEvent evnt, Tick tick)
    {
      RailEvent clone = evnt.Clone();
      clone.Initialize(
        tick,
        EventId.UNRELIABLE);
      this.outgoingUnreliable.Add(clone);
    }

    public void QueueReliable(RailEvent evnt, Tick tick)
    {
      RailEvent clone = evnt.Clone();
      clone.Initialize(
        tick,
        EventId.Increment(ref this.lastEventId));
      this.outgoingReliable.Enqueue(clone);
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

    internal void CleanReliableEvents(EventId lastReceivedId)
    {
      while (true)
      {
        if (lastReceivedId.IsValid == false) // They haven't received anything
          break;
        if (this.outgoingReliable.Count == 0)
          break;
        if (this.outgoingReliable.Peek().EventId.IsNewerThan(lastReceivedId))
          break;
        RailPool.Free(this.outgoingReliable.Dequeue());
      }
    }

    internal void CleanUnreliableEvents()
    {
      foreach (RailEvent evnt in this.outgoingUnreliable)
        RailPool.Free(evnt);
      this.outgoingUnreliable.Clear();
    }
  }
}
