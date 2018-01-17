/*
 *  RailgunNet - A Client/Server Network State-Synchronization Layer for Games
 *  Copyright (c) 2016-2018 - Alexander Shoulson - http://ashoulson.com
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
using System.Collections.Generic;

namespace Railgun
{
  internal class RailScope
  {
    private class EntityPriorityComparer : 
      Comparer<KeyValuePair<float, IRailEntity>>
    {
      private readonly Comparer<float> floatComparer;

      public EntityPriorityComparer()
      {
        this.floatComparer = Comparer<float>.Default;
      }

      public override int Compare(
        KeyValuePair<float, IRailEntity> x, 
        KeyValuePair<float, IRailEntity> y)
      {
        return this.floatComparer.Compare(x.Key, y.Key);
      }
    }

    internal RailScopeEvaluator Evaluator { private get; set; }

    private readonly RailController owner;
    private readonly RailResource resource;
    private readonly RailView lastSent;
    private readonly RailView ackedByClient;
    private readonly EntityPriorityComparer priorityComparer;

    // Pre-allocated reusable fill lists
    private readonly List<KeyValuePair<float, IRailEntity>> entryList;
    private readonly List<RailStateDelta> activeList;
    private readonly List<RailStateDelta> frozenList;
    private readonly List<RailStateDelta> removedList;

    internal RailScope(RailController owner, RailResource resource)
    {
      this.Evaluator = new RailScopeEvaluator();
      this.owner = owner;
      this.resource = resource;
      this.lastSent = new RailView();
      this.ackedByClient = new RailView();
      this.priorityComparer = new EntityPriorityComparer();

      this.entryList = new List<KeyValuePair<float, IRailEntity>>();
      this.activeList = new List<RailStateDelta>();
      this.frozenList = new List<RailStateDelta>();
      this.removedList = new List<RailStateDelta>();
    }

#if SERVER
    internal Tick GetLastAckedByClient(EntityId entityId)
    {
      if (entityId == EntityId.INVALID)
        return Tick.INVALID;
      return this.ackedByClient.GetLatest(entityId).LastReceivedTick;
    }

    internal bool IsPresentOnClient(EntityId entityId)
    {
      return this.GetLastAckedByClient(entityId).IsValid;
    }
#endif

    internal bool EvaluateEvent(
      RailEvent evnt)
    {
      return this.Evaluator.Evaluate(evnt);
    }

    internal void PopulateDeltas(
      Tick serverTick,
      RailServerPacket packet,
      IEnumerable<IRailEntity> activeEntities,
      IEnumerable<IRailEntity> removedEntities)
    {
      this.ProduceScoped(serverTick, activeEntities);
      this.ProduceRemoved(this.owner, removedEntities);

      packet.Populate(this.activeList, this.frozenList, this.removedList);

      this.removedList.Clear();
      this.frozenList.Clear();
      this.activeList.Clear();
    }

    internal void IntegrateAcked(RailView packetView)
    {
      this.ackedByClient.Integrate(packetView);
    }

    internal void RegisterSent(EntityId entityId, Tick tick, bool isFrozen)
    {
      // We don't care about the local tick on the server side
      this.lastSent.RecordUpdate(entityId, tick, Tick.INVALID, isFrozen);
    }

    private bool GetPriority(
      RailEntity entity, 
      Tick current,
      out float priority)
    {
      RailViewEntry lastSent = this.lastSent.GetLatest(entity.Id);
      RailViewEntry lastAcked = this.ackedByClient.GetLatest(entity.Id);

      int ticksSinceSend = int.MaxValue;
      int ticksSinceAck = int.MaxValue;

      if (lastSent.IsValid)
        ticksSinceSend = current - lastSent.LastReceivedTick;
      if (lastAcked.IsValid)
        ticksSinceAck = current - lastAcked.LastReceivedTick;

      return this.EvaluateEntity(
        entity,
        ticksSinceSend,
        ticksSinceAck, 
        out priority);
    }

    /// <summary>
    /// Divides the active entities into those that are in scope and those
    /// out of scope. If an entity is out of scope and hasn't been acked as
    /// such by the client, we will add it to the outgoing frozen delta list.
    /// Otherwise, if an entity is in scope we will add it to the sorted
    /// active delta list.
    /// </summary>
    private void ProduceScoped(
      Tick serverTick,
      IEnumerable<IRailEntity> activeEntities)
    {
      this.entryList.Clear();
      float priority;

      foreach (RailEntity entity in activeEntities)
      {
        if (entity.IsRemoving)
        {
          continue;
        }
        // Controlled entities are always in scope to their controller
        else if (entity.Controller == this.owner)
        {
          this.entryList.Add(
            new KeyValuePair<float, IRailEntity>(float.MinValue, entity));
        }
        else if (this.GetPriority(entity, serverTick, out priority))
        {
          this.entryList.Add(
            new KeyValuePair<float, IRailEntity>(priority, entity));
        }
        else
        {
          // We only want to send a freeze state if we aren't already frozen
          RailViewEntry latest = this.ackedByClient.GetLatest(entity.Id);
          if (latest.IsFrozen == false)
            this.frozenList.Add(
              RailStateDelta.CreateFrozen(
                this.resource, 
                serverTick, 
                entity.Id));
        }
      }

      this.entryList.Sort(this.priorityComparer);
      foreach (KeyValuePair<float, IRailEntity> entry in this.entryList)
      { 
        RailViewEntry latest = this.ackedByClient.GetLatest(entry.Value.Id);

        // Force an update if the entity is frozen so it unfreezes
        RailStateDelta delta = 
          entry.Value.AsBase.ProduceDelta(
            latest.LastReceivedTick,
            this.owner,
            latest.IsFrozen);

        if (delta != null)
          this.activeList.Add(delta);
      }
    }

    /// <summary>
    /// Produces deltas for all non-acked removed entities.
    /// </summary>
    private void ProduceRemoved(
      RailController target,
      IEnumerable<IRailEntity> removedEntities)
    {
      foreach (IRailEntity entity in removedEntities)
      {
        RailViewEntry latest = this.ackedByClient.GetLatest(entity.Id);

        // Note: Because the removed tick is valid, this should force-create
        if (latest.IsValid && (latest.LastReceivedTick < entity.AsBase.RemovedTick))
          this.removedList.Add(
            entity.AsBase.ProduceDelta(
              latest.LastReceivedTick, 
              target, 
              false));
      }
    }

    private bool EvaluateEntity(
      IRailEntity entity, 
      int ticksSinceSend, 
      int ticksSinceAck,
      out float priority)
    {
      return 
        this.Evaluator.Evaluate(
          entity, 
          ticksSinceSend, 
          ticksSinceAck,
          out priority);
    }
  }
}
#endif